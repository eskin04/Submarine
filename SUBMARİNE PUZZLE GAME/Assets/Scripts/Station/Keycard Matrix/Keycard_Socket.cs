using UnityEngine;
using PurrNet;

public enum SocketType { Dispenser, Technician, Engineer, Tester }

[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(Collider))]
public class Keycard_Socket : NetworkBehaviour
{
    public SocketType type;
    public int socketIndex;
    public Keycard_StationManager stationManager;

    public Keycard_Item slottedCard;

    // Host occupancy; slottedCard is a compatibility/presentation mirror.
    private SyncVar<ItemSocketOccupancy> occupancy = new SyncVar<ItemSocketOccupancy>();
    public ItemSocketOccupancy Occupancy => occupancy.value;

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        if (!asServer) return;

        occupancy.value = new ItemSocketOccupancy { Version = ItemLoot.NextStateVersion(this) };
    }

    private Interactable interactable;
    private Collider socketCollider;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        socketCollider = GetComponent<Collider>();
        InitializeSocket();
    }

    public void InitializeSocket(Keycard_Item preSlottedCard = null)
    {
        // The existing dispenser calls this locally on host before item spawn readiness.
        if (isServer && preSlottedCard)
        {
            if (Occupancy.Occupant.Matches(preSlottedCard.GetComponent<ItemLoot>())) return;
            ResetServer();
            pendingInitialCard = preSlottedCard;
        }
        ApplyOccupancy(true);
    }

    private void Update()
    {
        if (isServer && pendingInitialCard && pendingInitialCard.GetComponent<ItemLoot>().IsPossessionInitialized &&
            isSpawned && !Occupancy.Occupant.Identity.HasValue)
        {
            var item = pendingInitialCard.GetComponent<ItemLoot>();
            pendingInitialCard = null;
            if (item.Possession.Location == ItemSharedLocation.World)
            {
                ulong stamp = ItemLoot.NextStateVersion(this);
                item.SetPossessionServer(ItemSharedLocation.Socket, this, -1, stamp);
                OccupyServer(item, stamp);
            }
        }
        ApplyOccupancy();
    }
    public void HandleInteraction()
    {
        try
        {
            if (stationManager == null || !stationManager.isRoundActive) return;
            if (slottedCard == null) TryInsertCard();
        }
        finally
        {
            // Submission success is not the lifetime of this one-shot interaction.
            interactable.StopInteract();
        }
    }

    private void TryInsertCard()
    {
        var inv = InventoryManager.LocalPlayer;
        if (inv) inv.PlaceInKeycardSocket(this);
    }


    private void UpdateSocketState()
    {
        if (interactable == null || socketCollider == null) return;

        if (slottedCard != null)
        {
            interactable.SetInteractable(false);
            socketCollider.enabled = false;
        }
        else
        {
            interactable.SetInteractable(true);
            socketCollider.enabled = true;
        }
    }
    private Keycard_Item pendingInitialCard;

    internal void ResetServer()
    {
        if (!isServer || !isSpawned) return;
        var old = Occupancy.Occupant.Resolve<ItemLoot>(this);
        occupancy.value = new ItemSocketOccupancy { Version = ItemLoot.NextStateVersion(this) };
        pendingInitialCard = null;
        slottedCard = null;
        UpdateSocketState();
        if (old && old.Possession.Location == ItemSharedLocation.Socket && old.Possession.Context.Matches(this))
            Destroy(old.gameObject);
    }

    protected override void OnDespawned(bool asServer)
    {
        if (asServer) ResetServer();
        base.OnDespawned(asServer);
    }
    internal bool CanAcceptServer(ItemLoot item, ulong expected, out string rejectionReason)
    {
        rejectionReason = null;
        if (!isServer || !isSpawned) rejectionReason = "socket is not spawned on the host";
        else if (expected == 0 || Occupancy.Version != expected) rejectionReason = "socket version is unready or stale";
        else if (Occupancy.Occupant.Identity.HasValue) rejectionReason = "socket is occupied";
        else if (!item || !item.GetComponent<Keycard_Item>()) rejectionReason = "item is not a keycard";
        else if (!stationManager || !stationManager.isRoundActive.value) rejectionReason = "station round is inactive or missing";
        // Empty dispenser slots accepted returned cards before the transfer migration.
        else if (!(type == SocketType.Dispenser || type == SocketType.Engineer ||
            (type == SocketType.Technician && socketIndex >= 0 && socketIndex < 4) ||
            (type == SocketType.Tester && socketIndex >= 0 && socketIndex < 2)))
            rejectionReason = "socket type or puzzle index is invalid";
        return rejectionReason == null;
    }

    internal bool CanReleaseServer(ItemLoot item, ulong expected)
    {
        return isServer && expected != 0 && Occupancy.Version == expected && Occupancy.Occupant.Matches(item);
    }

    internal void OccupyServer(ItemLoot item, ulong stamp)
    {
        if (!isServer) return;
        ItemIdentityHandle.TryCreate(item, out var handle);
        occupancy.value = new ItemSocketOccupancy { Occupant = handle, Version = stamp };
        slottedCard = item.GetComponent<Keycard_Item>();
        var nt = item.GetComponent<NetworkTransform>();
        if (nt) { nt.RemoveOwnership(); nt.StopIgnoringParentChanges(); }
        item.transform.SetParent(transform);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        item.SetPhysicalState(ItemSharedLocation.Socket);
        item.SetVisible(true);
        UpdateSocketState();
    }

    internal void ReleaseServer(ItemLoot item, ulong stamp)
    {
        if (!isServer || !Occupancy.Occupant.Matches(item)) return;
        occupancy.value = new ItemSocketOccupancy { Version = stamp };
        slottedCard = null;
        UpdateSocketState();
        if (stationManager != null)
        {
            if (type == SocketType.Engineer) stationManager.EngineerRemoveCardServer();
            else if (type == SocketType.Technician) stationManager.TechnicianRemoveCardServer(socketIndex);
            else if (type == SocketType.Tester) stationManager.TesterRemoveCardServer(socketIndex);
        }
    }

    internal void RefreshPresentation() { ApplyOccupancy(true); }

    private void ApplyOccupancy(bool force = false)
    {
        var item = Occupancy.Occupant.Resolve<ItemLoot>(this);
        if (Occupancy.Occupant.Identity.HasValue && (!item || item.Possession.Location != ItemSharedLocation.Socket ||
            !item.Possession.Context.Matches(this))) return;
        if (!force && slottedCard == (item ? item.GetComponent<Keycard_Item>() : null)) return;
        slottedCard = item ? item.GetComponent<Keycard_Item>() : null;
        if (item)
        {
            item.SetPhysicalState(ItemSharedLocation.Socket);
            item.transform.SetParent(transform);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.SetVisible(true);
        }
        UpdateSocketState();
    }

    internal void NotifyInsertedServer(ItemLoot item)
    {
        int cardID = item.GetComponent<Keycard_Item>().myData.CardID;
        if (type == SocketType.Engineer) stationManager.EngineerInsertCardServer(cardID);
        else if (type == SocketType.Technician) stationManager.TechnicianInsertCardServer(cardID, socketIndex);
        else if (type == SocketType.Tester) stationManager.TesterInsertCardServer(cardID, socketIndex);
    }
}
