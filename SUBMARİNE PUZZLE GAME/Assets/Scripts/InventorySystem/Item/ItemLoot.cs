using System;
using PurrNet;
using PurrNet.Modules;
using PurrNet.Packing;
using UnityEngine;

public enum ItemSharedLocation { World, Inventory, Socket, Detached }

// Keep IDs in payloads: a component reference would decode to null before its spawn.
public struct ItemIdentityHandle : IPackedAuto
{
    public SceneID Scene;
    public NetworkID? Identity;

    public bool Matches(NetworkIdentity identity) => identity != null && identity.isSpawned &&
        Identity.HasValue && identity.id == Identity && identity.sceneId == Scene;

    public T Resolve<T>(NetworkIdentity context) where T : NetworkIdentity
    {
        if (!Identity.HasValue || context == null || context.networkManager == null) return null;
        if (!context.networkManager.TryGetModule<HierarchyFactory>(context.isServer, out var hierarchy) ||
            !hierarchy.TryGetIdentity(Scene, Identity.Value, out var identity)) return null;
        return identity as T;
    }

    public static bool TryCreate(NetworkIdentity identity, out ItemIdentityHandle handle)
    {
        handle = default;
        if (identity == null || !identity.isSpawned || !identity.id.HasValue) return false;

        handle = new ItemIdentityHandle { Scene = identity.sceneId, Identity = identity.id };
        return true;
    }
}

public struct ItemPossession : IPackedAuto
{
    public ItemSharedLocation Location;
    public ItemIdentityHandle Context; // Inventory holder OR socket, according to Location.
    public int Slot;
    public ulong Version;
}

public struct ItemSocketOccupancy : IPackedAuto
{
    public ItemIdentityHandle Occupant;
    public ulong Version;
}

public class ItemLoot : NetworkBehaviour
{
    public static Action<ItemLoot> OnLootAttempt;

    [SerializeField] private ItemData itemData;

    public ItemData Data => itemData;

    public bool CanBeLooted = true;
    public bool isInElevator = false;

    private SyncVar<ItemPossession> possession = new SyncVar<ItemPossession>();
    public ItemPossession Possession => possession.value;
    public bool IsPossessionInitialized => isSpawned && possession.value.Version != 0;

    private ItemPossession displayedPossession;
    private Renderer[] renderers;
    private bool[] rendererVisibility;
    private Canvas[] canvases;
    private bool[] canvasVisibility;
    private ulong previewVersion;
    private NetworkTransform networkTransform;
    private Rigidbody body;
    private Collider lootCollider;

