using UnityEngine;
using PurrNet;

[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(Collider))]
public class HullBreach_CrackSocket : NetworkBehaviour
{
    [Header("Location Data")]
    public int floorIndex;
    public CrackZone zone;
    public int spawnPointIndex;
    [Header("Particle System")]
    public ParticleSystem waterParticlePrefab; // Inspector'dan eklenecek asıl Prefab
    private ParticleSystem activeWaterParticle;
    public float offsetRateMultiplier = 0.15f; // Sızıntı modunda emisyon oranı çarpanı
    public float offsetSpeedMultiplier = 0.2f; // Sızıntı modunda parçacık hızı çarpanı

    [Header("References")]
    public HullBreach_StationManager stationManager;
    public ModuleInteraction moduleInteraction; // Soketin üzerindeki ModuleInteraction

    [Header("Live State")]
    public int currentCrackID = -1;
    public bool isCrackSpawned = false;
    public HullBreach_PlateItem slottedPlate;
    public bool isFixed = false; // Çatlak tamamen onarıldı mı?

    private Interactable interactable;
    private Collider socketCollider;
    private int weldedPointsCount = 0;
    private float originalRateMultiplier;
    private float originalSpeedMultiplier;
    private bool isOriginalsSaved = false;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        socketCollider = GetComponent<Collider>();

        // Başlangıçta modül sistemini kapalı tutuyoruz
        if (moduleInteraction != null) moduleInteraction.enabled = false;

        isCrackSpawned = false;
        UpdateSocketVisualsAndInteraction();

        if (stationManager != null) stationManager.RegisterSocket(this);

