using UnityEngine;
using PurrNet;

public struct AirlockStageData
{
    public int targetPressure;
    public int fluctuationValue;
}

public class Airlock_StationManager : NetworkBehaviour
{
    [Header("Live State (SyncVars)")]
    public SyncVar<bool> isRoundActive = new SyncVar<bool>(false);
    public SyncVar<int> currentStage = new SyncVar<int>(0);

    public SyncVar<bool> isTechSealed = new SyncVar<bool>(false);
    public SyncVar<bool> isEngSealed = new SyncVar<bool>(false);

    [Header("Backend Data (Server Only)")]
    private AirlockStageData[] stages = new AirlockStageData[3];
    private int techSubmittedDial = 1;
    private int engSubmittedDial = 1;

    [Header("References")]
    public StationController stationController;

    // Client event'leri
    public event System.Action<int, int, int> OnStageDataReceived;
    public event System.Action OnStageSuccessAnimTrigger;
    public event System.Action OnStationFailedReset;
    public event System.Action<bool> OnTechSealChanged;
    public event System.Action<bool> OnEngSealChanged;
    public event System.Action OnStationResolvedEvent;

    public void StartNewRound()
    {
        if (!isServer) return;

        GenerateStages();

        currentStage.value = 0;
        isRoundActive.value = true;
        ResetSealStates();

        SendCurrentStageData();
    }

    private void GenerateStages()
    {
        for (int i = 0; i < 3; i++)
        {
            int fluctuation = Random.Range(0, 98);

            int maxPossibleTotal = Mathf.Min(39, 100 - fluctuation);

            int maxOddIndex = (maxPossibleTotal - 1) / 2;
            int randomOddIndex = Random.Range(1, maxOddIndex + 1);
            int playerTotal = (randomOddIndex * 2) + 1;

            int target = playerTotal + fluctuation;

            stages[i] = new AirlockStageData
            {
                targetPressure = target,
                fluctuationValue = fluctuation
            };
        }
    }

    private void SendCurrentStageData()
    {
        AirlockStageData data = stages[currentStage.value];
        RpcUpdateStageData(data.targetPressure, data.fluctuationValue, currentStage.value);
    }
    // ==========================================
    // CLIENT -> SERVER RPC 
    // ==========================================

    [ServerRpc(requireOwnership: false)]
    public void SetSealStateRPC(bool isTechnician, bool state, int dialValue)
    {
        if (!isRoundActive.value) return;

        if (isTechnician)
        {
            isTechSealed.value = state;
            if (state) techSubmittedDial = dialValue;
            RpcUpdateSealState(true, state);
        }
        else
        {
            isEngSealed.value = state;
            if (state) engSubmittedDial = dialValue;
            RpcUpdateSealState(false, state);
        }

        if (isTechSealed.value && isEngSealed.value)
        {
            CheckStageResult();
        }
    }

    // ==========================================
    // SERVER LOGIC
    // ==========================================

    private void CheckStageResult()
    {
        AirlockStageData currentData = stages[currentStage.value];

        int totalCalculated = techSubmittedDial + engSubmittedDial + currentData.fluctuationValue;

        if (totalCalculated == currentData.targetPressure)
        {
            currentStage.value++;

            if (currentStage.value >= 3)
            {
                isRoundActive.value = false;
                StartCoroutine(DelayedStationRepair());
                RpcStationResolved();
            }
            else
            {
                RpcStageSuccess();
                ResetSealStates();
                SendCurrentStageData();
            }
        }
        else
        {
            stationController.ReportRepairMistake();

            currentStage.value = 0;
            GenerateStages();

            ResetSealStates();
            SendCurrentStageData();

            RpcStationFailed();
        }
    }

    private System.Collections.IEnumerator DelayedStationRepair()
    {
        yield return new WaitForSeconds(1.5f);

        stationController.SetReparied();
    }

    private void ResetSealStates()
    {
        isTechSealed.value = false;
        isEngSealed.value = false;
        techSubmittedDial = 1;
        engSubmittedDial = 1;

        RpcUpdateSealState(true, false);
        RpcUpdateSealState(false, false);
    }

    [ObserversRpc]
    private void RpcUpdateSealState(bool isTech, bool state)
    {
        if (isTech) OnTechSealChanged?.Invoke(state);
        else OnEngSealChanged?.Invoke(state);
    }

    // ==========================================
    // SERVER -> CLIENT RPC
    // ==========================================

    [ObserversRpc]
    private void RpcUpdateStageData(int targetPressure, int fluctuation, int stageIndex)
    {
        OnStageDataReceived?.Invoke(targetPressure, fluctuation, stageIndex);
    }

    [ObserversRpc]
    private void RpcStageSuccess()
    {
        OnStageSuccessAnimTrigger?.Invoke();

    }

    [ObserversRpc]
    private void RpcStationFailed()
    {
        OnStationFailedReset?.Invoke();
    }

    [ObserversRpc]
    private void RpcStationResolved()
    {
        OnStageSuccessAnimTrigger?.Invoke();
        OnStationResolvedEvent?.Invoke();
    }


    [ContextMenu("Test 1: Yeni Round Başlat")]
    public void Debug_StartNewRound()
    {
        StartNewRound();
        AirlockStageData data = stages[0];
        Debug.Log($"<color=cyan>[TEST]</color> Round Başladı! 1. Aşama -> Hedef Basınç: {data.targetPressure}, Dalgalanma: {data.fluctuationValue}");
    }



}