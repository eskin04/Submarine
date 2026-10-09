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

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
        if (!asServer) return;
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ResetReadyStates();
        }

        RpcStartQuestBase();
        OnQuestStart();


    }

    [ObserversRpc(runLocally: true)]
    private void RpcStartQuestBase()
    {
        if (this == null || !isCurrentState || ownsQuestPresentation) return;

        var view = InstanceHandler.GetInstance<TutorialQuestView>();
        if (view != null && questData != null)
        {
            view.LoadQuest(questData);
            introView = view;
            introSequence = view.SequenceId;
            ownsQuestPresentation = true;
            TutorialQuestView.IntroSequenceCompleted += HandleIntroSequenceCompleted;
        }

        InstanceHandler.GetInstance<GameViewManager>()?.ShowView<TutorialQuestView>(hideOthers: false);
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
        if (TutorialManager.Instance == null) return;


        bool engineerReady = TutorialManager.Instance.isEngineerReady.value;
        bool technicianReady = TutorialManager.Instance.isTechnicianReady.value;
        Debug.Log($"<color=blue>[Tutorial]</color> Engineer Ready: {engineerReady}, Technician Ready: {technicianReady}");

        if (TutorialManager.Instance.isSoloTestMode)
        {
            if (engineerReady || technicianReady)
            {
                Debug.Log($"<color=yellow>[Tutorial]</color> Solo Test Modu aktif. Makine Next yapıyor!");
                if (questData.questIndex == 6)
                {
                    MainGameState.OnTutorialFinished?.Invoke();
                    return;
                }
                machine.Next();
            }
        }
        else
        {
            if (engineerReady && technicianReady)
            {
                Debug.Log($"<color=green>[Tutorial]</color> İki oyuncu da hazır. Görev tamamlandı!");
                if (questData.questIndex == 6)
                {
                    MainGameState.OnTutorialFinished?.Invoke();
                    return;
                }
                machine.Next();
            }
        }
    }
    public override void Exit(bool asServer)
    {
        base.Exit(asServer);

        ReleaseIntro();
        if (asServer && TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ResetReadyStates();
        }
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

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        if (!IsSpawned(!asServer)) ReleaseIntro();
    }

    protected override void OnDespawned()
    {
        ReleaseIntro();
        base.OnDespawned();
    }

    protected virtual void OnDisable()
    {
        ReleaseIntro();
    }

    protected override void OnDestroy()
    {
        try { base.OnDestroy(); }
        finally { ReleaseIntro(); }
    }
}
