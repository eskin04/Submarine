using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using System.Linq;

public class EngineerLockDown_StationManager : NetworkBehaviour
{
    // --- EVENTS ---
    public static event System.Action<EngineerLockDownStationState> OnOverrideStateChanged;
    public static event System.Action<int> OnOverrideStepCompleted;
    public static event System.Action OnOverrideFailed;
    public static event System.Action OnOverrideSolved;
    public static event System.Action OnOverrideDataSynced;
    public static event System.Action OnEngineerDoorRequested;
    public StationController stationController;

    [Header("Override State (SyncVars)")]
    [SerializeField] private SyncVar<EngineerLockDownStationState> overrideState = new SyncVar<EngineerLockDownStationState>(EngineerLockDownStationState.Idle);
    [SerializeField] public SyncVar<int> currentOverrideStep = new SyncVar<int>(0);

    [Header("Generated Data")]
    public List<EngineerLockDownStepData> overrideSteps = new List<EngineerLockDownStepData>();

    [ContextMenu("1. START OVERRIDE EVENT (TEST)")]
    public void StartOverrideEvent()
    {
        if (!isServer) return;

        GenerateOverridePuzzle();

        overrideState.value = EngineerLockDownStationState.Active;
        currentOverrideStep.value = 0;

        Debug.Log("<color=magenta>[OVERRIDE] Event Started! Check numbers.</color>");

        RpcSyncOverrideData(overrideSteps.ToArray());
        RpcOverrideStateChanged(overrideState.value);
    }

    private void GenerateOverridePuzzle()
    {
        overrideSteps.Clear();
        List<int> overrideNumbers = GenerateUniqueNumbers(6, 10, 99);
        int stepTotal = 0;
        int numIndex = 0;

        for (int i = 0; i < 3; i++)
        {
            int tNum = overrideNumbers[numIndex++];
            int eNum = overrideNumbers[numIndex++];
            stepTotal = tNum + eNum;

            overrideSteps.Add(new EngineerLockDownStepData { techNumber = tNum, engNumber = eNum, expectedTotal = stepTotal });
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void SubmitOverrideEntryRPC(int enteredTotal)
    {
        if (overrideState.value != EngineerLockDownStationState.Active) return;

        if (currentOverrideStep.value >= 3 || currentOverrideStep.value >= overrideSteps.Count) return;

        int expected = overrideSteps[currentOverrideStep.value].expectedTotal;

        if (enteredTotal == expected)
        {
            RpcOverrideStepCompleted(currentOverrideStep.value);
            currentOverrideStep.value++;

            Debug.Log($"<color=cyan>[OVERRIDE SUCCESS]</color> Correct! Moving to Step {currentOverrideStep.value}/3");

            if (currentOverrideStep.value >= 3)
            {
                overrideState.value = EngineerLockDownStationState.Solved;
                if (stationController != null) stationController.SetReparied();
                RpcOverrideStateChanged(overrideState.value);
                RpcOverrideSolved();
                RequestEngineerDoorOpenRPC();
            }
        }
        else
        {
            Debug.LogWarning($"<color=orange>[OVERRIDE FAILED]</color> Wrong Total! Expected: [{expected}]. OVERRIDE RESET TRIGGERED!");
            GenerateOverridePuzzle();
            currentOverrideStep.value = 0;

            RpcSyncOverrideData(overrideSteps.ToArray());
            RpcTriggerOverrideFailed();
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void RequestEngineerDoorOpenRPC()
    {
        if (overrideState.value == EngineerLockDownStationState.Active)
        {
            Debug.LogWarning("[DOOR SWITCH] Override is active! Switch is locked.");
            return;
        }

        RpcTriggerEngineerDoor();
    }


    #region RPCS & UTILS

    [ObserversRpc]
    private void RpcSyncOverrideData(EngineerLockDownStepData[] overrides)
    {
        if (!isServer) { overrideSteps = overrides.ToList(); }
        OnOverrideDataSynced?.Invoke();
    }

    [ObserversRpc]
    private void RpcTriggerEngineerDoor()
    {
        OnEngineerDoorRequested?.Invoke();
    }


    [ObserversRpc] private void RpcOverrideStateChanged(EngineerLockDownStationState s) { OnOverrideStateChanged?.Invoke(s); }
    [ObserversRpc] private void RpcOverrideStepCompleted(int step) { OnOverrideStepCompleted?.Invoke(step); }
    [ObserversRpc] private void RpcTriggerOverrideFailed() { OnOverrideFailed?.Invoke(); }
    [ObserversRpc] private void RpcOverrideSolved() { OnOverrideSolved?.Invoke(); }

    private List<int> GenerateUniqueNumbers(int count, int min, int max)
    {
        HashSet<int> numbers = new HashSet<int>();
        while (numbers.Count < count) { numbers.Add(Random.Range(min, max + 1)); }
        return numbers.ToList();
    }

    #endregion
}