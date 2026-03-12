using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using System.Linq;

public class SecurityLockdown_StationManager : NetworkBehaviour
{
    public static event System.Action<LockDownStationState> OnStateChanged;
    public static event System.Action OnStationFailed;
    public static event System.Action OnSoftReset;
    public static event System.Action<RegionID> OnRegionSolved;

    [Header("References")]
    public SecurityLockdown_TechnicianUI techUI;
    public SecurityLockdown_EngineerUI engUI;

    [Header("Station State (SyncVars)")]
    [SerializeField] private SyncVar<LockDownStationState> currentState = new SyncVar<LockDownStationState>(LockDownStationState.Idle);
    [SerializeField] private SyncVar<int> currentSequenceIndex = new SyncVar<int>(0);
    [SerializeField] private SyncVar<int> currentOverrideStep = new SyncVar<int>(0);

    [Header("Generated Puzzle Data (Server Only)")]
    public List<LegendData> currentLegend = new List<LegendData>();
    public List<SequenceData> currentSequence = new List<SequenceData>();
    public List<OverrideStepData> overrideSteps = new List<OverrideStepData>();

    [Header("Region Pools")]
    public List<RegionID> technicianSideRegions;
    public List<RegionID> engineerSideRegions;

    public List<CodeVariation> codeVariations;

    public SyncVar<int> currentVariationDigits = new SyncVar<int>(5);
    public SyncVar<int> techViewCount = new SyncVar<int>(0);
    public SyncVar<int> engViewCount = new SyncVar<int>(0);

    [Header("Settings")]
    private int sequenceLength = 0;

    [ContextMenu("1. START STATION (TEST)")]
    public void StartStation()
    {
        if (!isServer) return;

        GenerateMainPuzzle();
        GenerateOverridePuzzle();

        currentState.value = LockDownStationState.Active;
        RpcStateChanged(currentState.value);
        currentSequenceIndex.value = 0;
        currentOverrideStep.value = 0;

        Debug.Log("<color=green>[STATION] Station Started! Check arrays for answers.</color>");

        RpcSyncPuzzleData(currentLegend.ToArray(), currentSequence.ToArray(), overrideSteps.ToArray());
    }

    #region GENERATION LOGIC (SERVER)

