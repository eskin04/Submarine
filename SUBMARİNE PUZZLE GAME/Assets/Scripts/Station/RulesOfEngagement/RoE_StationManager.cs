using UnityEngine;
using System.Collections.Generic;
using PurrNet;
using Cinemachine;
using System.Linq;

public class RoE_StationManager : NetworkBehaviour
{
    [Header("Sub-Managers")]
    public StationController stationController;
    public RoE_ThreatManager threatManager;
    public RoE_BoardManager boardManager;

    [Header("References")]
    public RoE_EngineerDisplay engineerDisplay;
    public RoE_TechnicianUI technicianUI;
    public RoE_HandbookLoader handbookLoader;

    [Header("Database References")]
    public List<RoE_ObjectData> allPossibleObjects;
    public List<RoE_RuleData> allRules;
    public List<DecryptionSymbol> availableSymbols;


    [Header("Game State")]
    public List<BoardEntry> currentBoardSetup = new List<BoardEntry>();


    public RoE_RuleData currentDailyRule;

    [Header("Settings")]

    public CinemachineImpulseSource hullBreachImpulse;
    public float avoidDistanceThreshold = 100f;

    private bool isSimulationRunning = false;
    private bool isRoundActive = false;
    private RoE_ObjectData previousDestroyedObject = null;
    private int currentRoundActionCount = 0;
    private RoE_ObjectData previousPassedObject = null;
    private List<RoE_ObjectData> lastTwoActedObjects = new List<RoE_ObjectData>();

    public ObjectCategory activeCategoryX;
    public ObjectCategory activeCategoryY;
    public string activeRuleDescription;

    private List<ActiveThreat> succesThreats = new List<ActiveThreat>();



    void OnEnable()
    {
        stationController.StateChanged += OnStationStateChanged;
    }

    void OnDisable()
    {
        stationController.StateChanged -= OnStationStateChanged;
    }

    private void OnStationStateChanged(StationState newState, StationController controller)
    {
        RPCStationStateChange(newState);
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        if (isServer)
        {
            PrepareStationData();
        }
        Invoke(nameof(RequestStationVisualsRpc), 1f);

    }

    [ServerRpc(requireOwnership: false)]
    public void RequestStationVisualsRpc()
    {
        if (currentBoardSetup != null && currentBoardSetup.Count > 0)
        {
            List<int> objectIndices = new List<int>();
            foreach (var entry in currentBoardSetup)
            {
                objectIndices.Add(allPossibleObjects.IndexOf(entry.linkedObject));
            }
            RpcHandbookSetup(objectIndices);

            boardManager.BroadcastBoardData(currentBoardSetup);


        }
    }

    public void PrepareStationData()
    {

        List<RoE_ObjectData> roundObjects = GenerateRoundObjects();

        currentBoardSetup = boardManager.GenerateNewBoardData(roundObjects, availableSymbols);

    }


    public void StartNewRound()
    {
        if (!isServer) return;

        SetNewRandomRule();



        threatManager.SpawnNewThreats(currentBoardSetup);

        SetDataPackage();

        isRoundActive = true;
        currentRoundActionCount = 0;
        previousDestroyedObject = null;
        previousPassedObject = null;
        lastTwoActedObjects.Clear();
    }

    public List<RoE_ObjectData> GenerateRoundObjects()
    {
        List<RoE_ObjectData> selectedObjects = new List<RoE_ObjectData>();

        List<RoE_ObjectData> singleCat = allPossibleObjects.Where(o => o.categories.Count == 1).ToList();
        List<RoE_ObjectData> tripleCat = allPossibleObjects.Where(o => o.categories.Count == 3).ToList();

        if (singleCat.Count < 2 || tripleCat.Count < 2)
        {
            Debug.LogWarning("[RoE_StationManager] Havuzda yeterli Tekli veya Üçlü kategori objesi yok! Lütfen 30 objeyi kontrol edin.");
        }

        singleCat = singleCat.OrderBy(x => Random.value).ToList();
        tripleCat = tripleCat.OrderBy(x => Random.value).ToList();

        selectedObjects.AddRange(singleCat.Take(2));
        selectedObjects.AddRange(tripleCat.Take(2));

        List<RoE_ObjectData> remainingPool = allPossibleObjects.Except(selectedObjects).OrderBy(x => Random.value).ToList();

        int objectsNeeded = 16 - selectedObjects.Count;
        selectedObjects.AddRange(remainingPool.Take(objectsNeeded));

        return selectedObjects.OrderBy(x => Random.value).ToList();
    }

    public void SetNewRandomRule()
    {
        if (!isServer) return;

        RoE_RuleData newRule = allRules[Random.Range(0, allRules.Count)];
        currentDailyRule = newRule;

        var allCats = System.Enum.GetValues(typeof(ObjectCategory)).Cast<ObjectCategory>().ToList();
        allCats = allCats.OrderBy(x => Random.value).ToList();

        activeCategoryX = allCats[0];
        activeCategoryY = allCats[1];

        activeRuleDescription = currentDailyRule.ruleDescription
            .Replace("{X}", activeCategoryX.ToString())
            .Replace("{Y}", activeCategoryY.ToString());

        RpcUpdateRuleDisplay(allRules.IndexOf(newRule), activeRuleDescription);
    }

