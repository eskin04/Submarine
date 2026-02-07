using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using PurrNet;

public class RoE_BoardManager : NetworkBehaviour
{
    [Header("References")]
    public RoE_StationManager stationManager;

    [Header("Visual Settings")]
    public Transform boardGridParent;
    public GameObject boardItemPrefab;


    public List<BoardEntry> GenerateNewBoardData(List<RoE_ObjectData> objects, List<DecryptionSymbol> symbols)
    {
        List<BoardEntry> newSetup = new List<BoardEntry>();
        HashSet<string> usedCombinations = new HashSet<string>();

        foreach (var obj in objects)
        {
            BoardEntry entry = new BoardEntry();
            entry.linkedObject = obj;
            entry.assignedSymbols = new List<DecryptionSymbol>();

            bool uniqueFound = false;
            int safetyCounter = 0;

            while (!uniqueFound && safetyCounter < 100)
            {
                List<DecryptionSymbol> tempSymbols = new List<DecryptionSymbol>();
                string signature = "";

                for (int i = 0; i < 3; i++)
                {
                    DecryptionSymbol randomSym = symbols[Random.Range(0, symbols.Count)];
                    tempSymbols.Add(randomSym);
                    signature += randomSym.shape.ToString() + randomSym.color.ToString() + "-";
                }

                if (!usedCombinations.Contains(signature))
                {
                    usedCombinations.Add(signature);
                    entry.assignedSymbols = tempSymbols;
                    uniqueFound = true;
                }
                else safetyCounter++;
            }


            newSetup.Add(entry);
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
        foreach (Transform child in boardGridParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var data in packet)
        {
            RoE_ObjectData realObj = stationManager.allPossibleObjects[data.objectIndex];

            List<DecryptionSymbol> realSymbols = new List<DecryptionSymbol>();
            foreach (int sIndex in data.symbolIndices)
            {
                realSymbols.Add(stationManager.availableSymbols[sIndex]);
            }

            GameObject newItem = Instantiate(boardItemPrefab, boardGridParent);
            newItem.GetComponent<RoE_BoardItem>().Setup(realObj, realSymbols);
        }

    }
}