        // Dinamik Etkileşim Koşulumuzu (Condition) Bağlıyoruz
        interactable.onInteractCondition += CanInteractWithSocket;
    }

    // ==========================================
    // DİNAMİK ETKİLEŞİM ŞARTLARI
    // ==========================================
    private bool CanInteractWithSocket()
    {
        // Çatlak yoksa veya tamamen onarıldıysa hiçbir şekilde etkileşim yok
        if (!isCrackSpawned || isFixed) return false;

        if (slottedPlate == null)
        {
            // DURUM 1: Çatlak var ama plaka yok -> Elimizde PLAKA olmalı
            return CheckIfHoldingPlate();
        }
        else
        {
            // DURUM 2: Plaka takılmış -> Modüle girebilmek için elimizde MATKAP olmalı
            return CheckIfHoldingDrill();
        }
    }

    private bool CheckIfHoldingPlate()
    {
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return false;
        GameObject heldItemObj = inv.GetCurrentHeldObject();
        if (heldItemObj == null) return false;
        HullBreach_PlateItem plate = heldItemObj.GetComponent<HullBreach_PlateItem>();
        if (plate != null) return true;

        return false;
        // TODO: Kendi envanter sisteminden kontrol et
        // Örnek: return InventoryManager.Instance.GetEquippedItem().type == ItemType.Plate;
    }

    private bool CheckIfHoldingDrill()
    {
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return false;
        GameObject heldItemObj = inv.GetCurrentHeldObject();
        if (heldItemObj == null) return false;
        HullBreach_DrillItem drill = heldItemObj.GetComponent<HullBreach_DrillItem>();
        if (drill != null) return true;

        return false;
        // TODO: Kendi envanter sisteminden kontrol et
    }

    // ==========================================
    // ETKİLEŞİM YÖNLENDİRİCİSİ (Router)
    // ==========================================
    // Inspector'dan Interactable.OnInteract eventine SADECE bu fonksiyonu bağla
    public void OnSocketInteracted()
    {
        if (stationManager == null || !stationManager.isRoundActive.value || !isCrackSpawned) return;

        if (slottedPlate == null)
        {
            // Plaka takılmamış: Plakayı yerleştirmeyi dene
            TryInsertPlate();
        }
        else if (!isFixed && moduleInteraction != null)
        {
            // Plaka takılmış: Modülü başlat ve kamerayı kilitle
            moduleInteraction.enabled = true;
            moduleInteraction.Interact();
            HullBreach_DrillItem drill = GetPlayerEquippedDrill();
            if (drill != null)
            {
                // Plakanın transform bilgisini veriyoruz ki matkap yüzeyi tanısın
                drill.StartMinigame(slottedPlate.transform);
            }
        }
    }

    public void OnStopInteract()
    {
        if (moduleInteraction != null && moduleInteraction.enabled)
        {
            HullBreach_DrillItem drill = GetPlayerEquippedDrill();
            if (drill != null) drill.StopMinigame();
        }
    }

    private void TryInsertPlate()
    {
        // TODO: Kendi envanterinden oyuncunun elindeki eşyayı (objeyi) al
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return;
        GameObject heldItemObj = inv.GetCurrentHeldObject();
        if (heldItemObj == null) return;

        HullBreach_PlateItem heldPlate = heldItemObj.GetComponent<HullBreach_PlateItem>();
        if (heldPlate != null)
        {
            interactable.StopInteract();
            stationManager.CmdTryPlacePlate(currentCrackID, heldItemObj, this);
        }
    }

    // ==========================================
    // AĞ VE DURUM GÜNCELLEMELERİ
    // ==========================================

    [ObserversRpc(runLocally: true)]
    public void RpcActivateCrack(int crackID)
    {
        currentCrackID = crackID;
        isCrackSpawned = true;
        isFixed = false;
        slottedPlate = null;
        UpdateSocketVisualsAndInteraction();
    }

    // Plaka takıldığında çalışacak RPC (Z ekseninde yamukluk ile)
    [ObserversRpc(runLocally: true)]
    public void RpcPlacePlateInSocket(GameObject plateObj, float randomZRot)
    {
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return;
        inv.RemoveCurrentItem();
        slottedPlate = plateObj.GetComponent<HullBreach_PlateItem>();

        plateObj.transform.SetParent(this.transform);
        plateObj.transform.localPosition = Vector3.zero;
        plateObj.transform.localRotation = Quaternion.Euler(0, 0, randomZRot);
        plateObj.SetActive(true);




        // Plakanın köşelerindeki vidaları sokete bağla
        weldedPointsCount = 0;
        HullBreach_WeldPoint[] points = plateObj.GetComponentsInChildren<HullBreach_WeldPoint>();
        foreach (var point in points)
        {
            point.Initialize(this);
        }
        interactable.SetDisplayName("Use Drill to Weld");
        UpdateSocketVisualsAndInteraction();
    }

    // WeldPoint scriptinden tetiklenir
    public void OnPointWelded()
    {
        weldedPointsCount++;
        if (weldedPointsCount >= 4)
        {
            stationManager.CmdFixCrack(currentCrackID);
        }
    }

    public void RpcOnCrackFixed()
    {
        isFixed = true;

        // Kaynak tamamen bittiğinde eğer hala modüldeysek otomatik çıkış yap
        if (moduleInteraction != null && moduleInteraction.enabled)
        {
            // TODO: Sende fonksiyonun adı neyse onu yaz (Örn: ExitModule(), StopInteraction())
            moduleInteraction.StopInteract();
            interactable.SetInteractable(false);
            HullBreach_DrillItem drill = GetPlayerEquippedDrill();
            if (drill != null) drill.StopMinigame();
        }

        UpdateSocketVisualsAndInteraction();
    }

    private HullBreach_DrillItem GetPlayerEquippedDrill()
    {
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return null;
        GameObject heldItemObj = inv.GetCurrentHeldObject();
        if (heldItemObj == null) return null;

        HullBreach_DrillItem heldDrill = heldItemObj.GetComponent<HullBreach_DrillItem>();
        return heldDrill;
    }

    private void UpdateSocketVisualsAndInteraction()
    {
        if (interactable == null || socketCollider == null) return;

        if (!isCrackSpawned || isFixed)
        {
            interactable.SetInteractable(false);
            socketCollider.enabled = false;

            // Eğer aktif bir su efekti varsa durdur ve sahneden tamamen sil
            if (activeWaterParticle != null)
            {
                activeWaterParticle.Stop(); // Önce durdur ki su aniden yok olmasın, kalan damlalar süzülsün

                // 2 saniye bekle ve objeyi RAM'den sil (Kalan partiküllerin yok olma süresi)
                Destroy(activeWaterParticle.gameObject, 2f);
                activeWaterParticle = null;
            }
            return;
        }

        // Çatlak aktifse ve plaka yoksa veya modül için hazırsa
        interactable.SetInteractable(true);
        socketCollider.enabled = true; // Hem plaka takarken hem modüle girerken tıklanabilir olmalı

        if (activeWaterParticle == null && waterParticlePrefab != null)
        {
            // Prefab'dan yeni bir su efekti üret ve bu soketin içine (Child olarak) koy
            activeWaterParticle = Instantiate(waterParticlePrefab, transform);

            // Pozisyonunu ve rotasyonunu soketin tam merkezine sıfırla
            activeWaterParticle.transform.localPosition = Vector3.zero;
            activeWaterParticle.transform.localRotation = Quaternion.identity;

            // Orijinal değerleri yeni üretilen bu kopyadan alıp hafızaya kaydet
            originalRateMultiplier = activeWaterParticle.emission.rateOverTimeMultiplier;
            originalSpeedMultiplier = activeWaterParticle.main.startSpeedMultiplier;
        }

        if (activeWaterParticle != null)
        {
            var main = activeWaterParticle.main;
            var emission = activeWaterParticle.emission;

            if (slottedPlate != null)
            {
                // PLAKA TAKILI -> Sızıntı Modu (Orijinal değerin %15'i ve %20'si)
                if (!activeWaterParticle.isPlaying) activeWaterParticle.Play();

                emission.rateOverTimeMultiplier = originalRateMultiplier * offsetRateMultiplier;
                main.startSpeedMultiplier = originalSpeedMultiplier * offsetSpeedMultiplier;
            }
            else
            {
                // PLAKA YOK -> Tam Tazyik (Burst) Modu
                if (!activeWaterParticle.isPlaying) activeWaterParticle.Play();

                emission.rateOverTimeMultiplier = originalRateMultiplier;
                main.startSpeedMultiplier = originalSpeedMultiplier;
            }
        }
    }
}