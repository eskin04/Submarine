using UnityEngine;
using System.Collections.Generic;
using PurrNet;
using Cinemachine;

public class RoE_StationManager : NetworkBehaviour
{
    [Header("Sub-Managers")]
    public StationController stationController;
    public RoE_ThreatManager threatManager;
    public RoE_BoardManager boardManager;

    [Header("References")]
    public RoE_EngineerDisplay engineerDisplay;
    public RoE_TechnicianUI technicianUI;

    [Header("Database References")]
    public List<RoE_ObjectData> allPossibleObjects;
    public List<RoE_RuleData> allRules;
    public List<DecryptionSymbol> availableSymbols;


    [Header("Game State")]
    public List<BoardEntry> currentBoardSetup = new List<BoardEntry>();


    public RoE_RuleData currentDailyRule;

    [Header("Settings")]

    public CinemachineImpulseSource hullBreachImpulse;

    private bool isSimulationRunning = false;
    private bool isRoundActive = false;






    public void StartNewRound()
    {
        if (!isServer) return;


        currentDailyRule = allRules[Random.Range(0, allRules.Count)];


        if (boardManager != null)
        {
            currentBoardSetup = boardManager.GenerateNewBoardData(allPossibleObjects, availableSymbols);
        }


        threatManager.SpawnNewThreats(currentBoardSetup);

        SetDataPackage();
        if (engineerDisplay != null)
        {
            engineerDisplay.UpdateRuleDisplay(currentDailyRule.ruleDescription);
        }
        isRoundActive = true;
    }




    private void SetDataPackage()
    {
        List<NetworkThreatData> dataPackage = new List<NetworkThreatData>();
        var activeThreats = threatManager.activeThreats;

        for (int i = 0; i < activeThreats.Count; i++)
        {
            var threat = activeThreats[i];

            NetworkThreatData data = new NetworkThreatData
            {
                threatID = i,
                codeEnumIndex = (int)threat.codeEnum,
                realObjectIndex = allPossibleObjects.IndexOf(threat.realIdentity.linkedObject),
                startDistance = threat.currentDistance,
                speed = threat.approachSpeed
            };
            dataPackage.Add(data);
        }

        RpcSyncRoundData(dataPackage, allRules.IndexOf(currentDailyRule));
    }

    [ObserversRpc]
    private void RpcSyncRoundData(List<NetworkThreatData> threatData, int ruleIndex)
    {
        if (isServer) return;

        currentDailyRule = allRules[ruleIndex];
        if (engineerDisplay != null)
        {
            engineerDisplay.UpdateRuleDisplay(currentDailyRule.ruleDescription);
        }


        threatManager.SyncThreatsFromNetwork(threatData, allPossibleObjects);
    }



    [ServerRpc(requireOwnership: false)]
    public void ActivateStationRPC()
    {
        if (isSimulationRunning || !isRoundActive) return;

        isSimulationRunning = true;

        RpcSetSimulationState(true);
    }




    [ServerRpc(requireOwnership: false)]
    public void SelectThreatRPC(int threatIndex)
    {

        ActiveThreat threat = threatManager.GetThreat(threatIndex);

        if (threat != null)
        {
            List<int> symbolIndicesToSend = new List<int>();

            foreach (var sym in threat.realIdentity.assignedSymbols)
            {
                int index = availableSymbols.IndexOf(sym);
                symbolIndicesToSend.Add(index);
            }

            RpcUpdateEngineerScreen(symbolIndicesToSend, threat.displayName);

        }
    }


    [ServerRpc(requireOwnership: false)]
    public void SubmitActionRPC(int threatIndex, Roe_PlayerAction action)
    {
        ActiveThreat threat = threatManager.GetThreat(threatIndex);

        if (threat == null || threat.isDestroyed)
        {
            return;
        }

        if (action == Roe_PlayerAction.Evade)
        {
            HandleEvade(threat);
            return;
        }

        bool shouldHaveShot = RoE_RuleEvaluator.ShouldShoot(threat.realIdentity.linkedObject, currentDailyRule);
        bool isCorrect = (action == Roe_PlayerAction.Shoot && shouldHaveShot) ||
                         (action == Roe_PlayerAction.Pass && !shouldHaveShot);

        ResolveResult(threat, action, isCorrect);
    }

