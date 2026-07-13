using UnityEngine;
using PurrNet;
using UnityEngine.UI; // Slider için eklendi

[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(Collider))]
public class HullBreach_ChargeStation : NetworkBehaviour
{
    [Header("Station Settings")]
    public float chargeRate = 10f; // Saniyede %10
    public Transform stationDrillSlot; // Matkabın istasyonda oturacağı pivot noktası

    [Header("UI Visuals")]
    public Slider stationChargeBar;

    [Header("Light Shader")]
    public MeshRenderer lightRenderer;
    public float lightIntensity = 5.0f;

    [Header("Live State")]
    public HullBreach_DrillItem slottedDrill;

    private Interactable interactable;
    private Collider stationCollider;

    // Shader Değişkenleri
    private Material runtimeLightMaterial;
    private static readonly int LightSelectionProp = Shader.PropertyToID("_ColorIndex");
    private static readonly int IntensityProp = Shader.PropertyToID("_LightIntensity");
    private int lastColorIndex = 0;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        stationCollider = GetComponent<Collider>();

        // Shader materyalini kopyala
        if (lightRenderer != null) runtimeLightMaterial = lightRenderer.materials[0];

        if (stationChargeBar != null)
        {
            stationChargeBar.minValue = 0f;
            stationChargeBar.maxValue = 100f;
            stationChargeBar.value = 0f;
        }

        SetLightState(false);
        UpdateStationState();
    }

    // ==========================================
    // ŞARJ VE ÇEKİLME KONTROLÜ (Sadece Sunucu)
    // ==========================================
    private void Update()
    {
        if (!isServer) return;

        if (slottedDrill != null)
        {
            // OYUNCU MATKABI GERİ ALDI MI? (Keycard mantığı ile parent kontrolü)
            if (slottedDrill.transform.parent != stationDrillSlot || !slottedDrill.gameObject.activeSelf)
            {
                ServerHandleDrillRemoved();
            }
            else
            {
                // ŞARJ DOLUM MANTIĞI
                if (slottedDrill.currentCharge < 100f)
                {
                    slottedDrill.currentCharge += chargeRate * Time.deltaTime;
                    if (slottedDrill.currentCharge > 100f) slottedDrill.currentCharge = 100f;

                    // Görselleri herkese bildir
                    RpcUpdateChargeVisuals(slottedDrill.currentCharge);
                }
            }
        }
    }

    // ==========================================
    // ETKİLEŞİM (Interactable'ın OnInteract Eventi)
    // ==========================================
    public void HandleInteraction()
    {
        if (slottedDrill == null)
        {
            TryInsertDrill();
        }
    }

    private void TryInsertDrill()
    {
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return;

        GameObject heldObj = inv.GetCurrentHeldObject();
        if (heldObj == null) return;

        HullBreach_DrillItem drill = heldObj.GetComponent<HullBreach_DrillItem>();
        if (drill != null)
        {
            inv.ExtractCurrentHeldItem(); // Matkabı elden çıkar
            interactable.StopInteract();
            CmdPlaceDrillInStation(drill.gameObject);
        }
    }

    private void ServerHandleDrillRemoved()
    {
        slottedDrill = null;
        RpcClearStation();
    }

    // ==========================================
    // AĞ (NETWORK) GÖRSEL SENKRONİZASYONU
    // ==========================================
    [ServerRpc(requireOwnership: false)]
    private void CmdPlaceDrillInStation(GameObject drillObj)
    {
        RpcPlaceDrillInStation(drillObj);
    }

    [ObserversRpc(runLocally: true)]
    private void RpcPlaceDrillInStation(GameObject drillObj)
    {
        slottedDrill = drillObj.GetComponent<HullBreach_DrillItem>();

        // Matkabı yuvaya oturt
        drillObj.transform.SetParent(stationDrillSlot);
        drillObj.transform.localPosition = Vector3.zero;
        drillObj.transform.localRotation = Quaternion.identity; // Veya istasyonun pivotuna göre ayarla
        drillObj.GetComponent<Collider>().enabled = true; // Oyuncu tekrar lootlayabilsin diye açık bırakıyoruz
        drillObj.SetActive(true);

        Rigidbody rb = drillObj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        UpdateStationState();
    }

    [ObserversRpc(runLocally: true)]
    private void RpcClearStation()
    {
        slottedDrill = null;
        UpdateStationState();
    }

    // Şarj değerini slider'lara ve matkaba iletir
    [ObserversRpc]
    private void RpcUpdateChargeVisuals(float charge)
    {
        if (slottedDrill != null)
        {
            slottedDrill.currentCharge = charge;
            slottedDrill.UpdateDrillVisuals(); // Matkabın kendi ekranı
        }

        if (stationChargeBar != null)
        {
            stationChargeBar.value = charge; // İstasyonun ekranı
        }

        // Şarj 100 olunca yeşil ışığı yak
        SetLightState(charge >= 100f);
    }

    // ==========================================
    // DURUM VE IŞIK KONTROLLERİ
    // ==========================================
    private void UpdateStationState()
    {
        if (interactable == null || stationCollider == null) return;

        if (slottedDrill != null)
        {
            interactable.SetInteractable(false); // İstasyonun kendi tıklamasını kapat
            stationCollider.enabled = false;

            if (stationChargeBar != null) stationChargeBar.value = slottedDrill.currentCharge;
            SetLightState(slottedDrill.currentCharge >= 100f);
        }
        else
        {
            interactable.SetInteractable(true); // İstasyon matkap bekliyor
            stationCollider.enabled = true;

            if (stationChargeBar != null) stationChargeBar.value = 0f;
            SetLightState(false);
        }
    }

    // LightsOut sisteminden uyarlanan Shader kontrolü
    private void SetLightState(bool isFull)
    {
        if (runtimeLightMaterial == null) return;

        int index = isFull ? 2 : 0; // 2 = Yeşil, 0 = Kapalı

        runtimeLightMaterial.SetFloat(LightSelectionProp, index); // Renk seçimi

        if (lastColorIndex == 0 && index != 0)
        {
            runtimeLightMaterial.SetFloat(IntensityProp, lightIntensity); // Işığı aç
        }
        else if (lastColorIndex != 0 && index == 0)
        {
            runtimeLightMaterial.SetFloat(IntensityProp, 0.0f); // Işığı kapat
        }

        lastColorIndex = index;
    }
}