using UnityEngine;
using DG.Tweening; // DOTween kütüphanesi eklendi

public class LocalRadioVisuals : MonoBehaviour
{
    public Vector3 hiddenLocalPosition = new Vector3(-0.5f, -1.0f, 1.0f);

    public Vector3 visibleLocalPosition = new Vector3(-0.5f, -0.3f, 1.0f);

    public float animationDuration = 0.35f;

    public Ease showEase = Ease.OutBack;

    public Ease hideEase = Ease.InBack;

    private Tween currentTween;

    void Start()
    {
        transform.localPosition = hiddenLocalPosition;
    }

    void OnEnable()
    {
        if (RadioVoiceManager.Instance != null)
        {
            RadioVoiceManager.Instance.OnRadioStateChanged += HandleRadioState;
        }
    }

    void OnDisable()
    {
        if (RadioVoiceManager.Instance != null)
        {
            RadioVoiceManager.Instance.OnRadioStateChanged -= HandleRadioState;
        }

        currentTween?.Kill();
    }

    private void HandleRadioState(bool isActive)
    {
        currentTween?.Kill();

        if (isActive)
        {
            currentTween = transform.DOLocalMove(visibleLocalPosition, animationDuration).SetEase(showEase);
        }
        else
        {
            currentTween = transform.DOLocalMove(hiddenLocalPosition, animationDuration).SetEase(hideEase);
        }
    }
}