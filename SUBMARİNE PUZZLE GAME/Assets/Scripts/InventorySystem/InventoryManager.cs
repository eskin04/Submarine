using System;
using System.Collections.Generic;
using UnityEngine;
using PurrNet;

public class InventoryManager : NetworkBehaviour
{
    public static Action<bool> OnItemEquipped;
    [SerializeField] private InventorySlot[] inventorySlots;
    private ItemData[] items;
    private Transform playerHandPosition;
    private Transform playerDropPosition;
    private GameObject currentEquippedItem;
    private ItemData currentEquippedItemData;
    private Interactable currentInteractable;
    private int currentSlotIndex = -1;


    void Awake()
    {
        ItemLoot.OnItemLooted += AddItem;
        ItemLoot.OnItemDropped += DropCurrentItem;
        PlayerInventory.OnSpawnPlayer += (handPos, dropPos) => HandlePlayerSpawn(handPos, dropPos);
    }

    private void Start()
    {

        items = new ItemData[inventorySlots.Length];
    }

    protected override void OnDestroy()
    {
        ItemLoot.OnItemLooted -= AddItem;
        ItemLoot.OnItemDropped -= DropCurrentItem;
        PlayerInventory.OnSpawnPlayer -= (handPos, dropPos) => HandlePlayerSpawn(handPos, dropPos);
    }

    private void HandlePlayerSpawn(Transform handPos, Transform dropPos)
    {
        playerHandPosition = handPos;
        playerDropPosition = dropPos;
        Debug.Log("Player inventory positions set.");
    }

    private void UnEquipCurrentItem()
    {
        if (currentEquippedItem != null)
        {
            OnItemEquipped?.Invoke(false);
            Destroy(currentEquippedItem);
            currentEquippedItem = null;
            currentEquippedItemData = null;
            currentInteractable = null;
            inventorySlots[currentSlotIndex].SetHighlight(false);
            currentSlotIndex = -1;
        }
    }

    private bool AddItem(ItemData itemData)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                items[i] = itemData;
                inventorySlots[i].AddItemToSlot(itemData);
                Debug.Log($"Item {itemData.itemName} added to inventory.");
                return true;
            }
        }
        return false;
    }

    private void EquipSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= items.Length || items[slotIndex] == null) return;
        if (items[slotIndex] == currentEquippedItemData)
        {
            UnEquipCurrentItem();
            return;
        }

        EquipItem(items[slotIndex]);
        HighlightSlot(slotIndex);

    }

    private void HighlightSlot(int slotIndex)
    {


        if (currentSlotIndex >= 0 && currentSlotIndex < inventorySlots.Length)
        {
            inventorySlots[currentSlotIndex].SetHighlight(false);
        }

        inventorySlots[slotIndex].SetHighlight(true);
        currentSlotIndex = slotIndex;
    }



    public void EquipItem(ItemData itemData)
    {
        if (playerHandPosition == null || playerDropPosition == null) return;
        if (currentEquippedItem != null) Destroy(currentEquippedItem);
        OnItemEquipped?.Invoke(true);
        currentEquippedItemData = itemData;
        currentEquippedItem = Instantiate(itemData.prefab, playerHandPosition.position, playerHandPosition.rotation, playerHandPosition);
        currentInteractable = currentEquippedItem.GetComponent<Interactable>();
        currentInteractable.SetInteracting(true);
        var rb = currentEquippedItem.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        var collider = currentEquippedItem.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;


    }
    [ObserversRpc(runLocally: true)]
    private void DropCurrentItem()
    {
        if (currentEquippedItem != null && playerDropPosition != null)
        {
            currentEquippedItem.transform.SetParent(null);
            currentEquippedItem.transform.position = playerDropPosition.position;
            OnItemEquipped?.Invoke(false);
            var rb = currentEquippedItem.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            var collider = currentEquippedItem.GetComponent<Collider>();
            if (collider != null) collider.enabled = true;

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == currentEquippedItemData)
                {
                    Debug.Log($"Dropped item {currentEquippedItemData.itemName} from inventory.");
                    items[i] = null;
                    inventorySlots[i].ClearSlot();
                    break;
                }
            }

            currentEquippedItem = null;
            currentEquippedItemData = null;
            currentInteractable = null;

        }
    }

    void Update()
    {
        if (currentInteractable != null && Input.GetKeyDown(KeyCode.Escape) && currentInteractable.IsInteract())
        {
            currentInteractable.StopInteract();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) EquipSlot(3);
    }
    public ItemData[] GetItems()
    {
        return items;
    }







}
