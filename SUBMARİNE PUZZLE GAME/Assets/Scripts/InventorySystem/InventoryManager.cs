using System;
using UnityEngine;
using PurrNet;
using PurrLobby;

[RequireComponent(typeof(PlayerInventory))]
public class InventoryManager : NetworkBehaviour
{
    public static Action<InventoryManager> OnLocalInventoryReady;
    public static Action<bool> OnEquipChange;

    [Header("Settings")]
    [SerializeField] private int inventorySize = 4;
    [SerializeField] private float maxDropInteractionDistance = 2.0f;

    [Header("Starting Items")]
    [SerializeField] private GameObject handbookPrefab;

    private InventoryItemContainer[] containers;
    private int currentSlotIndex = -1;

    private PlayerInventory playerInventory;
    private InventoryUI inventoryUI;
    private IInteractable currentInteractable;
    private IInteractable currentFocusedInteractable;
    private ItemSway itemSwayScript;
    private bool isHeldItemHidden = false;

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
        Interactor.OnInteractableChanged += Interactor_OnInteractableChanged;
        OnLocalInventoryReady?.Invoke(this);

        HandleStartingItems();

        EquipSlot(0);
    }

    protected override void OnDestroy()
    {
        ItemLoot.OnLootAttempt -= HandleLootAttempt;
        LiftManager.OnDropItemToLıft -= HandleLiftDrop;
        Interactor.OnInteract -= Interactor_OnInteract;
        Interactor.OnInteractableChanged -= Interactor_OnInteractableChanged;
        base.OnDestroy();
    }


    private void Interactor_OnInteractableChanged(IInteractable ınteractable)
    {

        currentFocusedInteractable = ınteractable;
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

        if (!containers[currentSlotIndex].IsEmpty)
        {
            SetCurrentItemVisibility(false);
            isHeldItemHidden = true;
        }

        HandleFlashlightLight(true);
    }

    private void Update()
    {
        if (!isOwner) return;



        if (currentInteractable != null && !currentInteractable.IsInteracting())
        {
            HandleFlashlightLight(false);

            if (isHeldItemHidden)
            {
                SetCurrentItemVisibility(true);
                isHeldItemHidden = false;
            }
            currentInteractable = null;

        }
        if (currentInteractable != null && currentInteractable.IsInteracting()) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) EquipSlot(3);


        if (Input.GetKeyDown(KeyCode.G)) DropCurrentItem();
    }

    // =================================================================================================
    // STARTING ITEMS
    // =================================================================================================

    private void HandleStartingItems()
    {

        if (handbookPrefab != null)
        {
            GameObject newItem = Instantiate(handbookPrefab);

            int targetSlot = inventorySize - 1;

            PickupServerRpc(newItem, targetSlot);
        }
    }

    // =================================================================================================
    // SLOT MANAGEMENT
    // =================================================================================================


    public void EquipSlot(int index)
    {
        if (!isOwner || index < 0 || index >= inventorySize) return;

        if (currentSlotIndex == index) return;

        HideCurrentItem();

        currentSlotIndex = index;
        if (inventoryUI) inventoryUI.HighlightSlot(index);

        RefreshActiveSlot();
    }


    private void RefreshActiveSlot()
    {
        if (currentSlotIndex == -1) return;
        var container = containers[currentSlotIndex];

        if (!container.IsEmpty)
        {
            GameObject itemObj = container.PhysicalObject;
            itemObj.SetActive(true);

            if (itemSwayScript) itemSwayScript.SetActiveItem(true);

            var itemLogic = itemObj.GetComponent<IInventoryItem>();
            itemLogic?.OnEquip();

            OnEquipChange?.Invoke(true);
        }
        else
        {
            if (itemSwayScript) itemSwayScript.SetActiveItem(false);
            OnEquipChange?.Invoke(false);
        }
    }

    private void HideCurrentItem()
    {
        if (currentSlotIndex == -1) return;

        var container = containers[currentSlotIndex];
        if (!container.IsEmpty)
        {
            GameObject itemObj = container.PhysicalObject;

            var itemLogic = itemObj.GetComponent<IInventoryItem>();
            itemLogic?.OnUnequip();

            itemObj.SetActive(false);
        }
    }




    // =================================================================================================
    // PICKUP
    // =================================================================================================


    private void HandleLootAttempt(ItemLoot loot)
    {
        if (!isOwner || loot == null) return;
        if (Vector3.Distance(transform.position, loot.transform.position) > 5f) return;

        int targetSlot = -1;

        if (containers[currentSlotIndex].IsEmpty)
        {
            targetSlot = currentSlotIndex;
        }
        else
        {
            targetSlot = GetFirstEmptySlot();
        }

        if (targetSlot == -1) return;

        PickupServerRpc(loot.gameObject, targetSlot);
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
            ItemLoot lootComponent = itemObj.GetComponent<ItemLoot>();
            if (lootComponent)
            {
                itemObj.transform.localPosition = lootComponent.Data.positionOffset;
                itemObj.transform.localRotation = Quaternion.Euler(lootComponent.Data.rotationOffset);
            }
            else
            {
                itemObj.transform.localPosition = Vector3.zero;
                itemObj.transform.localRotation = Quaternion.identity;
            }
        }

        if (isOwner)
        {
            var loot = itemObj.GetComponent<ItemLoot>();

            if (loot != null && loot.Data != null)
            {
                containers[slotIndex].Data = loot.Data;
                containers[slotIndex].PhysicalObject = itemObj;
                if (inventoryUI) inventoryUI.UpdateSlot(slotIndex, loot.Data);
                itemObj.SetActive(false);
                RefreshActiveSlot();
            }



        }

    }

    [ObserversRpc]
    private void ObserversPickupRpc(GameObject itemObj)
    {
        SetItemSettings(itemObj, true);

    }




    // =================================================================================================
    //  DROP
    // =================================================================================================

    private void DropCurrentItem()
    {
        if (currentFocusedInteractable != null && currentFocusedInteractable.transform.GetComponentInParent<LiftManager>() != null) return;
        if (currentSlotIndex != -1 && !containers[currentSlotIndex].IsEmpty)
        {
            GameObject itemToDrop = containers[currentSlotIndex].PhysicalObject;

            Vector3 finalPos;
            Quaternion finalRot = itemToDrop.transform.rotation;
            var lookHit = Interactor.CurrentLookHit;
            bool isValidHit = lookHit.HasValue && lookHit.Value.distance <= maxDropInteractionDistance;
            if (isValidHit)
            {
                RaycastHit hit = lookHit.Value;
                finalPos = hit.point + (hit.normal * 0.3f);
            }

            else
            {
                // --- BOŞLUĞA BAKIYORUZ ---
                if (playerInventory.DropPosition)
                {
                    finalPos = playerInventory.DropPosition.position;
                    finalRot = playerInventory.DropPosition.rotation;
                }
                else
                {
                    finalPos = transform.position + transform.forward * 1.5f;
                }
            }

            DropServerRpc(itemToDrop, null, finalPos, finalRot);
        }
    }

    private void HandleLiftDrop(Transform liftTransform, float range)
    {
        if (!isOwner || currentSlotIndex == -1) return;

        var container = containers[currentSlotIndex];
        if (!container.IsEmpty)
        {
            float rndX = UnityEngine.Random.Range(-range, range);
            Vector3 worldPos = liftTransform.TransformPoint(new Vector3(rndX, 0.3f, -.3f));

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
        if (!isOwner)
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
                inventoryUI.HighlightSlot(currentSlotIndex);
            }
            OnEquipChange?.Invoke(false);
        }
    }

    private void SetCurrentItemVisibility(bool isVisible)
    {
        if (currentSlotIndex == -1) return;
        var container = containers[currentSlotIndex];
        if (container.IsEmpty || container.PhysicalObject == null) return;

        Renderer[] renderers = container.PhysicalObject.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers) rend.enabled = isVisible;

        Canvas[] canvases = container.PhysicalObject.GetComponentsInChildren<Canvas>();
        foreach (var canvas in canvases) canvas.enabled = isVisible;
    }

    private void HandleFlashlightLight(bool isInteracting)
    {
        if (currentSlotIndex == -1 || containers[currentSlotIndex].IsEmpty) return;

        var flashlight = containers[currentSlotIndex].PhysicalObject.GetComponent<FlashlightItem>();

        if (flashlight != null)
        {
            Transform targetCam = isInteracting ? playerInventory.InteractCameraTrans : null;
            flashlight.SetInteractionMode(isInteracting, targetCam);
        }
    }
}