using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using System.Linq;

public class SecurityLockdown_StationManager : NetworkBehaviour
{
    public static event System.Action<LockDownStationState> OnStateChanged;
    public static event System.Action OnStationFailed;
    public static event System.Action<RegionID> OnRegionSolved;
    public static event System.Action OnPuzzleDataSynced;

    [Header("References")]
    public StationController stationController;
    public SecurityLockdown_TechnicianUI techUI;
    public SecurityLockdown_EngineerUI engUI;

    [Header("Station State (SyncVars)")]
    [SerializeField] private SyncVar<LockDownStationState> currentState = new SyncVar<LockDownStationState>(LockDownStationState.Idle);
    [SerializeField] public SyncVar<int> currentSequenceIndex = new SyncVar<int>(0);
    [SerializeField] public SyncVar<int> currentVariationDigits = new SyncVar<int>(5);

    [SerializeField] public SyncVar<bool> isTechReady = new SyncVar<bool>(false);
    [SerializeField] public SyncVar<bool> isEngReady = new SyncVar<bool>(false);

    public bool AreNumpadsActive => currentState.value == LockDownStationState.Active &&
                                    isTechReady.value &&
                                    isEngReady.value;

    [Header("Generated Puzzle Data (Server Only)")]
    public List<LegendData> currentLegend = new List<LegendData>();
    public List<SequenceData> currentSequence = new List<SequenceData>();

    [Header("Region Pools")]
    public List<RegionID> technicianSideRegions;
    public List<RegionID> engineerSideRegions;

    public List<CodeVariation> codeVariations;

    [Header("Settings")]
    public int sequenceLength = 4;

    [Header("Audio Settings")]
    public AudioEventChannelSO _channel;
    public FMODUnity.EventReference lockDownStartSound;

    [ContextMenu("1. START MAIN LOCKDOWN (TEST)")]
    public void StartStation()
    {
        if (!isServer) return;

        GenerateMainPuzzle();

        currentState.value = LockDownStationState.Active;
        currentSequenceIndex.value = 0;

        isTechReady.value = false;
        isEngReady.value = false;

        Debug.Log("<color=green>[STATION] Main Lockdown Started!</color>");

        RpcSyncPuzzleData(currentLegend.ToArray(), currentSequence.ToArray());
        RpcStateChanged(currentState.value);
        RpcStartLockdownSound();
    }

    [ObserversRpc(runLocally: true)]
    private void RpcStartLockdownSound()
    {
        if (_channel != null && !lockDownStartSound.IsNull)
        {
            AudioEventPayload payload = new AudioEventPayload(lockDownStartSound, this.transform.position);
            _channel.RaiseEvent(payload);
        }
    }

    #region GENERATION LOGIC (SERVER)

    private void GenerateMainPuzzle()
    {
        currentLegend.Clear();
        currentSequence.Clear();

        if (codeVariations == null || codeVariations.Count == 0) return;

        CodeVariation selectedVar = codeVariations[Random.Range(0, codeVariations.Count)];

        sequenceLength = 0;
        int maxDigits = 1;
        foreach (var config in selectedVar.digitConfigs)
        {
            sequenceLength += config.count;
            if (config.digits > maxDigits) maxDigits = config.digits;
        }

        currentVariationDigits.value = maxDigits;

        Debug.Log($"<color=yellow>[PUZZLE] Variation: {selectedVar.variationName} | Total Steps: {sequenceLength} | Tech: {selectedVar.techRegionCount}, Eng: {selectedVar.engRegionCount}</color>");

        List<RegionID> availableTech = new List<RegionID>(technicianSideRegions);
        List<RegionID> availableEng = new List<RegionID>(engineerSideRegions);

        if (availableTech.Count < selectedVar.techRegionCount)
        {
            Debug.LogError($"[STATION ERROR] Havuzda yeterli Teknisyen bölgesi yok! İstenen: {selectedVar.techRegionCount}, Bulunan: {availableTech.Count}");
            return;
        }
        if (availableEng.Count < selectedVar.engRegionCount)
        {
            Debug.LogError($"[STATION ERROR] Havuzda yeterli Mühendis bölgesi yok! İstenen: {selectedVar.engRegionCount}, Bulunan: {availableEng.Count}");
            return;
        }
        if (selectedVar.techRegionCount + selectedVar.engRegionCount != sequenceLength)
        {
            Debug.LogError($"[STATION ERROR] Varyasyon matematiği hatalı! Tech + Eng toplamı ({selectedVar.techRegionCount + selectedVar.engRegionCount}), üretilecek toplam sayı adedine ({sequenceLength}) eşit olmalı.");
            return;
        }

        ShuffleList(availableTech);
        ShuffleList(availableEng);

        List<RegionID> activeSequenceRegions = new List<RegionID>();
        activeSequenceRegions.AddRange(availableTech.Take(selectedVar.techRegionCount));
        activeSequenceRegions.AddRange(availableEng.Take(selectedVar.engRegionCount));

        ShuffleList(activeSequenceRegions);

        List<LockdownColor> availableColors = System.Enum.GetValues(typeof(LockdownColor)).Cast<LockdownColor>().ToList();
        ShuffleList(availableColors);

        List<int> uniqueNumbers = new List<int>();
        foreach (var config in selectedVar.digitConfigs)
        {
            int minNum = config.digits == 1 ? 0 : (int)Mathf.Pow(10, config.digits - 1);
            int maxNum = (int)Mathf.Pow(10, config.digits) - 1;
            uniqueNumbers.AddRange(GenerateUniqueNumbers(config.count, minNum, maxNum));
        }

        ShuffleList(uniqueNumbers);

        for (int i = 0; i < sequenceLength; i++)
        {
            LockdownColor assignedColor = availableColors[i];
            currentLegend.Add(new LegendData { color = assignedColor, assignedRegion = activeSequenceRegions[i] });
            currentSequence.Add(new SequenceData { color = assignedColor, targetNumber = uniqueNumbers[i] });
        }

        List<RegionID> remainingRegions = System.Enum.GetValues(typeof(RegionID)).Cast<RegionID>().ToList();
        remainingRegions.RemoveAll(r => activeSequenceRegions.Contains(r));
        ShuffleList(remainingRegions);

        for (int i = sequenceLength; i < availableColors.Count; i++)
        {
            if (i - sequenceLength < remainingRegions.Count)
            {
                currentLegend.Add(new LegendData { color = availableColors[i], assignedRegion = remainingRegions[i - sequenceLength] });
            }
        }

        ShuffleList(currentLegend);

        Debug.Log("--- MAIN NUMPAD PUZZLE GENERATED ---");
        for (int i = 0; i < currentSequence.Count; i++)
        {
            RegionID expectedRegion = currentLegend.First(l => l.color == currentSequence[i].color).assignedRegion;
            Debug.Log($"Step {i}: Go to [{expectedRegion}] and enter [{currentSequence[i].targetNumber}] (Color: {currentSequence[i].color})");
        }
    }

