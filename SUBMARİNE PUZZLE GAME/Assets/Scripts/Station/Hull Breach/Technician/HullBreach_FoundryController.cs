using UnityEngine;
using PurrNet;
using DG.Tweening;

public class HullBreach_FoundryController : NetworkBehaviour
{
    public event System.Action<bool> OnInteractableStateChanged;
    [Header("Manager Reference")]
    public HullBreach_StationManager stationManager;

    [Header("References")]
    public Transform plateSlot;

    [Header("Plate Item Prefabs")]
    public GameObject steelPlatePrefab;
    public GameObject carbonPlatePrefab;
    public GameObject titaniumPlatePrefab;

    [Header("Animation Settings")]
    public float animDuration = 0.4f;
    public Ease animEase = Ease.OutBack;
    public float printDuration = 5f;


    [Header("Live State (SyncVars)")]
    public SyncVar<bool> isFoundryBusy = new SyncVar<bool>(false);
    public SyncVar<bool> hasPlateInSlot = new SyncVar<bool>(false);

    // Backend Timer
    private float printTimer = 0f;
    private PlateMaterial currentlyPrintingMaterial = PlateMaterial.None;

    private void Update()
    {
        if (!isServer) return;

        if (isFoundryBusy.value)
        {
            printTimer -= Time.deltaTime;

            if (printTimer <= 0f)
            {
                FinishPrinting();
            }
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void CmdStartPrinting(PlateMaterial requestedMaterial)
    {
        if (!stationManager.isRoundActive.value || isFoundryBusy.value || hasPlateInSlot.value || requestedMaterial == PlateMaterial.None)
        {
            return;
        }

        isFoundryBusy.value = true;
        currentlyPrintingMaterial = requestedMaterial;
        printTimer = printDuration;

        RpcOnPrintStarted(requestedMaterial);
    }

    private void FinishPrinting()
    {
        isFoundryBusy.value = false;
        hasPlateInSlot.value = true;

        GameObject prefabToSpawn = GetPlatePrefab(currentlyPrintingMaterial);

        if (prefabToSpawn != null)
        {
            GameObject newPlateObj = Instantiate(prefabToSpawn, plateSlot.position, plateSlot.rotation);
            newPlateObj.transform.SetParent(plateSlot);


            HullBreach_PlateItem plateScript = newPlateObj.GetComponent<HullBreach_PlateItem>();
            if (plateScript != null)
            {
                plateScript.OnPlateTakenServer += HandlePlateLooted;
            }

            Rigidbody rb = newPlateObj.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            newPlateObj.transform.localScale = Vector3.zero;
            newPlateObj.transform.DOScale(Vector3.one, animDuration).SetEase(animEase);

            // PurrNet.NetworkManager.Instantiate(newPlateObj); 
        }

        RpcOnPrintFinished(currentlyPrintingMaterial);
        currentlyPrintingMaterial = PlateMaterial.None;
    }

    private void HandlePlateLooted(HullBreach_PlateItem takenPlate)
    {
        takenPlate.OnPlateTakenServer -= HandlePlateLooted;
        RpcOnPlateTaken();

        hasPlateInSlot.value = false;

        Debug.Log($"<color=green>[FOUNDRY]</color> {takenPlate.gameObject.name} alındı, abonelik temizlendi ve makine üretime açıldı.");
    }

    private GameObject GetPlatePrefab(PlateMaterial material)
    {
        switch (material)
        {
            case PlateMaterial.Steel: return steelPlatePrefab;
            case PlateMaterial.Carbon: return carbonPlatePrefab;
            case PlateMaterial.Titanium: return titaniumPlatePrefab;
            default: return null;
        }
    }

    // ==============================================================
    // Inventory Interaction Callbacks
    // ==============================================================


    [ObserversRpc(runLocally: true)]
    private void RpcOnPrintStarted(PlateMaterial material)
    {
        OnInteractableStateChanged?.Invoke(false);
        // Audio
    }

    [ObserversRpc(runLocally: true)]
    private void RpcOnPrintFinished(PlateMaterial material)
    {

        // Audio
    }

    [ObserversRpc(runLocally: true)]
    private void RpcOnPlateTaken()
    {
        OnInteractableStateChanged?.Invoke(true);
    }
}