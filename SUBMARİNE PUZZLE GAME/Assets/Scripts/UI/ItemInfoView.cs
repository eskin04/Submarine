using PurrNet;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ItemInfoView : View
{
    [Header("Battery UI")]
    [SerializeField] private Slider batteryBar;

    [Header("Color Settings")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Gradient batteryColorGradient;

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<ItemInfoView>();
    }

    public override void OnHide()
    {
    }

    public override void OnShow()
    {
    }

    public void UpdateBatteryUI(float fillAmount)
    {
        if (batteryBar != null)
        {
            batteryBar.DOValue(fillAmount, 0.4f);

            // Renk geçişini DOTween ile yumuşak bir şekilde yapıyoruz
            if (fillImage != null)
            {
                fillImage.DOColor(batteryColorGradient.Evaluate(fillAmount), 0.4f);
            }
        }
    }
}