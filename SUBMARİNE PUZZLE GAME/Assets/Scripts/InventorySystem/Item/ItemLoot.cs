using System;
using PurrNet;
using UnityEngine;

public class ItemLoot : NetworkBehaviour
{
    public static Func<ItemLoot, bool> OnLootAttempt;

    [SerializeField] private ItemData itemData;

    public ItemData Data => itemData;

    public void LootItem()
    {
        // Eventi tetikle, eğer true dönerse (başarılıysa) kendini kapat
        if (OnLootAttempt != null && OnLootAttempt.Invoke(this))
        {
            // Todo
        }
    }
}