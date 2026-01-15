using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class FloodManager : NetworkBehaviour
{
    public static System.Action<bool> OnGameEnd;

    [Header("Ayarlar")]
    [SerializeField] private float maxWater = 100f;
    [SerializeField] private float defaultFillRate = .25f;
    [SerializeField] private float brokenFillRate = .5f;

    // TABLO (Burayı doldurmayı unutma)
    [Header("Bozulma Zaman Çizelgesi")]
    public BreakdownProfile breakdownProfile;

    [Header("Network Verileri")]
    [SerializeField] private SyncVar<float> currentWater = new SyncVar<float>(0f);

    private bool criticalEventTriggered = false;
    private Queue<StationController> pendingStationsQueue = new Queue<StationController>();
    private List<StationController> brokenStations = new List<StationController>();
    private StationController destroyedStation;

    private bool isStart;
    private bool isGameOver;

    private float gameTimeCounter = 0f;
    private int currentWaveIndex = 0;
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
        InstanceHandler.UnregisterInstance<FloodManager>();
        LevelManager.OnLevelStarted -= StartFlood;
        currentWater.onChanged -= OnWaterChanged;
    }

    private void OnWaterChanged(float newVal)
    {
        UpdateWaterUI(newVal);
    }

    private void StartFlood(List<StationController> stations)
    {
        if (breakdownProfile == null || breakdownProfile.waves.Count == 0)
        {
            Debug.LogError("FloodManager: Breakdown Profile atanmamış veya içi boş!");
            return;
        }
        pendingStationsQueue.Clear();
        foreach (var station in stations)
        {
            station.StateChanged -= HandleStationState;
            station.StateChanged += HandleStationState;

            pendingStationsQueue.Enqueue(station);
        }

        isStart = true;
        gameTimeCounter = 0f;
        currentWaveIndex = 0;

    }

    private void Update()
    {
        if (!isServer || !isStart || isGameOver) return;

        gameTimeCounter += Time.deltaTime;

        CalculateWater();
        CalculateBrokenStations();
    }

    private void CalculateWater()
    {
        if (currentWater.value >= maxWater)
        {
            isGameOver = true;
            OnGameEnd?.Invoke(isGameOver);
            Debug.Log("Game Over");
            return;
        }

        int totalBrokenCount = brokenStations.Count + (destroyedStation != null ? 1 : 0);

        float fillRate = 0f;


        if (totalBrokenCount > 0)
        {
            fillRate = defaultFillRate;

            if (totalBrokenCount > 1)
            {
                fillRate += (totalBrokenCount - 1) * brokenFillRate;
            }
        }

        if (fillRate > 0)
        {
            currentWater.value += fillRate * Time.deltaTime;
        }

        if (!criticalEventTriggered && currentWater.value >= 60f)
            TriggerCriticalEvent();
    }

    private void CalculateBrokenStations()
    {
        if (breakdownProfile == null) return;
        if (pendingStationsQueue.Count == 0 || currentWaveIndex >= breakdownProfile.waves.Count) return;

        BreakdownWave currentWave = breakdownProfile.waves[currentWaveIndex];
        if (gameTimeCounter < currentWave.startTime) return;

        if (gameTimeCounter >= nextProbabilityCheckTime)
        {
            nextProbabilityCheckTime = gameTimeCounter + 1f;

            float totalDuration = currentWave.endTime - currentWave.startTime;
            float timePassed = gameTimeCounter - currentWave.startTime;
            float progress = Mathf.Clamp01(timePassed / totalDuration);

            float currentChance = Mathf.Lerp(currentWave.startProbability, currentWave.endProbability, progress);


            float diceRoll = Random.Range(0f, 100f);
            Debug.Log($"Zaman: {gameTimeCounter:F1} | Dalga: {currentWave.waveName} | Şans: %{currentChance:F1} | Zar: {diceRoll:F1}");

            if (diceRoll <= currentChance || progress >= 1.0f)
            {
                BreakNextStation();
                currentWaveIndex++;
            }
        }
    }

    private void BreakNextStation()
    {
        if (pendingStationsQueue.Count > 0)
        {
            StationController victim = pendingStationsQueue.Dequeue();
            victim.SetBroken();
            victim.StartStation();
            Debug.Log($"İstasyon Bozuldu: {victim.name}");
        }
    }

    private void HandleStationState(StationState state, StationController station)
    {
        switch (state)
        {
            case StationState.Broken:
                if (!brokenStations.Contains(station))
                    brokenStations.Add(station);
                break;

            case StationState.Destroyed:
                destroyedStation = station;
                if (brokenStations.Contains(station))
                    brokenStations.Remove(station);
                break;

            case StationState.Reparied:
                if (brokenStations.Contains(station))
                    brokenStations.Remove(station);
                break;
        }
    }

    private void UpdateWaterUI(float value)
    {
        var view = InstanceHandler.GetInstance<MainGameView>();
        if (view != null)
            view.SetWaterLevelText(value, maxWater);
    }

    private void TriggerCriticalEvent()
    {
        criticalEventTriggered = true;
        if (brokenStations.Count > 0)
        {
            var victim = brokenStations[Random.Range(0, brokenStations.Count)];
            victim.SetDestroyed();
        }
    }

    public void AddPenalty(float penaltyAmount)
    {
        if (!isServer) return;

        currentWater.value += penaltyAmount;

        Debug.Log($"<color=red>HATA YAPILDI! Su seviyesi {penaltyAmount} arttı.</color>");


        if (currentWater.value >= maxWater)
        {
            isGameOver = true;
            OnGameEnd?.Invoke(isGameOver);
            Debug.Log("Game Over (Ceza sebebiyle)");
        }

        if (!criticalEventTriggered && currentWater.value >= 60f)
        {
            TriggerCriticalEvent();
        }
    }
}

