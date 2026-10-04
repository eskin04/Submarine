using UnityEngine;
using PurrNet;
using PurrNet.Packing;

public struct HullSocketSnapshot : IPackedAuto
{
    public ItemSocketOccupancy Occupancy;
    public int CrackID;
    public CrackState State;
    public float PlacementRotation;
}

public struct HullWeldContext : IPackedAuto
{
    public ItemIdentityHandle Socket;
    public ItemIdentityHandle Plate;
    public ItemIdentityHandle Inventory;
    public ItemIdentityHandle Tool;
    public int CrackID;
    public int ToolSlot;
    public ulong SocketVersion;
    public ulong PlateVersion;
    public ulong InventoryVersion;
    public ulong ToolVersion;
}

[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(Collider))]
public class HullBreach_CrackSocket : NetworkBehaviour
{
    [Header("Location Data")]
    public int floorIndex;
    public CrackZone zone;
    public int spawnPointIndex;
    [Header("Particle System")]
    public ParticleSystem waterParticlePrefab;
    private ParticleSystem activeWaterParticle;
    public float offsetRateMultiplier = 0.15f;
    public float offsetSpeedMultiplier = 0.2f;

    [Header("References")]
    public HullBreach_StationManager stationManager;
    public ModuleInteraction moduleInteraction;

    [Header("Live State")]
    public int currentCrackID = -1;
    public bool isCrackSpawned = false;
    public HullBreach_PlateItem slottedPlate;
    public bool isFixed = false;

    // Host snapshot drives socket presentation without observer-side consumption.
    private SyncVar<HullSocketSnapshot> snapshot = new SyncVar<HullSocketSnapshot>();
    public HullSocketSnapshot Snapshot => snapshot.value;

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        if (!asServer) return;
        if (stationManager) stationManager.RegisterSocket(this);