    private void Awake()
    {
        networkTransform = GetComponent<NetworkTransform>();
        body = GetComponent<Rigidbody>();
        lootCollider = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>(true);
        rendererVisibility = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) rendererVisibility[i] = renderers[i].enabled;
        canvases = GetComponentsInChildren<Canvas>(true);
        canvasVisibility = new bool[canvases.Length];
        for (int i = 0; i < canvases.Length; i++) canvasVisibility[i] = canvases[i].enabled;
    }

    internal void SetPossessionServer(ItemSharedLocation location, NetworkIdentity context, int slot, ulong version)
    {
        if (!isServer || version == 0) throw new InvalidOperationException("Invalid possession writer/version.");
        ItemIdentityHandle.TryCreate(context, out var handle);
        possession.value = new ItemPossession { Location = location, Context = handle, Slot = slot, Version = version };
    }

    internal void SetVisible(bool visible)
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i]) renderers[i].enabled = visible && rendererVisibility[i];
        for (int i = 0; i < canvases.Length; i++)
            if (canvases[i]) canvases[i].enabled = visible && canvasVisibility[i];
    }

    internal void PreviewPickup(Transform hand)
    {
        // Visual-only on a remote owner. No ownership, possession or container write.
        previewVersion = Possession.Version;
        if (networkTransform) { networkTransform.StartIgnoringParentChanges(); networkTransform.enabled = false; }
        transform.SetParent(hand);
        transform.localPosition = Data.positionOffset;
        transform.localRotation = Quaternion.Euler(Data.rotationOffset);
        SetVisible(true);
    }

    internal void RestoreAcceptedPresentation()
    {
        previewVersion = 0;
        var current = Possession;
        if (current.Location == ItemSharedLocation.Inventory)
            current.Context.Resolve<InventoryManager>(this)?.ApplyItemHeld(this, current.Slot);
        else if (current.Location == ItemSharedLocation.Socket)
        {
            current.Context.Resolve<HullBreach_ChargeStation>(this)?.RefreshPresentation();
            current.Context.Resolve<Keycard_Socket>(this)?.RefreshPresentation();
            current.Context.Resolve<HullBreach_CrackSocket>(this)?.RefreshPresentation();
        }
        else
        {
            if (current.Location == ItemSharedLocation.World)
                transform.SetParent(networkTransform && networkTransform.parent ? networkTransform.parent.transform : null);
            if (!(current.Location == ItemSharedLocation.Detached && GetComponent<TornPageItem>()))
                SetPhysicalState(current.Location);
            SetVisible(true);
        }
    }

    internal void SetPhysicalState(ItemSharedLocation location)
    {
        bool world = location == ItemSharedLocation.World;
        bool detached = location == ItemSharedLocation.Detached;
        if (networkTransform)
        {
            if (world) networkTransform.StopIgnoringParentChanges();
            else networkTransform.StartIgnoringParentChanges();
            networkTransform.enabled = world || detached;
        }
        if (body) { body.isKinematic = !world || !isServer; body.useGravity = world; }
        if (lootCollider) lootCollider.enabled = world || detached ||
            (location == ItemSharedLocation.Socket && !GetComponent<HullBreach_PlateItem>());
        CanBeLooted = world || detached ||
            (location == ItemSharedLocation.Socket && !GetComponent<HullBreach_PlateItem>());
        enabled = true;
    }

    private void Update()
    {
        if (!IsPossessionInitialized) return;
        var current = Possession;
        if (previewVersion == current.Version) return;
        previewVersion = 0;
        if (current.Version == displayedPossession.Version) return;
        // Do not re-enable owner-auth NT using the old held pose before ownership removal arrives.
        if (!isServer && current.Location == ItemSharedLocation.World &&
            displayedPossession.Location == ItemSharedLocation.Inventory && owner.HasValue) return;
        var holder = current.Location == ItemSharedLocation.Inventory ? current.Context.Resolve<InventoryManager>(this) : null;
        if (current.Location == ItemSharedLocation.Inventory && holder == null) return;
        if (displayedPossession.Location == ItemSharedLocation.Inventory)
            displayedPossession.Context.Resolve<InventoryManager>(this)?.ApplyItemRelease(this, displayedPossession.Slot);
        if (holder != null) holder.ApplyItemHeld(this, current.Slot);
        else if (current.Location != ItemSharedLocation.Socket)
        {
            // Initial world state preserves producer setup (e.g. a kinematic foundry plate).
            // Attached pages retain their existing local pose/physics contract.
            if (!(current.Location == ItemSharedLocation.World && displayedPossession.Version == 0 && isServer) &&
                !(current.Location == ItemSharedLocation.Detached && GetComponent<TornPageItem>()))
                SetPhysicalState(current.Location);
            SetVisible(true);
        }
        displayedPossession = current;
    }

    protected override void OnDespawned(bool asServer)
    {
        if (asServer && Possession.Location == ItemSharedLocation.Inventory)
            Possession.Context.Resolve<InventoryManager>(this)?.ForgetDespawnedItem(this, Possession.Slot);
        if (asServer && Possession.Location == ItemSharedLocation.Socket)
        {
            ulong stamp = NextStateVersion(this);
            Possession.Context.Resolve<HullBreach_ChargeStation>(this)?.ReleaseServer(this, stamp);
            Possession.Context.Resolve<Keycard_Socket>(this)?.ReleaseServer(this, stamp);
            Possession.Context.Resolve<HullBreach_CrackSocket>(this)?.ForgetPlateServer(this, stamp);
        }
        displayedPossession.Context.Resolve<InventoryManager>(this)?.ApplyItemRelease(this, displayedPossession.Slot);
        displayedPossession = default;
        previewVersion = 0;
        base.OnDespawned(asServer);
    }

    // Shared stamp issuer, not a registry. Do not reset while session messages can arrive.
    private static ulong lastStateVersion;

    internal static ulong NextStateVersion(NetworkIdentity writer)
    {
        if (writer == null || !writer.isServer)
            throw new InvalidOperationException("Only the server can issue shared item state versions.");

        return checked(++lastStateVersion);
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        if (!asServer) return;

        // Also replaces a retained stamp when a pooled identity is spawned again.
        possession.value = new ItemPossession
        {
            Location = ItemSharedLocation.World,
            Slot = -1,
            Version = NextStateVersion(this)
        };
    }

    public void LootItem()
    {
        if (!CanBeLooted) return;
        OnLootAttempt?.Invoke(this);

    }
}
