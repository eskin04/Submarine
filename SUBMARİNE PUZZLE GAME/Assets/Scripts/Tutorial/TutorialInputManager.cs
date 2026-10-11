using UnityEngine;
using PurrNet;
using System;
using PurrLobby;


public class TutorialInputManager : NetworkBehaviour
{
    public static TutorialInputManager Instance { get; private set; }

    public SyncVar<bool> CanUseRadio = new SyncVar<bool>(false);

    public static event Action<bool> OnNotebookInteractStateChanged;
    public static event Action<bool> OnEngineerLeverInteractStateChanged;

    private bool rejectedDuplicate;

    private void Awake()
    {
        if (!TryClaimInstance())
        {
            rejectedDuplicate = true;
            Destroy(gameObject);
        }
    }

    private bool TryClaimInstance()
    {
        if (rejectedDuplicate || (Instance != null && !ReferenceEquals(Instance, this))) return false;
        Instance = this;
        return true;
    }

    private void ReleaseInstance()
    {
        CancelInvoke(nameof(LockInitialInteractions));
        if (ReferenceEquals(Instance, this)) Instance = null;
    }

    protected override void OnEarlySpawn(bool asServer)
    {
        base.OnEarlySpawn(asServer);
        TryClaimInstance();
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        if (rejectedDuplicate || !ReferenceEquals(Instance, this)) return;
        if (isServer)
        {
            CanUseRadio.value = false;
        }

        Invoke(nameof(LockInitialInteractions), 0.5f);
    }

    protected override void OnDespawned(bool asServer)
    {
        // Keep registration until both host sides have ended, including early-spawn teardown.
        if (!IsSpawned(!asServer)) ReleaseInstance();
        base.OnDespawned(asServer);
    }

    protected override void OnDespawned()
    {
        ReleaseInstance();
        base.OnDespawned();
    }

    protected override void OnDestroy()
    {
        ReleaseInstance();
        base.OnDestroy();
    }

    public void LockInitialInteractions()
    {
        OnNotebookInteractStateChanged?.Invoke(false);
        OnEngineerLeverInteractStateChanged?.Invoke(false);
    }




    [ServerRpc(requireOwnership: false)]
    public void UnlockRadio() { CanUseRadio.value = true; }

    [ServerRpc(requireOwnership: false)]
    public void LockRadio() { CanUseRadio.value = false; }


    [ObserversRpc]
    public void UnlockNotebookInteractionObserverRpc()
    {
        OnNotebookInteractStateChanged?.Invoke(true);
    }

    [ServerRpc(requireOwnership: false)]
    public void UnlockNotebookInteractionServerRpc()
    {
        UnlockNotebookInteractionObserverRpc();
    }

    [ObserversRpc]
    public void UnlockEngineerDoorLeverObserverRpc()
    {
        OnEngineerLeverInteractStateChanged?.Invoke(true);
    }

    [ServerRpc(requireOwnership: false)]
    public void UnlockEngineerDoorLeverServerRpc()
    {
        UnlockEngineerDoorLeverObserverRpc();
    }

    [ServerRpc(requireOwnership: false)]
    public void CompleteTaskForPlayerServerRpc(TutorialManager questOwner, int questIndex, ulong entryId,
        int targetRoleInt, int actionTypeInt)
    {
        if (questOwner == null || !questOwner.CanAwardTask(questIndex, entryId,
            (PlayerRole)targetRoleInt, (TutorialAction)actionTypeInt)) return;
        CompleteTaskForPlayerObserverRpc(questOwner, questIndex, entryId, targetRoleInt, actionTypeInt);
    }

    [ObserversRpc]
    private void CompleteTaskForPlayerObserverRpc(TutorialManager questOwner, int questIndex, ulong entryId,
        int targetRoleInt, int actionTypeInt)
    {
        PlayerRole targetRole = (PlayerRole)targetRoleInt;
        TutorialAction actionType = (TutorialAction)actionTypeInt;

        if (PlayerStats.LocalInstance != null && PlayerStats.LocalInstance.Role == targetRole)
        {
            if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var view))
            {
                view.OnActionPerformed(actionType, questOwner, questIndex, entryId);
            }
        }
    }
}
