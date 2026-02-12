using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class InventorySlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;

    void Awake()
    {
        ClearSlot();

    }

    public void AddItemToSlot(ItemData newItem)
    {
        if (newItem == null) return;

        iconImage.sprite = newItem.icon;
        iconImage.enabled = true;
    }

    public void ClearSlot()
    {
        iconImage.sprite = null;
        iconImage.enabled = false;
    }

    public void SetHighlight(bool isHighlighted)
    {
        transform.DOScale(isHighlighted ? 1.2f : 1f, 0.2f).SetEase(Ease.OutBack);
    }
}