using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Magnetic_ChannelButton : MonoBehaviour
{
    public int channelIndex;

    public Magnetic_WaveOscilloscope oscilloscope;
    public Transform buttonMesh;

    private Vector3 originalLocalPos;
    private bool isLocked = false;
    private bool isActive = false;

    private void Awake()
    {
        if (buttonMesh != null)
            originalLocalPos = buttonMesh.localPosition;
    }

    public void UpdateButtonState(bool active, bool locked)
    {
        isActive = active;
        isLocked = locked;

        if (buttonMesh != null)
        {
            buttonMesh.DOKill();
            if (isActive)
            {
                buttonMesh.DOLocalMove(originalLocalPos + (Vector3.back * 0.02f), 0.2f);
            }
            else
            {
                buttonMesh.DOLocalMove(originalLocalPos, 0.2f);
            }

            bool canBeHighlighted = !isLocked && !isActive;

            if (HighlightManager.Instance != null)
            {
                HighlightManager.Instance.SetInteractableState(buttonMesh.gameObject, canBeHighlighted);
            }
        }
    }

    private void OnMouseDown()
    {
        if (oscilloscope == null) return;

        if (isLocked)
        {

            // RuntimeManager.PlayOneShot("event:/UI/Error_Buzzer");
            return;
        }

        if (isActive) return;

        // RuntimeManager.PlayOneShot("event:/UI/Button_Press");

        oscilloscope.ChangeViewedChannel(channelIndex);
    }
}