using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using PurrNet;
using System.Linq;

public class RoE_BoardManager : NetworkBehaviour
{
    [Header("References")]
    public RoE_StationManager stationManager;

    [Header("Visual Settings")]
    [SerializeField] private Transform[] pinLocations;
    public GameObject boardItemPrefab;


    public List<BoardEntry> GenerateNewBoardData(List<RoE_ObjectData> objects, List<DecryptionSymbol> symbols)
    {
        List<BoardEntry> newSetup = new List<BoardEntry>();

        foreach (var obj in objects)
        {
            newSetup.Add(new BoardEntry
            {
                linkedObject = obj,
                assignedSymbols = new List<DecryptionSymbol>()
            });
        }

        List<int> allGroups = symbols.Select(s => s.groupID).Where(g => g > 0).Distinct().ToList();

        List<int> selectedGroups = allGroups.OrderBy(x => Random.Range(0f, 1f)).Take(4).ToList();

        List<DecryptionSymbol> requiredSymbols = symbols.Where(s => selectedGroups.Contains(s.groupID)).ToList();

        requiredSymbols = requiredSymbols.OrderBy(x => Random.Range(0f, 1f)).ToList();

        foreach (var reqSym in requiredSymbols)
        {
            var validObjects = newSetup.Where(entry =>
                entry.assignedSymbols.Count < 3 &&
                !entry.assignedSymbols.Any(s => s.groupID == reqSym.groupID)
            ).OrderBy(x => Random.Range(0f, 1f)).ToList();

            if (validObjects.Count > 0)
            {
                validObjects[0].assignedSymbols.Add(reqSym);
            }

        }

        foreach (var entry in newSetup)
        {
            int safetyCounter = 0;

            while (entry.assignedSymbols.Count < 3 && safetyCounter < 200)
            {
                safetyCounter++;

                DecryptionSymbol randomSym = symbols[Random.Range(0, symbols.Count)];

                if (entry.assignedSymbols.Contains(randomSym)) continue;

                if (randomSym.groupID > 0 && entry.assignedSymbols.Any(s => s.groupID == randomSym.groupID))
                {
                    continue;
                }

                entry.assignedSymbols.Add(randomSym);
            }
        }

        foreach (var entry in newSetup)
        {
            entry.assignedSymbols = entry.assignedSymbols.OrderBy(x => Random.Range(0f, 1f)).ToList();
        }
        BroadcastBoardData(newSetup);
        return newSetup;
    }

    private void BroadcastBoardData(List<BoardEntry> currentSetup)
    {
        if (!isServer) return;

        List<NetworkBoardData> boardPacket = new List<NetworkBoardData>();

        foreach (var entry in currentSetup)
        {
            NetworkBoardData data = new NetworkBoardData();

            data.objectIndex = stationManager.allPossibleObjects.IndexOf(entry.linkedObject);

            List<int> syms = new List<int>();
            foreach (var s in entry.assignedSymbols)
            {
                syms.Add(stationManager.availableSymbols.IndexOf(s));
            }
            data.symbolIndices = syms.ToArray();

            boardPacket.Add(data);
        }

        RpcSpawnBoardVisuals(boardPacket);
    }

    [ObserversRpc]
    private void RpcSpawnBoardVisuals(List<NetworkBoardData> packet)
    {

        foreach (var data in packet)
        {
            RoE_ObjectData realObj = stationManager.allPossibleObjects[data.objectIndex];

            List<DecryptionSymbol> realSymbols = new List<DecryptionSymbol>();
            foreach (int sIndex in data.symbolIndices)
            {
                realSymbols.Add(stationManager.availableSymbols[sIndex]);
            }

            GameObject newItem = Instantiate(boardItemPrefab, pinLocations[packet.IndexOf(data)]);
            newItem.GetComponent<RoE_BoardItem>().Setup(realObj, realSymbols);
        }

    }
}