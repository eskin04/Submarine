using UnityEngine;
using TMPro;

public class RoE_TargetInfoScreen : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI codeNameText;
    public TextMeshProUGUI distanceText;

    [Header("Managers")]
    public RoE_ThreatManager threatManager;
    public RoE_TechnicianUI technicianUI;

    private string lastKnownCode = "";

    private ActiveThreat cachedThreat = null;

    private void Update()
    {
        if (threatManager == null || technicianUI == null) return;

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
            distanceText.text = "---";
            cachedThreat = null;
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