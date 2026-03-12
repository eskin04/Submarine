using UnityEngine;
using TMPro;

public class SecurityLockdown_OverrideScreen : MonoBehaviour
{
    public enum DoorRole { Technician, Engineer }

    [Header("Settings")]
    public DoorRole myRole;
    public SecurityLockdown_StationManager stationManager;

    [Header("References")]
    public TMP_Text numberText;
    public GameObject screenPanel;

    private void OnEnable()
    {
        SecurityLockdown_StationManager.OnStateChanged += HandleStateChanged;
        SecurityLockdown_StationManager.OnOverrideStepCompleted += UpdateScreen;
        SecurityLockdown_StationManager.OnOverrideFailed += RefreshScreen;

        SecurityLockdown_StationManager.OnPuzzleDataSynced += RefreshScreen;
    }

    private void OnDisable()
    {
        SecurityLockdown_StationManager.OnStateChanged -= HandleStateChanged;
        SecurityLockdown_StationManager.OnOverrideStepCompleted -= UpdateScreen;
        SecurityLockdown_StationManager.OnOverrideFailed -= RefreshScreen;

        SecurityLockdown_StationManager.OnPuzzleDataSynced -= RefreshScreen;
    }

    private void HandleStateChanged(LockDownStationState state)
    {
        if (state == LockDownStationState.Active)
        {
            screenPanel.SetActive(true);
            RefreshScreen();
        }
        else
        {
            screenPanel.SetActive(false);
        }
    }

    private void RefreshScreen()
    {
        UpdateScreen(-1);
    }

    private void UpdateScreen(int completedStep)
    {
        if (stationManager == null || stationManager.overrideSteps.Count == 0) return;

        int targetStep = completedStep == -1 ? stationManager.currentOverrideStep.value : completedStep + 1;

        if (targetStep < 3)
        {
            OverrideStepData currentData = stationManager.overrideSteps[targetStep];
            numberText.text = myRole == DoorRole.Technician ? currentData.techNumber.ToString() : currentData.engNumber.ToString();
        }
        else
        {
            numberText.text = "OK";
        }
    }
}