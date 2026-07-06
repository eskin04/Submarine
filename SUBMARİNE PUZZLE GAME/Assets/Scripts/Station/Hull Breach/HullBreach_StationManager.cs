using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using System.Linq;

// ==========================================
// VERİ TİPLERİ
// ==========================================

public enum PlateMaterial { None, Steel, Carbon, Titanium }
public enum CrackZone { Left, Right, Front, Back }
public enum CrackState { Inactive, Active, Plated, Fixed, PermanentDamage }

[System.Serializable]
public struct CrackData
{
    public int crackID;
    public int floorIndex;
    public CrackZone zone;
    public int spawnPointIndex;
    public CrackState state;
}

public class HullBreach_StationManager : NetworkBehaviour
{
    [Header("Station Settings (Inspector)")]
    [SerializeField] private float damageTimerDuration = 85f; // Toplam süre
    [SerializeField] private float depthChangeInterval = 15f;
    [SerializeField] private float waterLevelPenaltyPerCrack = 0.05f;

    [Header("Live State (SyncVars)")]
    public SyncVar<bool> isRoundActive = new SyncVar<bool>(false);
    public SyncVar<int> currentDepth = new SyncVar<int>(200);
    public SyncVar<float> welderCharge = new SyncVar<float>(100f);

    public SyncVar<int> displayTimeRemaining = new SyncVar<int>(85);
    public SyncVar<bool> isPermanentDamageTriggered = new SyncVar<bool>(false);

    [Header("Backend Data (Server Only)")]
    private List<CrackData> activeCracks = new List<CrackData>();
    private int nextCrackID = 0;
    public List<HullBreach_CrackSocket> allSockets = new List<HullBreach_CrackSocket>();

    // Timer takipleri
    private float nextDepthChangeTime;
    private float nextCrackSpawnTime;
    private float internalTimeRemaining;

    // Sabit kat indeksleri (Kendi sistemine göre değiştirebilirsin)
    private const int ENGINEER_FLOOR = 0;
    private const int TECHNICIAN_FLOOR = 1;
    private const int MAX_FLOOR_COUNT = 2;

    private void Update()
    {
        if (!isServer || !isRoundActive.value) return;

        HandleGlobalDamageTimer();
        HandleDepthTimer();

        // 85 saniye dolmadıysa çatlak spawn etmeye devam et
        if (!isPermanentDamageTriggered.value)
        {
            HandleCrackSpawnTimer();
        }
    }

    #region SERVER BACKEND LOGIC

    public void RegisterSocket(HullBreach_CrackSocket socket)
    {
        if (!allSockets.Contains(socket))
        {
            allSockets.Add(socket);
        }
    }

    private void HandleGlobalDamageTimer()
    {
        if (isPermanentDamageTriggered.value) return;

        internalTimeRemaining -= Time.deltaTime;

        int secondsLeft = Mathf.CeilToInt(internalTimeRemaining);
        if (secondsLeft != displayTimeRemaining.value)
        {
            displayTimeRemaining.value = secondsLeft;
        }

        if (internalTimeRemaining <= 0)
        {
            internalTimeRemaining = 0;
            displayTimeRemaining.value = 0;
            TriggerGlobalPermanentDamage();
        }
    }

    private void TriggerGlobalPermanentDamage()
    {
        isPermanentDamageTriggered.value = true;
        int unclosedCrackCount = 0;

        for (int i = 0; i < activeCracks.Count; i++)
        {
            CrackData crack = activeCracks[i];
            if (crack.state == CrackState.Active || crack.state == CrackState.Plated)
            {
                crack.state = CrackState.PermanentDamage;
                activeCracks[i] = crack;
                unclosedCrackCount++;
            }
        }

        if (unclosedCrackCount > 0)
        {
            float totalPenalty = unclosedCrackCount * waterLevelPenaltyPerCrack;
            Debug.LogWarning($"<color=red>[SERVER]</color> 85 Saniye doldu! {unclosedCrackCount} adet açık çatlak kalıcı hasara dönüştü. Toplam Su Artışı: %{totalPenalty * 100}");
        }
    }

