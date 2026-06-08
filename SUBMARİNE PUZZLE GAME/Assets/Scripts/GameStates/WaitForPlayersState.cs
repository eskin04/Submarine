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
        if (isServer)
        {
            RpcShowLoadingScreen();
        }
        while (networkManager.players.Count < minPlayersToStart)
        {
            yield return new WaitForSeconds(1f);
        }

        if (isServer)
        {
            RpcHideLoadingScreen();
        }
        machine.Next();
    }


    [ObserversRpc]
    private void RpcShowLoadingScreen()
    {
        // Ağ üzerinden bu emri alan herkes kendi lokalindeki UI'ı açar
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowLoadingScreen();
        }
    }

    [ObserversRpc]
    private void RpcHideLoadingScreen()
    {
        // Ağ üzerinden bu emri alan herkes kendi lokalindeki UI'ı kapatır
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.HideLoadingScreen();
        }
    }


    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
    }

}
