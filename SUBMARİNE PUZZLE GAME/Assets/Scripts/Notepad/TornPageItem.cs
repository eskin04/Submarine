using System.Collections;
using UnityEngine;
using PurrNet;

public class TornPageItem : NetworkBehaviour, IInventoryItem
{
    [Header("References")]
    [SerializeField] private MeshRenderer pageRenderer;
    [SerializeField] private Material baseMaterial;
    [SerializeField] private MeshFilter pageMeshFilter;

    [Header("Placement & Hologram Settings")]
    [SerializeField] private LayerMask placementLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private Material validHologramMat;
    [SerializeField] private Material invalidHologramMat;
    [SerializeField] private Vector3 pageExtents = new Vector3(0.15f, 0.2f, 0.05f);

    private Texture2D pageTexture;
    private Material instancedMat;

    private byte[] clientImageBuffer;

    private GameObject hologramObj;
    private MeshRenderer hologramRenderer;
    private bool isEquippedLocally = false;
    private bool isPlacementModeActive = false;
    private bool isValidPlacement = false;

    private Vector3 currentPlacePos;
    private Quaternion currentPlaceRot;
    private Transform currentPlaceParent;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        InitializeMaterial();
    }

    private void InitializeMaterial()
    {
        if (instancedMat != null) return;
        pageTexture = new Texture2D(2, 2);
        instancedMat = new Material(baseMaterial);
        instancedMat.SetTexture("_BaseMap", pageTexture);
        pageRenderer.material = instancedMat;
    }

    public void SetImageDataAndDistribute(byte[] completeData)
    {
        if (!isServer) return;

        ApplyImageData(completeData);

        StartCoroutine(DistributeChunksRoutine(completeData));
    }

    private IEnumerator DistributeChunksRoutine(byte[] imageData)
    {
        int chunkSize = 1000;
        int totalSize = imageData.Length;
        int totalChunks = Mathf.CeilToInt((float)totalSize / chunkSize);

        for (int i = 0; i < totalChunks; i++)
        {
            int length = Mathf.Min(chunkSize, totalSize - i * chunkSize);
            byte[] chunk = new byte[length];
            System.Array.Copy(imageData, i * chunkSize, chunk, 0, length);

            ReceiveChunkObserversRpc(chunk, i * chunkSize, totalSize);

            yield return null;
        }
    }

    [ObserversRpc]
    private void ReceiveChunkObserversRpc(byte[] chunk, int startIndex, int totalSize)
    {
        if (isServer) return;

        if (clientImageBuffer == null || clientImageBuffer.Length != totalSize)
        {
            clientImageBuffer = new byte[totalSize];
        }

        System.Array.Copy(chunk, 0, clientImageBuffer, startIndex, chunk.Length);

        if (startIndex + chunk.Length >= totalSize)
        {
            ApplyImageData(clientImageBuffer);
            clientImageBuffer = null;
        }
    }

    private void ApplyImageData(byte[] data)
    {
        if (data != null && data.Length > 0)
        {
            InitializeMaterial();
            pageTexture.LoadImage(data);
            pageTexture.Apply();
        }
    }

    private void TogglePromptView(bool isPlacementActive)
    {

        if (InstanceHandler.TryGetInstance<PromptView>(out var promptView))
        {
            if (isPlacementActive)
            {
                promptView.AddPrompt("page_snap", "Left Click", "Snap To Surface");
                promptView.RemovePrompt("page_place");
            }
            else
            {
                promptView.AddPrompt("page_place", "R", "Place Page");
                promptView.RemovePrompt("page_snap");
            }
        }
    }


    private void Update()
    {
        if (!isEquippedLocally) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            isPlacementModeActive = !isPlacementModeActive;
            if (hologramObj != null) hologramObj.SetActive(isPlacementModeActive);
            TogglePromptView(isPlacementModeActive);
        }

        if (isPlacementModeActive)
        {
            UpdateHologram();

            if (isValidPlacement && Input.GetMouseButtonDown(0))
            {
                PlacePageOnSurface();
            }
        }
    }


    private void UpdateHologram()
    {
        LayerMask raycastMask = placementLayer | obstacleLayer;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, 3f, raycastMask))
        {
            if (hologramObj == null) CreateHologram();
            hologramObj.SetActive(true);

            currentPlacePos = hit.point + (hit.normal * 0.01f);

            currentPlaceRot = Quaternion.LookRotation(-hit.normal);

            hologramObj.transform.position = currentPlacePos;
            hologramObj.transform.rotation = currentPlaceRot;
            currentPlaceParent = hit.collider.transform;

            bool isHitSurfacePlaceable = (placementLayer.value & (1 << hit.collider.gameObject.layer)) > 0;

            bool isOverlapping = Physics.CheckBox(currentPlacePos, pageExtents, currentPlaceRot, obstacleLayer);

            isValidPlacement = isHitSurfacePlaceable && !isOverlapping;

            hologramRenderer.material = isValidPlacement ? validHologramMat : invalidHologramMat;
        }
        else
        {
            if (hologramObj != null) hologramObj.SetActive(false);
            isValidPlacement = false;
        }
    }

    private void CreateHologram()
    {
        hologramObj = new GameObject("Page_Hologram_Root");

        GameObject visualObj = new GameObject("Hologram_Visual");
        visualObj.transform.SetParent(hologramObj.transform);

        MeshFilter mf = visualObj.AddComponent<MeshFilter>();
        hologramRenderer = visualObj.AddComponent<MeshRenderer>();

        mf.mesh = pageMeshFilter.mesh;

        visualObj.transform.localPosition = pageMeshFilter.transform.localPosition;
        visualObj.transform.localRotation = pageMeshFilter.transform.localRotation;
        visualObj.transform.localScale = pageMeshFilter.transform.localScale;
    }

    private void PlacePageOnSurface()
    {
        isPlacementModeActive = false;
        isEquippedLocally = false;
        if (hologramObj != null)
        {
            Destroy(hologramObj);
            hologramObj = null;
        }

        gameObject.SetActive(true);

        var rbPhysics = GetComponent<Rigidbody>();
        if (rbPhysics != null)
        {
            rbPhysics.isKinematic = true;
            rbPhysics.useGravity = false;
        }

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        transform.SetParent(null);
        transform.position = currentPlacePos;
        transform.rotation = currentPlaceRot;

        if (InventoryManager.LocalPlayer != null)
        {
            InventoryManager.LocalPlayer.RemoveCurrentItem();
        }
    }
    // =========================================================================================
    // IInventoryItem Implementation
    // =========================================================================================
    public void OnEquip()
    {
        isEquippedLocally = true;
        if (InstanceHandler.TryGetInstance<PromptView>(out var promptView))
        {
            promptView.AddPrompt("page_place", "R", "Place Page");
        }
    }

    public void OnUnequip()
    {
        isEquippedLocally = false;
        isPlacementModeActive = false;
        if (hologramObj != null) hologramObj.SetActive(false);
        if (InstanceHandler.TryGetInstance<PromptView>(out var promptView))
        {
            promptView.RemovePrompt("page_place");
            promptView.RemovePrompt("page_snap");


        }
    }

    public void OnDrop()
    {
        OnUnequip();
    }

    public void CanOperate(bool canOperate) { }
}