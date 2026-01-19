using UnityEngine;
using PurrNet;
using DG.Tweening;

public class LightsOut_Lever : NetworkBehaviour
{
    public LightsOut_StationManager stationManager;
    public GameObject leverHandle;
    public float pullAngle = 45f;
    public float pullDuration = 0.2f;
    private Vector3 originalRotation;
    private void Start()
    {
        if (leverHandle != null)
        {
            originalRotation = leverHandle.transform.localEulerAngles;
        }
    }

    public void OnPullLever()
    {
        if (stationManager != null)
        {
            stationManager.PullLeverActionRPC();

            if (leverHandle != null)
            {
                leverHandle.transform.DOLocalRotate(new Vector3(pullAngle, originalRotation.y, originalRotation.z), pullDuration);
            }
        }
    }

    public void ResetLever()
    {
        if (leverHandle != null)
        {
            leverHandle.transform.DOLocalRotate(originalRotation, pullDuration).SetDelay(0.2f);

        }
    }
}