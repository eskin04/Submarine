using System;
using UnityEngine;
using PurrNet;

[RequireComponent(typeof(PlayerInventory))]
public class InventoryManager : NetworkBehaviour
{
    public static Action<InventoryManager> OnLocalInventoryReady;
    public static Action<bool> OnEquipChange;

    [Header("Settings")]
    [SerializeField] private int inventorySize = 4;

    private InventoryItemContainer[] containers;
    private int currentSlotIndex = -1;

    private PlayerInventory playerInventory;
    private InventoryUI inventoryUI;
    private IInteractable currentInteractable;
    private ItemSway itemSwayScript;

    [Serializable]
    private class InventoryItemContainer
    {
        public ItemData Data;
        public GameObject PhysicalObject;
        public bool IsEmpty => Data == null || PhysicalObject == null;

        public void Clear()
        {
            Data = null;
            PhysicalObject = null;
        }
    }

    protected override void OnSpawned()
    {
        playerInventory = GetComponent<PlayerInventory>();

        if (playerInventory.HandPosition)
        {
            itemSwayScript = playerInventory.HandPosition.GetComponent<ItemSway>();
        }

        if (!isOwner) return;

        containers = new InventoryItemContainer[inventorySize];
        for (int i = 0; i < inventorySize; i++) containers[i] = new InventoryItemContainer();

        inventoryUI = InstanceHandler.GetInstance<InventoryUI>();

        ItemLoot.OnLootAttempt += HandleLootAttempt;
        LiftManager.OnDropItemToLıft += HandleLiftDrop;
        Interactor.OnInteract += Interactor_OnInteract;
        OnLocalInventoryReady?.Invoke(this);
    }

    protected override void OnDestroy()
    {
        ItemLoot.OnLootAttempt -= HandleLootAttempt;
        LiftManager.OnDropItemToLıft -= HandleLiftDrop;
        Interactor.OnInteract -= Interactor_OnInteract;
        base.OnDestroy();
    }

    private void Interactor_OnInteract(IInteractable ınteractable)
    {
        currentInteractable = ınteractable;
        MonoBehaviour monoObj = ınteractable as MonoBehaviour;
        if (monoObj == null) return;

        if (monoObj.GetComponent<ItemLoot>() != null)
        {
            currentInteractable.StopInteract();
            return;
        }
        if (monoObj.GetComponentInParent<LiftManager>() != null) return;

        UnequipCurrent();
    }

