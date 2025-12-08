using PurrNet.StateMachine;
using UnityEngine;

public class MainGameState : StateNode
{
    [SerializeField] private float gameTimer = 300f; // 5 minutes
    [SerializeField] private float warningTime = 60f; // 1 minute
    private float reduceTimer = 1f;
    private float currentTime;
    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
        if (!asServer) return;
        currentTime = gameTimer;
    }

    public override void StateUpdate(bool asServer)
    {
        base.StateUpdate(asServer);
        if (!asServer) return;
        currentTime -= Time.deltaTime * reduceTimer;

    }

    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
    }
}
