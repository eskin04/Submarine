using UnityEngine;
using TMPro;
using System.Collections;

public class RoE_TargetInfoScreen : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI codeNameText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI cooldownText;

    [Header("Managers")]
    public RoE_ThreatManager threatManager;
    public RoE_TechnicianUI technicianUI;

    private string lastKnownCode = "";

    private ActiveThreat cachedThreat = null;
    private Coroutine cooldownCoroutine = null;
    private bool isThreatSelected = false;

    void OnEnable()
    {
        technicianUI.OnLockStateChanged += OnLockStateChanged;
    }

    void OnDisable()
    {
        technicianUI.OnLockStateChanged -= OnLockStateChanged;
    }

    private void OnLockStateChanged()
    {
        if (!isThreatSelected) isThreatSelected = true;
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }

        cooldownCoroutine = StartCoroutine(LockCooldownRoutine());
    }

    private IEnumerator LockCooldownRoutine()
    {
        float cooldownDuration = technicianUI.selectionCooldown;
        float elapsed = 0f;
        cooldownText.color = Color.orange;

        while (elapsed < cooldownDuration)
        {

            float remaining = cooldownDuration - elapsed;
            cooldownText.text = $"Locked: {remaining}s";
            elapsed++;
            yield return new WaitForSeconds(1f);
        }
        cooldownText.text = "Locked: 0s";
        cooldownText.color = Color.green;

    }



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

            if (cachedThreat.currentDistance <= 100f)
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
            codeNameText.text = "NO TARGET";
            distanceText.text = "";
            cachedThreat = null;
            if (cooldownCoroutine != null)
            {
                StopCoroutine(cooldownCoroutine);
                cooldownText.text = "";
                cooldownCoroutine = null;
            }
        }
        else
        {
            codeNameText.text = newCode;

            cachedThreat = threatManager.GetThreat(newCode);

            if (cachedThreat == null)
            {
                codeNameText.text = "LOST SIGNAL";
            }
        }
    }
}