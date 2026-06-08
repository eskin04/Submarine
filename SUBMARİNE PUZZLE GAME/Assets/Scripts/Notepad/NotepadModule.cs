using UnityEngine;
using PurrNet;
using DG.Tweening;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;

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

    // Durum Değişkenleri
    private int currentPageIndex = 0;
    private bool isInteracting = false;
    private bool isAnimating = false;
    private bool isDrawingCursorActive = false;
    private int remainingPages = 4;

    private Texture2D[] pageTextures;
    private Vector2 lastDrawPosition = -Vector2.one;

    private void Start()
    {
        InitializeDrawingPages();
    }

    private void Update()
    {
        if (!isInteracting || isAnimating) return;

        HandlePageScrolling();
        HandleDrawingAndCursor();
        HandlePageTearing();
    }

    private void HandlePageTearing()
    {
        if (Input.GetKeyDown(KeyCode.E) && remainingPages > 0)
        {
            if (pageMeshes[currentPageIndex].activeSelf == false) return;

            isAnimating = true;
            PlaySound(pageFlipSound);

            Texture2D currentTex = pageTextures[currentPageIndex];
            byte[] compressedData = currentTex.EncodeToJPG(50);

            pageMeshes[currentPageIndex].SetActive(false);
            remainingPages--;

            if (remainingPages > 0)
            {
                int nextActive = FindNextActivePage(currentPageIndex);
                if (nextActive != -1)
                {
                    currentPageIndex = nextActive;
                }

            }

            StartCoroutine(SendImageInChunksRoutine(compressedData, InventoryManager.LocalPlayer));

            if (isDrawingCursorActive)
            {
                CursorManager.OnClearCustomCursor?.Invoke();
                isDrawingCursorActive = false;
            }

            DOVirtual.DelayedCall(0.3f, () => isAnimating = false);
        }
    }

    private IEnumerator SendImageInChunksRoutine(byte[] imageData, InventoryManager targetInventory)
    {
        int chunkSize = 1000;
        int totalChunks = Mathf.CeilToInt((float)imageData.Length / chunkSize);

        string uniqueImageId = System.Guid.NewGuid().ToString();

        PrepareImageServerRpc(uniqueImageId, imageData.Length, targetInventory);

        yield return null;

        for (int i = 0; i < totalChunks; i++)
        {
            int length = Mathf.Min(chunkSize, imageData.Length - i * chunkSize);
            byte[] chunk = new byte[length];
            System.Array.Copy(imageData, i * chunkSize, chunk, 0, length);

            SendChunkServerRpc(uniqueImageId, chunk, i * chunkSize);

            yield return null;
        }
    }


    private Dictionary<string, byte[]> incomingImages = new Dictionary<string, byte[]>();
    private Dictionary<string, InventoryManager> imageOwners = new Dictionary<string, InventoryManager>();

    [ServerRpc(requireOwnership: false)]
    private void PrepareImageServerRpc(string imageId, int totalSize, InventoryManager targetInventory)
    {
        incomingImages[imageId] = new byte[totalSize];
        imageOwners[imageId] = targetInventory;
    }

    [ServerRpc(requireOwnership: false)]
    private void SendChunkServerRpc(string imageId, byte[] chunk, int startIndex)
    {
        if (!incomingImages.ContainsKey(imageId)) return;

        System.Array.Copy(chunk, 0, incomingImages[imageId], startIndex, chunk.Length);

        if (startIndex + chunk.Length >= incomingImages[imageId].Length)
        {
            byte[] completeImageData = incomingImages[imageId];
            InventoryManager ownerInventory = imageOwners[imageId];

            incomingImages.Remove(imageId);
            imageOwners.Remove(imageId);

            SpawnTornPageWithImage(completeImageData, ownerInventory);
        }
    }

    private void SpawnTornPageWithImage(byte[] completeData, InventoryManager targetInventory)
    {
        GameObject tornPage = Instantiate(tornPagePrefab, transform.position, transform.rotation);

        var netObj = tornPage.GetComponent<NetworkTransform>();
        if (netObj != null && targetInventory != null)
            netObj.GiveOwnership(targetInventory.owner);

        TornPageItem tornItem = tornPage.GetComponent<TornPageItem>();

        if (tornItem != null)
        {
            tornItem.SetImageDataAndDistribute(completeData);
        }

        if (targetInventory != null)
        {
            DOVirtual.DelayedCall(0.1f, () =>
            {
                if (tornPage != null) targetInventory.ForcePickupClientRpc(tornPage);
            });
        }
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
        if (isInteracting) return;

        isInteracting = true;
        isAnimating = true;

        PlaySound(openSound);

        DOVirtual.Vector3(Vector3.zero, coverOpenRotation, flipDuration, (v) =>
        {
            cover.localEulerAngles = v;
        })
        .SetEase(Ease.OutSine)
        .OnComplete(() => isAnimating = false);
    }

    public void OnNotebookStopInteract()
    {
        if (!isInteracting) return;

        isAnimating = true;
        isInteracting = false;

        if (isDrawingCursorActive)
        {
            isDrawingCursorActive = false;
            CursorManager.OnClearCustomCursor?.Invoke();
        }


        PlaySound(closeSound);

        DOVirtual.Vector3(coverOpenRotation, Vector3.zero, flipDuration, (v) =>
         {
             cover.localEulerAngles = v;
         })
         .SetEase(Ease.InSine)
         .OnComplete(() => isAnimating = false);
    }

    private void HandlePageScrolling()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (scroll == 0) return;

        if (scroll < 0)
        {
            int nextActive = FindNextActivePage(currentPageIndex);

            if (nextActive != -1)
            {
                isAnimating = true;
                PlaySound(pageFlipSound);

                Transform pageToFlip = pageMeshes[currentPageIndex].transform;

                DOVirtual.Vector3(Vector3.zero, pageFlippedRotation, flipDuration, (v) =>
                {
                    pageToFlip.localEulerAngles = v;
                })
                .SetEase(Ease.InOutSine)
                .OnComplete(() => isAnimating = false);

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

                DOVirtual.Vector3(pageFlippedRotation, Vector3.zero, flipDuration, (v) =>
                {
                    pageToFlip.localEulerAngles = v;
                })
                .SetEase(Ease.InOutSine)
                .OnComplete(() => isAnimating = false);
            }
        }
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