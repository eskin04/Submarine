using UnityEngine;
using PurrNet;
using PurrNet.StateMachine;
using PurrLobby;
using System;

[RequireComponent(typeof(StateMachine))]
public class TutorialManager : NetworkBehaviour
{
    public static TutorialManager Instance { get; private set; }
    [Header("Test Settings")]
    public bool isSoloTestMode = false;

    [Header("Player Ready States")]
    public SyncVar<bool> isEngineerReady = new SyncVar<bool>(false);
    public SyncVar<bool> isTechnicianReady = new SyncVar<bool>(false);

    public SyncVar<int> engineerTaskProgress = new SyncVar<int>(0);
    public SyncVar<int> technicianTaskProgress = new SyncVar<int>(0);

    public static event Action<PlayerRole, int> OnPlayerProgressUpdated;

    private StateMachine stateMachine;

    public TutorialQuestBaseState CurrentQuestState
    {
        get
        {
            if (stateMachine == null) return null;
            return stateMachine.currentStateNode as TutorialQuestBaseState;
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        stateMachine = GetComponent<StateMachine>();
    }

    public void ResetReadyStates()
    {
        isEngineerReady.value = false;
        isTechnicianReady.value = false;
        engineerTaskProgress.value = 0;
        technicianTaskProgress.value = 0;
    }

    [ServerRpc(requireOwnership: false)]
    public void UpdateProgressServerRpc(int roleInt, int currentProgress)
    {
        PlayerRole role = (PlayerRole)roleInt;

        if (role == PlayerRole.Engineer)
            engineerTaskProgress.value = currentProgress;
        else if (role == PlayerRole.Technician)
            technicianTaskProgress.value = currentProgress;

        NotifyProgressChangedObserversRpc(roleInt, currentProgress);
    }
    [ObserversRpc]
    private void NotifyProgressChangedObserversRpc(int roleInt, int currentProgress)
    {
        PlayerRole role = (PlayerRole)roleInt;

        OnPlayerProgressUpdated?.Invoke(role, currentProgress);
    }

    [ServerRpc(requireOwnership: false)]
    public void PlayerReadyServerRpc(int roleInt)
    {
        PlayerRole role = (PlayerRole)roleInt;
        Debug.Log($"<color=purple>[Tutorial]</color> PlayerReadyServerRpc called for role: {role}");

        if (role == PlayerRole.Engineer)
            isEngineerReady.value = true;
        else if (role == PlayerRole.Technician)
            isTechnicianReady.value = true;

        if (CurrentQuestState != null)
        {
            CurrentQuestState.CheckCompletion();
        }
    }
}