using UnityEngine;
using DG.Tweening;

public class EngineerLockDown_Door : MonoBehaviour
{
    [Header("Door Panel")]
    public Transform doorPanel;

    [Header("Animation Settings")]
    public Vector3 openOffset = new Vector3(-1.5f, 0, 0);
    public float openDuration = 1.0f;
    public float stayOpenTime = 5.0f;
    public Ease doorEase = Ease.InOutSine;

    private Vector3 closedPos;
    private Tween currentTween;
    private bool isDoorOpen = false;

    private void Awake()
    {
        if (doorPanel != null)
            closedPos = doorPanel.localPosition;
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

    private void OpenDoorTemporarily()
    {
        if (doorPanel == null || isDoorOpen) return;

        currentTween?.Kill();
        isDoorOpen = true;
        currentTween = doorPanel.DOLocalMove(closedPos + openOffset, openDuration)
            .SetEase(doorEase)
            .OnComplete(() =>
            {
                currentTween = doorPanel.DOLocalMove(closedPos, openDuration)
                    .SetDelay(stayOpenTime)
                    .SetEase(doorEase).OnComplete(() =>
                    {
                        isDoorOpen = false;
                    });
            });
    }

    private void ForceCloseDoor()
    {
        if (doorPanel == null) return;
        isDoorOpen = false;
        currentTween?.Kill();
        currentTween = doorPanel.DOLocalMove(closedPos, openDuration).SetEase(doorEase);
    }
}