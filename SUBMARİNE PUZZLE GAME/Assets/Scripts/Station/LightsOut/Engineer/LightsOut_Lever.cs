using UnityEngine;
using PurrNet;
using DG.Tweening;
using System.Collections;

public class LightsOut_Lever : NetworkBehaviour
{
    public LightsOut_StationManager stationManager;
    public GameObject leverHandle;
    public Transform leverDoor;
    public float pullAngle = 45f;
    public float pullDuration = 0.2f;
    public float resetDuration = 0.1f;

    [Header("Audio Settings")]
    public AudioEventChannelSO _channel;
    public FMODUnity.EventReference leverPullSound;
    public FMODUnity.EventReference leverResetSound;
    public FMODUnity.EventReference doorOpenSound;
    public FMODUnity.EventReference doorCloseSound;
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
        PlaySwitchSound(false);
    }

    private void PlaySwitchSound(bool isReset = false)
    {
        if (_channel != null)
        {
            FMODUnity.EventReference soundToPlay = isReset ? leverResetSound : leverPullSound;

            if (!soundToPlay.IsNull)
            {
                AudioEventPayload payload = new AudioEventPayload(soundToPlay, this.transform.position);
                _channel.RaiseEvent(payload);
            }
        }
    }


    public void ResetLever()
    {
        if (leverHandle != null)
        {
            leverHandle.transform.DOLocalRotate(originalRotation, resetDuration).OnComplete(() =>
            {

                DOVirtual.DelayedCall(.7f, () =>
                {
                    ınteractable.SetInteractable(true);
                    ToggleDoor(false);
                });

            });
            PlaySwitchSound(true);


        }

    }


    public void ToggleDoor(bool isOpening)
    {
        // if (!isFirstTime) return;
        if (isOpening)
        {
            leverDoor.DOLocalRotate(new Vector3(0, 180, 90), 0.2f);
            isFirstTime = false;
            PlayDoorSound(true);
        }
        else
        {
            leverDoor.DOLocalRotate(originalDoorRotation, 0.2f);
            PlayDoorSound(false);
        }
    }

    private void PlayDoorSound(bool isOpening)
    {
        if (_channel != null)
        {
            FMODUnity.EventReference soundToPlay = isOpening ? doorOpenSound : doorCloseSound;

            if (!soundToPlay.IsNull)
            {
                AudioEventPayload payload = new AudioEventPayload(soundToPlay, this.transform.position);
                _channel.RaiseEvent(payload);
            }
        }
    }
}