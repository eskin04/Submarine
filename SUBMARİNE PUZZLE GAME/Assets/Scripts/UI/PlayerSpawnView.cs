using PurrLobby;
using TMPro;
using UnityEngine;
using PurrNet;
using DG.Tweening;

public class PlayerSpawnView : View
{
    [SerializeField] private TMP_Text roleText;

    private Sequence tweenSequence;

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<PlayerSpawnView>();
        tweenSequence?.Kill();
    }

    public override void OnShow()
    {
        if (roleText == null) return;

        tweenSequence?.Kill();
        tweenSequence = DOTween.Sequence();

        roleText.alpha = 0f;
        roleText.characterSpacing = 60f;
        roleText.rectTransform.localScale = Vector3.one * 1.15f;

        tweenSequence.Append(roleText.DOFade(1f, 2f).SetEase(Ease.InOutSine))
                     .Join(DOTween.To(() => roleText.characterSpacing, x => roleText.characterSpacing = x, 0f, 2f).SetEase(Ease.OutCubic))
                     .Join(roleText.rectTransform.DOScale(1f, 2f).SetEase(Ease.OutCubic));
    }

    public override void OnHide()
    {
        if (roleText == null) return;

        tweenSequence?.Kill();
        tweenSequence = DOTween.Sequence();

        tweenSequence.Append(roleText.DOFade(0f, 0.2f).SetEase(Ease.OutQuad))
                     .Join(roleText.rectTransform.DOScale(1.1f, 0.2f).SetEase(Ease.OutQuad))
                     .Join(DOTween.To(() => roleText.characterSpacing, x => roleText.characterSpacing = x, 30f, 0.2f).SetEase(Ease.OutQuad));
    }

    public void SetRoleText(PlayerRole role)
    {
        if (roleText != null) //
        {
            roleText.text = role.ToString().ToUpper();
        }
    }
}