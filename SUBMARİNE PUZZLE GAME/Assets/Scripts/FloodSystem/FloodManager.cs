using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using System.Linq;

public class FloodManager : NetworkBehaviour
{
    public static System.Action<bool> OnGameEnd;

    [Header("Ayarlar")]
    [SerializeField] private float maxWater = 100f;

    [Header("Main İstasyon Ayarları")]
    [SerializeField] private float mainBaseFillRate = 0.25f;
    [SerializeField] private float mainExtraFillRate = 0.50f;
    public BreakdownProfile mainBreakdownProfile;

    [Header("Utility İstasyon Ayarları")]
    [SerializeField] private float utilityFillRate = 0.10f;
    public BreakdownProfile utilityBreakdownProfile;

    [Header("Network Verileri")]
    [SerializeField] private SyncVar<float> currentWater = new SyncVar<float>(0f);

    private bool criticalEventTriggered = false;

    private Queue<StationController> pendingMainStations = new Queue<StationController>();
    private Queue<StationController> pendingUtilityStations = new Queue<StationController>();

    private List<StationController> brokenStations = new List<StationController>();
    private StationController destroyedStation;

    private bool isStart;
    private bool isGameOver;

    // Zamanlayıcılar
    private float gameTimeCounter = 0f;
    private int currentMainWaveIndex = 0;
    private int currentUtilityWaveIndex = 0;

    private float nextProbabilityCheckTime = 0f;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        InstanceHandler.RegisterInstance(this);

