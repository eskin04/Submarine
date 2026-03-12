using UnityEngine;
using DG.Tweening;

public class SecurityLockdown_OverrideDoor : MonoBehaviour
{
    [Header("Door Panel")]
    public Transform doorPanel;

    [Header("Animation Settings")]
    public Vector3 openOffset = new Vector3(-1.5f, 0, 0);
    public float openDuration = 1.5f;
    public Ease doorEase = Ease.InOutSine;

    private Vector3 closedPos;

    private void Awake()
    {
        if (doorPanel != null)
            closedPos = doorPanel.localPosition;

        OpenDoor();
    }

    private void OnEnable()
    {
        SecurityLockdown_StationManager.OnStateChanged += HandleStateChanged;
        SecurityLockdown_StationManager.OnOverrideSolved += OpenDoor;
    }

    private void OnDisable()
    {
        SecurityLockdown_StationManager.OnStateChanged -= HandleStateChanged;
        SecurityLockdown_StationManager.OnOverrideSolved -= OpenDoor;
    }

    private void HandleStateChanged(LockDownStationState state)
    {
        if (state == LockDownStationState.Active)
        {
            CloseDoor();
        }

    }

    [ContextMenu("Test Open Door")]
    private void OpenDoor()
    {
        if (doorPanel != null)
            doorPanel.DOLocalMove(closedPos + openOffset, openDuration).SetEase(doorEase);
    }

    [ContextMenu("Test Close Door")]
    private void CloseDoor()
    {
        if (doorPanel != null)
            doorPanel.DOLocalMove(closedPos, openDuration).SetEase(doorEase);
    }
}