    public void RegisterHullBreach(ActiveThreat threat)
    {

        stationController.ReportTimeOutFailure();

        DestroyThreat(threat);
        RpcTriggerImpactEffect();


    }

    [ObserversRpc]
    private void RpcTriggerImpactEffect()
    {

        if (hullBreachImpulse != null)
        {
            hullBreachImpulse.GenerateImpulse(.2f);
        }
    }


    private void HandleEvade(ActiveThreat threat)
    {
        if (threat.currentDistance > 100f)
        {
            RpcSendFeedback("TOO FAR TO EVADE!", Color.yellow);
            return;
        }
        stationController.ReportRepairMistake(0.5f);
        RpcSendFeedback("Avoided!", Color.cyan);
        DestroyThreat(threat);
    }

    private void ResolveResult(ActiveThreat threat, Roe_PlayerAction action, bool success)
    {
        if (success)
        {
            RpcSendFeedback("success!", Color.green);

            DestroyThreat(threat);
        }
        else
        {
            stationController.ReportRepairMistake();

            RpcSendFeedback("failure!", Color.red);

            DestroyThreat(threat);

        }
    }

    private void DestroyThreat(ActiveThreat threat)
    {
        threat.isDestroyed = true;

        RpcThreatDestroyed(threatManager.activeThreats.IndexOf(threat));
        CheckRoundCompletion();
    }

    private void CheckRoundCompletion()
    {
        bool allDestroyed = true;
        foreach (var t in threatManager.activeThreats)
        {
            if (!t.isDestroyed)
            {
                allDestroyed = false;
                break;
            }
        }

        if (allDestroyed)
        {
            EndRoundAndRepair();
        }
    }

    private void EndRoundAndRepair()
    {

        if (stationController != null)
        {
            stationController.SetReparied();
        }

        isRoundActive = false;
        isSimulationRunning = false;

        RpcSetSimulationState(false);


    }

    public bool GetSimulateRunning()
    {
        return isSimulationRunning;
    }

    [ObserversRpc]
    private void RpcUpdateEngineerScreen(List<int> indices, string codeName)
    {
        if (engineerDisplay != null)
        {
            engineerDisplay.StartDisplaySequence(indices, codeName);
        }
    }



    [ObserversRpc]
    private void RpcSetSimulationState(bool state)
    {
        isSimulationRunning = state;
        if (state == false && technicianUI != null)
        {
            technicianUI.ForceClearInterface();
        }
    }

    [ObserversRpc]
    private void RpcSendFeedback(string message, Color msgColor)
    {
        if (technicianUI != null)
        {
            technicianUI.UpdateFeedBack(message, msgColor);
        }
    }

    [ObserversRpc]
    private void RpcThreatDestroyed(int threatIndex)
    {
        ActiveThreat threat = threatManager.GetThreat(threatIndex);
        if (threat != null) threat.isDestroyed = true;
        if (engineerDisplay != null)
        {
            engineerDisplay.OnTargetDestroyed(threat.displayName);
        }
    }

    [ContextMenu("Debug: Start Round")]
    private void DebugStartRound()
    {
        if (isServer)
        {
            StartNewRound();
        }
    }




}


[System.Serializable]
public class BoardEntry
{
    public RoE_ObjectData linkedObject;
    public List<DecryptionSymbol> assignedSymbols;
}

[System.Serializable]
public class ActiveThreat
{
    public ThreatCodeName codeEnum;
    public string displayName;
    public float currentDistance;
    public float approachSpeed;
    public BoardEntry realIdentity;
    public bool isDestroyed = false;
}

public enum Roe_PlayerAction
{
    Shoot,
    Pass,
    Evade
}
