using UnityEngine;

public class Inversion_TechnicianModule : MonoBehaviour
{
    [Header("References")]
    public Inversion_Relay_StationManager stationManager;
    public Inversion_PipeVisual[] pipes = new Inversion_PipeVisual[5];

    public void SetupPipes(PipeLetter[] assignedLetters)
    {
        for (int i = 0; i < 5; i++)
        {
            pipes[i].SetLetter(assignedLetters[i]);
            pipes[i].SetLight(0);


        }
    }

    public void ResetPipeLights()
    {
        Invoke(nameof(ResetPipeLightsDelayed), 3f);
    }

    private void ResetPipeLightsDelayed()
    {
        foreach (var pipe in pipes)
        {
            pipe.SetLight(0);
        }

    }

    public void UpdatePipeLight(int pipeIndex, bool isSuccess)
    {
        int statusIndex = isSuccess ? 2 : 1;
        pipes[pipeIndex].SetLight(statusIndex);
    }

    public void OnValveDragged(int pipeIndex, ValveState desiredState)
    {
        if (!stationManager.isRoundActive.value || stationManager.isTesting.value) return;

        stationManager.TechnicianChangeValveRPC(pipeIndex, desiredState);
    }
}