    #endregion

    #region VALIDATION & GAMEPLAY

    [ServerRpc(requireOwnership: false)]
    public void SubmitNumpadEntryRPC(RegionID region, int enteredNumber)
    {
        if (currentState.value != LockDownStationState.Active) return;

        SequenceData expectedData = currentSequence[currentSequenceIndex.value];
        RegionID expectedRegion = currentLegend.First(l => l.color == expectedData.color).assignedRegion;

        Debug.Log($"[NUMPAD INPUT] Checking Region: {region} | Number: {enteredNumber}");

        if (region == expectedRegion && enteredNumber == expectedData.targetNumber)
        {
            currentSequenceIndex.value++;
            Debug.Log($"<color=cyan>[NUMPAD SUCCESS]</color> Correct! Moving to Numpad Step {currentSequenceIndex.value}/{sequenceLength}");
            RpcRegionSolved(region);
            CheckWinCondition();
        }
        else
        {
            Debug.LogWarning($"<color=red>[NUMPAD FAILED]</color> Wrong Entry! Expected: [{expectedRegion}] with code [{expectedData.targetNumber}]. HARD RESET TRIGGERED!");
            TriggerHardReset();
        }
    }



    private void CheckWinCondition()
    {
        if (currentSequenceIndex.value >= sequenceLength)
        {
            currentState.value = LockDownStationState.Solved;
            Debug.Log("<color=green>*** MAIN STATION FULLY SOLVED! ***</color>");
            if (stationController != null) stationController.SetReparied();
            RpcStateChanged(currentState.value);
        }
    }

    private void TriggerHardReset()
    {
        GenerateMainPuzzle();
        currentSequenceIndex.value = 0;

        isTechReady.value = false;
        isEngReady.value = false;

        RpcSyncPuzzleData(currentLegend.ToArray(), currentSequence.ToArray());
        RpcTriggerError();
    }

    [ServerRpc(requireOwnership: false)]
    public void SetTechReadyRPC()
    {
        if (currentState.value == LockDownStationState.Active)
            isTechReady.value = true;
    }

    [ServerRpc(requireOwnership: false)]
    public void SetEngReadyRPC()
    {
        if (currentState.value == LockDownStationState.Active)
            isEngReady.value = true;
    }

    [ServerRpc(requireOwnership: false)]
    public void RequestHardResetRPC()
    {
        if (currentState.value == LockDownStationState.Active)
            TriggerHardReset();
    }

    #endregion
    #region RPCS (CLIENT SYNC)

    [ObserversRpc]
    private void RpcSyncPuzzleData(LegendData[] legend, SequenceData[] seq)
    {
        if (!isServer)
        {
            currentLegend = legend.ToList();
            currentSequence = seq.ToList();
        }

        if (techUI != null) techUI.UpdatePuzzleData(seq);
        if (engUI != null) engUI.UpdateLegendData(legend);

        OnPuzzleDataSynced?.Invoke();
    }

    [ObserversRpc]
    private void RpcStateChanged(LockDownStationState newState)
    {
        OnStateChanged?.Invoke(newState);
    }

    [ObserversRpc]
    private void RpcTriggerError()
    {
        OnStationFailed?.Invoke();
        if (techUI != null) techUI.SetStateLocked();
        if (engUI != null) engUI.SetStateLocked();
    }

    [ObserversRpc]
    private void RpcRegionSolved(RegionID solvedRegion)
    {
        OnRegionSolved?.Invoke(solvedRegion);
    }



    #endregion

    #region UTILS & DEBUG TOOLS

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private List<int> GenerateUniqueNumbers(int count, int min, int max)
    {
        HashSet<int> numbers = new HashSet<int>();
        while (numbers.Count < count) { numbers.Add(Random.Range(min, max + 1)); }
        return numbers.ToList();
    }

    [ContextMenu("2. TEST: Submit Correct Numpad (Current Step)")]
    private void DebugSubmitCorrectNumpad()
    {
        if (currentSequenceIndex.value < sequenceLength)
        {
            SequenceData data = currentSequence[currentSequenceIndex.value];
            RegionID reg = currentLegend.First(l => l.color == data.color).assignedRegion;
            SubmitNumpadEntryRPC(reg, data.targetNumber);
        }
    }

    [ContextMenu("3. TEST: Submit WRONG Numpad (Hard Reset)")]
    private void DebugSubmitWrongNumpad()
    {
        SubmitNumpadEntryRPC(RegionID.M8, 11111);
    }

    #endregion
}
