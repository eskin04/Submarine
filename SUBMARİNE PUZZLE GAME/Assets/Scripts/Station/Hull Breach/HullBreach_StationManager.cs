using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using System.Linq;


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

[System.Serializable]
public struct StationPhaseConfig
{
    public string phaseName;
    [Tooltip("Minimum su seviyesi (Dahil)")]
    public float minWater;
    [Tooltip("Maksimum su seviyesi (Hariç)")]
    public float maxWater;
    public int initialTechCracks;
    public int initialEngCracks;
    public float newCrackInterval;
    public float crackLockDuration;
    public float waterDrainage;
}

public class HullBreach_StationManager : NetworkBehaviour
{
    [Header("Station Settings")]
    [SerializeField] private float depthChangeInterval = 15f;
    [SerializeField] private float depthChangeRangeStart = 75f;
    [SerializeField] private float depthChangeRangeEnd = 150f;
    [Header("Live State (SyncVars)")]
    public SyncVar<bool> isRoundActive = new SyncVar<bool>(false);
    public SyncVar<int> currentDepth = new SyncVar<int>(200);

    [Header("Phase Settings")]
    [Tooltip("Tablodaki değerlere göre ayarlanmış başlangıç durumları")]
    public List<StationPhaseConfig> phaseConfigs = new List<StationPhaseConfig>();
    private StationPhaseConfig activePhaseConfig;

    [Header("Dynamic Spawning System")]
    public bool isSpawningEnabled = true;
    public float waterPerCrack = 1.0f;
    private float rollTimer = 0f;
    private float cooldownTimer = 0f;

    [Header("Backend Data (Server Only)")]
    public List<CrackData> activeCracks = new List<CrackData>();
    private int nextCrackID = 0;
    public List<HullBreach_CrackSocket> allSockets = new List<HullBreach_CrackSocket>();
    float randomZRotation = 0f;

    private float nextDepthChangeTime;

    private const int ENGINEER_FLOOR = 1;
    private const int TECHNICIAN_FLOOR = 0;
    private const int MAX_FLOOR_COUNT = 2;


    public void StartStation()
    {
        randomZRotation = Random.Range(-30f, 30f);

        if (!isServer) return;

        if (isRoundActive.value) return;

        activeCracks.Clear();
        currentDepth.value = Random.Range(200, 601);
        isRoundActive.value = true;
        rollTimer = 0f;
        cooldownTimer = 0f;

        DetermineActivePhase();

        Debug.Log($"<color=orange>[SERVER]</color> Hull Breach İstasyonu Başlatıldı! Aktif Aşama: {activePhaseConfig.phaseName} | Başlangıç Derinliği: {currentDepth.value} m");

        nextDepthChangeTime = Time.time + depthChangeInterval;

        SpawnInitialCracks();

        SyncWithFloodManager();
    }

    private void DetermineActivePhase()
    {
        float currentWater = GetWaterLevel();
        bool phaseFound = false;

        foreach (var config in phaseConfigs)
        {
            if (currentWater >= config.minWater && currentWater < config.maxWater)
            {
                activePhaseConfig = config;
                phaseFound = true;
                break;
            }
        }

        if (!phaseFound)
        {
            Debug.LogWarning("<color=yellow>[SERVER]</color> Su seviyesi hiçbir aralığa uymadı! İlk aşama ayarları kullanılıyor.");
            if (phaseConfigs.Count > 0) activePhaseConfig = phaseConfigs[0];
        }
    }

    private void Update()
    {
        if (!isServer || !isRoundActive.value) return;

        HandleDepthTimer();

        if (!isSpawningEnabled) return;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        HandlePhaseSpawning();
    }

    private void SyncWithFloodManager()
    {
        if (InstanceHandler.TryGetInstance<FloodManager>(out FloodManager floodManager))
        {
            int activeCount = activeCracks.Count(c => c.state == CrackState.Active);
            int platedCount = activeCracks.Count(c => c.state == CrackState.Plated);

            floodManager.UpdateHullBreachData(
                isRoundActive.value,
                activeCount,
                platedCount,
                waterPerCrack
            );
        }
    }