        LevelManager.OnLevelStarted += StartFlood;
        currentWater.onChanged += OnWaterChanged;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        LevelManager.OnLevelStarted -= StartFlood;
        currentWater.onChanged -= OnWaterChanged;
    }

    private void StartFlood(List<StationController> mainStations, List<StationController> utilityStations)
    {
        if (mainBreakdownProfile == null || utilityBreakdownProfile == null)
        {
            Debug.LogError("FloodManager: Profiller eksik!");
            return;
        }

        pendingMainStations.Clear();
        foreach (var station in mainStations)
        {
            PrepareStation(station);
            pendingMainStations.Enqueue(station);
        }

        pendingUtilityStations.Clear();
        foreach (var station in utilityStations)
        {
            PrepareStation(station);
            pendingUtilityStations.Enqueue(station);
        }

        isStart = true;
        gameTimeCounter = 0f;
        currentMainWaveIndex = 0;
        currentUtilityWaveIndex = 0;
        nextProbabilityCheckTime = 1f;

        Debug.Log($"FloodManager Başladı. Main: {mainStations.Count}, Utility: {utilityStations.Count}");
    }

    private void PrepareStation(StationController station)
    {
        station.StateChanged -= HandleStationState;
        station.StateChanged += HandleStationState;
        station.SetOperational();
    }

    private void Update()
    {
        if (!isServer || !isStart || isGameOver) return;

        gameTimeCounter += Time.deltaTime;

        CalculateWater();

        bool shouldRollDice = false;
        if (gameTimeCounter >= nextProbabilityCheckTime)
        {
            shouldRollDice = true;
            nextProbabilityCheckTime = gameTimeCounter + 1f;
        }

        ProcessBreakdownScenario(mainBreakdownProfile, pendingMainStations, ref currentMainWaveIndex, shouldRollDice);
        ProcessBreakdownScenario(utilityBreakdownProfile, pendingUtilityStations, ref currentUtilityWaveIndex, shouldRollDice);
    }

    private void ProcessBreakdownScenario(BreakdownProfile profile, Queue<StationController> queue, ref int waveIndex, bool shouldRollDice)
    {
        if (queue.Count == 0 || waveIndex >= profile.waves.Count) return;

        BreakdownWave currentWave = profile.waves[waveIndex];

        if (gameTimeCounter < currentWave.startTime) return;

        float totalDuration = currentWave.endTime - currentWave.startTime;
        float timePassed = gameTimeCounter - currentWave.startTime;
        float progress = Mathf.Clamp01(timePassed / totalDuration);

        if (shouldRollDice)
        {
            float currentChance = Mathf.Lerp(currentWave.startProbability, currentWave.endProbability, progress);
            float diceRoll = Random.Range(0f, 100f);

            if (diceRoll <= currentChance)
            {
                BreakNextStation(queue);
                waveIndex++;
                return;
            }
        }

        if (progress >= 1.0f)
        {
            if (currentWave.endProbability >= 99.9f)
            {
                BreakNextStation(queue);
                Debug.Log("Süre doldu ve %100 ihtimal olduğu için kesin bozuldu.");
            }
            else
            {
                Debug.Log($"Dalga bitti ama şans yaver gitti. İstasyon bozulmadı. (EndProb: %{currentWave.endProbability})");
            }

            waveIndex++;
        }
    }

    private void BreakNextStation(Queue<StationController> queue)
    {
        if (queue.Count > 0)
        {
            StationController victim = queue.Dequeue();
            victim.SetBroken();
            Debug.Log($"İstasyon Bozuldu ({victim.stationType}): {victim.name}");
        }
    }

    private void CalculateWater()
    {
        if (currentWater.value >= maxWater)
        {
            isGameOver = true;
            OnGameEnd?.Invoke(isGameOver);
            return;
        }

        int brokenMainCount = brokenStations.Count(s => s.stationType == StationType.Main);
        int brokenUtilityCount = brokenStations.Count(s => s.stationType == StationType.Utility);

        if (destroyedStation != null)
        {
            if (destroyedStation.stationType == StationType.Main) brokenMainCount++;
            else brokenUtilityCount++;
        }

        float totalFillRate = 0f;

        if (brokenMainCount > 0)
        {
            totalFillRate += mainBaseFillRate;
            if (brokenMainCount > 1) totalFillRate += (brokenMainCount - 1) * mainExtraFillRate;
        }

        if (brokenUtilityCount > 0)
        {
            totalFillRate += brokenUtilityCount * utilityFillRate;
        }

        if (totalFillRate > 0)
        {
            currentWater.value += totalFillRate * Time.deltaTime;
        }

        if (!criticalEventTriggered && currentWater.value >= (maxWater * 0.75f))
            TriggerCriticalEvent();
    }

    private void HandleStationState(StationState state, StationController station)
    {
        switch (state)
        {
            case StationState.Broken:
                if (!brokenStations.Contains(station)) brokenStations.Add(station);
                break;

            case StationState.Destroyed:
                destroyedStation = station;
                if (brokenStations.Contains(station)) brokenStations.Remove(station);
                break;

            case StationState.Operational:
                if (brokenStations.Contains(station)) brokenStations.Remove(station);
                break;
        }
    }

    private void UpdateWaterUI(float value)
    {
        var view = InstanceHandler.GetInstance<MainGameView>();
        if (view != null) view.SetWaterLevelText(value, maxWater);
    }

    private void OnWaterChanged(float newVal)
    {
        UpdateWaterUI(newVal);
    }

    private void TriggerCriticalEvent()
    {
        criticalEventTriggered = true;

        var brokenMainStations = brokenStations.Where(s => s.stationType == StationType.Main).ToList();

        if (brokenMainStations.Count > 0)
        {
            var victim = brokenMainStations[Random.Range(0, brokenMainStations.Count)];
            victim.SetDestroyed();
            Debug.Log($"KRİTİK OLAY: {victim.name} (Main) kalıcı olarak yok edildi!");
        }
        else
        {
            Debug.Log("Kritik olay tetiklendi ancak yok edilecek bozuk bir Main istasyon bulunamadı.");
        }
    }

    public void AddPenalty(float penaltyAmount)
    {
        if (!isServer) return;
        currentWater.value += penaltyAmount;
        if (currentWater.value >= maxWater)
        {
            isGameOver = true;
            OnGameEnd?.Invoke(isGameOver);
        }
    }
}