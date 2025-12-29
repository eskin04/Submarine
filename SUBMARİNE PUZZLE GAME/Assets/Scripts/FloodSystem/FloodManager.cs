using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using TMPro;

public class FloodManager : NetworkBehaviour
{
    public static System.Action<bool> OnGameEnd;
    [SerializeField] private List<StationController> stations;
    [SerializeField] private int brokenStationNum;
    [SerializeField] private float maxWater;
    [SerializeField] private float DefaultFillRate;
    [SerializeField] private float brokenFillRate;
    [SerializeField] private SyncVar<float> currentWater = new SyncVar<float>(0f);
    private bool criticalEventTriggered = false;
    private List<StationController> brokenStations = new List<StationController>();
    private List<StationController> destroyedStations = new List<StationController>();
    private bool isStart;
    private bool isGameOver;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        MainGameState.startGame += StartFlood;

    }


    protected override void OnDestroy()
    {
        base.OnDestroy();
        MainGameState.startGame -= StartFlood;
    }

    private void StartFlood()
    {
        foreach (StationController station in stations)
        {
            station.StateChanged += HandleStationState;
            station.SetOperational();
        }

        List<StationController> shuffled = new List<StationController>(stations);
        System.Random rng = new System.Random();
        int n = shuffled.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (shuffled[k], shuffled[n]) = (shuffled[n], shuffled[k]);
        }

        int count = Mathf.Min(brokenStationNum, shuffled.Count);
        for (int i = 0; i < count; i++)
        {
            shuffled[i].SetBroken();
            shuffled[i].StartStation();
        }
        isStart = true;
    }

    private void HandleStationState(StationState state, StationController station)
    {
        switch (state)
        {
            case StationState.Broken:
                brokenStations.Add(station);
                break;
            case StationState.Destroyed:
                destroyedStations.Add(station);
                brokenStations.Remove(station);
                break;
        }
    }

    private void Update()
    {
        if (!isServer || !isStart || isGameOver) return;

        CalculateWater();

    }

    private void CalculateWater()
    {
        if (currentWater >= maxWater)
        {
            isGameOver = true;
            OnGameEnd?.Invoke(isGameOver);
            Debug.Log("Game Over");
            return;
        }


        int brokenCount = brokenStations.Count;
        int destroyedCount = destroyedStations.Count;


        float fillRate = DefaultFillRate + brokenFillRate * brokenCount + destroyedCount * brokenFillRate;
        currentWater.value += fillRate * Time.deltaTime;
        SetWaterLevelText();

        if (!criticalEventTriggered && currentWater.value >= 60f)
            TriggerCriticalEvent();


    }
    [ObserversRpc]
    private void SetWaterLevelText()
    {
        InstanceHandler.GetInstance<MainGameView>().SetWaterLevelText(currentWater.value, maxWater);
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
}