    private void GenerateMainPuzzle()
    {
        currentLegend.Clear();
        currentSequence.Clear();

        CodeVariation selectedVar = codeVariations[Random.Range(0, codeVariations.Count)];
        sequenceLength = selectedVar.totalSteps;
        currentVariationDigits.value = selectedVar.digitsPerStep;

        Debug.Log($"<color=yellow>[PUZZLE] Variation Selected: {selectedVar.variationName} | Tech: {selectedVar.techRegionCount}, Eng: {selectedVar.engRegionCount}</color>");

        List<RegionID> availableTech = new List<RegionID>(technicianSideRegions);
        List<RegionID> availableEng = new List<RegionID>(engineerSideRegions);

        ShuffleList(availableTech);
        ShuffleList(availableEng);

        List<RegionID> activeSequenceRegions = new List<RegionID>();
        activeSequenceRegions.AddRange(availableTech.Take(selectedVar.techRegionCount));
        activeSequenceRegions.AddRange(availableEng.Take(selectedVar.engRegionCount));

        ShuffleList(activeSequenceRegions);

        List<LockdownColor> availableColors = System.Enum.GetValues(typeof(LockdownColor)).Cast<LockdownColor>().ToList();
        ShuffleList(availableColors);

        int minNum = selectedVar.digitsPerStep == 1 ? 0 : (int)Mathf.Pow(10, selectedVar.digitsPerStep - 1);
        int maxNum = (int)Mathf.Pow(10, selectedVar.digitsPerStep) - 1;
        List<int> uniqueNumbers = GenerateUniqueNumbers(sequenceLength, minNum, maxNum);

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

    private void GenerateOverridePuzzle()
    {
        overrideSteps.Clear();
        List<int> overrideNumbers = GenerateUniqueNumbers(6, 10, 99);

        int cumulativeTotal = 0;
        int numIndex = 0;

        Debug.Log("--- OVERRIDE PUZZLE GENERATED ---");
        for (int i = 0; i < 3; i++)
        {
            int tNum = overrideNumbers[numIndex++];
            int eNum = overrideNumbers[numIndex++];
            cumulativeTotal += (tNum + eNum);

            overrideSteps.Add(new OverrideStepData { techNumber = tNum, engNumber = eNum, expectedTotal = cumulativeTotal });
            Debug.Log($"Override Step {i}: Tech({tNum}) + Eng({eNum}) | Expected Total: [{cumulativeTotal}]");
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
            RpcSetRegionSolved(region);
            Debug.Log($"<color=cyan>[NUMPAD SUCCESS]</color> Correct! Moving to Numpad Step {currentSequenceIndex.value}/{sequenceLength}");
            CheckWinCondition();
        }
        else
        {
            Debug.LogWarning($"<color=red>[NUMPAD FAILED]</color> Wrong Entry! Expected: [{expectedRegion}] with code [{expectedData.targetNumber}]. HARD RESET TRIGGERED!");
            TriggerHardReset();
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void SubmitOverrideEntryRPC(int enteredTotal)
    {
        if (currentState.value != LockDownStationState.Active) return;

        int expected = overrideSteps[currentOverrideStep.value].expectedTotal;

        Debug.Log($"[OVERRIDE INPUT] Checking Total: {enteredTotal}");

        if (enteredTotal == expected)
        {
            currentOverrideStep.value++;
            Debug.Log($"<color=cyan>[OVERRIDE SUCCESS]</color> Correct! Moving to Override Step {currentOverrideStep.value}/3");
            CheckWinCondition();
        }
        else
        {
            Debug.LogWarning($"<color=orange>[OVERRIDE FAILED]</color> Wrong Total! Expected: [{expected}]. OVERRIDE RESET TRIGGERED!");
            GenerateOverridePuzzle();
            currentOverrideStep.value = 0;
            RpcSyncOverrideDataOnly(overrideSteps.ToArray());
            RpcTriggerError();
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void RequestTechnicianCodeRPC()
    {
        if (currentState.value != LockDownStationState.Active) return;

        techViewCount.value++;

        if (techViewCount.value == 1)
        {
            RpcShowTechnicianCode(5f);
            RpcEnableEngineerButton();
        }
        else if (techViewCount.value == 2)
        {
            currentSequenceIndex.value = 0;
            RpcShowTechnicianCode(5f);
            RpcTriggerSoftReset();
        }
        else if (techViewCount.value >= 3)
        {
            TriggerHardReset();
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void RequestEngineerLegendRPC()
    {
        if (currentState.value != LockDownStationState.Active) return;
        if (engViewCount.value >= 2) return;

        engViewCount.value++;

        if (engViewCount.value == 1)
        {
            RpcShowEngineerLegend(5f);
        }
        else if (engViewCount.value == 2)
        {
            currentSequenceIndex.value = 0;
            RpcShowEngineerLegend(7f);
            RpcTriggerSoftReset();
        }
    }

    private void TriggerHardReset()
    {
        GenerateMainPuzzle();
        currentSequenceIndex.value = 0;
        techViewCount.value = 0;
        engViewCount.value = 0;

        RpcSyncPuzzleData(currentLegend.ToArray(), currentSequence.ToArray(), overrideSteps.ToArray());
        RpcTriggerError();
    }

    private void CheckWinCondition()
    {
        if (currentSequenceIndex.value >= sequenceLength && currentOverrideStep.value >= 3)
        {
            currentState.value = LockDownStationState.Solved;
            Debug.Log("<color=green>*** STATION FULLY SOLVED! DOORS UNLOCKED! ***</color>");
            RpcStateChanged(currentState.value);
        }
    }






    #endregion

    #region RPCS (CLIENT SYNC)

    [ObserversRpc]
    private void RpcSyncPuzzleData(LegendData[] legend, SequenceData[] seq, OverrideStepData[] overrides)
    {
        if (techUI != null) techUI.UpdatePuzzleData(seq);
        if (engUI != null) engUI.UpdateLegendData(legend);
    }

    [ObserversRpc]
    private void RpcSyncOverrideDataOnly(OverrideStepData[] overrides) { }

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
    private void RpcTriggerSoftReset()
    {
        OnSoftReset?.Invoke();
    }

    [ObserversRpc]
    private void RpcEnableEngineerButton()
    {
        if (engUI != null) engUI.EnableShowLegendButton();
    }

    [ObserversRpc]
    private void RpcShowTechnicianCode(float duration)
    {
        if (techUI != null) techUI.ShowCode(duration);
    }

    [ObserversRpc]
    private void RpcShowEngineerLegend(float duration)
    {
        if (engUI != null) engUI.ShowLegend(duration);
    }
    [ObserversRpc]
    private void RpcSetRegionSolved(RegionID region)
    {
        OnRegionSolved?.Invoke(region);
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

    [ContextMenu("3. TEST: Submit Correct Override (Current Step)")]
    private void DebugSubmitCorrectOverride()
    {
        if (currentOverrideStep.value < 3)
        {
            SubmitOverrideEntryRPC(overrideSteps[currentOverrideStep.value].expectedTotal);
        }
    }

    [ContextMenu("4. TEST: Submit WRONG Numpad (Hard Reset)")]
    private void DebugSubmitWrongNumpad()
    {
        SubmitNumpadEntryRPC(RegionID.T8, 11111);
    }
    #endregion
}