    private void HandleDepthTimer()
    {
        if (Time.time >= nextDepthChangeTime)
        {
            ChangeDepthLogic();
            nextDepthChangeTime = Time.time + depthChangeInterval;
        }
    }

    private void HandleCrackSpawnTimer()
    {
        if (Time.time >= nextCrackSpawnTime)
        {
            SpawnNewCrackRandomFloor();

            // İlk dinamik çatlak 15. saniyede (Start'tan ayarlanmıştı), sonrakiler her 20 saniyede bir.
            nextCrackSpawnTime = Time.time + 20f;
        }
    }

    private void ChangeDepthLogic()
    {
        int changeAmount = Random.Range(75, 151);
        int newDepth = currentDepth.value + changeAmount;
        if (newDepth > 800) newDepth = currentDepth.value - changeAmount;
        if (newDepth < 200) newDepth = 200;

        currentDepth.value = newDepth;
    }

    #endregion

    #region CRACK SPAWN ALGORITHM

    private void SpawnInitialCracks()
    {
        // 0. Saniye: Mühendis ve Teknisyen katında birer tane çatlak çıkar
        SpawnCrackAtFloor(ENGINEER_FLOOR);
        SpawnCrackAtFloor(TECHNICIAN_FLOOR);
    }

    private void SpawnNewCrackRandomFloor()
    {
        // Rastgele bir kat seç
        int randomFloor = Random.Range(0, MAX_FLOOR_COUNT);
        SpawnCrackAtFloor(randomFloor);
    }



    private void SpawnCrackAtFloor(int targetFloor)
    {
        // 1. KURAL KONTROLÜ: O katta halihazırda aktif/kapanmamış çatlağı olan bölgeleri (Zone) bul
        var occupiedZones = activeCracks
            .Where(c => c.floorIndex == targetFloor && (c.state == CrackState.Active || c.state == CrackState.Plated))
            .Select(c => c.zone)
            .ToList();

        // 2. FİLTRELEME: Sahnede kayıtlı tüm soketler (allSockets) içerisinden;
        // - İstenilen katta olanları,
        // - Bölgesinde halihazırda çatlak OLMAYANLARI filtrele.
        var availableSockets = allSockets
            .Where(s => s.floorIndex == targetFloor && !occupiedZones.Contains(s.zone))
            .ToList();

        // 3. SEÇİM: Eğer kurallara uygun boş soket varsa, rastgele birini seç
        if (availableSockets.Count > 0)
        {
            HullBreach_CrackSocket chosenSocket = availableSockets[Random.Range(0, availableSockets.Count)];
            ActivateCrackOnSocket(chosenSocket);
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[SERVER]</color> {targetFloor}. katta boş bölge veya uygun soket kalmadı!");
        }
    }

    private void ActivateCrackOnSocket(HullBreach_CrackSocket socket)
    {
        // Seçilen soketin kimlik bilgilerini (Kat, Bölge, İndeks) alarak veriyi oluştur
        CrackData newCrack = new CrackData
        {
            crackID = nextCrackID++,
            floorIndex = socket.floorIndex,
            zone = socket.zone,
            spawnPointIndex = socket.spawnPointIndex,
            state = CrackState.Active
        };

        activeCracks.Add(newCrack);

        // Sokete "Sen aktif oldun, suyunu akıtmaya başla" komutunu gönder
        socket.RpcActivateCrack(newCrack.crackID);

        Debug.Log($"<color=orange>[SERVER]</color> Yeni Çatlak Spawn Oldu! ID: {newCrack.crackID} | Kat: {socket.floorIndex} | Bölge: {socket.zone}");
    }



    #endregion
    #region PLATE PLACEMENT & DEPTH RULES

