using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Localization;
using System;

public class RoE_TargetInfoScreen : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI codeNameText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI cooldownText;
    public TextMeshProUGUI targetSpeedText;
    private LocalizedString localizedFeedback = new LocalizedString { TableReference = "UI_General" };


    [Header("Managers")]
    public RoE_ThreatManager threatManager;
    public RoE_TechnicianUI technicianUI;
    public RoE_StationManager stationManager;

    private string lastKnownCode = "";

    private ActiveThreat cachedThreat = null;
    private Coroutine cooldownCoroutine = null;
    private bool isThreatSelected = false;

    void OnEnable()
    {
        technicianUI.OnLockStateChanged += OnLockStateChanged;
        localizedFeedback.StringChanged += OnTranslatedText;
        localizedFeedback.TableEntryReference = "NO TARGET";

    }

    void OnDisable()
    {
        technicianUI.OnLockStateChanged -= OnLockStateChanged;
        localizedFeedback.StringChanged -= OnTranslatedText;

    }

    private void OnTranslatedText(string translatedText)
    {
        codeNameText.text = translatedText;
    }

    private void OnLockStateChanged()
    {
        if (!isThreatSelected) isThreatSelected = true;
        // if (cooldownCoroutine != null)
        // {
        //     StopCoroutine(cooldownCoroutine);
        //     cooldownCoroutine = null;
        // }

        // cooldownCoroutine = StartCoroutine(LockCooldownRoutine());
    }

    // private IEnumerator LockCooldownRoutine()
    // {
    //     float cooldownDuration = technicianUI.selectionCooldown;
    //     float elapsed = 0f;
    //     cooldownText.color = Color.orange;

    //     while (elapsed < cooldownDuration)
    //     {

    //         float remaining = cooldownDuration - elapsed;
    //         cooldownText.text = $"Locked: {remaining}s";
    //         elapsed++;
    //         yield return new WaitForSeconds(1f);
    //     }
    //     cooldownText.text = "Locked: 0s";
    //     cooldownText.color = Color.green;

    // }



    private void Update()
    {
        if (threatManager == null || technicianUI == null || !isThreatSelected) return;

        string currentCode = technicianUI.GetCurrentSelectedCode();

        if (currentCode != lastKnownCode)
        {
            UpdateNameDisplay(currentCode);
            lastKnownCode = currentCode;
        }

        if (cachedThreat != null && !cachedThreat.isDestroyed)
        {
            distanceText.text = $"{cachedThreat.currentDistance:F0}m";

            if (cachedThreat.currentDistance <= stationManager.avoidDistanceThreshold)
                distanceText.color = Color.red;
            else
                distanceText.color = Color.green;
        }
        else if (!string.IsNullOrEmpty(currentCode) && cachedThreat == null)
        {
            distanceText.text = "---";
        }
    }


    private void UpdateNameDisplay(string newCode)
    {
        if (string.IsNullOrEmpty(newCode))
        {
            localizedFeedback.TableEntryReference = "NO TARGET";

            distanceText.text = "";
            cachedThreat = null;
            targetSpeedText.text = "";
            if (cooldownCoroutine != null)
            {
                StopCoroutine(cooldownCoroutine);
                cooldownText.text = "";
                cooldownCoroutine = null;
            }
        }
        else
        {
            localizedFeedback.TableEntryReference = newCode;

            cachedThreat = threatManager.GetThreat(newCode);
            targetSpeedText.text = $"({cachedThreat.approachSpeed:F1} m/s)";

            if (cachedThreat == null)
            {
                codeNameText.text = "LOST SIGNAL";
            }
        }
    }
}