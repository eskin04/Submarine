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
        // Her seçili gruptan 1 adet "Ana Sembol" (Prefix) seçiyoruz.
        List<DecryptionSymbol> prefixSymbols = new List<DecryptionSymbol>();
        foreach (int gId in selectedGroups)
        {
            var sym = requiredSymbols.First(s => s.groupID == gId);
            prefixSymbols.Add(sym);
        }

        // Bu 4 Ana Sembolü, cisimlere 1. SIRAYA (Index 0) zorunlu olarak paylaştırıyoruz.
        // Böylece her 4 cisim KESİNLİKLE aynı ilk sembolle başlayacak!
        int chunkSize = Mathf.CeilToInt((float)newSetup.Count / prefixSymbols.Count);
        for (int i = 0; i < newSetup.Count; i++)
        {
            int prefixIndex = Mathf.Min(i / chunkSize, prefixSymbols.Count - 1);
            newSetup[i].assignedSymbols.Add(prefixSymbols[prefixIndex]);
        }

        // --- 2. ZORUNLU SEMBOLLERİ DAĞIT ---
        // Ana semboller zaten atandı, geriye kalan zorunlu sembolleri dağıtıyoruz.
        var remainingRequired = requiredSymbols.Except(prefixSymbols).OrderBy(x => Random.value).ToList();

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

        // --- 3. DARALTILMIŞ HAVUZ (FILLER) ---
        // Eskiden tüm 40 sembolü kullanıyorduk, bu da tekrarları (zorluğu) azaltıyordu.
        // Artık sadece zorunlu semboller + seçilmeyen gruplardan 1'er tane alıp havuzu daraltıyoruz.
        List<DecryptionSymbol> fillerPool = new List<DecryptionSymbol>(requiredSymbols);

        List<int> unselectedGroups = allGroups.Except(selectedGroups).ToList();
        foreach (int unselectedId in unselectedGroups)
        {
            var randomUnselectedSym = symbols.Where(s => s.groupID == unselectedId).OrderBy(x => Random.value).First();
            fillerPool.Add(randomUnselectedSym);
        }

        // Kalan boşlukları (2. ve 3. slotlar) bu dar havuzdan doldur
        foreach (var entry in newSetup)
        {
            int safetyCounter = 0;
            while (entry.assignedSymbols.Count < 3 && safetyCounter < 200)
            {
                safetyCounter++;
                DecryptionSymbol randomSym = fillerPool[Random.Range(0, fillerPool.Count)];

                if (entry.assignedSymbols.Contains(randomSym)) continue;

                // GLOBAL KURAL: Aynı gruptan 2 sembol olamaz
                if (randomSym.groupID > 0 && entry.assignedSymbols.Any(s => s.groupID == randomSym.groupID))
                    continue;

                entry.assignedSymbols.Add(randomSym);
            }
        }

        // --- 4. KARIŞTIRMA VE GİZLEME (SHUFFLE) ---
        // İlk sembol (Kök) SABİT kalmalı. Sadece 2. ve 3. sembolleri kendi aralarında karıştırıyoruz.
        // Dağıtım yaparken cisimleri de kendi aralarında karıştırıyoruz ki, aynı ilk sembole sahip olanlar art arda dizilip yapay durmasın.
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