using PurrNet.StateMachine;
using UnityEngine;
using System.Collections;

public class TutorialWaitState : StateNode
{
    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer) return;
        if (MainGameState.isTutorialStartedFlag)
        {
            machine.StartCoroutine(WaitForNextFrameAndNextState());
            return;
        }

        MainGameState.startTutorial += OnMainTutorialStarted;
    }

    private void OnMainTutorialStarted()
    {

        MainGameState.startTutorial -= OnMainTutorialStarted;

        machine.StartCoroutine(WaitForNextFrameAndNextState());
    }

    private IEnumerator WaitForNextFrameAndNextState()
    {
        yield return new WaitForSeconds(2f);
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