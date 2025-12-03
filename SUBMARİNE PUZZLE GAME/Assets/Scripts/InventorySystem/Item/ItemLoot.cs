using System;
using PurrNet;
using UnityEngine;


public class ItemLoot : NetworkBehaviour
{
    public static Func<ItemData, bool> OnItemLooted;
    [SerializeField] private ItemData itemData;

    [ObserversRpc(runLocally: true)]
    public void LootItem()
    {
        if (OnItemLooted.Invoke(itemData))
        {
            Destroy(gameObject);
        }
    }

}