        snapshot.value = new HullSocketSnapshot
        {
            Occupancy = new ItemSocketOccupancy { Version = ItemLoot.NextStateVersion(this) },
            CrackID = -1,
            State = CrackState.Inactive
        };
    }

    private Interactable interactable;
    private Collider socketCollider;
    private HullWeldContext weldingContext;
    private HullBreach_DrillItem weldingDrill;
    private bool fixedPresentationApplied;
    private float originalRateMultiplier;
    private float originalSpeedMultiplier;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        socketCollider = GetComponent<Collider>();

        if (moduleInteraction != null) moduleInteraction.enabled = false;

        isCrackSpawned = false;
        UpdateSocketVisualsAndInteraction();

        if (stationManager != null) stationManager.RegisterSocket(this);

        interactable.onInteractCondition += CanInteractWithSocket;
    }

    // ==========================================
    // DİNAMİK ETKİLEŞİM ŞARTLARI
    // ==========================================
    private bool CanInteractWithSocket()
    {
        if (!isCrackSpawned || isFixed) return false;

        if (slottedPlate == null)
        {
            return CheckIfHoldingPlate();
        }
        else
        {
            return CheckIfHoldingDrill();
        }
    }

    private bool CheckIfHoldingPlate()
    {
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return false;
        GameObject heldItemObj = inv.GetCurrentHeldObject();
        if (heldItemObj == null) return false;
        HullBreach_PlateItem plate = heldItemObj.GetComponent<HullBreach_PlateItem>();
        if (plate != null) return true;

        return false;
    }

    private bool CheckIfHoldingDrill()
    {
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return false;
        GameObject heldItemObj = inv.GetCurrentHeldObject();
        if (heldItemObj == null) return false;
        HullBreach_DrillItem drill = heldItemObj.GetComponent<HullBreach_DrillItem>();
        if (drill != null) return true;

        return false;
    }

    // ==========================================
    // ETKİLEŞİM YÖNLENDİRİCİSİ (Router)
    // ==========================================
    public void OnSocketInteracted()
    {
        if (stationManager == null || !stationManager.isRoundActive.value || !isCrackSpawned)
        {
            interactable.StopInteract();
            return;
        }

        if (slottedPlate == null)
        {
            TryInsertPlate();
        }
        else if (!isFixed && moduleInteraction != null)
        {
            if (!CaptureWeldingContext())
            {
                interactable.StopInteract();
                return;
            }
            moduleInteraction.enabled = true;
            moduleInteraction.Interact();
            weldingDrill.StartMinigame(slottedPlate.transform);
            // Completed points cannot notify again after a same-context rejection.
            ReevaluateWeldingCompletion();
        }
    }

    public void OnStopInteract()
    {
        if (weldingDrill != null) weldingDrill.StopMinigame();
        weldingDrill = null;
        weldingContext = default;
    }

    private void TryInsertPlate()
    {
        // Plate insertion is one-shot; early local rejection must also end interaction.
        interactable.StopInteract();
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return;
        GameObject heldItemObj = inv.GetCurrentHeldObject();
        if (heldItemObj == null) return;

        HullBreach_PlateItem heldPlate = heldItemObj.GetComponent<HullBreach_PlateItem>();
        if (heldPlate != null)
        {
            inv.PlaceInHullSocket(this);
        }
    }

    // ==========================================
    // AĞ VE DURUM GÜNCELLEMELERİ
    // ==========================================

    [ObserversRpc(runLocally: true)]
    public void RpcActivateCrack(int crackID) { ApplySnapshot(); }

    [ObserversRpc(runLocally: true)]
    public void RpcPlacePlateInSocket(GameObject plateObj, float randomZRot) { ApplySnapshot(); }

    public void OnPointWelded()
    {
        ReevaluateWeldingCompletion();
    }

    private bool CaptureWeldingContext()
    {
        var inventory = InventoryManager.LocalPlayer;
        var drill = GetPlayerEquippedDrill();
        var tool = drill ? drill.GetComponent<ItemLoot>() : null;
        var plate = slottedPlate ? slottedPlate.GetComponent<ItemLoot>() : null;
        var current = Snapshot;
        if (!inventory || !inventory.isOwner || !drill || !drill.isOwner || !tool || !plate ||
            !stationManager || !stationManager.isRoundActive.value || current.State != CrackState.Plated ||
            current.Occupancy.Version == 0 || inventory.InventoryVersion == 0 ||
            !current.Occupancy.Occupant.Matches(plate) || plate.Possession.Version != current.Occupancy.Version ||
            plate.Possession.Location != ItemSharedLocation.Socket || !plate.Possession.Context.Matches(this) ||
            tool.Possession.Version == 0 || tool.Possession.Location != ItemSharedLocation.Inventory ||
            !tool.Possession.Context.Matches(inventory) ||
            !ItemIdentityHandle.TryCreate(this, out var socketHandle) ||
            !ItemIdentityHandle.TryCreate(inventory, out var inventoryHandle) ||
            !ItemIdentityHandle.TryCreate(tool, out var toolHandle)) return false;

        weldingContext = new HullWeldContext
        {
            Socket = socketHandle, Plate = current.Occupancy.Occupant,
            Inventory = inventoryHandle, Tool = toolHandle,
            CrackID = current.CrackID, SocketVersion = current.Occupancy.Version,
            PlateVersion = plate.Possession.Version, ToolSlot = tool.Possession.Slot,
            ToolVersion = tool.Possession.Version, InventoryVersion = inventory.InventoryVersion
        };
        weldingDrill = drill;
        return true;
    }

    public void ReevaluateWeldingCompletion()
    {
        var current = Snapshot;
        var plate = slottedPlate ? slottedPlate.GetComponent<ItemLoot>() : null;
        var inventory = weldingContext.Inventory.Resolve<InventoryManager>(this);
        var tool = weldingContext.Tool.Resolve<ItemLoot>(this);
        if (!interactable.IsInteracting() || !inventory || !inventory.isOwner || !inventory.owner.HasValue ||
            !weldingDrill || !weldingDrill.isOwner || !tool || !plate || !stationManager ||
            !stationManager.isRoundActive.value || !weldingContext.Socket.Matches(this) ||
            current.State != CrackState.Plated || current.CrackID != weldingContext.CrackID ||
            current.Occupancy.Version != weldingContext.SocketVersion ||
            !current.Occupancy.Occupant.Equals(weldingContext.Plate) || !weldingContext.Plate.Matches(plate) ||
            plate.Possession.Version != weldingContext.PlateVersion ||
            tool.Possession.Version != weldingContext.ToolVersion ||
            inventory.InventoryVersion != weldingContext.InventoryVersion ||
            inventory.GetCurrentHeldObject() != tool.gameObject) return;

        // A stale point callback/counter is not evidence about the currently placed plate.
        var points = plate.GetComponentsInChildren<HullBreach_WeldPoint>(true);
        if (points.Length != 4) return;
        foreach (var point in points) if (!point.isWelded) return;

        if (isServer) stationManager.TryFixCrackServer(inventory.owner.Value, this, weldingContext);
        else CompleteWeldingServerRpc(weldingContext);
    }

    [ServerRpc(requireOwnership: false, runLocally: false)]
    private void CompleteWeldingServerRpc(HullWeldContext context, RPCInfo info = default)
    {
        if (isServer && stationManager) stationManager.TryFixCrackServer(info.sender, this, context);
    }

    public void RpcOnCrackFixed()
    {
        if (fixedPresentationApplied) return;
        fixedPresentationApplied = true;
        isFixed = true;

        if (moduleInteraction != null && moduleInteraction.enabled && interactable.IsInteracting())
        {
            moduleInteraction.StopInteract();
            interactable.SetInteractable(false);
        }

        UpdateSocketVisualsAndInteraction();
    }

    private HullBreach_DrillItem GetPlayerEquippedDrill()
    {
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return null;
        GameObject heldItemObj = inv.GetCurrentHeldObject();
        if (heldItemObj == null) return null;

        HullBreach_DrillItem heldDrill = heldItemObj.GetComponent<HullBreach_DrillItem>();
        return heldDrill;
    }

    private void UpdateSocketVisualsAndInteraction()
    {
        if (interactable == null || socketCollider == null) return;

        if (!isCrackSpawned || isFixed)
        {
            interactable.SetInteractable(false);
            socketCollider.enabled = false;

            if (activeWaterParticle != null)
            {
                activeWaterParticle.Stop();

                Destroy(activeWaterParticle.gameObject, 2f);
                activeWaterParticle = null;
            }
            return;
        }

        interactable.SetInteractable(true);
        socketCollider.enabled = true;

        if (activeWaterParticle == null && waterParticlePrefab != null)
        {
            activeWaterParticle = Instantiate(waterParticlePrefab, transform);

            activeWaterParticle.transform.localPosition = Vector3.zero;
            activeWaterParticle.transform.localRotation = Quaternion.identity;

            originalRateMultiplier = activeWaterParticle.emission.rateOverTimeMultiplier;
            originalSpeedMultiplier = activeWaterParticle.main.startSpeedMultiplier;
        }

        if (activeWaterParticle != null)
        {
            var main = activeWaterParticle.main;
            var emission = activeWaterParticle.emission;

            if (slottedPlate != null)
            {
                if (!activeWaterParticle.isPlaying) activeWaterParticle.Play();

                emission.rateOverTimeMultiplier = originalRateMultiplier * offsetRateMultiplier;
                main.startSpeedMultiplier = originalSpeedMultiplier * offsetSpeedMultiplier;
            }
            else
            {
                if (!activeWaterParticle.isPlaying) activeWaterParticle.Play();

                emission.rateOverTimeMultiplier = originalRateMultiplier;
                main.startSpeedMultiplier = originalSpeedMultiplier;
            }
        }
    }
    private ulong displayedSocketVersion;
    private ItemIdentityHandle displayedPlate;
    private ulong displayedPlateVersion;
    private int displayedCrackID = -1;

    private void Update() { ApplySnapshot(); }

    internal void ResetServer()
    {
        if (!isServer || !isSpawned) return;
        var old = Snapshot.Occupancy.Occupant.Resolve<ItemLoot>(this);
        if (stationManager) stationManager.ForgetSocketServer(Snapshot.CrackID);
        snapshot.value = new HullSocketSnapshot { CrackID = -1, State = CrackState.Inactive,
            Occupancy = new ItemSocketOccupancy { Version = ItemLoot.NextStateVersion(this) } };
        if (old && old.Possession.Location == ItemSharedLocation.Socket && old.Possession.Context.Matches(this))
            Destroy(old.gameObject);
        ApplySnapshot();
    }

    protected override void OnDespawned(bool asServer)
    {
        if (asServer)
        {
            ResetServer();
            if (stationManager) stationManager.allSockets.Remove(this);
        }
        displayedSocketVersion = 0;
        displayedPlate = default;
        displayedPlateVersion = 0;
        displayedCrackID = -1;
        if (moduleInteraction != null && interactable.IsInteracting()) moduleInteraction.StopInteract();
        else OnStopInteract();
        fixedPresentationApplied = false;
        base.OnDespawned(asServer);
    }

    internal void ActivateCrackServer(int crack)
    {
        if (!isServer) return;
        var previous = Snapshot.Occupancy.Occupant.Resolve<ItemLoot>(this);
        snapshot.value = new HullSocketSnapshot { CrackID = crack, State = CrackState.Active,
            Occupancy = new ItemSocketOccupancy { Version = ItemLoot.NextStateVersion(this) } };
        if (previous && previous.Possession.Location == ItemSharedLocation.Socket &&
            previous.Possession.Context.Matches(this)) Destroy(previous.gameObject);
        ApplySnapshot();
    }

    internal void PlacePlateServer(ItemLoot item, float rotation, ulong stamp)
    {
        ItemIdentityHandle.TryCreate(item, out var handle);
        var current = Snapshot;
        current.Occupancy = new ItemSocketOccupancy { Occupant = handle, Version = stamp };
        current.State = CrackState.Plated; current.PlacementRotation = rotation;
        var nt = item.GetComponent<NetworkTransform>();
        if (nt) { nt.RemoveOwnership(); nt.StopIgnoringParentChanges(); }
        item.transform.SetParent(transform);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.Euler(0, 0, rotation);
        item.SetPhysicalState(ItemSharedLocation.Socket);
        snapshot.value = current;
        ApplySnapshot();
    }

    internal void MarkFixedServer()
    {
        if (!isServer || Snapshot.State != CrackState.Plated) return;
        var current = Snapshot; current.State = CrackState.Fixed;
        current.Occupancy.Version = ItemLoot.NextStateVersion(this);
        snapshot.value = current;
        ApplySnapshot();
    }

    internal void ForgetPlateServer(ItemLoot item, ulong stamp)
    {
        if (!isServer || !Snapshot.Occupancy.Occupant.Matches(item)) return;
        var current = Snapshot;
        current.Occupancy = new ItemSocketOccupancy { Version = stamp };
        // A lost plate reopens the same crack; a fixed crack stays fixed.
        if (current.State == CrackState.Plated)
        {
            current.State = CrackState.Active;
            stationManager?.ForgetPlateServer(current.CrackID);
        }
        snapshot.value = current;
        ApplySnapshot();
    }

    internal void RefreshPresentation() { displayedSocketVersion = 0; ApplySnapshot(); }

    private void ApplySnapshot()
    {
        var current = Snapshot;
        if (current.Occupancy.Version == 0 || current.Occupancy.Version == displayedSocketVersion) return;
        var item = current.Occupancy.Occupant.Resolve<ItemLoot>(this);
        if (current.Occupancy.Occupant.Identity.HasValue && (!item || item.Possession.Location != ItemSharedLocation.Socket ||
            !item.Possession.Context.Matches(this) ||
            (current.State == CrackState.Plated && item.Possession.Version != current.Occupancy.Version))) return;
        if (weldingContext.SocketVersion != 0 && (current.CrackID != weldingContext.CrackID ||
            current.Occupancy.Version != weldingContext.SocketVersion ||
            !current.Occupancy.Occupant.Equals(weldingContext.Plate)))
        {
            if (moduleInteraction != null && interactable.IsInteracting()) moduleInteraction.StopInteract();
            else OnStopInteract();
        }
        if (current.State != CrackState.Fixed) fixedPresentationApplied = false;
        currentCrackID = current.CrackID;
        isCrackSpawned = current.State != CrackState.Inactive;
        isFixed = current.State == CrackState.Fixed;
        slottedPlate = item ? item.GetComponent<HullBreach_PlateItem>() : null;
        bool newPlacement = item && (!displayedPlate.Equals(current.Occupancy.Occupant) ||
            displayedCrackID != current.CrackID || displayedPlateVersion != item.Possession.Version);
        if (item)
        {
            item.SetPhysicalState(ItemSharedLocation.Socket);
            item.transform.SetParent(transform);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.Euler(0, 0, current.PlacementRotation);
            item.SetVisible(true);
            if (newPlacement)
            {
                foreach (var point in item.GetComponentsInChildren<HullBreach_WeldPoint>(true)) point.Initialize(this);
            }
            interactable.SetDisplayName("Use Drill to Weld");
        }
        displayedPlate = current.Occupancy.Occupant;
        displayedPlateVersion = item ? item.Possession.Version : 0;
        displayedCrackID = current.CrackID;
        displayedSocketVersion = current.Occupancy.Version;
        if (isFixed)
        {
            foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
            RpcOnCrackFixed();
        }
        else UpdateSocketVisualsAndInteraction();
    }
}
