using System.Collections.Generic;
using PurrNet;
using Unity.VisualScripting;
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

    void Start()
    {
        foreach (StationController station in stations)
        {
            station.StateChanged += HandleStationState;
            station.SetOperational();
        }

        List<StationController> shuffled = new List<StationController>(stations);
        Debug.Log(shuffled.Count);
        System.Random rng = new System.Random();
        int n = shuffled.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (shuffled[k], shuffled[n]) = (shuffled[n], shuffled[k]); // Swap
        }

        // 3. İlk X tanesini boz
        int count = Mathf.Min(brokenStationNum, shuffled.Count);
        for (int i = 0; i < count; i++)
        {
            shuffled[i].SetBroken();
            Debug.Log($"Başlangıç Arızası: {shuffled[i].name}");
        }
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
        if (!isServer) return;

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
        Debug.Log(brokenCount + "broken destroyed " + destroyedCount);


        float fillRate = DefaultFillRate + brokenFillRate * brokenCount + destroyedCount * brokenFillRate;
        currentWater.value += fillRate * Time.deltaTime;

        if (!criticalEventTriggered && currentWater.value >= 5f)
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
