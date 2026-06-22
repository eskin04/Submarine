using UnityEngine;
using TMPro;
using DG.Tweening;

public class Airlock_StationFrontend : MonoBehaviour
{
    [Header("Backend Reference")]
    public Airlock_StationManager manager;

    [Header("Role Setup")]
    public bool isTechnician;

    [Header("UI & Screens")]
    public TextMeshPro screenText;

    [Header("Physical Interactables")]
    public Airlock_Lever physicsLever;
    public Airlock_SealButton physicsButton;

    [Header("Indicator Lights")]
    public Airlock_IndicatorLight myIndicatorLight;
    public Airlock_IndicatorLight partnerIndicatorLight;
    public Airlock_IndicatorLight[] stageLights;

    private int pendingStageIndex;

    private bool isTransitioning = false;

    private int pendingTargetPressure;
    private int pendingFluctuation;

    private Tween errorResetTween;

    private void OnEnable()
    {
        manager.OnStageDataReceived += HandleNewStageData;
        manager.OnStageSuccessAnimTrigger += PlaySuccessSequence;
        manager.OnStationFailedReset += ResetFrontend;

        manager.OnTechSealChanged += HandleTechSealChanged;
        manager.OnEngSealChanged += HandleEngSealChanged;
        manager.OnStationResolvedEvent += HandleStationResolved;

        if (physicsButton != null)
            physicsButton.OnToggled += OnSealButtonToggled;
    }

    private void OnDisable()
    {
        manager.OnStageDataReceived -= HandleNewStageData;
        manager.OnStageSuccessAnimTrigger -= PlaySuccessSequence;
        manager.OnStationFailedReset -= ResetFrontend;

        manager.OnTechSealChanged -= HandleTechSealChanged;
        manager.OnEngSealChanged -= HandleEngSealChanged;
        manager.OnStationResolvedEvent -= HandleStationResolved;

        if (physicsButton != null)
            physicsButton.OnToggled -= OnSealButtonToggled;

        errorResetTween?.Kill();
    }

    private void HandleStationResolved()
    {
        pendingStageIndex = 3;
    }

    private void OnSealButtonToggled(bool isSealed)
    {
        physicsLever.isLocked = isSealed;

        manager.SetSealStateRPC(isTechnician, isSealed, physicsLever.LeverValue);
    }

    private void HandleNewStageData(int targetPressure, int fluctuation, int stageIndex)
    {
        pendingTargetPressure = targetPressure;
        pendingFluctuation = fluctuation;
        pendingStageIndex = stageIndex;

        if (!isTransitioning)
        {
            ApplyPendingData();
        }
    }

    private void ApplyPendingData()
    {
        if (pendingStageIndex >= 3)
        {
            UpdateStageLights(pendingStageIndex);
            return;
        }
        if (isTechnician)
            screenText.text = $"TARGET\n{pendingTargetPressure}";
        else
            screenText.text = $"FLUCTUATION\n{pendingFluctuation}";

        UpdateStageLights(pendingStageIndex);
    }

    private void UpdateStageLights(int currentStage)
    {
        for (int i = 0; i < stageLights.Length; i++)
        {
            if (stageLights[i] == null) continue;

            if (i < currentStage)
            {
                stageLights[i].activeColorIndex = 2;
                stageLights[i].SetLightState(true);
            }
            else if (i == currentStage)
            {
                stageLights[i].activeColorIndex = 4;
                stageLights[i].SetLightState(true);
            }
            else
            {
                stageLights[i].ResetLight();
            }
        }
    }

    private void HandleTechSealChanged(bool isSealed)
    {
        if (isTransitioning) return;

        if (isTechnician) myIndicatorLight.SetLightState(isSealed);
        else partnerIndicatorLight.SetLightState(isSealed);
    }

    private void HandleEngSealChanged(bool isSealed)
    {
        if (isTransitioning) return;

        if (!isTechnician) myIndicatorLight.SetLightState(isSealed);
        else partnerIndicatorLight.SetLightState(isSealed);
    }

    private void PlaySuccessSequence()
    {
        isTransitioning = true;

        physicsButton.SetLocked(true);
        physicsLever.isLocked = true;

        myIndicatorLight.PlayFadeOut(1.5f, () =>
        {
            physicsLever.ResetLever();
            physicsButton.ResetButton();

            physicsButton.SetLocked(false);
            physicsLever.isLocked = false;

            ApplyPendingData();

            isTransitioning = false;
        });

        partnerIndicatorLight.PlayFadeOut(1.5f);
    }

    private void ResetFrontend()
    {
        errorResetTween?.Kill();

        isTransitioning = true;

        myIndicatorLight.ResetLight();
        partnerIndicatorLight.ResetLight();

        physicsLever.ResetLever();
        physicsLever.isLocked = true;

        foreach (var light in stageLights)
        {
            if (light != null) light.ResetLight();
        }

        physicsButton.ResetButton();
        physicsButton.SetLocked(true);

        screenText.text = "<color=red>ERROR</color>\nRESETTING...";

        errorResetTween = DOVirtual.DelayedCall(1.5f, () =>
        {
            physicsLever.isLocked = false;
            physicsButton.SetLocked(false);

            ApplyPendingData();

            isTransitioning = false;
        });
    }
}