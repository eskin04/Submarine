using PurrNet;
using UnityEngine;

public class TutorialQuest2State : TutorialQuestBaseState
{
    protected override void OnQuestStart()
    {


    }

    protected override void OnQuestAudioFinished()
    {
        if (TutorialInputManager.Instance != null)
        {
            TutorialInputManager.Instance.UnlockNotebookInteractionServerRpc();
        }

    }


}