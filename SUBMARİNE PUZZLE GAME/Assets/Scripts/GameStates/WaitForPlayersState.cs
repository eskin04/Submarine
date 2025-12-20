using System.Collections;
using PurrNet.StateMachine;
using UnityEngine;

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
        while (networkManager.players.Count < minPlayersToStart)
        {
            Debug.Log($"Current players: {networkManager.players.Count}. Waiting for at least {minPlayersToStart} players to start...");
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("Required number of players joined. Proceeding to next state...");
        machine.Next();
    }

    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
    }

}
