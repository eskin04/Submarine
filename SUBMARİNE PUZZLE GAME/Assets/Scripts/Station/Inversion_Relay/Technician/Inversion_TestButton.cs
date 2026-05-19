using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Inversion_TestButton : MonoBehaviour
{
    [Header("References")]
    public Inversion_Relay_StationManager stationManager;
    public Inversion_ValidationSwitch validationSwitch;

    public Transform buttonMesh;

    [Header("Animation Settings")]
    public Vector3 pressedOffset = new Vector3(0, -0.05f, 0);
    public float pressDuration = 0.15f;

    private Vector3 originalPosition;
    private bool isAnimating = false;
    private Interactable interactable;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        if (buttonMesh != null)
            originalPosition = buttonMesh.localPosition;
    }

    public void ClickTestButton()
    {
        if (!stationManager.isRoundActive.value ||
             stationManager.isTesting.value ||
             isAnimating)
            return;
        interactable.SetInteractable(false);

        PressAnim();

        stationManager.PressTestButtonRPC();
        validationSwitch.SetDisplayName(true);
    }

    public void OnTestComplete()
    {
        interactable.SetInteractable(true);
        validationSwitch.SetDisplayName(false);
    }

    private void PressAnim()
    {
        if (buttonMesh == null) return;

        isAnimating = true;
        buttonMesh.DOKill();

        buttonMesh.DOLocalMove(originalPosition + pressedOffset, pressDuration).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            buttonMesh.DOLocalMove(originalPosition, pressDuration).SetEase(Ease.InQuad).OnComplete(() =>
            {
                isAnimating = false;
            });
        });
    }
}