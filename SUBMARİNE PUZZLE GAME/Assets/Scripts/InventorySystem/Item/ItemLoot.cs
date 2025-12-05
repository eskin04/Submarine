using System;
using PurrNet;
using UnityEngine;


public class ItemLoot : NetworkBehaviour
{
    public static Func<ItemData, bool> OnItemLooted;
    public static Action OnItemDropped;
    [SerializeField] private ItemData itemData;



    public void LootItem()
    {
        if (OnItemLooted.Invoke(itemData))
        {
            Destroy(gameObject);
        }
    }


}
