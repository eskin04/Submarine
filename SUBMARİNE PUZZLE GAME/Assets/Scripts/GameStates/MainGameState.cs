using System;
using PurrNet.StateMachine;
using UnityEngine;

public class MainGameState : StateNode
{
    public static Action startGame;
    public bool isTestMode = false;

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
        if (!asServer || isTestMode) return;
        startGame?.Invoke();
        FloodManager.OnGameEnd += HandleGameEnd;
    }

    private void HandleGameEnd(int isGameWin)
    {
        machine.Next((ushort)isGameWin);
    }

    public void StartGame()
    {
        startGame?.Invoke();
    }



    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
        if (!asServer) return;
        FloodManager.OnGameEnd -= HandleGameEnd;
    }
}
