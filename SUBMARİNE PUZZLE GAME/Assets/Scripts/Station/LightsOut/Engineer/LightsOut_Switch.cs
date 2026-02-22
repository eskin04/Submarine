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

    [Header("References")]
    public TMP_Text labelText;
    public GameObject switchObject;

    private bool isPressed = false;


    public void Setup(WireColor color)
    {
        myLabelColor = color;
        isPressed = false;
        if (labelText) labelText.text = color.ToString();

        ResetButton();
    }

    private void OnClicked()
    {
        if (switchObject != null)
        {
            switchObject.transform.DOLocalRotate(new Vector3(RotateAngle, 0, 0), 0.2f);
            isPressed = true;
        }

        if (stationManager != null)
        {
            stationManager.RegisterSwitchPressRPC(myLabelColor);
        }
    }

    private void ResetButton()
    {
        if (switchObject != null)
        {
            switchObject.transform.DOLocalRotate(Vector3.zero, 0.2f);
            isPressed = false;
        }
    }

    private void OnMouseDown()
    {
        if (isPressed || (moduleInteractable != null && !moduleInteractable.IsInteracting())) return;

        OnClicked();

    }

}