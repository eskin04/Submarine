using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using System.Linq;

public class LevelManager : NetworkBehaviour
{
    public static System.Action<List<StationController>, List<StationController>> OnLevelStarted;

    [Header("Level Data")]
    public LevelData currentLevelData;

    [Header("References")]
    public List<StationController> stationControllers;

    public List<StationController> resultMains = new List<StationController>();
    public List<StationController> resultUtilities = new List<StationController>();

    protected override void OnSpawned()
    {
        base.OnSpawned();
        MainGameState.startGame += StartLevel;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MainGameState.startGame -= StartLevel;
    }

    private void StartLevel()
    {
        Dictionary<StationTier, List<StationController>> mainPool = new Dictionary<StationTier, List<StationController>>();
        Dictionary<StationTier, List<StationController>> utilityPool = new Dictionary<StationTier, List<StationController>>();

        foreach (var station in stationControllers)
        {
            station.SetOperational();

            if (station.stationType == StationType.Main)
                AddToPool(mainPool, station);
            else if (station.stationType == StationType.Utility)
                AddToPool(utilityPool, station);
        }

        List<StationController> selectedMains = SelectStationsFromPool(mainPool, currentLevelData.mainStationCount);
        List<StationController> selectedUtilities = SelectStationsFromPool(utilityPool, currentLevelData.utilityStationCount);

        ShuffleList(selectedMains);
        ShuffleList(selectedUtilities);

        resultMains = new List<StationController>(selectedMains);
        resultUtilities = new List<StationController>(selectedUtilities);

        OnLevelStarted?.Invoke(selectedMains, selectedUtilities);

        Debug.Log($"Level Başladı. Main: {selectedMains.Count}, Utility: {selectedUtilities.Count}");
    }

    private void ShuffleList<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    private List<StationController> SelectStationsFromPool(Dictionary<StationTier, List<StationController>> pool, int countToSelect)
    {
        List<StationController> selectedList = new List<StationController>();
        float totalWeight = 0f;
        foreach (var config in currentLevelData.stationConfigs) totalWeight += config.TierRatio;

        for (int i = 0; i < countToSelect; i++)
        {
            if (pool.Values.Sum(list => list.Count) == 0) break;

            StationTier targetTier = GetRandomTierBasedOnWeight(totalWeight);
            StationController picked = GetRandomStationFromTier(pool, targetTier);

            if (picked != null) selectedList.Add(picked);
        }
        return selectedList;
    }

    private void AddToPool(Dictionary<StationTier, List<StationController>> pool, StationController station)
    {
        if (!pool.ContainsKey(station.stationTier)) pool[station.stationTier] = new List<StationController>();
        pool[station.stationTier].Add(station);
    }

    private StationTier GetRandomTierBasedOnWeight(float totalWeight)
    {
        float randomValue = Random.Range(0, totalWeight);
        float cursor = 0;
        foreach (var config in currentLevelData.stationConfigs)
        {
            cursor += config.TierRatio;
            if (randomValue <= cursor) return config.stationTier;
        }
        return currentLevelData.stationConfigs[0].stationTier;
    }

    private StationController GetRandomStationFromTier(Dictionary<StationTier, List<StationController>> pool, StationTier preferredTier)
    {
        if (pool.ContainsKey(preferredTier) && pool[preferredTier].Count > 0)
        {
            int randomIndex = Random.Range(0, pool[preferredTier].Count);
            StationController station = pool[preferredTier][randomIndex];
            pool[preferredTier].RemoveAt(randomIndex);
            return station;
        }
        var availableTiers = pool.Keys.Where(k => pool[k].Count > 0).ToList();
        if (availableTiers.Count > 0)
        {
            StationTier fallbackTier = availableTiers[Random.Range(0, availableTiers.Count)];
            int randomIndex = Random.Range(0, pool[fallbackTier].Count);
            StationController station = pool[fallbackTier][randomIndex];
            pool[fallbackTier].RemoveAt(randomIndex);
            return station;
        }
        return null;
    }
}