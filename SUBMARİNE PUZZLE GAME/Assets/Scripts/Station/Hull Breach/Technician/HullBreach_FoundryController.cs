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

    [Header("Live State (SyncVars)")]
    public SyncVar<bool> isFoundryBusy = new SyncVar<bool>(false);
    public SyncVar<bool> hasPlateInSlot = new SyncVar<bool>(false); // YENİ: Plaka yuvada mı?

    // Backend Timer
    private float printTimer = 0f;
    private const float PRINT_DURATION = 5f;
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
        // İstasyon aktif değilse, makine meşgulse, slotta eşya varsa engelle
        if (!stationManager.isRoundActive.value || isFoundryBusy.value || hasPlateInSlot.value || requestedMaterial == PlateMaterial.None)
        {
            return;
        }

        isFoundryBusy.value = true;
        currentlyPrintingMaterial = requestedMaterial;
        printTimer = PRINT_DURATION;

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

            // ========================================================
            // EVENT ABONELİĞİ (Parent'tan bağımsız referans kurgusu)
            // ========================================================
            HullBreach_PlateItem plateScript = newPlateObj.GetComponent<HullBreach_PlateItem>();
            if (plateScript != null)
            {
                // Plaka alındığında HandlePlateLooted fonksiyonumuzu tetiklemesini söylüyoruz
                plateScript.OnPlateTakenServer += HandlePlateLooted;
            }
            // ========================================================

            Rigidbody rb = newPlateObj.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            newPlateObj.transform.localScale = Vector3.zero;
            newPlateObj.transform.DOScale(Vector3.one, animDuration).SetEase(animEase);

            // EĞER AĞDA DOĞURMAN GEREKİYORSA:
            // PurrNet.NetworkManager.Instantiate(newPlateObj); 
        }

        RpcOnPrintFinished(currentlyPrintingMaterial);
        currentlyPrintingMaterial = PlateMaterial.None;
    }

    private void HandlePlateLooted(HullBreach_PlateItem takenPlate)
    {
        // 1. İŞLEM: Memory Leak ve mantık hatalarını önlemek için aboneliği anında kaldır (-=)
        takenPlate.OnPlateTakenServer -= HandlePlateLooted;
        RpcOnPlateTaken();

        // 2. İŞLEM: Makineyi yeni üretime aç
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
    // ENVARTER SİSTEMİ İLE BAĞLANTI (ITEM ALINDIĞINDA TETİKLENECEK)
    // ==============================================================


    // ==============================================================

    [ObserversRpc(runLocally: true)]
    private void RpcOnPrintStarted(PlateMaterial material)
    {
        OnInteractableStateChanged?.Invoke(false);
        // Ses ve Animasyon başlangıcı
    }

    [ObserversRpc(runLocally: true)]
    private void RpcOnPrintFinished(PlateMaterial material)
    {
        // Bitiş sesi ve efektler
    }

    [ObserversRpc(runLocally: true)]
    private void RpcOnPlateTaken()
    {
        // Makine boşaldı, butonları tekrar etkileşime aç
        OnInteractableStateChanged?.Invoke(true);
    }
}