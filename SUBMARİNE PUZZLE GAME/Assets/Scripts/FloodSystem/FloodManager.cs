using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class FloodManager : NetworkBehaviour
{
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
        if (!isServer && !isStart) return;

        CalculateWater();

    }

    private void CalculateWater()
    {
        if (currentWater >= maxWater)
        {
            Debug.Log("Game Over");
            return;
        }


        int brokenCount = brokenStations.Count;
        int destroyedCount = destroyedStations.Count;


        float fillRate = DefaultFillRate + brokenFillRate * brokenCount + destroyedCount * brokenFillRate;
        currentWater.value += fillRate * Time.deltaTime;

        if (!criticalEventTriggered && currentWater.value >= 60f)
            TriggerCriticalEvent();


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
