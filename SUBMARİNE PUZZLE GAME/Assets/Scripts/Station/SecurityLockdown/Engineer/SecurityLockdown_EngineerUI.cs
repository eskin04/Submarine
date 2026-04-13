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
    public GameObject resetPanel;

    public GameObject legendItemPrefab;
    public Transform legendContainer;

    [Header("Audio Settings")]
    public AudioEventChannelSO _channel;
    private FMODEmitter _activeWarningEmitter;

    public FMODUnity.EventReference clickSound;
    public FMODUnity.EventReference warningSound;

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
        if (ColorUtility.TryParseHtmlString("#18191a", out Color newColor))
        {
            backgroundImage.DOColor(newColor, 0.5f);
        }


        lockedPanel.SetActive(false);
        legendPanel.SetActive(true);
        resetPanel.SetActive(false);


        foreach (Transform child in legendContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var data in currentLegend)
        {
            GameObject newItem = Instantiate(legendItemPrefab, legendContainer);

            Image colorBox = newItem.GetComponentInChildren<Image>();
            TMP_Text regionText = newItem.GetComponentInChildren<TMP_Text>();

            if (colorBox != null)
            {
                if (ColorUtility.TryParseHtmlString(GetHexCode(data.color), out Color parsedColor))
                {
                    colorBox.color = parsedColor;
                }
            }

            if (regionText != null)
            {
                regionText.text = data.assignedRegion.ToString();
            }
        }
    }

    private void PlayClickSound()
    {
        if (_channel != null && !clickSound.IsNull)
        {
            AudioEventPayload payload = new AudioEventPayload(clickSound, this.transform.position);
            _channel.RaiseEvent(payload);
        }
    }

    public void OnShowLegendPressed()
    {
        ShowLegendScreen();
        PlayClickSound();
        StopWarningSound();
    }



    public void OnReadyButtonPressed()
    {
        if (isReady) return;
        isReady = true;
        stationManager.SetEngReadyRPC();
        ShowResetScreen();
        PlayClickSound();
    }

    public void OnHardResetPressed()
    {
        stationManager.RequestHardResetRPC();
        PlayClickSound();
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
        PlayWarningSound();
    }

    private void PlayWarningSound()
    {
        if (_activeWarningEmitter != null) return;

        if (!warningSound.IsNull)
        {
            _activeWarningEmitter = AudioManager.Instance.PlayLoopingOrAttachedSound(warningSound, this.transform);
        }
    }

    private void StopWarningSound()
    {
        if (_activeWarningEmitter != null)
        {
            _activeWarningEmitter.StopSound();
            _activeWarningEmitter = null;

        }
    }

    private string GetHexCode(LockdownColor color)
    {
        switch (color)
        {
            case LockdownColor.Purple: return "#8411D6";
            case LockdownColor.Red: return "#d93232";
            case LockdownColor.Blue: return "#0A5FC7";
            case LockdownColor.Green: return "#20C40A";
            case LockdownColor.Yellow: return "#F0A62B";
            case LockdownColor.White: return "#E6E6E6";
            default: return "#FFFFFF";
        }
    }
}