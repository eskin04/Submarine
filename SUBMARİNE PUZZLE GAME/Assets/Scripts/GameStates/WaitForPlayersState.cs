using System.Collections;
using System.Collections.Generic;
using PurrNet.StateMachine;
using UnityEngine;
using PurrNet;

public class WaitForPlayersState : StateNode
{
    [SerializeField] private int minPlayersToStart = 2;

    private HashSet<PlayerID> readyPlayers = new HashSet<PlayerID>();

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (asServer)
        {
            readyPlayers.Clear();
            readyPlayers.Add(networkManager.localPlayer);

            machine.StartCoroutine(WaitForPlayers());
        }
        else
        {
            SendClientReadyServerRpc();
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void SendClientReadyServerRpc(RPCInfo info = default)
    {
        readyPlayers.Add(info.sender);
    }

    private IEnumerator WaitForPlayers()
    {
        if (isServer)
        {
            RpcShowLoadingScreen();
        }

        while (readyPlayers.Count < minPlayersToStart)
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (isServer)
        {
            Debug.Log("Hiding Loading Screen");
            RpcHideLoadingScreen();
        }

        machine.Next();
    }

    [ObserversRpc]
    private void RpcShowLoadingScreen()
    {
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowLoadingScreen();
        }
    }

    [ObserversRpc]
    private void RpcHideLoadingScreen()
    {
        Debug.Log("RpcHideLoadingScreen called");
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.HideLoadingScreen();
        }
    }

    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
        if (asServer)
        {
            readyPlayers.Clear();
        }
    }
}