using UnityEngine;
using PurrNet;
using TMPro;
using DG.Tweening;

public class LightsOut_Switch : NetworkBehaviour
{
    [Header("Settings")]
    public LightsOut_StationManager stationManager;
    public Interactable moduleInteractable;
    public int buttonID;
    public WireColor myLabelColor;
    public float RotateAngle = 30f;
    public float originalRot = 160;

    [Header("References")]
    public TMP_Text labelText;
    public GameObject switchObject;

    [Header("Audio Settings")]
    public AudioEventChannelSO _channel;
    public FMODUnity.EventReference switchSound;

    private bool isPressed = false;


    public void Setup(WireColor color, WireColor fakeVisualColor)
    {
        myLabelColor = color;
        isPressed = false;
        if (labelText)
        {
            labelText.text = color.ToString();
            labelText.color = GetUnityColor(fakeVisualColor);
        }


        ResetButton();
    }



    private Color GetUnityColor(WireColor colorEnum)
    {
        switch (colorEnum)
        {
            case WireColor.Yellow: return Color.yellow;
            case WireColor.Green: return Color.green;
            case WireColor.Blue: return Color.blue;
            case WireColor.Red: return Color.red;
            default: return Color.white;
        }
    }

    private void OnClicked()
    {
        if (!isPressed)
        {
            if (switchObject != null)
            {
                switchObject.transform.DOLocalRotate(new Vector3(RotateAngle, 0, -90), 0.2f);
            }
            isPressed = true;

            if (stationManager != null)
            {
                stationManager.RegisterSwitchPressRPC(myLabelColor);
            }
        }
        else
        {
            if (switchObject != null)
            {
                switchObject.transform.DOLocalRotate(new Vector3(originalRot, 0, -90), 0.2f);
            }
            isPressed = false;

            if (stationManager != null)
            {
                stationManager.UnregisterSwitchPressRPC(myLabelColor);
            }
        }
        PlaySwitchSound();
    }

    private void PlaySwitchSound()
    {
        if (_channel != null && !switchSound.IsNull)
        {
            AudioEventPayload payload = new AudioEventPayload(switchSound, this.transform.position);
            _channel.RaiseEvent(payload);
        }
    }

    private void ResetButton()
    {
        if (switchObject != null)
        {
            switchObject.transform.DOLocalRotate(new Vector3(originalRot, 0, -90), 0.2f);
            isPressed = false;
        }
    }

    private void OnMouseDown()
    {
        if (moduleInteractable != null && !moduleInteractable.IsInteracting()) return;

        OnClicked();

    }

}