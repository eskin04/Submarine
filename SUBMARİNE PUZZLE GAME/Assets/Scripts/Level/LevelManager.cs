using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using System.Linq;

public class LevelManager : NetworkBehaviour
{
    public static System.Action<List<StationController>> OnLevelStarted;
    public static System.Action<int> OnCurrentLevelData;

    [Header("Level Data")]
    public LevelData currentLevelData;

    private List<StationController> availableMainStations = new List<StationController>();
    [Header("Debug Info")]

    public List<StationController> resultMains = new List<StationController>();
    private bool ownsEarlySpawnRegistrations;
    private bool ownsFullSpawnRegistrations;

    protected override void OnEarlySpawn()
    {
        base.OnEarlySpawn();
        AcquireEarlySpawnRegistrations();
    }
    protected override void OnSpawned()
    {
        base.OnSpawned();
        if (!AcquireFullSpawnRegistrations()) return;

        OnCurrentLevelData?.Invoke(currentLevelData.levelID);
        if (!ownsFullSpawnRegistrations) return;

        if (InstanceHandler.TryGetInstance(out LevelView levelView)) levelView.SetLevelText(currentLevelData.levelID);
        if (ownsFullSpawnRegistrations) InstanceHandler.RegisterInstance(this);
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        // Early-only lifetimes may not reach the parameterless final hook.
        if (!IsSpawned(!asServer)) ReleaseSpawnRegistrations();
    }

    protected override void OnDespawned()
    {
        base.OnDespawned();
        ReleaseSpawnRegistrations();
    }

    protected override void OnPoolReset()
    {
        base.OnPoolReset();
        ReleaseSpawnRegistrations();
    }

    protected override void OnDestroy()
    {
        try
        {
            base.OnDestroy();
        }
        finally
        {
            ReleaseSpawnRegistrations();
        }
    }

    private void AcquireEarlySpawnRegistrations()
    {
        if (ownsEarlySpawnRegistrations) return;

        GlobalEvents.OnRegisterMainStation += RegisterMainStation;
        ownsEarlySpawnRegistrations = true;
    }

    private bool AcquireFullSpawnRegistrations()
    {
        if (ownsFullSpawnRegistrations) return false;

        MainGameState.startGame += StartLevel;
        ownsFullSpawnRegistrations = true;
        return true;
    }

    private void ReleaseSpawnRegistrations()
    {
        if (ownsEarlySpawnRegistrations)
        {
            ownsEarlySpawnRegistrations = false;
            GlobalEvents.OnRegisterMainStation -= RegisterMainStation;
        }

        if (ownsFullSpawnRegistrations)
        {
            ownsFullSpawnRegistrations = false;
            MainGameState.startGame -= StartLevel;

            // A replacement may have registered before this owner's teardown.
            if (InstanceHandler.TryGetInstance<LevelManager>(out var current) && ReferenceEquals(current, this))
                InstanceHandler.UnregisterInstance<LevelManager>();
        }
    }

    public int GetCurrentLevelID()
    {
        return currentLevelData.levelID;
    }

    private void RegisterMainStation(StationController station)
    {
        if (!availableMainStations.Contains(station))
        {
            availableMainStations.Add(station);
            station.SetOperational();
        }
    }

    private void StartLevel()
    {
        Dictionary<StationTier, List<StationController>> mainPool = new Dictionary<StationTier, List<StationController>>();

        foreach (var station in availableMainStations)
        {
            station.SetOperational();
            AddToPool(mainPool, station);
        }

        if (availableMainStations.Count == 0)
        {
            return;
        }

        List<StationController> selectedMains = SelectStationsFromPool(mainPool, currentLevelData.mainStationCount);

        ShuffleList(selectedMains);

        resultMains = new List<StationController>(selectedMains);

        OnLevelStarted?.Invoke(selectedMains);

        Debug.Log($"Level Başladı. Havuzdaki Main: {availableMainStations.Count}, Seçilen: {selectedMains.Count}");
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
