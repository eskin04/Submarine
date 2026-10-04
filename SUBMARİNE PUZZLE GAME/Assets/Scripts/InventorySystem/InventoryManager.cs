using System;
using UnityEngine;
using PurrNet;
using PurrLobby;
using DG.Tweening;
using System.Collections;
using PurrNet.Packing;
using PurrNet.Modules;

[RequireComponent(typeof(PlayerInventory))]
public class InventoryManager : NetworkBehaviour
{
    public static Action<InventoryManager> OnLocalInventoryReady;
    public static Action<bool> OnEquipChange;
    public bool IsScrollLocked { get; set; } = false;

    [Header("Settings")]
    [SerializeField] private int inventorySize = 4;
    [SerializeField] private float maxDropInteractionDistance = 2.0f;

    [Header("Starting Items")]
    [SerializeField] private GameObject handbookPrefab;

    private InventoryItemContainer[] containers;
    private SyncVar<ulong> inventoryVersion = new SyncVar<ulong>();
    // Owner selection is a visual hint only; possession remains host-written truth.
    private SyncVar<ItemIdentityHandle> selectedItem = new SyncVar<ItemIdentityHandle>(ownerAuth: true);
    public ulong InventoryVersion => inventoryVersion.value;
    private int currentSlotIndex = -1;

    private PlayerInventory playerInventory;
    private InventoryUI inventoryUI;
    private IInteractable currentInteractable;
    private IInteractable currentFocusedInteractable;
    private ItemSway itemSwayScript;
    private bool isHeldItemHidden = false;
    private ItemLoot equippedPresentation;
    private ItemLoot remoteHeldPresentation;
    public static InventoryManager LocalPlayer { get; private set; }

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

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        if (!asServer) return;

