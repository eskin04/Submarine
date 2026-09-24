using PurrNet.StateMachine;
using UnityEngine;

public class TutorialWaitState : StateNode
{
    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer) return;
        if (MainGameState.isTutorialStartedFlag)
        {
            machine.Next();
            return;
        }

        MainGameState.startTutorial += OnMainTutorialStarted;
    }

    private void OnMainTutorialStarted()
    {

        MainGameState.startTutorial -= OnMainTutorialStarted;

        machine.Next();
    }

    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
        if (asServer)
        {
            MainGameState.startTutorial -= OnMainTutorialStarted;
        }
    }
}