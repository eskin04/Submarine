using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using System.Linq;

public class FloodManager : NetworkBehaviour
{
    public static System.Action<bool> OnGameEnd;

    [Header("Settings")]
    [SerializeField] private float maxWater = 100f;

    [Header("Main Station Setting")]
    [SerializeField] private float mainBaseFillRate = 0.25f;
    [SerializeField] private float mainExtraFillRate = 0.50f;
    public BreakdownProfile mainBreakdownProfile;

    [Header("Utility Station Setting")]
    [SerializeField] private float utilityFillRate = 0.10f;

    [Header("Network Data")]
    [SerializeField] private SyncVar<float> currentWater = new SyncVar<float>(0f);

    private bool criticalEventTriggered = false;

    private Queue<StationController> pendingMainStations = new Queue<StationController>();

    private List<StationController> brokenStations = new List<StationController>();
    private StationController destroyedStation;

    private bool isStart;
    private bool isGameOver;

    private float gameTimeCounter = 0f;
    private int currentMainWaveIndex = 0;

    private float nextProbabilityCheckTime = 0f;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        InstanceHandler.RegisterInstance(this);

        LevelManager.OnLevelStarted += StartFlood;
        GlobalEvents.OnAddFloodPenalty += AddPenalty;
        GlobalEvents.OnStationStatusChanged += HandleStationStatusChanged;
        currentWater.onChanged += OnWaterChanged;

    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        LevelManager.OnLevelStarted -= StartFlood;
        GlobalEvents.OnAddFloodPenalty -= AddPenalty;
        GlobalEvents.OnStationStatusChanged -= HandleStationStatusChanged;
        currentWater.onChanged -= OnWaterChanged;

    }

    private void StartFlood(List<StationController> mainStations)
    {
        if (mainBreakdownProfile == null)
        {
            Debug.LogError("FloodManager: Main Profil eksik!");
            return;
        }

        pendingMainStations.Clear();
        foreach (var station in mainStations)
        {

            pendingMainStations.Enqueue(station);
        }

        isStart = true;
        gameTimeCounter = 0f;
        currentMainWaveIndex = 0;
        nextProbabilityCheckTime = 1f;

        Debug.Log($"FloodManager Başladı. Main İstasyon Sayısı: {mainStations.Count}");
    }

    private void HandleStationStatusChanged(StationController station, StationState state)
    {

        switch (state)
        {
            case StationState.Broken:
                BlackOutModifier(station, 1);
                if (!brokenStations.Contains(station))
                    brokenStations.Add(station);
                break;

            case StationState.Destroyed:
                destroyedStation = station;
                if (brokenStations.Contains(station))
                    brokenStations.Remove(station);
                break;

            case StationState.Reparied:
                BlackOutModifier(station, -1);

                if (brokenStations.Contains(station))
                    brokenStations.Remove(station);
                break;
        }
    }

    private void BlackOutModifier(StationController station, int amount)
    {
        if (station.stationType == StationType.Main)
        {
            GlobalEvents.OnModifyStat?.Invoke(StatType.BlackOut, amount);
        }

    }

    private void Update()
    {
        if (!isServer || !isStart || isGameOver) return;

        gameTimeCounter += Time.deltaTime;
        TimerTextUpdate(gameTimeCounter);
        CalculateWater();

        bool shouldRollDice = false;
        if (gameTimeCounter >= nextProbabilityCheckTime)
        {
            shouldRollDice = true;
            nextProbabilityCheckTime = gameTimeCounter + 1f;
        }

        // SADECE MAIN SENARYOSUNU İŞLE
        ProcessBreakdownScenario(mainBreakdownProfile, pendingMainStations, ref currentMainWaveIndex, shouldRollDice);
    }

    private void ProcessBreakdownScenario(BreakdownProfile profile, Queue<StationController> queue, ref int waveIndex, bool shouldRollDice)
    {
        if (queue.Count == 0 || waveIndex >= profile.waves.Count) return;

        BreakdownWave currentWave = profile.waves[waveIndex];

        if (gameTimeCounter < currentWave.startTime) return;

        float totalDuration = currentWave.endTime - currentWave.startTime;
        float timePassed = gameTimeCounter - currentWave.startTime;
        float progress = Mathf.Clamp01(timePassed / totalDuration);

        // 1. ZAR ATMA
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

        // 2. SÜRE BİTİMİ (Zorunlu geçiş)
        if (progress >= 1.0f)
        {
            if (currentWave.endProbability >= 99.9f)
            {
                BreakNextStation(queue);
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

        // Listede hem Main hem Utility olabilir (HandleStationState sayesinde)
        int brokenMainCount = brokenStations.Count(s => s.stationType == StationType.Main);
        int brokenUtilityCount = brokenStations.Count(s => s.stationType == StationType.Utility);

        if (destroyedStation != null)
        {
            if (destroyedStation.stationType == StationType.Main) brokenMainCount++;
        }

        float totalFillRate = 0f;

        // Main Hesabı
        if (brokenMainCount > 0)
        {
            totalFillRate += mainBaseFillRate;
            if (brokenMainCount > 1) totalFillRate += (brokenMainCount - 1) * mainExtraFillRate;
        }

        // Utility Hesabı (StressManager bozsa bile burada sayılır)
        if (brokenUtilityCount > 0)
        {
            totalFillRate += brokenUtilityCount * utilityFillRate;
        }

        if (totalFillRate > 0)
        {
            currentWater.value += totalFillRate * Time.deltaTime;
        }

        if (!criticalEventTriggered && currentWater.value >= 60f)
            TriggerCriticalEvent();
    }



    private void UpdateWaterUI(float value)
    {
        var view = InstanceHandler.GetInstance<MainGameView>();
        if (view != null) view.SetWaterLevelText(value, maxWater);
    }
    [ObserversRpc]
    private void TimerTextUpdate(float timeInSeconds)
    {
        var view = InstanceHandler.GetInstance<MainGameView>();
        if (view != null)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
            string timeString = $"{minutes:00}:{seconds:00}";
            view.SetTimerText(timeString);
        }
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
    }

    // GlobalEvents tarafından çağrılır
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