using UnityEngine;
using PurrNet;
using DG.Tweening;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using System;
using PurrNet.Modules;
using PurrNet.Transports;

[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(ModuleInteraction))]
public class NotepadModule : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cover;
    [SerializeField] private GameObject[] pageMeshes;

    [Header("Drawing & Texture Settings")]
    [SerializeField] private Material basePageMaterial;
    [SerializeField] private int textureResolution = 1024;
    [SerializeField] private Color defaultPageColor = Color.white;
    [SerializeField] private int brushSize = 5;
    [SerializeField] private Color brushColor = Color.black;
    [Header("Cursor Settings")]
    [SerializeField] private Texture2D brushCursor;
    [SerializeField] private Vector2 brushHotspot = Vector2.zero;
    [SerializeField] private bool changeCursorOnHover = false;

    [Header("Animation Settings")]
    [SerializeField] private float flipDuration = 0.4f;
    [SerializeField] private Vector3 coverOpenRotation = new Vector3(0, 0, 120);
    [SerializeField] private Vector3 pageFlippedRotation = new Vector3(-180, 0, 0);

    [Header("Tearing Settings")]
    [SerializeField] private GameObject tornPagePrefab;

    [Header("Audio Settings")]
    [SerializeField] private AudioEventChannelSO audioChannel;
    [SerializeField] private EventReference openSound;
    [SerializeField] private EventReference closeSound;
    [SerializeField] private EventReference pageFlipSound;
    [Header("Tutorial")]
    [SerializeField] private InteractionIndicator indicator;

    // Durum Değişkenleri
    private int currentPageIndex = 0;
    private bool isInteracting = false;
    private bool isAnimating = false;
    private Tween pageTurnTween;
    private Tween coverTween;
    private Vector3 coverPresentationRotation;
    private ModuleInteraction moduleInteraction;
    private bool isDrawingCursorActive = false;
    private int remainingPages = 4;
    private Interactable interactableComponent;

    private Texture2D[] pageTextures;
    private Vector2 lastDrawPosition = -Vector2.one;

    private const int UploadChunkSize = 1000;
    private const int MaxUploadBytes = 262144;
    private const int PageResolution = 512;
    private const double UploadIdleSeconds = 30;
    private const string UploadPrompt = "notepad_upload";

    // At most two active operations per exact session, shared by notebook instances.
    private static readonly List<PageUpload> activeUploads = new List<PageUpload>();
    private readonly List<PageUpload> uploads = new List<PageUpload>();
    private NetworkManager uploadManager;
    private string localUploadId;
    private int localUploadPage;
    private bool localUploadAdmitted;
    private bool localCancelRequested;
    private double localLastProgress;
    private Coroutine localSend;

    private sealed class PageUpload
    {
        public NetworkManager Manager;
        public PlayerID Sender;
        public string Id;
        public InventoryManager Target;
        public ItemIdentityHandle TargetHandle;
        public byte[] Buffer;
        public BitArray Received;
        public int ReceivedCount;
        public double LastProgress;
        public GameObject Page;
        public Coroutine Readiness;
    }

    private void Awake()
    {
        // Interactable componentini Awake'de alıyoruz
        interactableComponent = GetComponent<Interactable>();
        moduleInteraction = GetComponent<ModuleInteraction>();
    }
    private void Start()
    {
        InitializeDrawingPages();
    }

    private void OnEnable()
    {
        moduleInteraction.OnTerminalInteractionEnded += ResetNotebookPresentation;
        TutorialInputManager.OnNotebookInteractStateChanged += HandleNotebookInteractState;
        SubscribeUploadLifetime();
    }

    private void OnDisable()
    {
        EndNotebookPresentation();
        moduleInteraction.OnTerminalInteractionEnded -= ResetNotebookPresentation;
        TutorialInputManager.OnNotebookInteractStateChanged -= HandleNotebookInteractState;
        ClearUploadLifetime();
    }

    private void HandleNotebookInteractState(bool isInteractable)
    {
        if (interactableComponent != null)
        {
            interactableComponent.SetInteractable(isInteractable);
        }

        if (isInteractable && !isInteracting)
        {
            indicator?.Show();
        }
        else
        {
            indicator?.Hide();
        }
    }

    private void Update()
    {
        CheckUploadLifetimes();
        if (localUploadId != null) return;
        if (!isInteracting || isAnimating) return;

        HandlePageScrolling();
        HandleDrawingAndCursor();
        HandlePageTearing();
    }

    private void HandlePageTearing()
    {
        if (!Input.GetKeyDown(KeyCode.E) || remainingPages <= 0 || !pageMeshes[currentPageIndex].activeSelf) return;
        if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var questView) &&
            questView.ShouldBlockAction(TutorialAction.RipPage)) return;
        var inventory = InventoryManager.LocalPlayer;
        if (!isSpawned || !inventory || !inventory.isOwner || !localPlayer.HasValue || textureResolution != PageResolution)
        {
            ShowUploadProblem("Notebook upload unavailable");
            return;
        }
        var data = pageTextures[currentPageIndex].EncodeToJPG(50);
        if (data == null || data.Length == 0 || data.Length > MaxUploadBytes)
        {
            ShowUploadProblem("Page image exceeds upload limit");
            return;
        }
        localUploadId = Guid.NewGuid().ToString();
        localUploadPage = currentPageIndex;
        localUploadAdmitted = false;
        localCancelRequested = false;
        localLastProgress = Time.realtimeSinceStartupAsDouble;
        localSend = StartCoroutine(SendImageInChunksRoutine(data, inventory, localUploadId));
    }

    private IEnumerator SendImageInChunksRoutine(byte[] data, InventoryManager inventory, string imageId)
    {
        // Yield first so synchronous host rejection cannot leave a stale coroutine handle.
        yield return null;
        if (isServer) PrepareUpload(localPlayer.Value, imageId, data.Length, inventory);
        else PrepareImageServerRpc(imageId, data.Length, inventory);
        while (localUploadId == imageId && !localUploadAdmitted) yield return null;
        for (int offset = 0; localUploadId == imageId && !localCancelRequested && offset < data.Length; offset += UploadChunkSize)
        {
            int length = Math.Min(UploadChunkSize, data.Length - offset);
            var chunk = new byte[length];
            Array.Copy(data, offset, chunk, 0, length);
            if (isServer) ReceiveUploadChunk(localPlayer.Value, imageId, chunk, offset);
            else SendChunkServerRpc(imageId, chunk, offset);
            localLastProgress = Time.realtimeSinceStartupAsDouble;
            yield return null;
        }
        localSend = null;
    }

    [ServerRpc(requireOwnership: false, runLocally: false)]
    private void PrepareImageServerRpc(string imageId, int totalSize, InventoryManager targetInventory, RPCInfo info = default)
    {
        if (info.asServer && info.manager == networkManager) PrepareUpload(info.sender, imageId, totalSize, targetInventory);
    }

    private void PrepareUpload(PlayerID sender, string imageId, int totalSize, InventoryManager target)
    {
        if (!isServer || !isSpawned || !isActiveAndEnabled || !IsConnected(networkManager, sender)) return;
        if (imageId == null || imageId.Length != 36 || !Guid.TryParseExact(imageId, "D", out _))
        {
            Debug.LogWarning("Notepad upload rejected: invalid ID.", this);
            return;
        }
        string rejection = null;
        if (totalSize <= 0 || totalSize > MaxUploadBytes || textureResolution != PageResolution) rejection = "Invalid page size/configuration";
        else if (!IsValidTarget(target, sender, networkManager) || !ItemIdentityHandle.TryCreate(target, out _)) rejection = "Page target unavailable";
        else
        {
            int count = 0;
            foreach (var existing in activeUploads)
            {
                if (existing.Manager != networkManager) continue;
                count++;
                if (existing.Sender == sender) { rejection = "Notebook upload busy"; break; }
            }
            if (rejection == null && count >= 2) rejection = "Notebook upload busy";
        }
        if (rejection != null)
        {
            Debug.LogWarning("Notepad upload rejected: " + rejection, this);
            SendUploadOutcome(sender, imageId, false, false, rejection);
            return;
        }
        ItemIdentityHandle.TryCreate(target, out var targetHandle);
        var upload = new PageUpload { Manager = networkManager, Sender = sender, Id = imageId,
            Target = target, TargetHandle = targetHandle, Buffer = new byte[totalSize],
            Received = new BitArray((totalSize + UploadChunkSize - 1) / UploadChunkSize), LastProgress = Time.realtimeSinceStartupAsDouble };
        uploads.Add(upload);
        activeUploads.Add(upload);
        SendUploadOutcome(sender, imageId, true, false, null);
    }

    [ServerRpc(requireOwnership: false, runLocally: false)]
    private void SendChunkServerRpc(string imageId, byte[] chunk, int offset, RPCInfo info = default)
    {
        if (info.asServer && info.manager == networkManager) ReceiveUploadChunk(info.sender, imageId, chunk, offset);
    }

    private void ReceiveUploadChunk(PlayerID sender, string imageId, byte[] chunk, int offset)
    {
        var upload = FindUpload(sender, imageId);
        if (upload == null || upload.Received == null) return;
        if (!IsUploadAlive(upload)) { EndUpload(upload, false, "Page target lifetime ended"); return; }
        var result = StoreChunk(upload, chunk, offset);
        if (result == ChunkResult.Invalid || result == ChunkResult.Conflict)
        { EndUpload(upload, false, result == ChunkResult.Conflict ? "Conflicting page chunk" : "Malformed page chunk"); return; }
        if (result == ChunkResult.Duplicate) return;
        upload.LastProgress = Time.realtimeSinceStartupAsDouble;
        if (result != ChunkResult.Complete) return;
        upload.Received = null; // Detach chunk writes before any decode or yield.
        FinalizeUpload(upload);
    }

    private enum ChunkResult { Invalid, Conflict, Duplicate, Progress, Complete }

    private static ChunkResult StoreChunk(PageUpload upload, byte[] chunk, int offset)
    {
        if (!IsCanonicalChunk(upload.Buffer.Length, offset, chunk)) return ChunkResult.Invalid;
        int slot = offset / UploadChunkSize;
        if (upload.Received[slot])
        {
            for (int i = 0; i < chunk.Length; i++)
                if (upload.Buffer[offset + i] != chunk[i]) return ChunkResult.Conflict;
            return ChunkResult.Duplicate;
        }
        Array.Copy(chunk, 0, upload.Buffer, offset, chunk.Length);
        upload.Received[slot] = true;
        upload.ReceivedCount++;
        return upload.ReceivedCount == upload.Received.Length ? ChunkResult.Complete : ChunkResult.Progress;
    }

    private static bool IsCanonicalChunk(int total, int offset, byte[] chunk)
    {
        return total > 0 && total <= MaxUploadBytes && chunk != null && chunk.Length > 0 &&
            offset >= 0 && offset < total && offset % UploadChunkSize == 0 &&
            chunk.Length <= total - offset && chunk.Length == Math.Min(UploadChunkSize, total - offset);
    }

    private void FinalizeUpload(PageUpload upload)
    {
        if (!IsUploadAlive(upload) || !ValidatePageImage(upload.Buffer)) { EndUpload(upload, false, "Invalid page image/target"); return; }
        if (!tornPagePrefab || !tornPagePrefab.GetComponent<TornPageItem>() ||
            !tornPagePrefab.GetComponent<ItemLoot>() || !tornPagePrefab.GetComponent<NetworkTransform>())
        { EndUpload(upload, false, "Page prefab unavailable"); return; }
        try
        {
            upload.Page = Instantiate(tornPagePrefab, transform.position, transform.rotation);
            // Existing pickup validation honors this flag. No other accepted pickup may race producer cleanup.
            upload.Page.GetComponent<ItemLoot>().CanBeLooted = false;
            upload.Readiness = StartCoroutine(HandoffWhenReady(upload));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Notepad page setup failed: " + exception.Message, this);
            EndUpload(upload, false, "Page setup failed");
        }
    }

    private IEnumerator HandoffWhenReady(PageUpload upload)
    {
        // A real spawn boundary replaces the unrelated fixed DOTween delay.
        yield return null;
        while (uploads.Contains(upload))
        {
            if (!IsUploadAlive(upload) || !upload.Page) { EndUpload(upload, false, "Page lifetime ended"); yield break; }
            var page = upload.Page.GetComponent<TornPageItem>();
            var item = upload.Page.GetComponent<ItemLoot>();
            var nt = upload.Page.GetComponent<NetworkTransform>();
            if (page && page.isServer && item && item.IsPossessionInitialized && nt && nt.isSpawned)
            {
                try
                {
                    nt.GiveOwnership(upload.Sender);
                    page.SetImageDataAndDistribute(upload.Buffer);
                    if (upload.Target.TryBeginForcedPageDelivery(item))
                    {
                        item.CanBeLooted = true;
                        // Inventory owns this exact item now. Producer cleanup must never destroy it.
                        upload.Page = null;
                        EndUpload(upload, true, null);
                    }
                    else EndUpload(upload, false, "Page delivery busy/unavailable");
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Notepad page initialization failed: " + exception.Message, this);
                    EndUpload(upload, false, "Page initialization failed");
                }
                yield break;
            }
            yield return null;
        }
    }

    private static bool ValidatePageImage(byte[] bytes)
    {
        if (!HasSupportedJpegDimensions(bytes)) return false;
        Texture2D decoded = null;
        try
        {
            decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            return decoded.LoadImage(bytes) && decoded.width == PageResolution && decoded.height == PageResolution;
        }
        catch (Exception) { return false; }
        finally { if (decoded) Destroy(decoded); }
    }

    private static bool HasSupportedJpegDimensions(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4 || bytes.Length > MaxUploadBytes || bytes[0] != 255 || bytes[1] != 216) return false;
        int offset = 2;
        bool foundFrame = false;
        while (offset < bytes.Length)
        {
            if (bytes[offset++] != 255) return false;
            while (offset < bytes.Length && bytes[offset] == 255) offset++;
            if (offset >= bytes.Length) return false;
            int marker = bytes[offset++];
            if (marker == 218) return foundFrame; // Decode validates the scan itself.
            if (marker == 217 || marker == 0) return false;
            if (offset > bytes.Length - 2) return false;
            int length = bytes[offset] * 256 + bytes[offset + 1];
            if (length < 2 || length > bytes.Length - offset) return false;
            if (marker >= 192 && marker <= 207 && marker != 196 && marker != 200 && marker != 204)
            {
                // Actual Unity JPEG50 uses baseline SOF0. Reject unsupported/multiple frames before decode.
                if (marker != 192 || foundFrame || length < 8 || bytes[offset + 2] != 8 ||
                    bytes[offset + 3] * 256 + bytes[offset + 4] != PageResolution ||
                    bytes[offset + 5] * 256 + bytes[offset + 6] != PageResolution) return false;
                foundFrame = true;
            }
            offset += length;
        }
        return false;
    }

    private PageUpload FindUpload(PlayerID sender, string imageId)
    {
        foreach (var upload in uploads) if (upload.Sender == sender && upload.Id == imageId) return upload;
        return null;
    }

    private static bool IsConnected(NetworkManager manager, PlayerID sender)
    {
        return manager && manager.isServer && manager.TryGetModule<PlayersManager>(true, out var players) && players.IsPlayerConnected(sender);
    }

    private bool IsValidTarget(InventoryManager target, PlayerID sender, NetworkManager manager)
    {
        return target && target.isServer && target.isSpawned && target.isActiveAndEnabled &&
            target.networkManager == manager && target.sceneId == sceneId && target.owner == sender && target.InventoryVersion != 0;
    }

    private bool IsUploadAlive(PageUpload upload)
    {
        return isServer && isSpawned && isActiveAndEnabled && networkManager == upload.Manager &&
            IsConnected(upload.Manager, upload.Sender) && IsValidTarget(upload.Target, upload.Sender, upload.Manager) &&
            upload.TargetHandle.Resolve<InventoryManager>(this) == upload.Target &&
            Time.realtimeSinceStartupAsDouble - upload.LastProgress < UploadIdleSeconds;
    }

    private void CheckUploadLifetimes()
    {
        for (int i = uploads.Count - 1; i >= 0; i--)
            if (!IsUploadAlive(uploads[i])) EndUpload(uploads[i], false, "Page upload expired/lifetime ended");
        if (localUploadId != null && !localCancelRequested &&
            Time.realtimeSinceStartupAsDouble - localLastProgress >= UploadIdleSeconds) RequestLocalCancellation();
    }

    private void EndUpload(PageUpload upload, bool handedOff, string reason)
    {
        if (!uploads.Remove(upload)) return;
        activeUploads.Remove(upload);
        if (upload.Readiness != null) StopCoroutine(upload.Readiness);
        upload.Readiness = null;
        if (upload.Page)
        {
            var item = upload.Page.GetComponent<ItemLoot>();
            // Defensive preservation of any accepted possession, even during shutdown.
            if (!item || !item.IsPossessionInitialized || item.Possession.Location == ItemSharedLocation.World)
                Destroy(upload.Page);
            else handedOff = true;
        }
        upload.Page = null;
        upload.Buffer = null;
        upload.Received = null;
        if (!handedOff) Debug.LogWarning("Notepad upload cancelled: " + reason, this);
        if (IsConnected(upload.Manager, upload.Sender) && isSpawned)
            SendUploadOutcome(upload.Sender, upload.Id, false, handedOff, reason);
    }

    private void SendUploadOutcome(PlayerID sender, string imageId, bool admitted, bool handedOff, string reason)
    {
        if (localPlayer == sender) ApplyUploadOutcome(imageId, admitted, handedOff, reason);
        else UploadOutcomeTargetRpc(sender, imageId, admitted, handedOff, reason);
    }

    [TargetRpc]
    private void UploadOutcomeTargetRpc(PlayerID target, string imageId, bool admitted, bool handedOff, string reason)
    {
        ApplyUploadOutcome(imageId, admitted, handedOff, reason);
    }

    private void ApplyUploadOutcome(string imageId, bool admitted, bool handedOff, string reason)
    {
        if (localUploadId != imageId) return;
        if (admitted)
        {
            localUploadAdmitted = true;
            localLastProgress = Time.realtimeSinceStartupAsDouble;
            pageMeshes[localUploadPage].SetActive(false);
            PlaySound(pageFlipSound);
            if (isDrawingCursorActive) { CursorManager.OnClearCustomCursor?.Invoke(); isDrawingCursorActive = false; }
            return;
        }
        if (localSend != null) StopCoroutine(localSend);
        localSend = null;
        localUploadId = null;
        localUploadAdmitted = false;
        if (handedOff)
        {
            remainingPages--;
            SelectRemainingPageAfterTear(localUploadPage);
            if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var view)) view.OnActionPerformed(TutorialAction.RipPage);
            if (InstanceHandler.TryGetInstance<PromptView>(out var prompts)) prompts.RemovePrompt(UploadPrompt);
        }
        else
        {
            if (pageMeshes[localUploadPage]) pageMeshes[localUploadPage].SetActive(true);
            currentPageIndex = localUploadPage;
            ShowUploadProblem(reason ?? "Page upload cancelled");
        }
    }

    private void ShowUploadProblem(string reason)
    {
        if (isInteracting && InstanceHandler.TryGetInstance<PromptView>(out var prompts))
            prompts.AddPrompt(UploadPrompt, "E", reason, PromptGroup.Module);
    }

    private void RequestLocalCancellation()
    {
        if (localUploadId == null || localCancelRequested || !isSpawned) return;
        localCancelRequested = true;
        if (isServer && localPlayer.HasValue)
        {
            var upload = FindUpload(localPlayer.Value, localUploadId);
            if (upload != null) EndUpload(upload, false, "Local upload cancelled");
            else ApplyUploadOutcome(localUploadId, false, false, "Page upload cancelled");
        }
        else CancelUploadServerRpc(localUploadId);
    }

    [ServerRpc(requireOwnership: false, runLocally: false)]
    private void CancelUploadServerRpc(string imageId, RPCInfo info = default)
    {
        if (!info.asServer || info.manager != networkManager) return;
        var upload = FindUpload(info.sender, imageId);
        if (upload != null) EndUpload(upload, false, "Notebook closed by teardown");
        // Already queued successful outcome is delivered first by ReliableOrdered.
        else if (IsConnected(networkManager, info.sender)) SendUploadOutcome(info.sender, imageId, false, false, "Page upload cancelled");
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        SubscribeUploadLifetime();
    }

    protected override void OnDespawned()
    {
        EndNotebookPresentation();
        ClearUploadLifetime();
        // No source operation survives network identity loss.
        localUploadId = null;
        base.OnDespawned();
    }

    protected override void OnDestroy()
    {
        EndNotebookPresentation();
        ClearUploadLifetime();
        base.OnDestroy();
    }

    private void SubscribeUploadLifetime()
    {
        if (!isSpawned || !isActiveAndEnabled || uploadManager == networkManager) return;
        uploadManager = networkManager;
        uploadManager.onPlayerLeft += OnUploadPlayerLeft;
        uploadManager.onServerConnectionState += OnUploadServerState;
        uploadManager.onClientConnectionState += OnUploadClientState;
    }

    private void OnUploadPlayerLeft(PlayerID sender, bool asServer)
    {
        if (!asServer) return;
        for (int i = uploads.Count - 1; i >= 0; i--)
            if (uploads[i].Sender == sender) EndUpload(uploads[i], false, "Sender disconnected");
    }

    private void OnUploadServerState(ConnectionState state)
    {
        if (state == ConnectionState.Disconnected) ClearUploadLifetime();
    }

    private void OnUploadClientState(ConnectionState state)
    {
        if (state != ConnectionState.Disconnected) return;
        ClearUploadLifetime();
        localUploadId = null;
    }

    private void ClearUploadLifetime()
    {
        if (uploadManager)
        {
            uploadManager.onPlayerLeft -= OnUploadPlayerLeft;
            uploadManager.onServerConnectionState -= OnUploadServerState;
            uploadManager.onClientConnectionState -= OnUploadClientState;
        }
        uploadManager = null;
        for (int i = uploads.Count - 1; i >= 0; i--) EndUpload(uploads[i], false, "Notebook teardown");
        RequestLocalCancellation();
        if (localSend != null) StopCoroutine(localSend);
        localSend = null;
        // Keep only source intent until ordered cancel/success feedback on disable.
        // Restoring immediately could duplicate a page whose handoff already succeeded.
        if (InstanceHandler.TryGetInstance<PromptView>(out var prompts)) prompts.RemovePrompt(UploadPrompt);
    }

    private void InitializeDrawingPages()
    {
        pageTextures = new Texture2D[pageMeshes.Length];

        for (int i = 0; i < pageMeshes.Length; i++)
        {
            if (pageMeshes[i] == null) continue;

            Texture2D tex = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);

            Color[] colors = new Color[textureResolution * textureResolution];
            for (int j = 0; j < colors.Length; j++) colors[j] = defaultPageColor;
            tex.SetPixels(colors);
            tex.Apply();

            pageTextures[i] = tex;

            MeshRenderer renderer = pageMeshes[i].GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material instancedMat = new Material(basePageMaterial);
                instancedMat.SetTexture("_BaseMap", tex);
                renderer.material = instancedMat;
            }
        }
    }

    private void HandleDrawingAndCursor()
    {
        bool isClicking = Input.GetMouseButton(0);
        bool isHoveringValidPage = false;
        RaycastHit validHit = new RaycastHit();

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            if (hit.collider.gameObject == pageMeshes[currentPageIndex])
            {
                isHoveringValidPage = true;
                validHit = hit;
            }
        }

        bool shouldShowBrush = changeCursorOnHover ? isHoveringValidPage : (isHoveringValidPage && isClicking);

        if (shouldShowBrush && !isDrawingCursorActive)
        {
            CursorManager.OnSetCustomCursor?.Invoke(brushCursor, brushHotspot);
            isDrawingCursorActive = true;
        }
        else if (!shouldShowBrush && isDrawingCursorActive)
        {
            CursorManager.OnClearCustomCursor?.Invoke();
            isDrawingCursorActive = false;
        }

        if (isHoveringValidPage && isClicking)
        {
            if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var questView))
            {
                if (questView.ShouldBlockAction(TutorialAction.DrawNotebook)) return;

                questView.OnActionPerformed(TutorialAction.DrawNotebook);
            }
            Vector2 currentUV = validHit.textureCoord;

            if (lastDrawPosition == -Vector2.one)
                lastDrawPosition = currentUV;

            DrawLineOnTexture(pageTextures[currentPageIndex], lastDrawPosition, currentUV);
            lastDrawPosition = currentUV;
        }
        else
        {
            lastDrawPosition = -Vector2.one;
        }
    }

    private void DrawLineOnTexture(Texture2D tex, Vector2 startUV, Vector2 endUV)
    {
        int x0 = (int)(startUV.x * tex.width);
        int y0 = (int)(startUV.y * tex.height);
        int x1 = (int)(endUV.x * tex.width);
        int y1 = (int)(endUV.y * tex.height);

        float distance = Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1));
        int steps = Mathf.Max(1, (int)distance);

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));

            DrawBrush(tex, x, y);
        }

        tex.Apply();
    }

    private void DrawBrush(Texture2D tex, int centerX, int centerY)
    {
        for (int x = -brushSize; x <= brushSize; x++)
        {
            for (int y = -brushSize; y <= brushSize; y++)
            {
                if (x * x + y * y <= brushSize * brushSize)
                {
                    int drawX = centerX + x;
                    int drawY = centerY + y;

                    if (drawX >= 0 && drawX < tex.width && drawY >= 0 && drawY < tex.height)
                    {
                        tex.SetPixel(drawX, drawY, brushColor);
                    }
                }
            }
        }
    }


    public void OnNotebookInteract()
    {
        if (isInteracting || !isActiveAndEnabled || !moduleInteraction || !moduleInteraction.HasInteraction) return;
        CancelPresentationTweens();
        NormalizePagePresentation();
        indicator?.Hide();
        if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var tutorialView))
            tutorialView.OnActionPerformed(TutorialAction.InteractNotebook);
        isInteracting = true;
        isAnimating = true;

        PlaySound(openSound);

        coverTween = OwnPresentationTween(DOVirtual.Vector3(coverPresentationRotation, coverOpenRotation, flipDuration, (v) =>
        {
            coverPresentationRotation = v;
            cover.localEulerAngles = v;
        })
        .SetEase(Ease.OutSine)
        .OnComplete(() => { coverTween = null; isAnimating = false; }), true);
    }

    public void OnNotebookStopInteract()
    {
        if (!isActiveAndEnabled || (moduleInteraction && moduleInteraction.IsTerminalCleanup))
        {
            ResetNotebookPresentation();
            return;
        }
        if (!isInteracting) return;
        CancelPresentationTweens();
        NormalizePagePresentation();
        lastDrawPosition = -Vector2.one;
        if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var tutorialView))
            tutorialView.OnActionPerformed(TutorialAction.CloseNotebook);
        isAnimating = true;
        isInteracting = false;

        if (isDrawingCursorActive)
        {
            isDrawingCursorActive = false;
            CursorManager.OnClearCustomCursor?.Invoke();
        }


        PlaySound(closeSound);

        coverTween = OwnPresentationTween(DOVirtual.Vector3(coverPresentationRotation, Vector3.zero, flipDuration, (v) =>
         {
             coverPresentationRotation = v;
             cover.localEulerAngles = v;
         })
         .SetEase(Ease.InSine)
         .OnComplete(() => { coverTween = null; isAnimating = false; }), true);
    }

    private void EndNotebookPresentation()
    {
        if (moduleInteraction) moduleInteraction.InterruptInteraction();
        ResetNotebookPresentation();
    }

    private void CancelPresentationTweens()
    {
        coverTween?.Kill(false);
        pageTurnTween?.Kill(false);
        coverTween = null;
        pageTurnTween = null;
    }

    private Tween OwnPresentationTween(Tween tween, bool isCover)
    {
        return tween.OnKill(() =>
        {
            if (isCover)
            {
                if (ReferenceEquals(coverTween, tween)) coverTween = null;
            }
            else if (ReferenceEquals(pageTurnTween, tween)) pageTurnTween = null;
        });
    }

    private void ResetNotebookPresentation()
    {
        CancelPresentationTweens();
        isInteracting = false;
        isAnimating = false;
        lastDrawPosition = -Vector2.one;
        if (isDrawingCursorActive)
        {
            isDrawingCursorActive = false;
            CursorManager.OnClearCustomCursor?.Invoke();
        }
        coverPresentationRotation = Vector3.zero;
        if (cover) cover.localEulerAngles = Vector3.zero;
        NormalizePagePresentation();
    }

    private void NormalizePagePresentation()
    {
        if (pageMeshes == null) return;
        for (int i = 0; i < pageMeshes.Length; i++)
            if (pageMeshes[i] && pageMeshes[i].activeSelf)
                pageMeshes[i].transform.localEulerAngles = i < currentPageIndex ? pageFlippedRotation : Vector3.zero;
    }

    private void HandlePageScrolling()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (scroll == 0) return;
        if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var questView))
        {
            if (questView.ShouldBlockAction(TutorialAction.TurnPage)) return;

            questView.OnActionPerformed(TutorialAction.TurnPage);
        }

        if (scroll < 0)
        {
            int nextActive = FindNextActivePage(currentPageIndex);

            if (nextActive != -1)
            {
                isAnimating = true;
                PlaySound(pageFlipSound);

                Transform pageToFlip = pageMeshes[currentPageIndex].transform;

                pageTurnTween = OwnPresentationTween(DOVirtual.Vector3(Vector3.zero, pageFlippedRotation, flipDuration, (v) =>
                {
                    pageToFlip.localEulerAngles = v;
                })
                .SetEase(Ease.InOutSine)
                .OnComplete(() => { pageTurnTween = null; isAnimating = false; }), false);

                currentPageIndex = nextActive;
            }
        }
        else if (scroll > 0)
        {
            int prevActive = FindPrevActivePage(currentPageIndex);

            if (prevActive != -1)
            {
                isAnimating = true;
                PlaySound(pageFlipSound);

                currentPageIndex = prevActive;
                Transform pageToFlip = pageMeshes[currentPageIndex].transform;

                pageTurnTween = OwnPresentationTween(DOVirtual.Vector3(pageFlippedRotation, Vector3.zero, flipDuration, (v) =>
                {
                    pageToFlip.localEulerAngles = v;
                })
                .SetEase(Ease.InOutSine)
                .OnComplete(() => { pageTurnTween = null; isAnimating = false; }), false);
            }
        }
    }

    private void SelectRemainingPageAfterTear(int tornPageIndex)
    {
        // A turn and tear can start in the same input frame. Stop that writer before resetting the view.
        if (pageTurnTween != null)
        {
            pageTurnTween.Kill();
            pageTurnTween = null;
            isAnimating = false;
        }
        int next = FindNextActivePage(tornPageIndex);
        if (next == -1) next = FindPrevActivePage(tornPageIndex);
        if (next == -1) return;

        currentPageIndex = next;
        // Restore the same stack as a completed navigation: preceding pages turned, current/later pages open.
        for (int i = 0; i < pageMeshes.Length; i++)
            if (pageMeshes[i].activeSelf)
                pageMeshes[i].transform.localEulerAngles = i < next ? pageFlippedRotation : Vector3.zero;
        lastDrawPosition = -Vector2.one;
    }

    private int FindNextActivePage(int currentIndex)
    {
        for (int i = currentIndex + 1; i < pageMeshes.Length; i++)
        {
            if (pageMeshes[i].activeSelf) return i;
        }
        return -1;
    }

    private int FindPrevActivePage(int currentIndex)
    {
        for (int i = currentIndex - 1; i >= 0; i--)
        {
            if (pageMeshes[i].activeSelf) return i;
        }
        return -1;
    }



    private void PlaySound(EventReference sound)
    {
        if (audioChannel != null && !sound.IsNull)
        {
            AudioEventPayload payload = new AudioEventPayload(sound, transform.position);
            audioChannel.RaiseEvent(payload);
        }
    }
}
