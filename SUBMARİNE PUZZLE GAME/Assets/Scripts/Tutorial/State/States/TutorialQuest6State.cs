using UnityEngine;
using System.Collections;
using Cinemachine;
using PurrNet;

public class TutorialQuest6State : TutorialQuestBaseState
{
    public StationController overrideStationController;
    public CinemachineImpulseSource hullBreachImpulse;

    public override void Enter()
    {
        if (TutorialInputManager.Instance != null)
        {
            TutorialInputManager.Instance.UnlockRadio();
        }

        StartCoroutine(BreakdownRoutine());
    }

    private IEnumerator BreakdownRoutine()
    {
        yield return new WaitForSeconds(3f);
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
            hullBreachImpulse.GenerateImpulse(.5f);
        }
    }

    protected override void OnQuestAudioFinished()
    {
        Debug.Log("[Tutorial] Quest 6 Anonsu bitti. Oyuncular tamir için serbest.");
    }

    public override void Exit()
    {
        base.Exit();
        MainGameState.OnTutorialFinished?.Invoke();

        Debug.Log("<color=green>[Tutorial]</color> Bütün görevler tamamlandı! Ana seviyeye geçiliyor...");
    }
}