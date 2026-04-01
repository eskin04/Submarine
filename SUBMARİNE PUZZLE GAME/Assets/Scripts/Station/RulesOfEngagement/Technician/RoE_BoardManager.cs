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
            newSetup.Add(new BoardEntry { linkedObject = obj, assignedSymbols = new List<DecryptionSymbol>() });
        }

        List<int> allGroups = symbols.Select(s => s.groupID).Where(g => g > 0).Distinct().ToList();
        List<int> selectedGroups = allGroups.OrderBy(x => Random.value).Take(4).ToList();

        List<DecryptionSymbol> requiredSymbols = symbols.Where(s => selectedGroups.Contains(s.groupID)).ToList();

        // --- 1. AĞAÇ/KÖK (PREFIX) OLUŞTURMA ---
        List<DecryptionSymbol> prefixSymbols = new List<DecryptionSymbol>();
        foreach (int gId in selectedGroups)
        {
            var sym = requiredSymbols.First(s => s.groupID == gId);
            prefixSymbols.Add(sym);
        }

        int chunkSize = Mathf.CeilToInt((float)newSetup.Count / prefixSymbols.Count);
        for (int i = 0; i < newSetup.Count; i++)
        {
            int prefixIndex = Mathf.Min(i / chunkSize, prefixSymbols.Count - 1);
            newSetup[i].assignedSymbols.Add(prefixSymbols[prefixIndex]);
        }

        // --- 2. DARALTILMIŞ HAVUZ (FILLER POOL) ---
        List<DecryptionSymbol> fillerPool = new List<DecryptionSymbol>(requiredSymbols);
        List<int> unselectedGroups = allGroups.Except(selectedGroups).ToList();
        foreach (int unselectedId in unselectedGroups)
        {
            var randomUnselectedSym = symbols.Where(s => s.groupID == unselectedId).OrderBy(x => Random.value).First();
            fillerPool.Add(randomUnselectedSym);
        }

        List<DecryptionSymbol> remainingRequired = requiredSymbols.Except(prefixSymbols).ToList();

        // --- 3. DESEN ŞIRINGALAMA (PATTERN INJECTION) ---

        // 1. Grup (Index 0 ve 1) için ortak 2. sembolü seç ve zorla ekle
        DecryptionSymbol patternSym1 = fillerPool.OrderBy(x => Random.value)
            .FirstOrDefault(s => s.groupID == 0 || s.groupID != prefixSymbols[0].groupID);

        if (patternSym1.icon != null)
        {
            newSetup[0].assignedSymbols.Add(patternSym1);
            newSetup[1].assignedSymbols.Add(patternSym1);
            // Eğer bu zorunlu bir sembolse, "kullanıldı" olarak işaretlemek için listeden düşüyoruz
            remainingRequired.RemoveAll(s => s.icon == patternSym1.icon);
        }

        // 2. Grup (Index 4 ve 5) için ortak 2. sembolü seç ve zorla ekle
        DecryptionSymbol patternSym2 = fillerPool.OrderBy(x => Random.value)
            .FirstOrDefault(s => s.groupID == 0 || s.groupID != prefixSymbols[1].groupID);

        if (patternSym2.icon != null)
        {
            newSetup[4].assignedSymbols.Add(patternSym2);
            newSetup[5].assignedSymbols.Add(patternSym2);
            remainingRequired.RemoveAll(s => s.icon == patternSym2.icon);
        }

        // --- 4. ZORUNLU SEMBOLLERİ DAĞIT ---
        remainingRequired = remainingRequired.OrderBy(x => Random.value).ToList();
        foreach (var reqSym in remainingRequired)
        {
            var validObjects = newSetup.Where(entry =>
                entry.assignedSymbols.Count < 3 &&
                !entry.assignedSymbols.Any(s => s.groupID == reqSym.groupID)
            ).OrderBy(x => Random.value).ToList();

            if (validObjects.Count > 0)
            {
                validObjects[0].assignedSymbols.Add(reqSym);
            }
        }

        // --- 5. HAVUZDAN BOŞLUKLARI DOLDUR ---
        foreach (var entry in newSetup)
        {
            int safetyCounter = 0;
            while (entry.assignedSymbols.Count < 3 && safetyCounter < 200)
            {
                safetyCounter++;
                DecryptionSymbol randomSym = fillerPool[Random.Range(0, fillerPool.Count)];

                if (entry.assignedSymbols.Contains(randomSym)) continue;

                if (randomSym.groupID > 0 && entry.assignedSymbols.Any(s => s.groupID == randomSym.groupID))
                    continue;

                entry.assignedSymbols.Add(randomSym);
            }
        }

        // --- 6. KARIŞTIRMA VE GİZLEME (SHUFFLE) ---
        // Hem cisimleri yer değiştiriyoruz (birebir aynı sembole sahip olanlar yan yana durmasın diye)
        // Hem de 2. ve 3. sembolleri kendi içinde yer değiştiriyoruz.
        var shuffledSetup = newSetup.OrderBy(x => Random.value).ToList();

        foreach (var entry in shuffledSetup)
        {
            if (entry.assignedSymbols.Count == 3)
            {
                var firstSym = entry.assignedSymbols[0];
                var rest = entry.assignedSymbols.Skip(1).OrderBy(x => Random.value).ToList();

                entry.assignedSymbols.Clear();
                entry.assignedSymbols.Add(firstSym);
                entry.assignedSymbols.AddRange(rest);
            }
        }

        return shuffledSetup;
    }

    public void BroadcastBoardData(List<BoardEntry> currentSetup)
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
        foreach (Transform pin in pinLocations)
        {
            foreach (Transform child in pin)
            {
                Destroy(child.gameObject);
            }
        }

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