        InitializeContainers();
        inventoryVersion.value = ItemLoot.NextStateVersion(this);
    }

    private void InitializeContainers()
    {
        containers = new InventoryItemContainer[inventorySize];
        for (int i = 0; i < inventorySize; i++) containers[i] = new InventoryItemContainer();
    }

    protected override void OnSpawned()
    {
        playerInventory = GetComponent<PlayerInventory>();
        if (playerInventory.HandPosition)
        {
            itemSwayScript = playerInventory.HandPosition.GetComponent<ItemSway>();
        }
        if (isOwner)
        {
            LocalPlayer = this;
            selectedItem.value = default;
        }


        if (!isOwner) return;

        // Host already allocated this same array on its server-side spawn.
        // Remote owners keep presentation membership in this same container shape.
        if (!isServer) InitializeContainers();

        inventoryUI = InstanceHandler.GetInstance<InventoryUI>();

        ItemLoot.OnLootAttempt += HandleLootAttempt;
        LiftManager.OnDropItemToLıft += HandleLiftDrop;
        Interactor.OnInteract += Interactor_OnInteract;
        Interactor.OnInteractableChanged += Interactor_OnInteractableChanged;
        CameraLayerController.OnInteractionStarted += SetInteractItemParent;
        CameraLayerController.OnInteractionEnded += SetNormalItemParent;
        OnLocalInventoryReady?.Invoke(this);

        HandleStartingItems();

        EquipSlot(0);
    }

    protected override void OnDestroy()
    {
        forcedPage = null;
        ItemLoot.OnLootAttempt -= HandleLootAttempt;
        LiftManager.OnDropItemToLıft -= HandleLiftDrop;
        Interactor.OnInteract -= Interactor_OnInteract;
        Interactor.OnInteractableChanged -= Interactor_OnInteractableChanged;
        CameraLayerController.OnInteractionStarted -= SetInteractItemParent;
        CameraLayerController.OnInteractionEnded -= SetNormalItemParent;
        base.OnDestroy();
    }

    protected override void OnDespawned(bool asServer)
    {
        ClearRemoteHeldPresentation();
        if (asServer && containers != null)
        {
            for (int slot = 0; slot < containers.Length; slot++)
            {
                var obj = containers[slot].PhysicalObject;
                var item = obj ? obj.GetComponent<ItemLoot>() : null;
                if (!item || !item.isSpawned || item.Possession.Location != ItemSharedLocation.Inventory ||
                    !item.Possession.Context.Matches(this) || item.Possession.Slot != slot) continue;
                ulong stamp = ReleaseHeldServer(item, slot);
                item.SetPossessionServer(ItemSharedLocation.World, null, -1, stamp);
                var nt = item.GetComponent<NetworkTransform>();
                if (nt) { nt.RemoveOwnership(); nt.StopIgnoringParentChanges(); }
                item.transform.SetParent(null);
                item.SetPhysicalState(ItemSharedLocation.World);
                item.SetVisible(true);
                if (nt) nt.ForceSync();
            }
        }
        base.OnDespawned(asServer);
    }

    protected override void OnDespawned()
    {
        StopAllCoroutines();
        forcedPage = null;
        resolvingPageDelivery = false;
        pendingItem = null; pendingOperation = InventoryTransferOperation.None; transferReplied = false; waitingForPickup = false; resolvingForcedPickup = false;
        equippedPresentation?.GetComponent<IInventoryItem>()?.OnUnequip();
        equippedPresentation = null;
        currentSlotIndex = -1;
        if (LocalPlayer == this) LocalPlayer = null;
        ItemLoot.OnLootAttempt -= HandleLootAttempt;
        LiftManager.OnDropItemToLıft -= HandleLiftDrop;
        Interactor.OnInteract -= Interactor_OnInteract;
        Interactor.OnInteractableChanged -= Interactor_OnInteractableChanged;
        CameraLayerController.OnInteractionStarted -= SetInteractItemParent;
        CameraLayerController.OnInteractionEnded -= SetNormalItemParent;
        base.OnDespawned();
    }

    private void SetInteractItemParent()
    {
        if (containers == null || currentSlotIndex < 0 || currentSlotIndex >= containers.Length) return;
        var container = containers[currentSlotIndex];
        if (!container.IsEmpty && container.PhysicalObject != null)
        {
            var itemObj = container.PhysicalObject;
            itemObj.transform.SetParent(playerInventory.InteractCameraTrans);
            ItemLoot lootComponent = itemObj.GetComponent<ItemLoot>();
            if (lootComponent)
            {
                itemObj.transform.DOLocalMove(lootComponent.Data.positionOffsetInInteract, 0.5f);
                itemObj.transform.DOLocalRotate(Vector3.zero, 0.5f);
            }
            else
            {
                itemObj.transform.localPosition = Vector3.zero;
                itemObj.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private void SetNormalItemParent()
    {
        if (containers == null || currentSlotIndex < 0 || currentSlotIndex >= containers.Length) return;
        var container = containers[currentSlotIndex];
        if (!container.IsEmpty && container.PhysicalObject != null)
        {

            var itemObj = container.PhysicalObject;
            DOTween.Kill(itemObj.transform);
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
        if (containers == null || currentSlotIndex < 0 || currentSlotIndex >= containers.Length) return;

        if (!containers[currentSlotIndex].IsEmpty)
        {
            SetCurrentItemVisibility(false);
            isHeldItemHidden = true;
        }

    }

    private void Update()
    {
        if (!isOwner)
        {
            ApplyRemoteHeldPresentation();
            return;
        }
        FinishPending();
        PublishSelectedItem();
        if (containers == null || currentSlotIndex < 0) return;
        if (currentInteractable != null && !currentInteractable.IsInteracting())
        {

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

        if (!IsScrollLocked)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var view))
                {
                    view.OnActionPerformed(TutorialAction.CycleInventory);
                }
                int nextSlot = currentSlotIndex;

                if (scroll > 0f)
                {
                    nextSlot = currentSlotIndex <= 0 ? inventorySize - 1 : currentSlotIndex - 1;
                }
                else if (scroll < 0f)
                {
                    nextSlot = currentSlotIndex == -1 ? 0 : (currentSlotIndex + 1) % inventorySize;
                }

                if (nextSlot != currentSlotIndex) EquipSlot(nextSlot);
            }
        }


        if (Input.GetKeyDown(KeyCode.G)) DropCurrentItem();
    }

    public GameObject GetCurrentHeldObject()
    {
        if (containers == null || currentSlotIndex < 0 || currentSlotIndex >= containers.Length) return null;
        return containers[currentSlotIndex].PhysicalObject;
    }

    public void ExtractCurrentHeldItem()
    {
        if (!isOwner) { Debug.LogWarning("Extraction requires exact owner input context."); return; }
        var item = GetCurrentHeldObject();
        if (item) ReleaseItemFromInventory(item, false);
    }

    // =================================================================================================
    // STARTING ITEMS
    // =================================================================================================

    private void HandleStartingItems()
    {
        if (TutorialManager.Instance != null || handbookPrefab == null) return;
        StartCoroutine(WaitForPickup(Instantiate(handbookPrefab).GetComponent<ItemLoot>(), inventorySize - 1));
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
        PublishSelectedItem();
        if (pendingOperation == InventoryTransferOperation.Pickup && pendingItem && pendingItem.Possession.Version == pendingItemVersion)
            pendingItem.SetVisible(index == pendingSlot);
    }


    private void RefreshActiveSlot()
    {
        if (containers == null || currentSlotIndex < 0 || currentSlotIndex >= containers.Length) return;
        var container = containers[currentSlotIndex];

        if (!container.IsEmpty)
        {
            GameObject itemObj = container.PhysicalObject;
            if (pendingOperation != InventoryTransferOperation.None && pendingItem && itemObj == pendingItem.gameObject) return;
            itemObj.GetComponent<ItemLoot>().SetVisible(true);

            if (itemSwayScript) itemSwayScript.SetActiveItem(true);

            var itemLogic = itemObj.GetComponent<IInventoryItem>();
            if (equippedPresentation != itemObj.GetComponent<ItemLoot>())
            {
                if (equippedPresentation) equippedPresentation.GetComponent<IInventoryItem>()?.OnUnequip();
                equippedPresentation = itemObj.GetComponent<ItemLoot>();
                itemLogic?.OnEquip();
            }

            if (itemObj.GetComponent<Handbook>() == null)
            {
                OnEquipChange?.Invoke(true);
                if (InstanceHandler.TryGetInstance<PromptView>(out var promptView))
                    promptView.AddPrompt("item_drop", "G", "Drop The Item");
            }
            else
            {
                OnEquipChange?.Invoke(false);
                itemObj.GetComponent<Handbook>().SetInspectPosition(playerInventory.InspectPosition);
            }
        }
        else
        {
            if (itemSwayScript) itemSwayScript.SetActiveItem(false);
            OnEquipChange?.Invoke(false);
        }
    }

    private void HideCurrentItem()
    {
        if (containers == null || currentSlotIndex < 0 || currentSlotIndex >= containers.Length) return;
        var container = containers[currentSlotIndex];
        if (!container.IsEmpty)
        {
            IsScrollLocked = false;
            GameObject itemObj = container.PhysicalObject;

            var itemLogic = itemObj.GetComponent<IInventoryItem>();
            if (equippedPresentation == itemObj.GetComponent<ItemLoot>())
            {
                itemLogic?.OnUnequip();
                equippedPresentation = null;
            }

            itemObj.GetComponent<ItemLoot>().SetVisible(false);
            if (InstanceHandler.TryGetInstance<PromptView>(out var promptView))
                promptView.RemovePrompt("item_drop");
        }
    }

    private void PublishSelectedItem()
    {
        if (!isSpawned || !isOwner) return;
        var obj = GetCurrentHeldObject();
        var item = obj ? obj.GetComponent<ItemLoot>() : null;
        ItemIdentityHandle handle = default;
        if (item && item.IsPossessionInitialized && item.Possession.Location == ItemSharedLocation.Inventory &&
            item.Possession.Context.Matches(this)) ItemIdentityHandle.TryCreate(item, out handle);
        // SyncVar compares values: unchanged selection produces no per-frame traffic.
        selectedItem.value = handle;
    }

    private bool IsAcceptedHeldItem(ItemLoot item)
    {
        return isSpawned && item && item.IsPossessionInitialized &&
            item.Possession.Location == ItemSharedLocation.Inventory && item.Possession.Context.Matches(this);
    }

    private void ClearRemoteHeldPresentation()
    {
        // A transferred item is now World/Socket/Detached presentation, not ours to hide.
        if (IsAcceptedHeldItem(remoteHeldPresentation)) remoteHeldPresentation.SetVisible(false, false);
        remoteHeldPresentation = null;
    }

    private void ApplyRemoteHeldPresentation()
    {
        if (!isSpawned || isOwner) return;
        var item = selectedItem.value.Resolve<ItemLoot>(this);
        if (!IsAcceptedHeldItem(item)) item = null;
        if (remoteHeldPresentation != item) ClearRemoteHeldPresentation();
        if (!item) return;
        if (remoteHeldPresentation == item)
        {
            // Another inventory's rejected preview may have hidden this accepted item.
            if (!item.IsPresentationVisible) item.SetVisible(true, false);
            return;
        }
        if (!playerInventory) playerInventory = GetComponent<PlayerInventory>();
        var hand = playerInventory.HandPosition;
        if (!hand || !item.Data) return;

        item.SetPhysicalState(ItemSharedLocation.Inventory);
        // Held parent changes are already suppressed by the accepted physical profile.
        item.transform.SetParent(hand);
        item.transform.localPosition = item.Data.positionOffset;
        item.transform.localRotation = Quaternion.Euler(item.Data.rotationOffset);
        item.SetVisible(true, false);
        remoteHeldPresentation = item;
    }


    // =================================================================================================
    // PICKUP
    // =================================================================================================


    private void HandleLootAttempt(ItemLoot loot)
    {
        if (!isOwner || containers == null || loot == null || Vector3.Distance(transform.position, loot.transform.position) > 5f) return;
        int slot = currentSlotIndex >= 0 && containers[currentSlotIndex].IsEmpty ? currentSlotIndex : GetFirstEmptySlot();
        BeginPickup(loot, slot);
    }

    [ServerRpc(runLocally: false, requireOwnership: false)]
    private void PickupServerRpc(ItemIdentityHandle item, int slot, ulong itemVersion, ulong expectedInventory,
        ulong socketVersion, RPCInfo info = default)
    {
        TryPickupServer(info.sender, item, slot, itemVersion, expectedInventory, socketVersion);
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
            Handbook handbookScript = itemToDrop.GetComponent<Handbook>();
            if (handbookScript != null) return;


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

            if (InstanceHandler.TryGetInstance<PromptView>(out var promptView))
                promptView.RemovePrompt("item_drop");

            BeginDrop(itemToDrop.GetComponent<ItemLoot>(), null, finalPos, finalRot);
        }
    }

    private void HandleLiftDrop(Transform liftTransform, float range)
    {
        if (!isOwner || currentSlotIndex == -1) return;

        if (containers == null || currentSlotIndex < 0 || currentSlotIndex >= containers.Length) return;
        var container = containers[currentSlotIndex];
        if (!container.IsEmpty)
        {
            float rndX = UnityEngine.Random.Range(-range, range);
            Vector3 worldPos = liftTransform.TransformPoint(new Vector3(rndX, .5f, 0f));


            BeginDrop(container.PhysicalObject.GetComponent<ItemLoot>(), liftTransform.gameObject, worldPos, Quaternion.identity);

        }
    }

    [ServerRpc(runLocally: false)]
    private void DropServerRpc(ItemIdentityHandle item, int slot, ulong itemVersion, ulong expectedInventory,
        GameObject parentObj, Vector3 pos, Quaternion rot, bool lift, RPCInfo info = default)
    {
        TryDropServer(info.sender, item, slot, itemVersion, expectedInventory, parentObj, pos, rot, lift);
    }


    // =================================================================================================
    // HELPERS
    // =================================================================================================


    private int GetFirstEmptySlot()
    {
        if (containers == null) return -1;
        for (int i = inventorySize - 1; i >= 0; i--)
        {
            if (containers[i].IsEmpty) return i;
        }
        return -1;
    }

    public void RemoveCurrentItem()
    {
        // Owner input compatibility only. Observer callers have been removed.
        if (isOwner) ExtractCurrentHeldItem();
        else Debug.LogWarning("Removal requires exact owner input context.");
    }

    private void SetCurrentItemVisibility(bool isVisible)
    {
        if (containers == null || currentSlotIndex < 0 || currentSlotIndex >= containers.Length) return;
        var container = containers[currentSlotIndex];
        if (container.IsEmpty || container.PhysicalObject == null) return;

        var itemLogic = container.PhysicalObject.GetComponent<IInventoryItem>();
        itemLogic?.CanOperate(isVisible);
    }


    public void ForcePickupClientRpc(GameObject networkedTornPage)
    {
        if (!isServer || !networkedTornPage) return;
        StartCoroutine(SendForcedPickupWhenReady(networkedTornPage.GetComponent<ItemLoot>()));
    }

    private ItemLoot forcedPage;
    private ulong forcedPageVersion;
    private bool resolvingPageDelivery;

    // Synchronous responsibility handoff only. Membership still uses existing accepted transfers.
    internal bool TryBeginForcedPageDelivery(ItemLoot page)
    {
        if (!isServer || !isSpawned || !isActiveAndEnabled || !owner.HasValue || forcedPage ||
            !page || !page.IsPossessionInitialized || page.networkManager != networkManager ||
            page.owner != owner || page.Possession.Location != ItemSharedLocation.World ||
            !ItemIdentityHandle.TryCreate(page, out var handle)) return false;
        forcedPage = page;
        forcedPageVersion = page.Possession.Version;
        StartCoroutine(SendPageDelivery(handle, forcedPageVersion, owner.Value));
        return true;
    }

    private IEnumerator SendPageDelivery(ItemIdentityHandle handle, ulong version, PlayerID recipient)
    {
        yield return null; // Producer has relinquished cleanup before any submission can occur.
        if (IsPageDeliveryAlive(handle, version, recipient))
        {
            if (isOwner) StartCoroutine(ResolvePageDelivery(handle, version));
            else TargetPageDelivery(recipient, handle, version);
        }
        while (IsPageDeliveryAlive(handle, version, recipient)) yield return null;
        if (forcedPage && handle.Matches(forcedPage) && forcedPageVersion == version) forcedPage = null;
    }

    private bool IsPageDeliveryAlive(ItemIdentityHandle handle, ulong version, PlayerID recipient)
    {
        return isSpawned && isActiveAndEnabled && owner == recipient && forcedPage && handle.Matches(forcedPage) &&
            forcedPageVersion == version && forcedPage.Possession.Version == version &&
            forcedPage.Possession.Location == ItemSharedLocation.World && forcedPage.owner == recipient &&
            networkManager.TryGetModule<PlayersManager>(true, out var players) && players.IsPlayerConnected(recipient);
    }

    [TargetRpc]
    private void TargetPageDelivery(PlayerID target, ItemIdentityHandle handle, ulong version)
    {
        if (isOwner) StartCoroutine(ResolvePageDelivery(handle, version));
    }

    private IEnumerator ResolvePageDelivery(ItemIdentityHandle handle, ulong version)
    {
        if (!isOwner || resolvingPageDelivery) { AbandonPageDelivery(handle, version); yield break; }
        resolvingPageDelivery = true;
        double started = Time.realtimeSinceStartupAsDouble;
        ItemLoot page = null;
        while (isSpawned && isActiveAndEnabled && isOwner)
        {
            page = handle.Resolve<ItemLoot>(this);
            if (page && page.Possession.Version > version) break;
            if (page && page.IsPossessionInitialized && page.Possession.Version == version && page.owner == owner &&
                InventoryVersion != 0 && pendingOperation == InventoryTransferOperation.None && !waitingForPickup && !resolvingForcedPickup)
            {
                resolvingPageDelivery = false;
                if (page.Possession.Location == ItemSharedLocation.World)
                {
                    // Pick a CURRENT empty slot/overflow only after existing owner work is idle.
                    TryForcePickup(page.gameObject);
                    yield break;
                }
                break;
            }
            if (Time.realtimeSinceStartupAsDouble - started >= 30) break;
            yield return null;
        }
        resolvingPageDelivery = false;
        AbandonPageDelivery(handle, version);
    }

    private void AbandonPageDelivery(ItemIdentityHandle handle, ulong version)
    {
        if (!isSpawned || !isOwner) return;
        if (isServer) EndPageDelivery(owner.Value, handle, version);
        else AbandonPageDeliveryServerRpc(handle, version);
    }

    [ServerRpc(runLocally: false)]
    private void AbandonPageDeliveryServerRpc(ItemIdentityHandle handle, ulong version, RPCInfo info = default)
    {
        if (info.asServer && info.manager == networkManager) EndPageDelivery(info.sender, handle, version);
    }

    private void EndPageDelivery(PlayerID sender, ItemIdentityHandle handle, ulong version)
    {
        if (!isServer || owner != sender || !forcedPage || !handle.Matches(forcedPage) || forcedPageVersion != version) return;
        Debug.LogWarning("Forced page delivery ended before submission; page remains in World.", this);
        forcedPage = null;
    }

    private bool TryForcePickup(GameObject itemObj)
    {
        if (!isOwner || itemObj == null) return false;

        int targetSlot = GetFirstEmptySlot();

        if (targetSlot == -1)
        {
            Vector3 dropPos;
            Quaternion dropRot = itemObj.transform.rotation;

            if (playerInventory != null && playerInventory.DropPosition != null)
            {
                dropPos = playerInventory.DropPosition.position;
                dropRot = playerInventory.DropPosition.rotation;
            }
            else
            {
                dropPos = transform.position + transform.forward * 1.5f;
            }

            var item = itemObj.GetComponent<ItemLoot>();
            if (!BeginPending(item, 0, InventoryTransferOperation.ForcedOverflow)) return false;
            ItemIdentityHandle.TryCreate(item, out var handle);
            if (isServer) TryForcedFallbackServer(owner.Value, handle, pendingItemVersion, pendingInventoryVersion, dropPos, dropRot);
            else ForcedFallbackServerRpc(handle, pendingItemVersion, pendingInventoryVersion, dropPos, dropRot);

            return false;
        }

        StartCoroutine(WaitForPickup(itemObj.GetComponent<ItemLoot>(), targetSlot));
        return true;
    }


    private ItemLoot pendingItem;
    private int pendingSlot;
    private ulong pendingItemVersion, pendingInventoryVersion, pendingSocketVersion;
    private ItemIdentityHandle pendingDestination;
    private InventoryTransferOperation pendingOperation;
    private bool committing;
    private bool transferReplied;
    private InventoryTransferReply transferReply;

    private bool waitingForPickup;
    private bool resolvingForcedPickup;

    [ServerRpc(runLocally: false)]
    private void ForcedFallbackServerRpc(ItemIdentityHandle item, ulong version, ulong inventory,
        Vector3 position, Quaternion rotation, RPCInfo info = default)
    { TryForcedFallbackServer(info.sender, item, version, inventory, position, rotation); }

    private void TryForcedFallbackServer(PlayerID sender, ItemIdentityHandle handle, ulong version,
        ulong inventory, Vector3 position, Quaternion rotation)
    {
        var item = handle.Resolve<ItemLoot>(this);
        bool accepted = ValidateInventory(sender, 0, inventory) && item && item.owner == sender &&
            item.Possession.Location == ItemSharedLocation.World && item.Possession.Version == version &&
            version != 0 && GetFirstEmptySlot() == -1 &&
            float.IsFinite(position.x) && float.IsFinite(position.y) && float.IsFinite(position.z) &&
            float.IsFinite(rotation.x) && float.IsFinite(rotation.y) && float.IsFinite(rotation.z) && float.IsFinite(rotation.w);
        if (accepted)
        {
            committing = true;
            try
            {
                item.SetPossessionServer(ItemSharedLocation.World, null, -1, ItemLoot.NextStateVersion(this));
                var nt = item.GetComponent<NetworkTransform>();
                if (nt) { nt.RemoveOwnership(); nt.StopIgnoringParentChanges(); }
                item.transform.SetParent(null);
                item.transform.SetPositionAndRotation(position, rotation);
                item.SetPhysicalState(ItemSharedLocation.World);
                item.SetVisible(true);
                if (nt) nt.ForceSync();
                var body = item.GetComponent<Rigidbody>();
                if (body) body.AddForce((transform.forward + Vector3.up * .25f) * 3f, ForceMode.Impulse);
            }
            finally { committing = false; }
        }
        Reply(sender, InventoryTransferOperation.ForcedOverflow, handle, 0, version, inventory, default, 0, accepted);
    }

    private IEnumerator WaitForPickup(ItemLoot item, int slot)
    {
        if (waitingForPickup) yield break;
        waitingForPickup = true;
        while (isSpawned && item && (!item.IsPossessionInitialized || InventoryVersion == 0 || pendingItem))
            yield return null;
        waitingForPickup = false;
        if (isSpawned && item) BeginPickup(item, slot);
    }

    private IEnumerator SendForcedPickupWhenReady(ItemLoot item)
    {
        while (isSpawned && item && !item.IsPossessionInitialized) yield return null;
        if (!isSpawned || !item || !owner.HasValue || !ItemIdentityHandle.TryCreate(item, out var handle)) yield break;
        if (isOwner) TryForcePickup(item.gameObject);
        else TargetForcePickup(owner.Value, handle, item.Possession.Version);
    }

    [TargetRpc]
    private void TargetForcePickup(PlayerID target, ItemIdentityHandle item, ulong version)
    {
        if (isOwner) StartCoroutine(ResolveForcedPickup(item, version));
    }

    private IEnumerator ResolveForcedPickup(ItemIdentityHandle handle, ulong version)
    {
        if (resolvingForcedPickup) yield break;
        resolvingForcedPickup = true;
        ItemLoot item;
        while (isSpawned && (item = handle.Resolve<ItemLoot>(this)) == null) yield return null;
        item = handle.Resolve<ItemLoot>(this);
        while (isSpawned && item && item.Possession.Version < version) yield return null;
        resolvingForcedPickup = false;
        if (isSpawned && item && item.Possession.Version == version) TryForcePickup(item.gameObject);
    }

    private bool BeginPending(ItemLoot item, int slot, InventoryTransferOperation operation, NetworkIdentity destination = null, ulong socketVersion = 0)
    {
        if (!isOwner || pendingOperation != InventoryTransferOperation.None || item == null || !item.IsPossessionInitialized || InventoryVersion == 0 ||
            !ItemIdentityHandle.TryCreate(item, out _) || slot < 0 || slot >= inventorySize) return false;
        pendingItem = item; pendingSlot = slot; pendingOperation = operation;
        pendingItemVersion = item.Possession.Version; pendingInventoryVersion = InventoryVersion;
        pendingSocketVersion = socketVersion;
        ItemIdentityHandle.TryCreate(destination, out pendingDestination);
        transferReplied = false;
        // Immediate visual feedback; containers, ownership and shared physics stay accepted.
        item.SetVisible(false);
        if (operation != InventoryTransferOperation.Pickup && currentSlotIndex == slot && containers[slot].PhysicalObject == item.gameObject) HideCurrentItem();
        if (operation == InventoryTransferOperation.Pickup && !isServer && currentSlotIndex == slot)
            item.PreviewPickup(playerInventory.HandPosition);
        return true;
    }

    private void BeginPickup(ItemLoot item, int slot)
    {
        ulong sourceVersion = 0;
        if (item != null && item.Possession.Location == ItemSharedLocation.Socket)
        {
            var charge = item.Possession.Context.Resolve<HullBreach_ChargeStation>(this);
            var card = item.Possession.Context.Resolve<Keycard_Socket>(this);
            sourceVersion = charge ? charge.Occupancy.Version : card ? card.Occupancy.Version : 0;
        }
        if (!BeginPending(item, slot, InventoryTransferOperation.Pickup, null, sourceVersion)) return;
        ItemIdentityHandle.TryCreate(item, out var handle);
        if (isServer) TryPickupServer(owner.Value, handle, slot, pendingItemVersion, pendingInventoryVersion, sourceVersion);
        else PickupServerRpc(handle, slot, pendingItemVersion, pendingInventoryVersion, sourceVersion);
    }

    private void BeginDrop(ItemLoot item, GameObject parent, Vector3 pos, Quaternion rot)
    {
        if (!BeginPending(item, currentSlotIndex, InventoryTransferOperation.Drop)) return;
        ItemIdentityHandle.TryCreate(item, out var handle);
        if (isServer) TryDropServer(owner.Value, handle, pendingSlot, pendingItemVersion, pendingInventoryVersion, parent, pos, rot, parent != null);
        else DropServerRpc(handle, pendingSlot, pendingItemVersion, pendingInventoryVersion, parent, pos, rot, parent != null);
    }

    public bool ReleaseItemFromInventory(GameObject itemObj, bool page = true)
    {
        var item = itemObj ? itemObj.GetComponent<ItemLoot>() : null;
        int slot = FindItemSlot(itemObj);
        if (!BeginPending(item, slot, page ? InventoryTransferOperation.PageRelease : InventoryTransferOperation.Extract)) return false;
        ItemIdentityHandle.TryCreate(item, out var handle);
        if (isServer) TryExtractServer(owner.Value, handle, slot, pendingItemVersion, pendingInventoryVersion, page);
        else ExtractServerRpc(handle, slot, pendingItemVersion, pendingInventoryVersion, page);
        return true;
    }

    [ServerRpc(runLocally: false)]
    private void ExtractServerRpc(ItemIdentityHandle item, int slot, ulong version, ulong inventory, bool page, RPCInfo info = default)
    {
        TryExtractServer(info.sender, item, slot, version, inventory, page);
    }

    private bool ValidateInventory(PlayerID sender, int slot, ulong expected)
    {
        return isServer && isSpawned && !committing && owner.HasValue && owner.Value == sender &&
            containers != null && slot >= 0 && slot < containers.Length && expected != 0 && InventoryVersion == expected;
    }

    internal bool ValidateHeldServer(PlayerID sender, ItemLoot item, int slot, ulong version, ulong inventory)
    {
        return ValidateInventory(sender, slot, inventory) && item && item.isSpawned && version != 0 &&
            item.Possession.Version == version && item.Possession.Location == ItemSharedLocation.Inventory &&
            item.Possession.Context.Matches(this) && item.Possession.Slot == slot &&
            containers[slot].PhysicalObject == item.gameObject;
    }

    internal ulong ReleaseHeldServer(ItemLoot item, int slot)
    {
        // Caller has validated exact membership and destination before entering the commit.
        ulong stamp = ItemLoot.NextStateVersion(this);
        containers[slot].Clear();
        inventoryVersion.value = stamp;
        return stamp;
    }

    private void TryPickupServer(PlayerID sender, ItemIdentityHandle handle, int slot, ulong version, ulong inventory, ulong socketVersion)
    {
        var item = handle.Resolve<ItemLoot>(this);
        bool accepted = false;
        bool fromLift = item && item.isInElevator;
        if (ValidateInventory(sender, slot, inventory) && item && item.sceneId == sceneId && item.Data && item.IsPossessionInitialized &&
            GetComponent<PlayerInventory>().HandPosition != null &&
            item.Possession.Version == version && version != 0 && containers[slot].PhysicalObject == null &&
            (Vector3.Distance(transform.position, item.transform.position) <= 5f || item.GetComponent<Handbook>() || item.owner == owner))
        {
            var charge = item.Possession.Context.Resolve<HullBreach_ChargeStation>(this);
            var card = item.Possession.Context.Resolve<Keycard_Socket>(this);
            bool source = item.Possession.Location == ItemSharedLocation.World || item.Possession.Location == ItemSharedLocation.Detached ||
                (item.Possession.Location == ItemSharedLocation.Socket &&
                 ((charge && charge.CanReleaseServer(item, socketVersion)) || (card && card.CanReleaseServer(item, socketVersion))));
            var printedPlate = item.GetComponent<HullBreach_PlateItem>();
            bool validFoundry = !printedPlate || !printedPlate.SourceFoundry || printedPlate.SourceFoundry.IsCurrentPlate(printedPlate);
            if (source && validFoundry && (item.CanBeLooted || item.GetComponent<Handbook>()))
            {
                committing = true;
                try
                {
                    ulong stamp = ItemLoot.NextStateVersion(this);
                    containers[slot].PhysicalObject = item.gameObject; containers[slot].Data = item.Data;
                    inventoryVersion.value = stamp;
                    item.SetPossessionServer(ItemSharedLocation.Inventory, this, slot, stamp);
                    var nt = item.GetComponent<NetworkTransform>();
                    if (nt) { nt.StartIgnoringParentChanges(); nt.GiveOwnership(owner); }
                    item.SetPhysicalState(ItemSharedLocation.Inventory);
                    item.transform.SetParent(GetComponent<PlayerInventory>().HandPosition);
                    item.transform.localPosition = item.Data.positionOffset;
                    item.transform.localRotation = Quaternion.Euler(item.Data.rotationOffset);
                    item.isInElevator = false;
                    if (charge && charge.CanReleaseServer(item, socketVersion)) charge.ReleaseServer(item, stamp);
                    if (card && card.CanReleaseServer(item, socketVersion)) card.ReleaseServer(item, stamp);
                    item.GetComponent<HullBreach_PlateItem>()?.NotifyPickupAcceptedServer();
                    if (fromLift) LiftManager.OnItemInElevator?.Invoke(false);
                    accepted = true;
                }
                finally { committing = false; }
            }
        }
        Reply(sender, InventoryTransferOperation.Pickup, handle, slot, version, inventory, default, socketVersion, accepted, fromLift);
    }

    private void TryDropServer(PlayerID sender, ItemIdentityHandle handle, int slot, ulong version, ulong inventory,
        GameObject parent, Vector3 pos, Quaternion rot, bool lift)
    {
        var item = handle.Resolve<ItemLoot>(this);
        bool accepted = false;
        if (ValidateHeldServer(sender, item, slot, version, inventory) &&
            (!lift || parent != null) && (lift || parent == null) &&
            (parent != null || !item.GetComponent<Handbook>()) &&
            (parent == null || parent.GetComponentInParent<LiftManager>() != null) &&
            float.IsFinite(pos.x) && float.IsFinite(pos.y) && float.IsFinite(pos.z) &&
            float.IsFinite(rot.x) && float.IsFinite(rot.y) && float.IsFinite(rot.z) && float.IsFinite(rot.w))
        {
            committing = true;
            try
            {
                ulong stamp = ReleaseHeldServer(item, slot);
                item.SetPossessionServer(ItemSharedLocation.World, null, -1, stamp);
                var nt = item.GetComponent<NetworkTransform>();
                if (nt) { nt.RemoveOwnership(); nt.StopIgnoringParentChanges(); }
                item.transform.SetParent(parent ? parent.transform : null);
                item.transform.SetPositionAndRotation(pos, rot);
                item.SetPhysicalState(ItemSharedLocation.World);
                item.SetVisible(true);
                item.isInElevator = parent != null;
                if (nt) nt.ForceSync();
                var rb = item.GetComponent<Rigidbody>();
                if (parent == null && rb) rb.AddForce((transform.forward + Vector3.up * .5f) * 2f, ForceMode.Impulse);
                if (parent) LiftManager.OnItemInElevator?.Invoke(true);
                accepted = true;
            }
            finally { committing = false; }
        }
        Reply(sender, InventoryTransferOperation.Drop, handle, slot, version, inventory, default, 0, accepted, lift);
    }

    private void TryExtractServer(PlayerID sender, ItemIdentityHandle handle, int slot, ulong version, ulong inventory, bool page)
    {
        var item = handle.Resolve<ItemLoot>(this);
        bool accepted = ValidateHeldServer(sender, item, slot, version, inventory) && (!page || item.GetComponent<TornPageItem>());
        if (accepted)
        {
            committing = true;
            try
            {
                ulong stamp = ReleaseHeldServer(item, slot);
                item.SetPossessionServer(ItemSharedLocation.Detached, null, -1, stamp);
                if (!page) item.SetPhysicalState(ItemSharedLocation.Detached);
            }
            finally { committing = false; }
        }
        Reply(sender, page ? InventoryTransferOperation.PageRelease : InventoryTransferOperation.Extract, handle, slot, version, inventory, default, 0, accepted, false);
    }

    internal void Reply(PlayerID sender, InventoryTransferOperation operation, ItemIdentityHandle handle, int slot, ulong version,
        ulong inventory, ItemIdentityHandle destination, ulong socketVersion, bool accepted, bool lift = false)
    {
        if (forcedPage && handle.Matches(forcedPage) && version == forcedPageVersion && owner == sender &&
            (operation == InventoryTransferOperation.Pickup || operation == InventoryTransferOperation.ForcedOverflow))
        {
            // Settle bootstrap on the existing exact response; never undo its accepted commit.
            forcedPage = null;
            if (!accepted) Debug.LogWarning("Forced page transfer rejected; page retains accepted World state.", this);
        }
        var item = handle.Resolve<ItemLoot>(this);
        var reply = new InventoryTransferReply { Operation = operation, Item = handle, Slot = slot,
            ExpectedItem = version, ExpectedInventory = inventory, Destination = destination, ExpectedSocket = socketVersion,
            Accepted = accepted, InventoryVersion = InventoryVersion, ItemVersion = item ? item.Possession.Version : 0,
            CurrentPossession = item ? item.Possession : default, Lift = lift,
            WorldPosition = item ? item.transform.position : default, WorldRotation = item ? item.transform.rotation : Quaternion.identity };
        if (containers != null && slot >= 0 && slot < containers.Length && containers[slot].PhysicalObject)
        {
            ItemIdentityHandle.TryCreate(containers[slot].PhysicalObject.GetComponent<ItemLoot>(), out reply.CurrentSlotItem);
            reply.CurrentSlotVersion = containers[slot].PhysicalObject.GetComponent<ItemLoot>().Possession.Version;
        }
        if (isOwner && owner == sender) ApplyTransferReply(reply);
        else TargetTransferReply(sender, reply);
    }

    [TargetRpc]
    private void TargetTransferReply(PlayerID target, InventoryTransferReply reply) { ApplyTransferReply(reply); }

    private void ApplyTransferReply(InventoryTransferReply reply)
    {
        if (!isOwner || !pendingItem || !reply.Item.Matches(pendingItem) || reply.Operation != pendingOperation ||
            reply.Slot != pendingSlot || reply.ExpectedItem != pendingItemVersion ||
            reply.ExpectedInventory != pendingInventoryVersion || reply.ExpectedSocket != pendingSocketVersion ||
            !reply.Destination.Equals(pendingDestination)) return;
        // A repeated rejection must not replace the accepted response for this exact pending intent.
        if (transferReplied && transferReply.Accepted) return;
        transferReply = reply; transferReplied = true;
    }

    private void FinishPending()
    {
        if (pendingOperation == InventoryTransferOperation.None) return;
        if (!pendingItem) { pendingOperation = InventoryTransferOperation.None; transferReplied = false; return; }
        if (!transferReplied) return;
        if (InventoryVersion < transferReply.InventoryVersion || pendingItem.Possession.Version < transferReply.ItemVersion) return;
        if (!isServer && pendingItem.Possession.Location == ItemSharedLocation.Inventory &&
            pendingItem.Possession.Context.Matches(this) && pendingItem.owner != owner) return;
        if (!isServer && pendingItem.Possession.Location == ItemSharedLocation.World &&
            pendingItem.Possession.Version != pendingItemVersion && pendingItem.owner.HasValue) return;
        var slotItem = transferReply.CurrentSlotItem.Resolve<ItemLoot>(this);
        if (InventoryVersion == transferReply.InventoryVersion && transferReply.CurrentSlotItem.Identity.HasValue &&
            (!slotItem || slotItem.Possession.Version < transferReply.CurrentSlotVersion)) return;
        var item = pendingItem; var reply = transferReply;
        pendingItem = null; pendingOperation = InventoryTransferOperation.None; transferReplied = false;
        if (InventoryVersion == reply.InventoryVersion && slotItem &&
            slotItem.Possession.Location == ItemSharedLocation.Inventory && slotItem.Possession.Context.Matches(this) &&
            slotItem.Possession.Slot == reply.Slot) ApplyItemHeld(slotItem, reply.Slot);
        item.RestoreAcceptedPresentation();
        if (!isServer && item.Possession.Location == ItemSharedLocation.World &&
            item.Possession.Version == reply.ItemVersion)
        {
            item.transform.SetPositionAndRotation(reply.WorldPosition, reply.WorldRotation);
            var nt = item.GetComponent<NetworkTransform>();
            // All current item prefabs use PurrNet World position/rotation sync.
            if (nt) nt.ClearInterpolation(reply.WorldPosition, reply.WorldRotation, item.transform.localScale);
        }
        if (reply.Accepted && reply.Operation == InventoryTransferOperation.Drop && item.Possession.Version == reply.ItemVersion)
            item.GetComponent<IInventoryItem>()?.OnDrop();
        if (reply.Accepted && reply.Lift && InstanceHandler.TryGetInstance<TutorialQuestView>(out var view))
            view.OnActionPerformed(reply.Operation == InventoryTransferOperation.Pickup ? TutorialAction.PickUpItem : TutorialAction.UseElevator);
        if (item.Possession.Location == ItemSharedLocation.Inventory && item.Possession.Context.Matches(this))
            ApplyItemHeld(item, item.Possession.Slot);
        else item.SetVisible(item.Possession.Location != ItemSharedLocation.Inventory);
        if (!reply.Accepted && reply.Operation == InventoryTransferOperation.PageRelease && equippedPresentation == item)
            item.GetComponent<TornPageItem>()?.OnEquip();
    }

    private int FindItemSlot(GameObject item)
    {
        if (containers == null || item == null) return -1;
        for (int i = 0; i < containers.Length; i++) if (containers[i].PhysicalObject == item) return i;
        return -1;
    }

    internal void ApplyItemRelease(ItemLoot item, int slot)
    {
        if (!isOwner)
        {
            if (remoteHeldPresentation == item) ClearRemoteHeldPresentation();
            return;
        }
        if (!isOwner || containers == null || slot < 0 || slot >= containers.Length) return;
        if (!isServer && containers[slot].PhysicalObject == item.gameObject) containers[slot].Clear();
        if (containers[slot].PhysicalObject == null && inventoryUI) inventoryUI.ClearSlot(slot);
        if (currentSlotIndex == slot && containers[slot].PhysicalObject == null)
        {
            if (equippedPresentation == item)
            {
                item.GetComponent<IInventoryItem>()?.OnUnequip();
                equippedPresentation = null;
            }
            RefreshActiveSlot();
        }
    }

    internal void ApplyItemHeld(ItemLoot item, int slot)
    {
        item.SetPhysicalState(ItemSharedLocation.Inventory);
        if (!isOwner)
        {
            item.SetVisible(false, false);
            // Reapplication must also reconstruct the selected item after state replay.
            if (remoteHeldPresentation == item) remoteHeldPresentation = null;
            ApplyRemoteHeldPresentation();
            return;
        }
        if (containers == null || slot < 0 || slot >= containers.Length) return;

        if (!isServer) { containers[slot].PhysicalObject = item.gameObject; containers[slot].Data = item.Data; }
        var hand = GetComponent<PlayerInventory>().HandPosition;
        if (item.transform.parent == null || (item.transform.parent != hand &&
            item.transform.parent != playerInventory.InteractCameraTrans && item.transform.parent != playerInventory.InspectPosition))
        {
            item.transform.SetParent(hand);
            item.transform.localPosition = item.Data.positionOffset;
            item.transform.localRotation = Quaternion.Euler(item.Data.rotationOffset);
        }
        item.SetVisible(currentSlotIndex == slot && (pendingItem != item || pendingOperation == InventoryTransferOperation.Pickup));
        if (inventoryUI) inventoryUI.UpdateSlot(slot, item.Data);
        if (currentSlotIndex == slot && equippedPresentation != item) RefreshActiveSlot();
    }

    internal void ForgetDespawnedItem(ItemLoot item, int slot)
    {
        if (isServer && containers != null && slot >= 0 && slot < containers.Length &&
            containers[slot].PhysicalObject == item.gameObject) ReleaseHeldServer(item, slot);
    }
    public bool PlaceInChargeStation(HullBreach_ChargeStation socket)
    {
        var obj = GetCurrentHeldObject();
        var item = obj ? obj.GetComponent<ItemLoot>() : null;
        if (!socket || !item || !item.GetComponent<HullBreach_DrillItem>() ||
            !BeginPending(item, FindItemSlot(obj), InventoryTransferOperation.ChargeInsertion, socket, socket.Occupancy.Version)) return false;
        ItemIdentityHandle.TryCreate(item, out var handle);
        if (isServer) TryPlaceChargeServer(owner.Value, handle, pendingSlot, pendingItemVersion, pendingInventoryVersion, pendingDestination, pendingSocketVersion);
        else PlaceChargeServerRpc(handle, pendingSlot, pendingItemVersion, pendingInventoryVersion, pendingDestination, pendingSocketVersion);
        return true;
    }

    [ServerRpc(runLocally: false)]
    private void PlaceChargeServerRpc(ItemIdentityHandle item, int slot, ulong version, ulong inventory,
        ItemIdentityHandle socket, ulong socketVersion, RPCInfo info = default)
    { TryPlaceChargeServer(info.sender, item, slot, version, inventory, socket, socketVersion); }

    private void TryPlaceChargeServer(PlayerID sender, ItemIdentityHandle handle, int slot, ulong version,
        ulong inventory, ItemIdentityHandle destination, ulong socketVersion)
    {
        var item = handle.Resolve<ItemLoot>(this);
        var socket = destination.Resolve<HullBreach_ChargeStation>(this);
        bool accepted = socket && socket.sceneId == sceneId && ValidateHeldServer(sender, item, slot, version, inventory) &&
            socket.CanAcceptServer(item, socketVersion);
        if (accepted)
        {
            committing = true;
            try
            {
                ulong stamp = ReleaseHeldServer(item, slot);
                item.SetPossessionServer(ItemSharedLocation.Socket, socket, -1, stamp);
                socket.OccupyServer(item, stamp);
            }
            finally { committing = false; }
        }
        Reply(sender, InventoryTransferOperation.ChargeInsertion, handle, slot, version, inventory, destination, socketVersion, accepted);
    }

    public bool PlaceInKeycardSocket(Keycard_Socket socket)
    {
        var obj = GetCurrentHeldObject();
        var item = obj ? obj.GetComponent<ItemLoot>() : null;
        if (!socket || !item || !item.GetComponent<Keycard_Item>() ||
            !BeginPending(item, FindItemSlot(obj), InventoryTransferOperation.KeycardInsertion, socket, socket.Occupancy.Version)) return false;
        ItemIdentityHandle.TryCreate(item, out var handle);
        if (isServer) TryPlaceKeycardServer(owner.Value, handle, pendingSlot, pendingItemVersion, pendingInventoryVersion, pendingDestination, pendingSocketVersion);
        else PlaceKeycardServerRpc(handle, pendingSlot, pendingItemVersion, pendingInventoryVersion, pendingDestination, pendingSocketVersion);
        return true;
    }

    [ServerRpc(runLocally: false)]
    private void PlaceKeycardServerRpc(ItemIdentityHandle item, int slot, ulong version, ulong inventory,
        ItemIdentityHandle socket, ulong socketVersion, RPCInfo info = default)
    { TryPlaceKeycardServer(info.sender, item, slot, version, inventory, socket, socketVersion); }

    private void TryPlaceKeycardServer(PlayerID sender, ItemIdentityHandle handle, int slot, ulong version,
        ulong inventory, ItemIdentityHandle destination, ulong socketVersion)
    {
        var item = handle.Resolve<ItemLoot>(this);
        var socket = destination.Resolve<Keycard_Socket>(this);
        string rejectionReason = null;
        bool accepted = false;
        if (!socket) rejectionReason = "socket identity did not resolve";
        else if (socket.sceneId != sceneId) rejectionReason = "socket belongs to a different scene";
        else if (!ValidateHeldServer(sender, item, slot, version, inventory)) rejectionReason = "sender, exact membership or source versions did not match";
        else accepted = socket.CanAcceptServer(item, socketVersion, out rejectionReason);
        if (!accepted)
        {
            // One diagnostic per rejected request, never per frame. Keep the actual validation intact.
            Debug.LogWarning($"[Keycard transfer rejected] reason={rejectionReason}; commit=false; " +
                $"sender={sender}, owner={owner}, inventory={sceneId}/{id}, slot={slot}; " +
                $"item={handle.Scene}/{handle.Identity}, itemVersion={version}/{(item ? item.Possession.Version : 0)}, " +
                $"inventoryVersion={inventory}/{InventoryVersion}; " +
                $"socket={destination.Scene}/{destination.Identity}, socketVersion={socketVersion}/{(socket ? socket.Occupancy.Version : 0)}, " +
                $"socketType={(socket ? socket.type.ToString() : "unresolved")}");
        }
        if (accepted)
        {
            committing = true;
            try
            {
                ulong stamp = ReleaseHeldServer(item, slot);
                item.SetPossessionServer(ItemSharedLocation.Socket, socket, -1, stamp);
                socket.OccupyServer(item, stamp);
                socket.NotifyInsertedServer(item);
            }
            finally { committing = false; }
        }
        Reply(sender, InventoryTransferOperation.KeycardInsertion, handle, slot, version, inventory, destination, socketVersion, accepted);
    }

    public bool PlaceInHullSocket(HullBreach_CrackSocket socket)
    {
        var obj = GetCurrentHeldObject();
        var item = obj ? obj.GetComponent<ItemLoot>() : null;
        if (!socket || !BeginPending(item, FindItemSlot(obj), InventoryTransferOperation.HullPlacement, socket, socket.Snapshot.Occupancy.Version)) return false;
        ItemIdentityHandle.TryCreate(item, out var handle);
        int crack = socket.Snapshot.CrackID;
        if (isServer) TryPlaceHullServer(owner.Value, handle, pendingSlot, pendingItemVersion, pendingInventoryVersion, pendingDestination, pendingSocketVersion, crack);
        else PlaceHullServerRpc(handle, pendingSlot, pendingItemVersion, pendingInventoryVersion, pendingDestination, pendingSocketVersion, crack);
        return true;
    }

    [ServerRpc(runLocally: false)]
    private void PlaceHullServerRpc(ItemIdentityHandle item, int slot, ulong version, ulong inventory,
        ItemIdentityHandle socket, ulong socketVersion, int crack, RPCInfo info = default)
    { TryPlaceHullServer(info.sender, item, slot, version, inventory, socket, socketVersion, crack); }

    private void TryPlaceHullServer(PlayerID sender, ItemIdentityHandle handle, int slot, ulong version, ulong inventory,
        ItemIdentityHandle destination, ulong socketVersion, int crack)
    {
        var item = handle.Resolve<ItemLoot>(this);
        var socket = destination.Resolve<HullBreach_CrackSocket>(this);
        bool accepted = false;
        if (socket && socket.sceneId == sceneId && socket.stationManager &&
            ValidateHeldServer(sender, item, slot, version, inventory))
        {
            committing = true;
            try { accepted = socket.stationManager.TryPlacePlateServer(this, item, slot, socket, socketVersion, crack); }
            finally { committing = false; }
        }
        Reply(sender, InventoryTransferOperation.HullPlacement, handle, slot, version, inventory, destination, socketVersion, accepted);
    }
}

public struct InventoryTransferReply : IPackedAuto
{
    public InventoryTransferOperation Operation;
    public ItemIdentityHandle Item, Destination;
    public int Slot;
    public ulong ExpectedItem, ExpectedInventory, ExpectedSocket, InventoryVersion, ItemVersion, CurrentSlotVersion;
    public bool Accepted, Lift;
    public Vector3 WorldPosition;
    public Quaternion WorldRotation;
    public ItemPossession CurrentPossession;
    public ItemIdentityHandle CurrentSlotItem;
}

public enum InventoryTransferOperation : byte
{
    None, Pickup, Drop, Extract, PageRelease, ChargeInsertion, KeycardInsertion, HullPlacement, ForcedOverflow
}
