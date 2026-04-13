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

    [Header("Audio Settings")]
    public AudioEventChannelSO _channel;
    private FMODEmitter _activeWarningEmitter;

    public FMODUnity.EventReference clickSound;
    public FMODUnity.EventReference warningSound;

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

        if (ColorUtility.TryParseHtmlString("#18191a", out Color newColor))
        {
            backgroundImage.DOColor(newColor, 0.5f);
        }
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

    private void PlayClickSound()
    {
        if (_channel != null && !clickSound.IsNull)
        {
            AudioEventPayload payload = new AudioEventPayload(clickSound, this.transform.position);
            _channel.RaiseEvent(payload);
        }
    }

    public void OnShowDataPressed()
    {
        ShowDataScreen();
        PlayClickSound();
        StopWarningSound();
    }


    public void OnReadyPressed()
    {
        if (isReady) return;
        isReady = true;
        stationManager.SetTechReadyRPC();
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
        displayDataPanel.SetActive(false);
        resetPanel.SetActive(true);
    }

    public void SetStateLocked()
    {
        backgroundImage.DOColor(new Color(0.8f, 0.1f, 0.1f), 0.3f);
        lockedWarningPanel.SetActive(true);
        displayDataPanel.SetActive(false);
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