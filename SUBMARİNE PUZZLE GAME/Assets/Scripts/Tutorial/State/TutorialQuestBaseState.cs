using PurrLobby;
using PurrNet;
using PurrNet.StateMachine;
using UnityEngine;
using System;

public abstract class TutorialQuestBaseState : StateNode
{
    [Header("Quest Data")]
    public TutorialQuestData questData;
    private TutorialQuestView introView;
    private int introSequence;
    private bool ownsQuestPresentation;
    private TutorialManager admissionOwner;
    private ulong admissionEntryId;

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
        if (!asServer)
        {
            TutorialManager.Instance?.TryPresentCurrentQuest();
            return;
        }
        var owner = TutorialManager.Instance;
        if (owner == null || !owner.BeginQuest(this, out var entryId)) return;
        admissionOwner = owner;
        admissionEntryId = entryId;
        OnQuestStart();


    }

    internal void TryStartPresentation(TutorialManager questOwner, int questIndex, ulong entryId)
    {
        if (this == null || !isCurrentState || ownsQuestPresentation || questOwner == null ||
            questOwner != TutorialManager.Instance || questOwner.CurrentQuestState != this ||
            questData == null || questData.questIndex != questIndex ||
            !questOwner.MatchesPresentationContext(this, questIndex, entryId)) return;

        if (!InstanceHandler.TryGetInstance<TutorialQuestView>(out var view) || view == null ||
            !view.isActiveAndEnabled || !view.HasPlayerRole ||
            !InstanceHandler.TryGetInstance<GameViewManager>(out var ui) || ui == null ||
            !ui.CanPresent(view)) return;
        if (view.LoadQuest(questData, questOwner, questIndex, entryId))
        {
            introView = view;
            introSequence = view.SequenceId;
            ownsQuestPresentation = true;
            TutorialQuestView.IntroSequenceCompleted += HandleIntroSequenceCompleted;
        }
        else return;

        ui.ShowView<TutorialQuestView>(hideOthers: false);
        Debug.Log($"[Tutorial] {gameObject.name} başladı.");
    }

    private void HandleIntroSequenceCompleted(TutorialQuestView view, int sequence)
    {
        if (this == null || !ownsQuestPresentation || !isCurrentState ||
            view != introView || sequence != introSequence || !view.HasSequence(sequence)) return;
        TutorialQuestView.IntroSequenceCompleted -= HandleIntroSequenceCompleted;
        OnQuestAudioFinished();
    }

    protected virtual void OnQuestAudioFinished() { }

    protected virtual void OnQuestStart() { }


    public virtual void CheckCompletion()
    {
        var owner = admissionOwner;
        var entryId = admissionEntryId;
        if (owner == null || questData == null || !owner.MatchesAdmission(questData.questIndex, entryId)) return;
        bool ready = owner.isSoloTestMode
            ? owner.isEngineerReady.value || owner.isTechnicianReady.value
            : owner.isEngineerReady.value && owner.isTechnicianReady.value;
        if (!ready || !owner.TryReserveCompletion(this, entryId)) return;

        if (questData.questIndex == 6)
        {
            MainGameState.OnTutorialFinished?.Invoke();
            return;
        }
        // Next queues work until LateUpdate; ownership stays reserved in that interval.
        if (!machine.Next())
        {
            owner.ReleaseCompletion(this, entryId);
            Debug.LogWarning("[Tutorial] Current quest transition was rejected.");
        }
    }
    public override void Exit(bool asServer)
    {
        if (asServer) ReleaseAdmission();
        base.Exit(asServer);

        ReleaseIntro();
    }

    public override void Exit()
    {
        ReleaseIntro();
        base.Exit();
    }

    private void ReleaseIntro()
    {
        TutorialQuestView.IntroSequenceCompleted -= HandleIntroSequenceCompleted;
        ownsQuestPresentation = false;
        var view = introView;
        introView = null;
        if (view != null) view.CancelQuest(introSequence);
    }

    private void ReleaseAdmission()
    {
        var owner = admissionOwner;
        var entryId = admissionEntryId;
        admissionOwner = null;
        admissionEntryId = 0;
        if (owner != null) owner.EndQuest(this, entryId);
    }

    protected override void OnDespawned(bool asServer)
    {
        if (asServer) ReleaseAdmission();
        base.OnDespawned(asServer);
        if (!IsSpawned(!asServer)) ReleaseIntro();
    }

    protected override void OnDespawned()
    {
        ReleaseAdmission();
        ReleaseIntro();
        base.OnDespawned();
    }

    protected virtual void OnDisable()
    {
        ReleaseAdmission();
        ReleaseIntro();
    }

    protected override void OnDestroy()
    {
        ReleaseAdmission();
        try { base.OnDestroy(); }
        finally { ReleaseIntro(); }
    }
}