    // ==========================================
    //  (NETWORK)
    // ==========================================

    #region SERVER BACKEND LOGIC


    [ServerRpc(requireOwnership: false)]
    private void UpdateWaterLevelServerRpc(float fillAmount)
    {
        GlobalEvents.OnAddFloodPenalty?.Invoke(fillAmount);
    }

    public void RegisterSocket(HullBreach_CrackSocket socket)
    {
        if (!allSockets.Contains(socket))
        {
            allSockets.Add(socket);
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

    private void ChangeDepthLogic()
    {
        int changeAmount = Random.Range((int)depthChangeRangeStart, (int)depthChangeRangeEnd + 1);
        Debug.Log("min: " + depthChangeRangeStart + " max: " + depthChangeRangeEnd + " changeAmount: " + changeAmount);
        int newDepth = currentDepth.value + changeAmount;
        if (newDepth > 800) newDepth = currentDepth.value - changeAmount;
        if (newDepth < 200) newDepth = 200;

        currentDepth.value = newDepth;
        Debug.Log($"<color=orange>[SERVER]</color> Su derinliği değişti! Yeni Derinlik: {currentDepth.value} m");
    }

    #endregion

    #region DYNAMIC CRACK SPAWN ALGORITHM

    private void HandlePhaseSpawning()
    {
        rollTimer += Time.deltaTime;

        if (rollTimer >= activePhaseConfig.newCrackInterval)
        {
            int randomFloor = Random.Range(0, MAX_FLOOR_COUNT);
            SpawnCrackAtFloor(randomFloor);

            cooldownTimer = activePhaseConfig.crackLockDuration;
            rollTimer = 0f;

            Debug.Log($"<color=orange>[SERVER]</color> Zamanlayıcı doldu. {randomFloor}. katta yeni çatlak oluşturuldu! Sistem {cooldownTimer}s kilitlendi.");
        }
    }

    private float GetWaterLevel()
    {
        if (InstanceHandler.TryGetInstance<FloodManager>(out FloodManager floodManager))
        {
            return floodManager.GetCurrentWaterLevel();
        }

        return 0f;
    }

    private void SpawnInitialCracks()
    {
        for (int i = 0; i < activePhaseConfig.initialEngCracks; i++)
        {
            SpawnCrackAtFloor(ENGINEER_FLOOR);
        }

        for (int i = 0; i < activePhaseConfig.initialTechCracks; i++)
        {
            SpawnCrackAtFloor(TECHNICIAN_FLOOR);
        }
    }

    private void SpawnCrackAtFloor(int targetFloor)
    {
        var occupiedZones = activeCracks
            .Where(c => c.floorIndex == targetFloor && (c.state == CrackState.Active || c.state == CrackState.Plated))
            .Select(c => c.zone)
            .ToList();

        var availableSockets = allSockets
            .Where(s => s.floorIndex == targetFloor &&
                        !occupiedZones.Contains(s.zone) &&
                        !activeCracks.Any(c => c.floorIndex == s.floorIndex && c.zone == s.zone && c.spawnPointIndex == s.spawnPointIndex))
            .ToList();

        if (availableSockets.Count > 0)
        {
            HullBreach_CrackSocket chosenSocket = availableSockets[Random.Range(0, availableSockets.Count)];
            ActivateCrackOnSocket(chosenSocket);
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[SERVER]</color> {targetFloor}. katta boş bölge veya hiç çatlamamış uygun soket kalmadı!");
        }
    }

    private void ActivateCrackOnSocket(HullBreach_CrackSocket socket)
    {
        CrackData newCrack = new CrackData
        {
            crackID = nextCrackID++,
            floorIndex = socket.floorIndex,
            zone = socket.zone,
            spawnPointIndex = socket.spawnPointIndex,
            state = CrackState.Active
        };

        activeCracks.Add(newCrack);

        socket.RpcActivateCrack(newCrack.crackID);
        SyncWithFloodManager();

        Debug.Log($"<color=orange>[SERVER]</color> Yeni Çatlak Spawn Oldu! ID: {newCrack.crackID} | Kat: {socket.floorIndex} | Bölge: {socket.zone}");
    }

    #endregion

    #region PLATE PLACEMENT & DEPTH RULES

    [ServerRpc(requireOwnership: false)]
    public void CmdTryPlacePlate(int crackID, GameObject plateObj, HullBreach_CrackSocket socket, RPCInfo info = default)
    {
        if (!isRoundActive.value) return;

        int crackIndex = activeCracks.FindIndex(c => c.crackID == crackID);
        if (crackIndex == -1) return;

        CrackData crack = activeCracks[crackIndex];
        if (crack.state != CrackState.Active) return;

        HullBreach_PlateItem plateScript = plateObj.GetComponent<HullBreach_PlateItem>();
        if (plateScript == null) return;

        if (IsPlateValidForCrack(crack.zone, plateScript.plateMaterial, currentDepth.value))
        {
            crack.state = CrackState.Plated;
            activeCracks[crackIndex] = crack;

            socket.RpcPlacePlateInSocket(plateObj, randomZRotation);
            SyncWithFloodManager();

            Debug.Log($"<color=green>[SERVER]</color> {plateScript.plateMaterial} plakası başarıyla yerleştirildi.");
        }
        else
        {
            Debug.LogWarning($"<color=orange>[SERVER]</color> Hatalı plaka denemesi!");
            TargetShowWarning(info.sender, "Wrong Plate!");
        }
    }

    [TargetRpc]
    public void TargetShowWarning(PlayerID target, string message)
    {
        InstanceHandler.GetInstance<GameViewManager>().ShowView<ModuleInfoView>(hideOthers: false);
        InstanceHandler.GetInstance<ModuleInfoView>().SetWarningText(message);
    }

    [ServerRpc(requireOwnership: false)]
    public void CmdFixCrack(int crackID)
    {
        if (!isRoundActive.value) return;

        int crackIndex = activeCracks.FindIndex(c => c.crackID == crackID);
        if (crackIndex == -1) return;

        CrackData crack = activeCracks[crackIndex];

        if (crack.state != CrackState.Plated) return;

        crack.state = CrackState.Fixed;
        activeCracks[crackIndex] = crack;

        Debug.Log($"<color=green>[SERVER]</color> Çatlak ID {crackID} tamamen kaynaklandı ve onarıldı!");

        RpcOnCrackFixed(crackID);
        SyncWithFloodManager();

        UpdateWaterLevelServerRpc(-activePhaseConfig.waterDrainage);

        bool hasActiveThreats = activeCracks.Any(c => c.state == CrackState.Active || c.state == CrackState.Plated);

        if (!hasActiveThreats)
        {
            cooldownTimer = activePhaseConfig.crackLockDuration;
            rollTimer = 0f;

        }
    }

    [ObserversRpc]
    private void RpcOnCrackFixed(int crackID)
    {
        HullBreach_CrackSocket socket = allSockets.FirstOrDefault(s => s.currentCrackID == crackID);
        if (socket != null)
        {
            Collider[] colliders = socket.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }
            socket.RpcOnCrackFixed();
        }
    }

    private bool IsPlateValidForCrack(CrackZone zone, PlateMaterial material, int depth)
    {
        bool isUclar = (zone == CrackZone.Front || zone == CrackZone.Back);
        bool isYanlar = (zone == CrackZone.Left || zone == CrackZone.Right);

        if (depth >= 200 && depth <= 400)
        {
            if (isUclar && material == PlateMaterial.Carbon) return true;
            if (isYanlar && material == PlateMaterial.Steel) return true;
        }
        else if (depth >= 401 && depth <= 650)
        {
            if (isUclar && material == PlateMaterial.Titanium) return true;
            if (isYanlar && material == PlateMaterial.Carbon) return true;
        }
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
        StartStation();
    }

    [ContextMenu("TEST: Spawn Crack")]
    public void Test_SpawnCrack()
    {
        int randomFloor = Random.Range(0, MAX_FLOOR_COUNT);
        SpawnCrackAtFloor(randomFloor);
    }

    #endregion
}