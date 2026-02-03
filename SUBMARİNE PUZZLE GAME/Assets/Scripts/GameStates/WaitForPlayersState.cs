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
        if (!isServer) return;

        machine.StartCoroutine(WaitForPlayers());
    }

    private IEnumerator WaitForPlayers()
    {
        ShowLoadingView();
        while (networkManager.players.Count < minPlayersToStart)
        {
            Debug.Log($"Current players: {networkManager.players.Count}. Waiting for at least {minPlayersToStart} players to start...");
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("Required number of players joined. Proceeding to next state...");
        yield return new WaitForSeconds(1f);

        HideLoadingView();
        machine.Next();
    }
    [ObserversRpc]
    private void ShowLoadingView()
    {
        InstanceHandler.GetInstance<GameViewManager>().ShowView<LoadingView>(hideOthers: false);
        Debug.Log("Showing loading view to all clients.");
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
