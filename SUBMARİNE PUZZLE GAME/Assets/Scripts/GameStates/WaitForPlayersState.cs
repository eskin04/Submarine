using System.Collections;
using PurrNet.StateMachine;
using UnityEngine;
using PurrNet;

public class WaitForPlayersState : StateNode
{
    [SerializeField] private int minPlayersToStart = 2;
    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
        if (!asServer) return;

        machine.StartCoroutine(WaitForPlayers());
    }

    private IEnumerator WaitForPlayers()
    {
        ShowLoadingView();
        while (networkManager.players.Count < minPlayersToStart)
        {
            yield return new WaitForSeconds(1f);
        }

        yield return new WaitForSeconds(1f);

        HideLoadingView();
        machine.Next();
    }
    [ObserversRpc]
    private void ShowLoadingView()
    {
        InstanceHandler.GetInstance<GameViewManager>().ShowView<LoadingView>(hideOthers: false);
    }

    [ObserversRpc]
    private void HideLoadingView()
    {
        InstanceHandler.GetInstance<GameViewManager>().HideView<LoadingView>();

    }

    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
    }

}
