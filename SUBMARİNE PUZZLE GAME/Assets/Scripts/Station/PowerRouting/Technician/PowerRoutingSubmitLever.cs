using UnityEngine;
using DG.Tweening;

public class PowerRoutingSubmitLever : MonoBehaviour
{
    [Header("Main Controller")]
    [SerializeField] private PowerRoutingTechnicianVisuals _technicianVisuals;

    [Header("Animation Settings")]
    public Transform leverTransform;
    [SerializeField] private float _animDuration = 0.3f;
    [SerializeField] private Vector3 _leverPullRotation = new Vector3(60f, 0, 0);
    [SerializeField] private Vector3 _leverDefaultRotation = new Vector3(-60f, 0, 0);

    private bool _isAnimating = false;

    public void ResetLever()
    {
        leverTransform.localEulerAngles = _leverDefaultRotation;
        _isAnimating = false;
    }

    public void PullLever()
    {
        if (_technicianVisuals == null || !_technicianVisuals.IsStationActive() || _isAnimating) return;

        _isAnimating = true;

        leverTransform.DOLocalRotate(_leverPullRotation, _animDuration).SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                leverTransform.DOLocalRotate(_leverDefaultRotation, _animDuration).SetDelay(0.5f)
                    .OnComplete(() => _isAnimating = false);
            });

        // FMOD: RuntimeManager.PlayOneShot("event:/Station/Lever_Pull", leverTransform.position);

        _technicianVisuals.NotifySubmitLeverPulled();
    }
}