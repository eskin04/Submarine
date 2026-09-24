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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        if (isServer)
        {
            CanUseRadio.value = false;
        }

        Invoke(nameof(LockInitialInteractions), 0.5f);
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
    public void CompleteTaskForPlayerServerRpc(int targetRoleInt, int actionTypeInt)
    {
        CompleteTaskForPlayerObserverRpc(targetRoleInt, actionTypeInt);
    }

    [ObserversRpc]
    private void CompleteTaskForPlayerObserverRpc(int targetRoleInt, int actionTypeInt)
    {
        PlayerRole targetRole = (PlayerRole)targetRoleInt;
        TutorialAction actionType = (TutorialAction)actionTypeInt;

        if (PlayerStats.LocalInstance != null && PlayerStats.LocalInstance.Role == targetRole)
        {
            if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var view))
            {
                view.OnActionPerformed(actionType);
            }
        }
    }
}