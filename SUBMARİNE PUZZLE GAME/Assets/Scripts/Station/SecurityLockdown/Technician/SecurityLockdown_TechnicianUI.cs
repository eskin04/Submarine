using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class SecurityLockdown_TechnicianUI : MonoBehaviour
{
    [Header("References")]
    public SecurityLockdown_StationManager stationManager;

    [Header("UI Elements")]
    public Image backgroundImage;
    public GameObject lockedWarningPanel;
    public GameObject displayDataPanel;
    public GameObject waitingPanel;
    public TMP_Text waitingPanelText;
    public TMP_Text codesText;

    [Header("Button Status")]
    public TMP_Text receiveCodeButtonText;

    [Header("Colors")]
    public Color lockedColor = new Color(0.8f, 0.1f, 0.1f);
    public Color displayingColor = new Color(0.1f, 0.4f, 0.8f);
    public Color waitingColor = new Color(0.8f, 0.7f, 0.1f);

    private SequenceData[] currentCodes;

    public void UpdatePuzzleData(SequenceData[] seq)
    {
        currentCodes = seq;
        SetStateLocked();
    }

    public void OnReceiveCodePressed()
    {
        stationManager.RequestTechnicianCodeRPC();
    }

    public void ShowCode(float duration)
    {
        StartCoroutine(DisplayCodesRoutine(duration));
    }

    private IEnumerator DisplayCodesRoutine(float duration)
    {
        backgroundImage.DOColor(displayingColor, 0.5f);
        lockedWarningPanel.SetActive(false);
        waitingPanel.SetActive(false);
        displayDataPanel.SetActive(true);

        codesText.text = "";
        foreach (var code in currentCodes)
        {
            if (code.targetNumber == currentCodes[currentCodes.Length - 1].targetNumber)
                codesText.text += $"<color={GetHexCode(code.color)}>{code.targetNumber}</color>";
            else
                codesText.text += $"<color={GetHexCode(code.color)}>{code.targetNumber}</color>-";
        }

        yield return new WaitForSeconds(duration);

        backgroundImage.DOColor(waitingColor, 0.5f);
        displayDataPanel.SetActive(false);
        waitingPanel.SetActive(true);

        int views = stationManager.techViewCount.value;
        if (views == 1)
        {
            receiveCodeButtonText.text = "Re-view Code";
            waitingPanelText.text = "Warning: Re-viewing the Code will cause reset the progress of the lockdown.";
        }
        else if (views >= 2)
        {
            receiveCodeButtonText.text = "Reset System";
            waitingPanelText.text = "Warning: Resetting the System will cause reset the all progress and system of the lockdown.";
        }
    }

    public void SetStateLocked()
    {
        StopAllCoroutines();
        backgroundImage.DOColor(lockedColor, 0.3f);
        lockedWarningPanel.SetActive(true);
        displayDataPanel.SetActive(false);
        waitingPanel.SetActive(false);
    }

    private string GetHexCode(LockdownColor color)
    {
        switch (color)
        {
            case LockdownColor.Purple: return "#800080";
            case LockdownColor.Red: return "#FF0000";
            case LockdownColor.Blue: return "#0000FF";
            case LockdownColor.Green: return "#008000";
            case LockdownColor.Brown: return "#571717";
            case LockdownColor.Yellow: return "#FFFF00";
            case LockdownColor.White: return "#FFFFFF";
            case LockdownColor.Pink: return "#FFC0CB";
            default: return "#FFFFFF";
        }
    }
}