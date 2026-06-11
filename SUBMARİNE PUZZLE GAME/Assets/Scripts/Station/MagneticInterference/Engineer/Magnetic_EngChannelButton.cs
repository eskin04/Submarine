using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Magnetic_EngChannelButton : MonoBehaviour
{
    public int channelIndex;

    public Magnetic_EngineerUI engineerUI;
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
        }
    }

    private void OnMouseDown()
    {
        if (engineerUI == null) return;

        if (isLocked)
        {

            return;
        }

        if (isActive) return;

        engineerUI.ChangeViewedChannel(channelIndex);
    }
}