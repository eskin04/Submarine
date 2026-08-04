using TMPro;
using UnityEngine;
using DG.Tweening;
using PurrNet;


public class ModuleInfoView : View
{
    [SerializeField] private TMP_Text warningText;

    [Header("Animation Settings")]
    [SerializeField] private float yOffset = 30f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float displayDuration = 1f;

    private RectTransform _warningRect;
    private Vector2 _originalPos;
    private Sequence _warningSequence;

    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
        if (warningText != null)
        {
            _warningRect = warningText.GetComponent<RectTransform>();
            _originalPos = _warningRect.anchoredPosition;

            warningText.alpha = 0f;
        }
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<ModuleInfoView>();
        _warningSequence?.Kill();
    }

    public override void OnHide()
    {
        _warningSequence?.Kill();
        if (warningText != null) warningText.alpha = 0f;
    }

    public override void OnShow()
    {
    }

    public void SetWarningText(string text)
    {
        if (warningText == null || _warningRect == null) return;

        _warningSequence?.Kill();

        warningText.text = text;

        _warningRect.anchoredPosition = _originalPos - new Vector2(0, yOffset);
        warningText.alpha = 0f;

        _warningSequence = DOTween.Sequence();

        _warningSequence.Append(_warningRect.DOAnchorPos(_originalPos, fadeDuration).SetEase(Ease.OutBack));
        _warningSequence.Join(warningText.DOFade(1f, fadeDuration));

        _warningSequence.AppendInterval(displayDuration);

        _warningSequence.Append(warningText.DOFade(0f, fadeDuration));
    }
}