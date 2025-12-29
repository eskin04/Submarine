using System;
using PurrNet.StateMachine;

public class MainGameState : StateNode
{
    public static Action startGame;

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
        if (!asServer) return;
        FloodManager.OnGameEnd += HandleGameEnd;
    }

    private void HandleGameEnd(bool isGameEnd)
    {
        if (isGameEnd)
            machine.Next();
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
