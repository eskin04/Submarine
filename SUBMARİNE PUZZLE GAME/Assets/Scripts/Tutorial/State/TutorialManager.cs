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

    public static event Action<TutorialManager, int, ulong, PlayerRole, int> OnPlayerProgressUpdated;

    private StateMachine stateMachine;
    private ulong lastEntryId;
    private ulong activeEntryId;
    private TutorialQuestBaseState admittedState;
    private bool completionReserved;
    private bool rejectedDuplicate;
    private int presentationQuestIndex = -1;
    private ulong presentationEntryId;

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
        if (!TryClaimInstance())
        {
            rejectedDuplicate = true;
            Destroy(gameObject);
            return;
        }

        stateMachine = GetComponent<StateMachine>();
    }

    private bool TryClaimInstance()
    {
        if (rejectedDuplicate || (Instance != null && !ReferenceEquals(Instance, this))) return false;
        Instance = this;
        return true;
    }

    private void ReleaseInstance()
    {
        if (stateMachine != null)
        {
            stateMachine.onStateChanged -= HandlePresentationStateChanged;
            stateMachine.onReceivedNewData -= TryPresentCurrentQuest;
        }
        presentationQuestIndex = -1;
        presentationEntryId = 0;
        if (ReferenceEquals(Instance, this)) Instance = null;
    }

    protected override void OnEarlySpawn(bool asServer)
    {
        base.OnEarlySpawn(asServer);
        // Retained respawn does not rerun Awake; reclaim before state-machine entry.
        if (TryClaimInstance())
        {
            stateMachine.onStateChanged -= HandlePresentationStateChanged;
            stateMachine.onStateChanged += HandlePresentationStateChanged;
            stateMachine.onReceivedNewData -= TryPresentCurrentQuest;
            stateMachine.onReceivedNewData += TryPresentCurrentQuest;
        }
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        TryPresentCurrentQuest();
    }

    private void HandlePresentationStateChanged(StateNode previous, StateNode current)
    {
        TryPresentCurrentQuest();
    }

    // One buffered snapshot per manager, not a separate obsolete start for every state node.
    [ObserversRpc(runLocally: true, bufferLast: true)]
    private void RpcQuestPresentationContext(int questIndex, ulong entryId)
    {
        if (!isSpawned || Instance != this || entryId == 0 || entryId < presentationEntryId) return;
        if (entryId == presentationEntryId)
        {
            // A cleared entry is a tombstone: a duplicate start cannot resurrect it.
            if (questIndex >= 0 && presentationQuestIndex != questIndex) return;
        }
        presentationQuestIndex = questIndex;
        presentationEntryId = entryId;
        TryPresentCurrentQuest();
    }

    internal bool MatchesPresentationContext(TutorialQuestBaseState state, int questIndex, ulong entryId)
    {
        return Instance == this && isActiveAndEnabled && isSpawned && state != null &&
            CurrentQuestState == state && state.isActiveAndEnabled && state.questData != null &&
            state.questData.questIndex == questIndex && presentationQuestIndex == questIndex &&
            entryId != 0 && presentationEntryId == entryId;
    }

    internal void TryPresentCurrentQuest()
    {
        var state = CurrentQuestState;
        if (MatchesPresentationContext(state, presentationQuestIndex, presentationEntryId))
            state.TryStartPresentation(this, presentationQuestIndex, presentationEntryId);
    }

    internal bool BeginQuest(TutorialQuestBaseState state, out ulong entryId)
    {
        entryId = 0;
        if (!isActiveAndEnabled || !IsSpawned(true) || !isServer || Instance != this ||
            state == null || !state.isActiveAndEnabled || state.questData == null || CurrentQuestState != state)
            return false;

        if (admittedState == state && activeEntryId != 0)
        {
            entryId = activeEntryId;
            return true;
        }
        if (lastEntryId == ulong.MaxValue) return false;

        admittedState = state;
        activeEntryId = ++lastEntryId;
        completionReserved = false;
        entryId = activeEntryId;
        ResetReadyStates();
        RpcQuestPresentationContext(state.questData.questIndex, entryId);
        return true;
    }

    internal void EndQuest(TutorialQuestBaseState state, ulong entryId)
    {
        if (entryId == 0 || admittedState != state || activeEntryId != entryId) return;
        InvalidateAdmission();
        if (IsSpawned(true) && isServer)
        {
            RpcQuestPresentationContext(-1, entryId);
            ResetReadyStates();
        }
    }

    internal bool MatchesAdmission(int questIndex, ulong entryId)
    {
        return isActiveAndEnabled && IsSpawned(true) && isServer && Instance == this &&
            entryId != 0 && entryId == activeEntryId && admittedState != null &&
            admittedState.isActiveAndEnabled && CurrentQuestState == admittedState &&
            admittedState.questData != null && admittedState.questData.questIndex == questIndex;
    }

    internal bool TryReserveCompletion(TutorialQuestBaseState state, ulong entryId)
    {
        if (state == null || state != admittedState || state.questData == null || completionReserved ||
            !MatchesAdmission(state.questData.questIndex, entryId)) return false;
        completionReserved = true;
        return true;
    }

    internal void ReleaseCompletion(TutorialQuestBaseState state, ulong entryId)
    {
        if (admittedState == state && activeEntryId == entryId && entryId != 0)
            completionReserved = false;
    }

    private bool TryGetRoleTasks(int questIndex, ulong entryId, PlayerRole role,
        out System.Collections.Generic.List<QuestTask> tasks)
    {
        tasks = null;
        if (completionReserved || !MatchesAdmission(questIndex, entryId)) return false;
        if (role == PlayerRole.Engineer) tasks = admittedState.questData.engineerTasks;
        else if (role == PlayerRole.Technician) tasks = admittedState.questData.technicianTasks;
        return tasks != null;
    }

    internal bool CanAwardTask(int questIndex, ulong entryId, PlayerRole role, TutorialAction action)
    {
        if (!TryGetRoleTasks(questIndex, entryId, role, out var tasks)) return false;
        return tasks.Exists(task => task.actionType == action);
    }

    private void ResetReadyStates()
    {
        isEngineerReady.value = false;
        isTechnicianReady.value = false;
        engineerTaskProgress.value = 0;
        technicianTaskProgress.value = 0;
    }

    [ServerRpc(requireOwnership: false)]
    public void UpdateProgressServerRpc(int questIndex, ulong entryId, int roleInt, int currentProgress)
    {
        PlayerRole role = (PlayerRole)roleInt;
        if (!TryGetRoleTasks(questIndex, entryId, role, out var tasks)) return;
        int previous = role == PlayerRole.Engineer ? engineerTaskProgress.value : technicianTaskProgress.value;
        if (currentProgress <= previous || currentProgress > tasks.Count) return;
        if (role == PlayerRole.Engineer)
            engineerTaskProgress.value = currentProgress;
        else if (role == PlayerRole.Technician)
            technicianTaskProgress.value = currentProgress;

        if (!MatchesAdmission(questIndex, entryId)) return;
        NotifyProgressChangedObserversRpc(questIndex, entryId, roleInt, currentProgress);
    }
    [ObserversRpc]
    private void NotifyProgressChangedObserversRpc(int questIndex, ulong entryId, int roleInt, int currentProgress)
    {
        PlayerRole role = (PlayerRole)roleInt;

        OnPlayerProgressUpdated?.Invoke(this, questIndex, entryId, role, currentProgress);
    }

    [ServerRpc(requireOwnership: false)]
    public void PlayerReadyServerRpc(int questIndex, ulong entryId, int roleInt)
    {
        PlayerRole role = (PlayerRole)roleInt;
        if (!TryGetRoleTasks(questIndex, entryId, role, out var tasks)) return;
        bool alreadyReady = role == PlayerRole.Engineer ? isEngineerReady.value : isTechnicianReady.value;
        int progress = role == PlayerRole.Engineer ? engineerTaskProgress.value : technicianTaskProgress.value;
        if (alreadyReady || progress != tasks.Count) return;
        var state = admittedState;
        Debug.Log($"<color=purple>[Tutorial]</color> PlayerReadyServerRpc called for role: {role}");

        if (role == PlayerRole.Engineer)
            isEngineerReady.value = true;
        else if (role == PlayerRole.Technician)
            isTechnicianReady.value = true;

        // SyncVar listeners may replace the state while the ready flag is written.
        if (MatchesAdmission(questIndex, entryId) && admittedState == state)
        {
            state.CheckCompletion();
        }
    }

    private void InvalidateAdmission()
    {
        admittedState = null;
        activeEntryId = 0;
        completionReserved = false;
    }

    protected override void OnDespawned(bool asServer)
    {
        if (asServer) InvalidateAdmission();
        // PurrNet clears this side's flag after the hook; the opposite side may still be valid.
        if (!IsSpawned(!asServer)) ReleaseInstance();
        base.OnDespawned(asServer);
    }

    protected override void OnDespawned()
    {
        InvalidateAdmission();
        ReleaseInstance();
        base.OnDespawned();
    }

    private void OnDisable() { InvalidateAdmission(); }

    protected override void OnDestroy()
    {
        InvalidateAdmission();
        ReleaseInstance();
        base.OnDestroy();
    }
}
