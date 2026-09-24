using PurrNet;
using UnityEngine;
using System.Collections;

public class TutorialOrientationState : TutorialQuestBaseState
{
    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
    }

    protected override void OnQuestStart()
    {

        RpcShowOrientationView();

        TutorialInputManager.Instance?.LockInitialInteractions();
    }

    [ObserversRpc(runLocally: true)]
    private void RpcShowOrientationView()
    {
        Debug.Log("[Tutorial UI] Oryantasyon Videosu ve Sözleşme gösteriliyor...");

    }
}