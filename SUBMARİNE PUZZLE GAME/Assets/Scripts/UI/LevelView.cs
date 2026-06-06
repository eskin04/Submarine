using TMPro;
using UnityEngine;
using PurrNet;
using DG.Tweening;

public class LevelView : View
{
    [SerializeField] private TMP_Text levelText;

    private Sequence tweenSequence;

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<LevelView>();
        tweenSequence?.Kill();
    }

    public override void OnShow()
    {
        if (levelText == null) return;

        tweenSequence?.Kill();
        tweenSequence = DOTween.Sequence();

        levelText.alpha = 0f;
        levelText.rectTransform.anchoredPosition = new Vector2(0, -500f);
        levelText.characterSpacing = 40f;
        levelText.rectTransform.localScale = Vector3.one * 0.9f;

        float animationDuration = 2.5f;
        tweenSequence.Append(levelText.DOFade(1f, animationDuration).SetEase(Ease.InOutSine))
                     .Join(levelText.rectTransform.DOAnchorPosY(-300f, animationDuration).SetEase(Ease.OutCubic))
                     .Join(DOTween.To(() => levelText.characterSpacing, x => levelText.characterSpacing = x, 0f, animationDuration).SetEase(Ease.OutCubic))
                     .Join(levelText.rectTransform.DOScale(1f, animationDuration).SetEase(Ease.OutBack));
    }

    public override void OnHide()
    {
        if (levelText == null) return;

        tweenSequence?.Kill();
        tweenSequence = DOTween.Sequence();

        tweenSequence.Append(levelText.DOFade(0f, 0.5f).SetEase(Ease.OutQuad))
                     .Join(levelText.rectTransform.DOScale(1.1f, 0.5f).SetEase(Ease.OutQuad))
                     .Join(DOTween.To(() => levelText.characterSpacing, x => levelText.characterSpacing = x, 20f, 0.5f).SetEase(Ease.OutQuad));
    }

    public void SetLevelText(int level)
    {
        if (levelText != null)
        {
            Debug.Log($"Setting Level Text: Level {level}");
            levelText.text = "LEVEL " + level.ToString();
        }
    }
}