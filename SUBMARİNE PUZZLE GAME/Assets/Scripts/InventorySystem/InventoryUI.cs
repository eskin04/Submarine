using PurrNet;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private InventorySlot[] slots;



    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
        if (slots == null || slots.Length == 0)
        {
            slots = GetComponentsInChildren<InventorySlot>(true);
        }


    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<InventoryUI>();
    }

    public void UpdateSlot(int index, ItemData data)
    {
        if (IsValidIndex(index))
        {
            slots[index].AddItemToSlot(data);
        }
    }

    public void ClearSlot(int index)
    {
        if (IsValidIndex(index))
        {
            slots[index].ClearSlot();
        }
    }

    public void HighlightSlot(int index)
    {
        foreach (var slot in slots) slot.SetHighlight(false);

        if (IsValidIndex(index))
        {
            slots[index].SetHighlight(true);
        }
    }

    public int GetSlotCount() => slots.Length;

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < slots.Length;
    }
}