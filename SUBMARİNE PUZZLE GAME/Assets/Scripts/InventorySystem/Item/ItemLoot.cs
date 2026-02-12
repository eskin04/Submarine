using System;
using PurrNet;
using UnityEngine;

public class ItemLoot : NetworkBehaviour
{
    public static Action<ItemLoot> OnLootAttempt;

    [SerializeField] private ItemData itemData;

    public ItemData Data => itemData;

    public void LootItem()
    {

        OnLootAttempt?.Invoke(this);

    }
}