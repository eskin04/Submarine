using UnityEngine;
using System.Collections;
using Cinemachine;
using PurrNet;
using PurrNet.StateMachine;

public class TutorialQuest6State : TutorialQuestBaseState
{
    public StationController overrideStationController;
    public CinemachineImpulseSource hullBreachImpulse;

    private Coroutine pendingBreakdown;
    private bool ownsLocalVisit;
    private bool ownsServerVisit;
    private bool completionPublished;

    public override void Enter(bool asServer)
    {
        if (asServer)
        {
            ReleaseVisit();
            ownsServerVisit = true;
            completionPublished = false;
        }
        base.Enter(asServer);
    }

    public override void Enter()
    {
        ReleaseBreakdown();
        if (!isCurrentState || completionPublished) return;
        ownsLocalVisit = true;
        if (TutorialInputManager.Instance != null)
        {
            TutorialInputManager.Instance.UnlockRadio();
        }

        pendingBreakdown = StartCoroutine(BreakdownRoutine(machine));
    }

    private IEnumerator BreakdownRoutine(StateMachine owner)
    {
        yield return new WaitForSeconds(3f);
        if (this == null) yield break;
        pendingBreakdown = null;
        if (!ownsLocalVisit || completionPublished || owner == null || machine != owner || owner.currentStateNode != this)
            yield break;

        RpcTriggerImpactEffect();
        if (overrideStationController != null)
        {
            overrideStationController.SetBroken();
        }

        base.Enter();
    }

    [ObserversRpc]
    private void RpcTriggerImpactEffect()
    {

        if (hullBreachImpulse != null)
        {
            hullBreachImpulse.GenerateImpulse(.25f);
        }
    }

    protected override void OnQuestAudioFinished()
    {
        Debug.Log("[Tutorial] Quest 6 Anonsu bitti. Oyuncular tamir için serbest.");
    }

    public override void Exit()
    {
        ReleaseVisit();
        base.Exit();
    }

    public override void Exit(bool asServer)
    {
        ReleaseBreakdown();
        if (asServer) ownsServerVisit = false;
        base.Exit(asServer);
    }

    public override void CheckCompletion()
    {
        if (this == null || !ownsServerVisit || !isCurrentState || completionPublished) return;

        var tutorial = TutorialManager.Instance;
        if (tutorial == null) return;
        bool ready = tutorial.isSoloTestMode
            ? tutorial.isEngineerReady.value || tutorial.isTechnicianReady.value
            : tutorial.isEngineerReady.value && tutorial.isTechnicianReady.value;

        // Reserve success before the base publication, including reentrant listeners.
        if (ready && questData != null && questData.questIndex == 6)
        {
            completionPublished = true;
            ReleaseBreakdown();
        }

        base.CheckCompletion();
        if (completionPublished)
            Debug.Log("<color=green>[Tutorial]</color> Bütün görevler tamamlandı! Ana seviyeye geçiliyor...");
    }

    private void ReleaseBreakdown()
    {
        ownsLocalVisit = false;
        var pending = pendingBreakdown;
        pendingBreakdown = null;
        if (pending != null) StopCoroutine(pending);
    }

    private void ReleaseVisit()
    {
        ownsServerVisit = false;
        ReleaseBreakdown();
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        if (asServer) ownsServerVisit = false;
        if (!IsSpawned(!asServer)) ReleaseVisit();
    }

    protected override void OnDespawned()
    {
        ReleaseVisit();
        base.OnDespawned();
    }

    protected override void OnDestroy()
    {
        try
        {
            base.OnDestroy();
        }
        finally
        {
            ReleaseVisit();
        }
    }
}
