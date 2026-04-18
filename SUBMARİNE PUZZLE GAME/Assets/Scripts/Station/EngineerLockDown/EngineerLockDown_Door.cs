using UnityEngine;
using DG.Tweening;
using FMODUnity;
using PurrNet;

public class EngineerLockDown_Door : NetworkBehaviour
{
    [Header("Door Panel")]
    public Transform doorPanel;

    [Header("Animation Settings")]
    public Vector3 openOffset = new Vector3(-1.5f, 0, 0);
    public float openDuration = 1.0f;
    public float closeDuration = 1.0f;
    public Ease doorEase = Ease.InOutSine;

    [Header("Audio Settings")]
    [SerializeField] private AudioEventChannelSO _channel;
    [SerializeField] private EventReference _doorOpenSound;
    [SerializeField] private EventReference _doorCloseSound;


    private Vector3 closedPos;
    private Tween currentTween;
    private bool isDoorOpen = true;

    private void Awake()
    {
        if (doorPanel != null)
        {
            closedPos = doorPanel.localPosition;
            doorPanel.localPosition = closedPos + openOffset;
        }

    }

    private void OnEnable()
    {
        EngineerLockDown_StationManager.OnOverrideStateChanged += HandleStateChanged;
        EngineerLockDown_StationManager.OnEngineerDoorRequested += OpenDoorTemporarily;
    }

    private void OnDisable()
    {
        EngineerLockDown_StationManager.OnOverrideStateChanged -= HandleStateChanged;
        EngineerLockDown_StationManager.OnEngineerDoorRequested -= OpenDoorTemporarily;
    }

    private void HandleStateChanged(EngineerLockDownStationState state)
    {
        if (state == EngineerLockDownStationState.Active)
        {
            ForceCloseDoor();
        }
    }

    private void OpenDoorTemporarily(float stayOpenTime)
    {
        if (doorPanel == null || isDoorOpen) return;
        PlayDoorSound(true);
        currentTween?.Kill();
        isDoorOpen = true;
        currentTween = doorPanel.DOLocalMove(closedPos + openOffset, openDuration)
            .SetEase(doorEase)
            .OnComplete(() =>
            {

                currentTween = doorPanel.DOLocalMove(closedPos, closeDuration)
                    .SetDelay(stayOpenTime)
                    .SetEase(doorEase).OnComplete(() =>
                    {
                        PlayDoorSound(false);
                        isDoorOpen = false;
                    });
            });
    }

    private void PlayDoorSound(bool opening)
    {
        if (_channel != null)
        {
            EventReference soundToPlay = opening ? _doorOpenSound : _doorCloseSound;
            if (!soundToPlay.IsNull)
            {
                AudioEventPayload payload = new AudioEventPayload(soundToPlay, doorPanel.position);
                _channel.RaiseEvent(payload);
            }
        }
    }

    private void ForceCloseDoor()
    {
        if (doorPanel == null) return;
        isDoorOpen = false;
        currentTween?.Kill();
        PlayDoorSound(false);
        currentTween = doorPanel.DOLocalMove(closedPos, closeDuration).SetEase(doorEase);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            transform.GetComponent<Collider>().enabled = false;
            CloseDoorRpc();
        }
    }

    [ObserversRpc]
    private void CloseDoorRpc()
    {
        transform.GetComponent<Collider>().enabled = false;
        Invoke(nameof(ForceCloseDoor), 1f);
    }
}