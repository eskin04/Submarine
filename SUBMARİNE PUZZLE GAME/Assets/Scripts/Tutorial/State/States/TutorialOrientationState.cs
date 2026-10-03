using PurrNet;
using UnityEngine;

public class TutorialOrientationState : TutorialQuestBaseState
{
    protected override void OnQuestStart()
    {
        TutorialInputManager.Instance?.LockInitialInteractions();
    }

    protected override void OnQuestAudioFinished()
    {
        RpcShowOrientationView();
    }

    [ObserversRpc(runLocally: true)]
    private void RpcShowOrientationView()
    {
        InstanceHandler.GetInstance<GameViewManager>()?.ShowView<ContractView>(hideOthers: false);

        ContractView.OnContractRead += HandleContractRead;
        ContractView.OnContractSigned += HandleContractSigned;
    }

    private void HandleContractRead()
    {
        if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var view))
        {
            view.OnActionPerformed(TutorialAction.ReadContract);
        }
    }

    private void HandleContractSigned()
    {
        if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var view))
        {
            view.OnActionPerformed(TutorialAction.SignContract);
        }

        InstanceHandler.GetInstance<GameViewManager>()?.HideView<ContractView>();
    }

    public override void Exit(bool asServer)
    {
        base.Exit(asServer);

        ContractView.OnContractRead -= HandleContractRead;
        ContractView.OnContractSigned -= HandleContractSigned;

        InstanceHandler.GetInstance<GameViewManager>()?.HideView<ContractView>();
    }
}