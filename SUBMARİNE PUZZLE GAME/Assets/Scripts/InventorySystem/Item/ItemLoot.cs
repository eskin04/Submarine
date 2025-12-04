using System;
using PurrNet;
using UnityEngine;


public class ItemLoot : NetworkBehaviour
{
    public static Func<ItemData, bool> OnItemLooted;
    public static Action OnItemDropped;
    [SerializeField] private ItemData itemData;
    private Interactable interactableComponent;

    void Awake()
    {
        interactableComponent = GetComponent<Interactable>();
    }

    [ObserversRpc(runLocally: true)]
    public void LootItem()
    {
        interactableComponent.SetInteracting(false);
        if (OnItemLooted.Invoke(itemData))
        {
            Destroy(gameObject);
        }
    }

    public void DropItem()
    {
        OnItemDropped?.Invoke();
    }

}