    private void Update()
    {
        if (!isOwner) return;
        if (currentInteractable != null && currentInteractable.IsInteracting()) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipItem(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipItem(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipItem(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) EquipItem(3);
        if (Input.GetKeyDown(KeyCode.G)) DropCurrentItem();
    }


    // =================================================================================================
    // PICKUP
    // =================================================================================================


    private bool HandleLootAttempt(ItemLoot loot)
    {
        if (!isOwner || loot == null) return false;
        if (Vector3.Distance(transform.position, loot.transform.position) > 5f) return false;

        int emptySlot = GetFirstEmptySlot();
        if (emptySlot == -1) return false;

        PickupServerRpc(loot.gameObject, emptySlot);
        return true;
    }

    [ServerRpc(runLocally: true, requireOwnership: false)]
    private void PickupServerRpc(GameObject itemObj, int slotIndex)
    {
        if (itemObj == null) return;

        var netObj = itemObj.GetComponent<NetworkTransform>();
        if (netObj != null) netObj.GiveOwnership(owner);


        SetItemSettings(itemObj, true);
        if (isServer) ObserversPickupRpc(itemObj);


        if (playerInventory.HandPosition)
        {
            itemObj.transform.SetParent(playerInventory.HandPosition);
            itemObj.transform.localPosition = Vector3.zero;
            itemObj.transform.localRotation = Quaternion.identity;
        }

        if (isOwner)
        {
            var loot = itemObj.GetComponent<ItemLoot>();

            if (loot != null && loot.Data != null)
            {
                containers[slotIndex].Data = loot.Data;
                containers[slotIndex].PhysicalObject = itemObj;
                if (inventoryUI) inventoryUI.UpdateSlot(slotIndex, loot.Data);
            }
        }

    }

    [ObserversRpc]
    private void ObserversPickupRpc(GameObject itemObj)
    {
        SetItemSettings(itemObj, true);

    }


    // =================================================================================================
    // EQUIP
    // =================================================================================================

    public void EquipItem(int index)
    {
        if (!isOwner || index < 0 || index >= inventorySize) return;

        if (currentSlotIndex == index)
        {
            UnequipCurrent();
            return;
        }

        UnequipCurrent();

        var container = containers[index];
        if (!container.IsEmpty)
        {
            currentSlotIndex = index;
            if (inventoryUI) inventoryUI.HighlightSlot(index);

            GameObject itemObj = container.PhysicalObject;

            itemObj.SetActive(true);

            if (itemSwayScript) itemSwayScript.SetActiveItem(true);
            var itemLogic = itemObj.GetComponent<IInventoryItem>();
            itemLogic?.OnEquip();


            OnEquipChange?.Invoke(true);
        }
    }

    private void UnequipCurrent()
    {
        if (currentSlotIndex != -1)
        {
            var container = containers[currentSlotIndex];
            if (!container.IsEmpty)
            {
                var itemObj = container.PhysicalObject;

                var itemLogic = itemObj.GetComponent<IInventoryItem>();
                itemLogic?.OnUnequip();

                itemObj.SetActive(false);
            }
            if (itemSwayScript) itemSwayScript.SetActiveItem(false);
            if (inventoryUI) inventoryUI.HighlightSlot(-1);
            currentSlotIndex = -1;
            OnEquipChange?.Invoke(false);
        }
    }

    // =================================================================================================
    //  DROP
    // =================================================================================================

    private void DropCurrentItem()
    {
        if (currentSlotIndex != -1 && !containers[currentSlotIndex].IsEmpty)
        {
            GameObject itemToDrop = containers[currentSlotIndex].PhysicalObject;

            Vector3 targetPos = itemToDrop.transform.position;
            Quaternion targetRot = itemToDrop.transform.rotation;

            if (playerInventory.DropPosition)
            {
                targetPos = playerInventory.DropPosition.position;
                targetRot = playerInventory.DropPosition.rotation;
            }

            DropServerRpc(itemToDrop, null, targetPos, targetRot);
        }
    }

    private void HandleLiftDrop(Transform liftTransform, float range)
    {
        if (!isOwner || currentSlotIndex == -1) return;

        var container = containers[currentSlotIndex];
        if (!container.IsEmpty)
        {
            float rndX = UnityEngine.Random.Range(-range, range);
            Vector3 worldPos = liftTransform.TransformPoint(new Vector3(rndX, 0.5f, 0));

            DropServerRpc(container.PhysicalObject, liftTransform.gameObject, worldPos, Quaternion.identity);
        }
    }

    [ServerRpc(runLocally: true)]
    private void DropServerRpc(GameObject itemObj, GameObject parentObj, Vector3 pos, Quaternion rot)
    {
        if (itemObj == null) return;

        itemObj.transform.SetParent(parentObj != null ? parentObj.transform : null);
        itemObj.transform.position = pos;
        itemObj.transform.rotation = rot;


        var netObj = itemObj.GetComponent<NetworkTransform>();
        if (netObj != null) netObj.RemoveOwnership();

        SetItemSettings(itemObj, false);
        if (isServer) ObserversDropRpc(itemObj);

        var rb = itemObj.GetComponent<Rigidbody>();

        if (parentObj == null && rb != null)
        {
            rb.AddForce((transform.forward + Vector3.up * 0.25f) * 3f, ForceMode.Impulse);
        }


        var itemLogic = itemObj.GetComponent<IInventoryItem>();
        itemLogic?.OnDrop();

        if (isOwner) RemoveCurrentItem();

    }

    [ObserversRpc]
    private void ObserversDropRpc(GameObject itemObj)
    {
        SetItemSettings(itemObj, false);
    }


    // =================================================================================================
    // HELPERS
    // =================================================================================================


    private void SetItemSettings(GameObject itemObj, bool isPickedUp)
    {

        var netTransform = itemObj.GetComponent<NetworkTransform>();
        if (netTransform) netTransform.enabled = !isPickedUp;

        itemObj.SetActive(!isPickedUp);

        var rb = itemObj.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = isPickedUp;
            rb.useGravity = !isPickedUp;
        }

        var col = itemObj.GetComponent<Collider>();
        if (col) col.enabled = !isPickedUp;

        var loot = itemObj.GetComponent<ItemLoot>();
        if (loot) loot.enabled = !isPickedUp;
    }

    private int GetFirstEmptySlot()
    {
        for (int i = 0; i < inventorySize; i++)
        {
            if (containers[i].IsEmpty) return i;
        }
        return -1;
    }

    private void RemoveCurrentItem()
    {
        if (currentSlotIndex != -1)
        {
            containers[currentSlotIndex].Clear();
            if (inventoryUI)
            {
                inventoryUI.ClearSlot(currentSlotIndex);
                inventoryUI.HighlightSlot(-1);
            }
            currentSlotIndex = -1;
            OnEquipChange?.Invoke(false);
        }
    }
}