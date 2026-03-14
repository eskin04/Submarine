using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SecurityLockdown_EngineerUI : MonoBehaviour
{
    public SecurityLockdown_StationManager stationManager;

    [Header("UI Elements")]
    public Image backgroundImage;
    public GameObject legendPanel;
    public GameObject lockedPanel;
    public TMP_Text legendText;
    public GameObject resetPanel;

    private LegendData[] currentLegend;
    private bool isReady = false;

    public void UpdateLegendData(LegendData[] legend)
    {
        currentLegend = legend;
        isReady = false;
        SetStateLocked();
    }

    private void ShowLegendScreen()
    {
        backgroundImage.DOColor(new Color(0.1f, 0.4f, 0.8f), 0.5f);
        lockedPanel.SetActive(false);
        legendPanel.SetActive(true);
        resetPanel.SetActive(false);

        legendText.text = "--- REGION MAP ---\n";
        foreach (var data in currentLegend)
        {
            legendText.text += $"<color={GetHexCode(data.color)}>{data.assignedRegion}</color>\n";
        }
    }

    public void OnShowLegendPressed()
    {
        ShowLegendScreen();
    }



    public void OnReadyButtonPressed()
    {
        if (isReady) return;
        isReady = true;
        stationManager.SetEngReadyRPC();
        ShowResetScreen();
    }

    public void OnHardResetPressed()
    {
        stationManager.RequestHardResetRPC();
    }

    private void ShowResetScreen()
    {
        backgroundImage.DOColor(new Color(0.8f, 0.5f, 0.1f), 0.5f);
        legendPanel.SetActive(false);
        resetPanel.SetActive(true);
    }

    public void SetStateLocked()
    {
        backgroundImage.DOColor(new Color(0.8f, 0.1f, 0.1f), 0.3f);
        lockedPanel.SetActive(true);
        legendPanel.SetActive(false);
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
            case LockdownColor.Brown: return "#571717";
            case LockdownColor.Yellow: return "#FFFF00";
            case LockdownColor.White: return "#FFFFFF";
            case LockdownColor.Pink: return "#FFC0CB";
            default: return "#FFFFFF";
        }
    }
}