using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject highlight;

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
        SetHighlight(false);
    }

    public void SetHighlight(bool isHighlighted)
    {
        if (highlight != null)
            highlight.SetActive(isHighlighted);
    }
}