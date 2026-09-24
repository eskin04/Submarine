using UnityEngine;

public class TutorialQuest5State : TutorialQuestBaseState
{
    protected override void OnQuestAudioFinished()
    {
        if (TutorialInputManager.Instance != null)
        {
            TutorialInputManager.Instance.UnlockEngineerDoorLeverServerRpc();
        }
        Debug.Log("[Tutorial] Quest 5 Anonsu bitti. Mühendis kapısı şalteri kullanıma açıldı.");
    }
}