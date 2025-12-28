using System;
using PurrNet.StateMachine;

public class MainGameState : StateNode
{
    public static Action startGame;

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
        if (!asServer) return;
        startGame?.Invoke();
    }



    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
    }
}
