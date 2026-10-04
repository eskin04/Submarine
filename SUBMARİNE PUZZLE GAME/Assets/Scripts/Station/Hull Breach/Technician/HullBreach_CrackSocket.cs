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
    private int weldedPointsCount = 0;
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
        if (stationManager == null || !stationManager.isRoundActive.value || !isCrackSpawned) return;

        if (slottedPlate == null)
        {
            TryInsertPlate();
        }
        else if (!isFixed && moduleInteraction != null)
        {
            moduleInteraction.enabled = true;
            moduleInteraction.Interact();
            HullBreach_DrillItem drill = GetPlayerEquippedDrill();
            if (drill != null)
            {
                drill.StartMinigame(slottedPlate.transform);
            }
        }
    }

    public void OnStopInteract()
    {
        if (moduleInteraction != null && moduleInteraction.enabled)
        {
            HullBreach_DrillItem drill = GetPlayerEquippedDrill();
            if (drill != null) drill.StopMinigame();
        }
    }

    private void TryInsertPlate()
    {
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return;
        GameObject heldItemObj = inv.GetCurrentHeldObject();
        if (heldItemObj == null) return;

        HullBreach_PlateItem heldPlate = heldItemObj.GetComponent<HullBreach_PlateItem>();
        if (heldPlate != null)
        {
            interactable.StopInteract();
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
        weldedPointsCount++;
        if (weldedPointsCount >= 4)
        {
            stationManager.CmdFixCrack(currentCrackID);
        }
    }

    public void RpcOnCrackFixed()
    {
        isFixed = true;

        if (moduleInteraction != null && moduleInteraction.enabled)
        {
            moduleInteraction.StopInteract();
            interactable.SetInteractable(false);
            HullBreach_DrillItem drill = GetPlayerEquippedDrill();
            if (drill != null) drill.StopMinigame();
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
        displayedCrackID = -1;
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
            !item.Possession.Context.Matches(this))) return;
        currentCrackID = current.CrackID;
        isCrackSpawned = current.State != CrackState.Inactive;
        isFixed = current.State == CrackState.Fixed;
        slottedPlate = item ? item.GetComponent<HullBreach_PlateItem>() : null;
        bool newPlacement = item && (!displayedPlate.Equals(current.Occupancy.Occupant) || displayedCrackID != current.CrackID);
        if (item)
        {
            item.SetPhysicalState(ItemSharedLocation.Socket);
            item.transform.SetParent(transform);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.Euler(0, 0, current.PlacementRotation);
            item.SetVisible(true);
            if (newPlacement)
            {
                weldedPointsCount = 0;
                foreach (var point in item.GetComponentsInChildren<HullBreach_WeldPoint>(true)) point.Initialize(this);
            }
            interactable.SetDisplayName("Use Drill to Weld");
        }
        displayedPlate = current.Occupancy.Occupant;
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
