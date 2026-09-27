using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

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

    // Lokalizasyon değişkenleri
    private LocalizedString uiScreenTextString = new LocalizedString { TableReference = "UI_General" };
    private bool isErrorState = false;

    private void OnEnable()
    {
        manager.OnStageDataReceived += HandleNewStageData;
        manager.OnStageSuccessAnimTrigger += PlaySuccessSequence;
        manager.OnStationFailedReset += ResetFrontend;
        manager.OnTechSealChanged += HandleTechSealChanged;
        manager.OnEngSealChanged += HandleEngSealChanged;
        manager.OnStationResolvedEvent += HandleStationResolved;

        if (physicsButton != null) physicsButton.OnToggled += OnSealButtonToggled;

        uiScreenTextString.StringChanged += OnTranslatedScreenTextReady;
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        manager.OnStageDataReceived -= HandleNewStageData;
        manager.OnStageSuccessAnimTrigger -= PlaySuccessSequence;
        manager.OnStationFailedReset -= ResetFrontend;
        manager.OnTechSealChanged -= HandleTechSealChanged;
        manager.OnEngSealChanged -= HandleEngSealChanged;
        manager.OnStationResolvedEvent -= HandleStationResolved;

        if (physicsButton != null) physicsButton.OnToggled -= OnSealButtonToggled;

        uiScreenTextString.StringChanged -= OnTranslatedScreenTextReady;
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;

        errorResetTween?.Kill();
    }

    private void OnTranslatedScreenTextReady(string text)
    {
        if (screenText != null)
            screenText.text = isErrorState ? $"<color=red>{text}</color>" : text;
    }

    private void OnLanguageChanged(Locale newLocale) => uiScreenTextString.RefreshString();

    private void HandleStationResolved() => pendingStageIndex = 3;

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

        if (!isTransitioning) ApplyPendingData();
    }

    private void UpdateScreenText(string key, bool isError, int? value = null)
    {
        isErrorState = isError;

        if (value.HasValue)
            uiScreenTextString.Arguments = new object[] { new Dictionary<string, string> { { "Value", value.Value.ToString() } } };
        else
            uiScreenTextString.Arguments = null;

        uiScreenTextString.TableEntryReference = key;
        uiScreenTextString.RefreshString();
    }

    private void ApplyPendingData()
    {
        if (pendingStageIndex >= 3)
        {
            UpdateStageLights(pendingStageIndex);
            return;
        }

        int value = isTechnician ? pendingTargetPressure : pendingFluctuation;
        string key = isTechnician ? "airlock_target" : "airlock_fluctuation";

        UpdateScreenText(key, false, value);
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
        var light = isTechnician ? myIndicatorLight : partnerIndicatorLight;
        light.SetLightState(isSealed);
    }

    private void HandleEngSealChanged(bool isSealed)
    {
        if (isTransitioning) return;
        var light = !isTechnician ? myIndicatorLight : partnerIndicatorLight;
        light.SetLightState(isSealed);
    }

    private void SetPhysicalControlsLocked(bool lockState)
    {
        physicsLever.isLocked = lockState;
        physicsButton.SetLocked(lockState);
    }

    private void ResetPhysicalControls()
    {
        physicsLever.ResetLever();
        physicsButton.ResetButton();
    }

    private void PlaySuccessSequence()
    {
        isTransitioning = true;
        SetPhysicalControlsLocked(true);

        myIndicatorLight.PlayFadeOut(1.5f, () =>
        {
            ResetPhysicalControls();
            SetPhysicalControlsLocked(false);
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

        foreach (var light in stageLights)
            if (light != null) light.ResetLight();

        ResetPhysicalControls();
        SetPhysicalControlsLocked(true);

        UpdateScreenText("airlock_error_reset", true);

        errorResetTween = DOVirtual.DelayedCall(1.5f, () =>
        {
            SetPhysicalControlsLocked(false);
            ApplyPendingData();
            isTransitioning = false;
        });
    }
}