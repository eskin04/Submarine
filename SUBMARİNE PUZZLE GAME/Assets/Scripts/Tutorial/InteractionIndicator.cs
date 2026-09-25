using UnityEngine;
using DG.Tweening;
using PurrNet;

public class InteractionIndicator : MonoBehaviour
{
    public enum Axis { X, Y, Z }
    [Header("Animation Settings")]
    [SerializeField] private Axis bounceAxis = Axis.Y;
    [SerializeField] private float moveDistance = 0.3f;
    [SerializeField] private float duration = 0.8f;

    [Header("Rotation Settings")]
    [SerializeField] private Axis frontAxis = Axis.Z;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    private Vector3 startLocalPos;
    private Tween bounceTween;
    private Camera mainCam;
    private Vector3 startScale;
    private Tween scaleTween;

    private void Awake()
    {
        startLocalPos = transform.localPosition;
        startScale = transform.localScale;
        mainCam = Camera.main;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        if (!InstanceHandler.TryGetInstance<TutorialQuestView>(out _)) return;
        if (gameObject.activeSelf) return;

        gameObject.SetActive(true);
        transform.localPosition = startLocalPos;
        Vector3 targetPos = startLocalPos;
        switch (bounceAxis)
        {
            case Axis.X: targetPos.x += moveDistance; break;
            case Axis.Y: targetPos.y += moveDistance; break;
            case Axis.Z: targetPos.z += moveDistance; break;
        }

        bounceTween = transform.DOLocalMove(targetPos, duration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        scaleTween = transform.DOScale(startScale * 1.25f, duration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void Hide()
    {
        bounceTween?.Kill();
        scaleTween?.Kill();
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!gameObject.activeSelf) return;
        if (mainCam != null)
        {
            Vector3 camForward = mainCam.transform.forward;
            Quaternion targetRot = Quaternion.identity;

            switch (frontAxis)
            {
                case Axis.Z:
                    targetRot = Quaternion.LookRotation(camForward, Vector3.up);
                    break;
                case Axis.Y:
                    targetRot = Quaternion.LookRotation(Vector3.up, camForward);
                    break;
                case Axis.X:
                    targetRot = Quaternion.LookRotation(Vector3.up, -camForward);
                    break;
            }

            transform.rotation = targetRot * Quaternion.Euler(rotationOffset);
        }
    }

    private void OnDestroy()
    {
        bounceTween?.Kill();
        scaleTween?.Kill();

    }
}