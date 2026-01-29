using UnityEngine;
using System.Collections.Generic;
using PurrNet;
using System.Linq;

public class RoE_ThreatManager : NetworkBehaviour
{
    [Header("Configuration")]
    public List<RoE_SpawnProfile> spawnProfiles;

    [Header("State")]
    public List<ActiveThreat> activeThreats = new List<ActiveThreat>();

    public RoE_StationManager mainManager;

    private void Update()
    {
        if (!mainManager.GetSimulateRunning()) return;

        for (int i = 0; i < activeThreats.Count; i++)
        {
            var threat = activeThreats[i];
            if (!threat.isDestroyed)
            {
                threat.currentDistance -= threat.approachSpeed * Time.deltaTime;

                // Çarpışma Kontrolü
                if (isServer)
                {
                    if (threat.currentDistance <= 0)
                    {
                    }
                }
            }
        }
    }

    public void SpawnNewThreats(int level, List<BoardEntry> incomingBoardSetup)
    {
        activeThreats.Clear();

        List<BoardEntry> availableIdentities = new List<BoardEntry>(incomingBoardSetup);

        int maxThreatCount = (level <= 4) ? level + 1 : 4;
        int threatCount = Random.Range(maxThreatCount - 1, maxThreatCount + 1);

        List<ThreatCodeName> availableNames = new List<ThreatCodeName>()
    {
        ThreatCodeName.Alpha, ThreatCodeName.Beta, ThreatCodeName.Charlie, ThreatCodeName.Delta
    };

        for (int i = 0; i < threatCount; i++)
        {
            if (availableNames.Count == 0 || availableIdentities.Count == 0) break;

            ActiveThreat threat = new ActiveThreat();

            ThreatCodeName selectedCode = availableNames[0];
            availableNames.RemoveAt(0);

            threat.codeEnum = selectedCode;
            threat.displayName = selectedCode.ToDisplayString();

            SetThreatStatsFromProfile(threat);

            int randomIndex = Random.Range(0, availableIdentities.Count);
            threat.realIdentity = availableIdentities[randomIndex];

            availableIdentities.RemoveAt(randomIndex);

            activeThreats.Add(threat);
        }
    }

    private void SetThreatStatsFromProfile(ActiveThreat threat)
    {
        if (spawnProfiles == null || spawnProfiles.Count == 0)
        {
            Debug.LogWarning("Spawn profili atanmamış! Varsayılan değerler kullanılıyor.");
            threat.currentDistance = 800f;
            threat.approachSpeed = 10f;
            return;
        }

        float totalWeight = spawnProfiles.Sum(p => p.spawnChance);
        float randomPoint = Random.Range(0, totalWeight);
        float currentSum = 0;

        foreach (var profile in spawnProfiles)
        {
            currentSum += profile.spawnChance;

            if (randomPoint <= currentSum)
            {
                threat.currentDistance = Random.Range(profile.minDistance, profile.maxDistance);
                threat.approachSpeed = Random.Range(profile.minSpeed, profile.maxSpeed);
                return;
            }
        }
    }
    public ActiveThreat GetThreat(int index)
    {
        if (index < 0 || index >= activeThreats.Count) return null;
        return activeThreats[index];
    }

    public ActiveThreat GetThreat(string codeName)
    {
        return activeThreats.Find(t => t.displayName == codeName);
    }




    public void SyncThreatsFromNetwork(List<NetworkThreatData> dataPackage, List<RoE_ObjectData> objectDatabase)
    {
        activeThreats.Clear();

        foreach (var data in dataPackage)
        {
            ActiveThreat newThreat = new ActiveThreat();

            newThreat.codeEnum = (ThreatCodeName)data.codeEnumIndex;
            newThreat.displayName = newThreat.codeEnum.ToDisplayString();
            newThreat.currentDistance = data.startDistance;
            newThreat.approachSpeed = data.speed;

            if (data.realObjectIndex >= 0 && data.realObjectIndex < objectDatabase.Count)
            {
                BoardEntry entry = new BoardEntry();
                entry.linkedObject = objectDatabase[data.realObjectIndex];

                entry.assignedSymbols = new List<DecryptionSymbol>();

                newThreat.realIdentity = entry;
            }

            activeThreats.Add(newThreat);
        }

        Debug.Log($"[CLIENT] Tehdit listesi güncellendi. {activeThreats.Count} adet.");
    }

}