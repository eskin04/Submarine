using UnityEngine;
using PurrNet;
using DG.Tweening;

public class LightsOut_Lever : NetworkBehaviour
{
    public LightsOut_StationManager stationManager;
    public GameObject leverHandle;
    public Transform leverDoor;
    public float pullAngle = 45f;
    public float pullDuration = 0.2f;
    private Vector3 originalRotation;
    private Vector3 originalDoorRotation;
    private Interactable ınteractable;
    private bool isFirstTime = true;
    private void Start()
    {
        if (leverHandle != null)
        {
            originalRotation = leverHandle.transform.localEulerAngles;
        }
        ınteractable = GetComponent<Interactable>();
        originalDoorRotation = leverDoor.localEulerAngles;

    }

    public void OnPullLever()
    {
        if (stationManager != null)
        {
            stationManager.PullLeverActionRPC();

            if (leverHandle != null)
            {
                leverHandle.transform.DOLocalRotate(new Vector3(0, pullAngle, 0), pullDuration);
                ınteractable.SetInteractable(false);
            }
        }
    }

    public void ResetLever()
    {
        if (leverHandle != null)
        {
            leverHandle.transform.DOLocalRotate(originalRotation, pullDuration).SetDelay(0.2f);
            ınteractable.SetInteractable(true);


        }
    }

    public void ToggleDoor(bool isOpening)
    {
        if (!isFirstTime) return;
        if (isOpening)
        {
            leverDoor.DOLocalRotate(new Vector3(0, 180, 90), 0.2f);
            isFirstTime = false;
        }
        else
        {
            leverDoor.DOLocalRotate(originalDoorRotation, 0.2f);
        }
    }
}