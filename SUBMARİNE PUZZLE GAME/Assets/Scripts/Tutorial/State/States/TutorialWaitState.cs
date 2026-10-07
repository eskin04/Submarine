using PurrNet.StateMachine;
using UnityEngine;
using System.Collections;

public class TutorialWaitState : StateNode
{
    private bool ownsServerVisit;
    private Coroutine pendingProgression;
    private StateMachine progressionOwner;

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer) return;
        ReleaseServerVisit();
        ownsServerVisit = true;
        if (MainGameState.isTutorialStartedFlag)
        {
            ScheduleProgression();
            return;
        }

        MainGameState.startTutorial += OnMainTutorialStarted;
    }

    private void OnMainTutorialStarted()
    {

        MainGameState.startTutorial -= OnMainTutorialStarted;

        ScheduleProgression();
    }

    private void ScheduleProgression()
    {
        if (!ownsServerVisit || !isCurrentState || pendingProgression != null) return;
        progressionOwner = machine;
        pendingProgression = progressionOwner.StartCoroutine(WaitForNextFrameAndNextState(progressionOwner));
    }

    private IEnumerator WaitForNextFrameAndNextState(StateMachine owner)
    {
        yield return new WaitForSeconds(2f);
        pendingProgression = null;
        progressionOwner = null;
        if (ownsServerVisit && owner != null && machine == owner && owner.currentStateNode == this)
        {
            owner.Next();
        }
    }

    private void ReleaseServerVisit()
    {
        ownsServerVisit = false;
        MainGameState.startTutorial -= OnMainTutorialStarted;
        var owner = progressionOwner;
        var pending = pendingProgression;
        progressionOwner = null;
        pendingProgression = null;
        if (owner != null && pending != null)
        {
            owner.StopCoroutine(pending);
        }
    }

    protected override void OnDestroy()
    {
        try
        {
            base.OnDestroy();
        }
        finally
        {
            ReleaseServerVisit();
        }
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        if (asServer) ReleaseServerVisit();
    }

    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
        if (asServer)
        {
            ReleaseServerVisit();
        }
    }
}
