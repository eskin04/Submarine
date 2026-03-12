using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class SecurityLockdown_EngineerUI : MonoBehaviour
{
    [Header("References")]
    public SecurityLockdown_StationManager stationManager;

    [Header("UI Elements")]
    public Image backgroundImage;
    public GameObject lockedPanel;
    public GameObject legendPanel;

    [Header("Button Panel Elements")]
    public GameObject showLegendBtnPanel;
    public TMP_Text showLegendPanelText;
    public Button showLegendButton;
    public CanvasGroup showLegendCanvasGroup;
    public TMP_Text legendText;

    private LegendData[] currentLegend;

    public void UpdateLegendData(LegendData[] legend)
    {
        currentLegend = legend;
        SetStateLocked();
    }

    public void EnableShowLegendButton()
    {
        lockedPanel.SetActive(false);
        legendPanel.SetActive(false);

        showLegendBtnPanel.SetActive(true);

        showLegendButton.interactable = true;
        showLegendCanvasGroup.alpha = 1f;
        showLegendPanelText.text = "View Region Legend";
        showLegendPanelText.color = Color.white;
        backgroundImage.DOColor(new Color(0.8f, 0.5f, 0.1f), 0.5f);
    }

    public void OnShowLegendPressed()
    {
        stationManager.RequestEngineerLegendRPC();
    }

    public void ShowLegend(float duration)
    {
        StartCoroutine(DisplayLegendRoutine(duration));
    }

    private IEnumerator DisplayLegendRoutine(float duration)
    {
        backgroundImage.DOColor(new Color(0.1f, 0.4f, 0.8f), 0.5f);

        lockedPanel.SetActive(false);
        showLegendBtnPanel.SetActive(false);
        legendPanel.SetActive(true);

        legendText.text = "--- REGION MAP ---\n";
        foreach (var data in currentLegend)
        {
            legendText.text += $"<color={GetHexCode(data.color)}>{data.assignedRegion}</color>\n";
        }

        yield return new WaitForSeconds(duration);

        backgroundImage.DOColor(new Color(0.8f, 0.5f, 0.1f), 0.5f);
        showLegendPanelText.text = "Warning: Re-viewing the legend will cause reset the progress";
        showLegendPanelText.color = Color.red;
        legendPanel.SetActive(false);
        showLegendBtnPanel.SetActive(true);

        if (stationManager.engViewCount.value >= 2)
        {
            showLegendButton.interactable = false;
            showLegendCanvasGroup.alpha = 0.4f;
        }
    }

    public void SetStateLocked()
    {
        StopAllCoroutines();
        backgroundImage.DOColor(new Color(0.8f, 0.1f, 0.1f), 0.3f);
        lockedPanel.SetActive(true);
        showLegendBtnPanel.SetActive(false);
        legendPanel.SetActive(false);
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