    // İstemciden gelen plaka takma isteği
    [ServerRpc(requireOwnership: false)]
    public void CmdTryPlacePlate(int crackID, GameObject plateObj, HullBreach_CrackSocket socket)
    {
        if (!isRoundActive.value) return;

        int crackIndex = activeCracks.FindIndex(c => c.crackID == crackID);
        if (crackIndex == -1) return;

        CrackData crack = activeCracks[crackIndex];
        if (crack.state != CrackState.Active) return; // Zaten kapanmışsa engelle

        HullBreach_PlateItem plateScript = plateObj.GetComponent<HullBreach_PlateItem>();
        if (plateScript == null) return;

        // PDF Tablosuna göre doğruluk kontrolü (Bir önceki mesajdaki tablo fonksiyonu)
        if (IsPlateValidForCrack(crack.zone, plateScript.plateMaterial, currentDepth.value))
        {


            // Plaka doğru! Durum kilitlendi, artık derinlik değişse de sorun yok.
            crack.state = CrackState.Plated;
            activeCracks[crackIndex] = crack;

            // Sokete plakayı görsel olarak yerleştirmesini söyle
            socket.RpcPlacePlateInSocket(plateObj);

            Debug.Log($"<color=green>[SERVER]</color> {plateScript.plateMaterial} plakası başarıyla yerleştirildi.");
        }
        else
        {
            Debug.LogWarning($"<color=orange>[SERVER]</color> Hatalı plaka denemesi!");
            // İsteğe bağlı hata sesi
        }
    }

    // Oyuncu takılı ama kaynaklanmamış plakayı geri almak isterse
    public void ServerSetCrackActive(int crackID)
    {
        int crackIndex = activeCracks.FindIndex(c => c.crackID == crackID);
        if (crackIndex == -1) return;

        CrackData crack = activeCracks[crackIndex];
        crack.state = CrackState.Active;
        activeCracks[crackIndex] = crack;

        Debug.Log($"<color=yellow>[SERVER]</color> Çatlak ID {crackID} üzerindeki plaka söküldü, su tekrar akıyor.");
    }

    private bool IsPlateValidForCrack(CrackZone zone, PlateMaterial material, int depth)
    {
        bool isUclar = (zone == CrackZone.Front || zone == CrackZone.Back);
        bool isYanlar = (zone == CrackZone.Left || zone == CrackZone.Right);

        // Derinlik 200m - 400m
        if (depth >= 200 && depth <= 400)
        {
            if (isUclar && material == PlateMaterial.Carbon) return true;
            if (isYanlar && material == PlateMaterial.Steel) return true;
        }
        // Derinlik 401m - 650m
        else if (depth >= 401 && depth <= 650)
        {
            if (isUclar && material == PlateMaterial.Titanium) return true;
            if (isYanlar && material == PlateMaterial.Carbon) return true;
        }
        // Derinlik 651m - 800m
        else if (depth >= 651 && depth <= 800)
        {
            if (isUclar && material == PlateMaterial.Steel) return true;
            if (isYanlar && material == PlateMaterial.Titanium) return true;
        }

        return false;
    }



    #endregion



    #region CONTEXT MENU TESTS (EDITOR)

    [ContextMenu("TEST: Start Hull Breach Round")]
    public void Test_StartRound()
    {
        if (isRoundActive.value) return;

        Debug.Log("<color=green>[TEST]</color> Hull Breach başlatılıyor...");

        activeCracks.Clear();
        welderCharge.value = 100f;
        currentDepth.value = Random.Range(200, 601);

        internalTimeRemaining = damageTimerDuration;
        displayTimeRemaining.value = Mathf.CeilToInt(damageTimerDuration);
        isPermanentDamageTriggered.value = false;
        isRoundActive.value = true;

        // 1. ADIM: Başlangıçta 2 çatlak oluştur (0. saniye)
        SpawnInitialCracks();

        // 2. ADIM: İlk dinamik çatlak için zamanlayıcıyı 15 saniye sonrasına kur
        nextCrackSpawnTime = Time.time + 15f;

        // Derinlik zamanlayıcısını kur
        nextDepthChangeTime = Time.time + depthChangeInterval;
    }

    #endregion
}