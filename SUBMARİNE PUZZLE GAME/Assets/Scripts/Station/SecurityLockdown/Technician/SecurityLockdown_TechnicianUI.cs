using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SecurityLockdown_TechnicianUI : MonoBehaviour
{
    public SecurityLockdown_StationManager stationManager;

    [Header("UI Elements")]
    public Image backgroundImage;
    public GameObject displayDataPanel;
    public GameObject lockedWarningPanel;
    public GameObject resetPanel;
    public TMP_Text codesText;

    private SequenceData[] currentCodes;
    private bool isReady = false;

    public void UpdatePuzzleData(SequenceData[] seq)
    {
        currentCodes = seq;
        isReady = false;
        SetStateLocked();
    }

    private void ShowDataScreen()
    {

        backgroundImage.DOColor(new Color(0.25f, 0.25f, 0.25f), 0.5f);
        lockedWarningPanel.SetActive(false);
        displayDataPanel.SetActive(true);

        codesText.text = "";
        foreach (var code in currentCodes)
        {
            if (code.targetNumber == currentCodes[currentCodes.Length - 1].targetNumber)
                codesText.text += $"<color={GetHexCode(code.color)}>{code.targetNumber}</color>";
            else
                codesText.text += $"<color={GetHexCode(code.color)}>{code.targetNumber}</color>-";
        }
    }

    public void OnShowDataPressed()
    {
        ShowDataScreen();
    }


    public void OnReadyPressed()
    {
        if (isReady) return;
        isReady = true;
        stationManager.SetTechReadyRPC();
        ShowResetScreen();
    }

    public void OnHardResetPressed()
    {
        stationManager.RequestHardResetRPC();
    }

    private void ShowResetScreen()
    {
        backgroundImage.DOColor(new Color(0.8f, 0.5f, 0.1f), 0.5f);
        displayDataPanel.SetActive(false);
        resetPanel.SetActive(true);
    }

    public void SetStateLocked()
    {
        backgroundImage.DOColor(new Color(0.8f, 0.1f, 0.1f), 0.3f);
        lockedWarningPanel.SetActive(true);
        displayDataPanel.SetActive(false);
        resetPanel.SetActive(false);
    }

    private string GetHexCode(LockdownColor color)
    {
        switch (color)
        {
            case LockdownColor.Purple: return "#800080";
            case LockdownColor.Red: return "#FF0000";
            case LockdownColor.Blue: return "#0000FF";
            case LockdownColor.Green: return "#008000";
            case LockdownColor.Yellow: return "#FFFF00";
            case LockdownColor.White: return "#FFFFFF";
            default: return "#FFFFFF";
        }
    }
}