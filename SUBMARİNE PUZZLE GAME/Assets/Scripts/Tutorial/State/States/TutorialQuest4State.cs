using UnityEngine;
using PurrNet;

public class TutorialQuest4State : TutorialQuestBaseState
{
    protected override void OnQuestAudioFinished()
    {
        if (TutorialInputManager.Instance != null)
        {
            TutorialInputManager.Instance.UnlockRadio();
        }
    }

    public override void Exit()
    {
        base.Exit();

        if (TutorialInputManager.Instance != null)
        {
            TutorialInputManager.Instance.LockRadio();
        }

    }
}