    [ObserversRpc]
    private void RpcUpdateRuleDisplay(int ruleIndex, string ruleDescription)
    {
        currentDailyRule = allRules[ruleIndex];
        if (engineerDisplay != null)
        {
            engineerDisplay.UpdateRuleDisplay(ruleDescription);
        }
    }

    [ObserversRpc]
    private void RpcHandbookSetup(List<int> objectIndices)
    {
        List<RoE_ObjectData> reconstructedObjects = new List<RoE_ObjectData>();

        foreach (int index in objectIndices)
        {
            if (index >= 0 && index < allPossibleObjects.Count)
            {
                reconstructedObjects.Add(allPossibleObjects[index]);
            }
        }
        if (handbookLoader != null)
        {
            handbookLoader.LoadHandbook(reconstructedObjects);
        }
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

    private float GetCurrentWaterLevelPercentage()
    {
        InstanceHandler.TryGetInstance<FloodManager>(out FloodManager floodManager);
        if (floodManager != null)
        {
            return floodManager.GetCurrentWaterLevel();
        }
        return 50f;
    }


    [ServerRpc(requireOwnership: false)]
    public void SubmitActionRPC(int threatIndex, Roe_PlayerAction action)
    {
        ActiveThreat threat = threatManager.GetThreat(threatIndex);
        currentRoundActionCount++;


        if (threat == null || threat.isDestroyed)
        {
            return;
        }

        if (action == Roe_PlayerAction.Evade)
        {
            HandleEvade(threat);
            return;
        }

        bool shouldHaveShot = RoE_RuleEvaluator.ShouldShoot(
            threat,
            currentDailyRule,
            threatManager,
            previousDestroyedObject,
            previousPassedObject,
            lastTwoActedObjects,
            currentRoundActionCount,
            GetCurrentWaterLevelPercentage(),
            activeCategoryX,
            activeCategoryY
        );

        bool isCorrect = (action == Roe_PlayerAction.Shoot && shouldHaveShot) ||
                         (action == Roe_PlayerAction.Pass && !shouldHaveShot);


        ResolveResult(threat, action, isCorrect);
    }

    public void RegisterHullBreach(ActiveThreat threat)
    {

        stationController.ReportTimeOutFailure();

        DestroyThreat(threat, true);
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
        if (threat.currentDistance > avoidDistanceThreshold)
        {
            RpcSendFeedback("TOO FAR TO EVADE!", Color.yellow);
            return;
        }
        SetNewRandomRule();
        stationController.ReportRepairMistake(0.5f);
        RpcSendFeedback("Avoided!", Color.cyan);
        DestroyThreat(threat);
    }

    private void ResolveResult(ActiveThreat threat, Roe_PlayerAction action, bool success)
    {
        if (success)
        {
            RpcSendFeedback("success!", Color.green);
            succesThreats.Add(threat);
            DestroyThreat(threat);

        }
        else
        {
            stationController.ReportRepairMistake();

            RpcSendFeedback("failure!", Color.red);

            DestroyThreat(threat);
            RpcTriggerImpactEffect();

        }
        if (action == Roe_PlayerAction.Shoot)
        {
            previousDestroyedObject = threat.realIdentity.linkedObject;
            previousPassedObject = null;
            RpcPlayShootSound();
        }
        else if (action == Roe_PlayerAction.Pass)
        {
            previousPassedObject = threat.realIdentity.linkedObject;
            previousDestroyedObject = null;
        }
        SetNewRandomRule();
        UpdateHistory(threat.realIdentity.linkedObject);
    }

    [ObserversRpc(runLocally: true)]
    private void RpcPlayShootSound()
    {
        if (technicianUI != null)
        {
            technicianUI.PlayShootSound();
        }
    }

    private void UpdateHistory(RoE_ObjectData obj)
    {
        lastTwoActedObjects.Add(obj);
        if (lastTwoActedObjects.Count > 2)
        {
            lastTwoActedObjects.RemoveAt(0);
        }
    }

    private void DestroyThreat(ActiveThreat threat, bool isHullBreach = false)
    {
        threat.isDestroyed = true;

        RpcThreatDestroyed(threatManager.activeThreats.IndexOf(threat));
        CheckRoundCompletion(isHullBreach);
    }

    private void CheckRoundCompletion(bool isHullBreach)
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
            EndRoundAndRepair(isHullBreach);
        }
    }

    private void EndRoundAndRepair(bool isHullBreach)
    {

        if (isHullBreach && succesThreats.Count < threatManager.activeThreats.Count / 2)
        {
            stationController.SetDestroyed();
        }
        else
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
    private void RPCStationStateChange(StationState newState)
    {
        if (technicianUI != null)
        {
            technicianUI.UpdateStationStatus(newState);
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
