using UnityEngine;
using System.Collections.Generic;
using PurrNet;

public class RoE_StationManager : NetworkBehaviour
{
    [Header("Sub-Managers")]
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
    public int currentLevel = 1;
    private bool isSimulationRunning = false;
    private bool isRoundActive = false;





    public void StartNewRound()
    {
        if (!isServer) return;

        Debug.Log("RoE Station: Round Starting...");

        currentDailyRule = allRules[Random.Range(0, allRules.Count)];


        if (boardManager != null)
        {
            currentBoardSetup = boardManager.GenerateNewBoardData(allPossibleObjects, availableSymbols);
        }


        threatManager.SpawnNewThreats(currentLevel, currentBoardSetup);

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
            Debug.Log($"Server: {threat.displayName} selected. Sending symbols...");
            List<int> symbolIndicesToSend = new List<int>();

            foreach (var sym in threat.realIdentity.assignedSymbols)
            {
                int index = availableSymbols.IndexOf(sym);
                symbolIndicesToSend.Add(index);
            }

            RpcUpdateEngineerScreen(symbolIndicesToSend, threat.displayName);

            RpcUpdateTechnicianSelection(threat.displayName);
        }
    }


    [ServerRpc(requireOwnership: false)]
    public void SubmitActionRPC(int threatIndex, Roe_PlayerAction action)
    {
        // Tehdidi bul
        ActiveThreat threat = threatManager.GetThreat(threatIndex);

        if (threat == null || threat.isDestroyed)
        {
            Debug.LogWarning("Target not found or already destroyed.");
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

    private void HandleEvade(ActiveThreat threat)
    {
        if (threat.currentDistance > 100f)
        {
            RpcSendFeedback("TOO FAR TO EVADE!", Color.yellow);
            return;
        }

        Debug.Log("EVADE Successful.");
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
            RpcSendFeedback("failure!", Color.red);

            DestroyThreat(threat);

        }
    }

    private void DestroyThreat(ActiveThreat threat)
    {
        threat.isDestroyed = true;

        RpcThreatDestroyed(threatManager.activeThreats.IndexOf(threat));
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
    private void RpcUpdateTechnicianSelection(string selectedCodeName)
    {
        Debug.Log($"System: {selectedCodeName} is now ACTIVE.");
    }

    [ObserversRpc]
    private void RpcSetSimulationState(bool state)
    {
        isSimulationRunning = state;
        Debug.Log($"<color=yellow>[SİSTEM]</color> Simülasyon Durumu Değişti: {state.ToString().ToUpper()}");
    }

    [ObserversRpc]
    private void RpcSendFeedback(string message, Color msgColor) // bool kullanarak renk kararını basitleştirebiliriz
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
