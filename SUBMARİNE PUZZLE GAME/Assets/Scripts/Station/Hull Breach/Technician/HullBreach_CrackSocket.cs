using UnityEngine;
using PurrNet;

[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(Collider))]
public class HullBreach_CrackSocket : NetworkBehaviour
{
    [Header("Location Data (Kimlik)")]
    public int floorIndex;
    public CrackZone zone;
    public int spawnPointIndex;

    [Header("References")]
    public HullBreach_StationManager stationManager;
    public ParticleSystem waterParticle; // YENİ: Su fışkırtma efekti

    [Header("Live State")]
    public int currentCrackID = -1;
    public bool isCrackSpawned = false; // YENİ: Çatlak aktif mi?
    public HullBreach_PlateItem slottedPlate;

    private Interactable interactable;
    private Collider socketCollider;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        socketCollider = GetComponent<Collider>();

        // Başlangıçta çatlak yok kabul ediyoruz ve her şeyi kapatıyoruz
        isCrackSpawned = false;
        UpdateSocketVisualsAndInteraction();

        // Yöneticiye bu soketin varlığını bildir (Manuel referans atama derdinden kurtarır)
        if (stationManager != null)
        {
            stationManager.RegisterSocket(this);
        }
    }

    private void Update()
    {
        if (!isServer) return;

        if (slottedPlate != null)
        {
            if (slottedPlate.transform.parent != this.transform || !slottedPlate.gameObject.activeSelf)
            {
                ServerHandlePlateRemoved();
            }
        }
    }

    // =====================================
    // ETKİLEŞİM VE LOKAL GÜNCELLEMELER
    // =====================================

    public void HandleInteraction()
    {
        if (stationManager == null || !stationManager.isRoundActive.value) return;

        // Sadece çatlak Varsa ve plaka YOKSA etkileşime izin ver
        if (isCrackSpawned && slottedPlate == null)
        {
            TryInsertPlate();
        }
    }

    private void TryInsertPlate()
    {
        // TODO: Kendi envanter sisteminden oyuncunun elindeki eşyayı al.

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

    private void ServerHandlePlateRemoved()
    {
        slottedPlate = null;
        stationManager.ServerSetCrackActive(currentCrackID);
        RpcClearSocket();
    }

    // =====================================
    // AĞ ÜZERİNDEN TETİKLENEN FONKSİYONLAR (RPC)
    // =====================================

    // YENİ: İstasyon bozulduğunda StationManager tarafından tetiklenir
    [ObserversRpc(runLocally: true)]
    public void RpcActivateCrack(int crackID)
    {
        currentCrackID = crackID;
        isCrackSpawned = true; // Çatlak artık aktif
        UpdateSocketVisualsAndInteraction();
    }

    [ObserversRpc(runLocally: true)]
    public void RpcPlacePlateInSocket(GameObject plateObj)
    {
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return;
        inv.ExtractCurrentHeldItem();

        slottedPlate = plateObj.GetComponent<HullBreach_PlateItem>();



        plateObj.transform.SetParent(this.transform);
        plateObj.transform.localPosition = Vector3.zero;
        plateObj.transform.localRotation = Quaternion.identity;

        UpdateSocketVisualsAndInteraction();
    }

    [ObserversRpc(runLocally: true)]
    private void RpcClearSocket()
    {
        slottedPlate = null;
        UpdateSocketVisualsAndInteraction();
    }

    // =====================================
    // DURUM KONTROL MERKEZİ
    // =====================================

    private void UpdateSocketVisualsAndInteraction()
    {
        if (interactable == null || socketCollider == null) return;

        // 1. DURUM: Çatlak hiç oluşmadıysa
        if (!isCrackSpawned)
        {
            interactable.SetInteractable(false);
            socketCollider.enabled = false;
            if (waterParticle != null && waterParticle.isPlaying) waterParticle.Stop();
            return;
        }

        // 2. DURUM: Çatlak oluşmuş ama Üstüne Plaka Takılmışsa
        if (slottedPlate != null)
        {
            interactable.SetInteractable(false); // Etkileşimi kapat
            socketCollider.enabled = false;
            if (waterParticle != null && waterParticle.isPlaying) waterParticle.Stop(); // Suyu durdur
        }
        // 3. DURUM: Çatlak oluşmuş ve Üzeri BOŞSA
        else
        {
            interactable.SetInteractable(true); // Etkileşimi aç (Oyuncu plaka takabilsin)
            socketCollider.enabled = true;
            if (waterParticle != null && !waterParticle.isPlaying) waterParticle.Play(); // Suyu fışkırt
        }
    }
}