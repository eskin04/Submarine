using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using System.Linq;

public class LevelManager : NetworkBehaviour
{
    public static System.Action<List<StationController>> OnLevelStarted;

    [Header("Level Data")]
    public LevelData currentLevelData;

    [Header("References")]
    public List<StationController> stationControllers;

    public List<StationController> brokenStationsResult = new List<StationController>();

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
        Dictionary<StationTier, List<StationController>> stationsByTier = new Dictionary<StationTier, List<StationController>>();

        foreach (var station in stationControllers)
        {
            if (station.stationType == StationType.Main)
            {
                station.SetOperational();
                if (!stationsByTier.ContainsKey(station.stationTier))
                {
                    stationsByTier[station.stationTier] = new List<StationController>();
                }
                stationsByTier[station.stationTier].Add(station);
            }
        }

        float totalWeight = 0f;
        foreach (var config in currentLevelData.stationConfigs)
        {
            totalWeight += config.TierRatio;
        }

        int targetBrokenCount = currentLevelData.mainStationCount;
        List<StationController> selectedStations = new List<StationController>();

        for (int i = 0; i < targetBrokenCount; i++)
        {
            if (stationsByTier.Values.Sum(list => list.Count) == 0)
            {
                Debug.LogWarning("Bozulacak yeterli Main istasyon kalmadı!");
                break;
            }

            StationTier targetTier = GetRandomTierBasedOnWeight(totalWeight);

            StationController pickedStation = GetRandomStationFromTier(stationsByTier, targetTier);

            if (pickedStation != null)
            {
                selectedStations.Add(pickedStation);
            }
        }

        System.Random rng = new System.Random();
        int n = selectedStations.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (selectedStations[k], selectedStations[n]) = (selectedStations[n], selectedStations[k]);
        }

        brokenStationsResult = new List<StationController>(selectedStations);
        OnLevelStarted?.Invoke(selectedStations);

        Debug.Log($"Level Başladı. Seçilen İstasyon Sayısı: {selectedStations.Count}");
    }

    private StationTier GetRandomTierBasedOnWeight(float totalWeight)
    {
        float randomValue = Random.Range(0, totalWeight);
        float cursor = 0;

        foreach (var config in currentLevelData.stationConfigs)
        {
            cursor += config.TierRatio;
            if (randomValue <= cursor)
            {
                return config.stationTier;
            }
        }

        return currentLevelData.stationConfigs[0].stationTier;
    }

    private StationController GetRandomStationFromTier(Dictionary<StationTier, List<StationController>> stationsByTier, StationTier preferredTier)
    {
        if (stationsByTier.ContainsKey(preferredTier) && stationsByTier[preferredTier].Count > 0)
        {
            int randomIndex = Random.Range(0, stationsByTier[preferredTier].Count);
            StationController station = stationsByTier[preferredTier][randomIndex];

            stationsByTier[preferredTier].RemoveAt(randomIndex);
            return station;
        }

        var availableTiers = stationsByTier.Keys.Where(k => stationsByTier[k].Count > 0).ToList();

        if (availableTiers.Count > 0)
        {
            StationTier fallbackTier = availableTiers[Random.Range(0, availableTiers.Count)];

            int randomIndex = Random.Range(0, stationsByTier[fallbackTier].Count);
            StationController station = stationsByTier[fallbackTier][randomIndex];

            stationsByTier[fallbackTier].RemoveAt(randomIndex);
            return station;
        }

        return null;
    }
}