using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject highlight;


    // Başlangıç ayarı
    private void Start()
    {
        ClearSlot();
    }

    // Kutuya eşya koy
    public void AddItemToSlot(ItemData newItem)
    {
        iconImage.sprite = newItem.icon;
        iconImage.enabled = true;

    }

    // Kutuyu boşalt
    public void ClearSlot()
    {
        iconImage.sprite = null;
        iconImage.enabled = false;
        SetHighlight(false);
    }

    public void SetHighlight(bool isHighlighted)
    {
        highlight.SetActive(isHighlighted);
    }

}