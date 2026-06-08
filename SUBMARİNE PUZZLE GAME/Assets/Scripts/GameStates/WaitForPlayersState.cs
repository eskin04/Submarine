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
        if (LoadingScreenManager.Instance != null && !LoadingScreenManager.Instance.IsShowing)
        {
            LoadingScreenManager.Instance.ShowLoadingScreenRPC();
        }
        while (networkManager.players.Count < minPlayersToStart)
        {
            yield return new WaitForSeconds(1f);
        }

        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.HideLoadingScreenRPC();
        }
        machine.Next();
    }


    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
    }

}
