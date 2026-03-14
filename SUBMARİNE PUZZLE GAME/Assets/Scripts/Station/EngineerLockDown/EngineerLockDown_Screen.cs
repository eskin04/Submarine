using UnityEngine;
using TMPro;

public class EngineerLockDown_Screen : MonoBehaviour
{
    public enum DoorRole { Technician, Engineer }

    [Header("Settings")]
    public DoorRole myRole;
    public EngineerLockDown_StationManager overrideManager;

    [Header("References")]
    public TMP_Text numberText;
    public GameObject screenPanel;

    private void OnEnable()
    {
        screenPanel.SetActive(false);
        EngineerLockDown_StationManager.OnOverrideStateChanged += HandleStateChanged;
        EngineerLockDown_StationManager.OnOverrideStepCompleted += UpdateScreen;
        EngineerLockDown_StationManager.OnOverrideFailed += RefreshScreen;
        EngineerLockDown_StationManager.OnOverrideDataSynced += RefreshScreen;
    }

    private void OnDisable()
    {
        EngineerLockDown_StationManager.OnOverrideStateChanged -= HandleStateChanged;
        EngineerLockDown_StationManager.OnOverrideStepCompleted -= UpdateScreen;
        EngineerLockDown_StationManager.OnOverrideFailed -= RefreshScreen;
        EngineerLockDown_StationManager.OnOverrideDataSynced -= RefreshScreen;
    }

    private void HandleStateChanged(EngineerLockDownStationState state)
    {
        if (state == EngineerLockDownStationState.Active)
        {
            screenPanel.SetActive(true);
            RefreshScreen();
        }
        else
        {
            screenPanel.SetActive(false);
        }
    }

    private void RefreshScreen() { UpdateScreen(-1); }

    private void UpdateScreen(int completedStep)
    {
        if (overrideManager == null || overrideManager.overrideSteps.Count == 0) return;

        int targetStep = completedStep == -1 ? overrideManager.currentOverrideStep.value : completedStep + 1;

        if (targetStep < 3)
        {
            EngineerLockDownStepData currentData = overrideManager.overrideSteps[targetStep];
            numberText.text = myRole == DoorRole.Technician ? currentData.techNumber.ToString() : currentData.engNumber.ToString();
        }
        else
        {
            numberText.text = "OK";
        }
    }
}