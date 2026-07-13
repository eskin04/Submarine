using System;
using PurrNet;
using UnityEngine;

public class ItemLoot : NetworkBehaviour
{
    public static Action<ItemLoot> OnLootAttempt;

    [SerializeField] private ItemData itemData;

    public ItemData Data => itemData;

    public bool CanBeLooted = true;
    public bool isInElevator = false;

    public void LootItem()
    {
        if (!CanBeLooted) return;
        OnLootAttempt?.Invoke(this);

    }
}