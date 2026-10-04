using UnityEngine;
using PurrNet;
using UnityEngine.UI;

[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(Collider))]
public class HullBreach_ChargeStation : NetworkBehaviour
{
    [Header("Station Settings")]
    public float chargeRate = 10f;
    public Transform stationDrillSlot;

    [Header("UI Visuals")]
    public Slider stationChargeBar;

    [Header("Light Shader")]
    public MeshRenderer lightRenderer;
    public float lightIntensity = 5.0f;

    [Header("Live State")]
    public HullBreach_DrillItem slottedDrill;

    // Host occupancy; slottedDrill is a compatibility/presentation mirror.
    private SyncVar<ItemSocketOccupancy> occupancy = new SyncVar<ItemSocketOccupancy>();
    public ItemSocketOccupancy Occupancy => occupancy.value;

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        if (!asServer) return;

        occupancy.value = new ItemSocketOccupancy { Version = ItemLoot.NextStateVersion(this) };
    }

    private Interactable interactable;
    private Collider stationCollider;

    // Shader Değişkenleri
    private Material runtimeLightMaterial;
    private static readonly int LightSelectionProp = Shader.PropertyToID("_ColorIndex");
    private static readonly int IntensityProp = Shader.PropertyToID("_LightIntensity");
    private int lastColorIndex = 0;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        stationCollider = GetComponent<Collider>();

        if (lightRenderer != null) runtimeLightMaterial = lightRenderer.materials[0];

        if (stationChargeBar != null)
        {
            stationChargeBar.minValue = 0f;
            stationChargeBar.maxValue = 100f;
            stationChargeBar.value = 0f;
        }

        SetLightState(false);
        UpdateStationState();
    }


    private void Update()
    {
        ApplyOccupancy();
        if (!isServer || slottedDrill == null || !Occupancy.Occupant.Matches(slottedDrill.GetComponent<ItemLoot>())) return;
        if (slottedDrill.currentCharge < 100f)
        {
            slottedDrill.currentCharge = Mathf.Min(100f, slottedDrill.currentCharge + chargeRate * Time.deltaTime);
            RpcUpdateChargeVisuals(slottedDrill.currentCharge);
        }
    }


    public void HandleInteraction()
    {
        try
        {
            if (slottedDrill == null) TryInsertDrill();
        }
        finally
        {
            // This is a one-shot socket action, including rejected or unready requests.
            interactable.StopInteract();
        }
    }

    private void TryInsertDrill()
    {
        var inv = InventoryManager.LocalPlayer;
        if (inv) inv.PlaceInChargeStation(this);
    }


    [ObserversRpc]
    private void RpcUpdateChargeVisuals(float charge)
    {
        if (slottedDrill != null)
        {
            slottedDrill.currentCharge = charge;
            slottedDrill.UpdateDrillVisuals();
        }

        if (stationChargeBar != null)
        {
            stationChargeBar.value = charge;
        }

        SetLightState(charge >= 100f);
    }


    private void UpdateStationState()
    {
        if (interactable == null || stationCollider == null) return;

        if (slottedDrill != null)
        {
            interactable.SetInteractable(false);
            stationCollider.enabled = false;

            if (stationChargeBar != null) stationChargeBar.value = slottedDrill.currentCharge;
            SetLightState(slottedDrill.currentCharge >= 100f);
        }
        else
        {
            interactable.SetInteractable(true);
            stationCollider.enabled = true;

            if (stationChargeBar != null) stationChargeBar.value = 0f;
            SetLightState(false);
        }
    }

    private void SetLightState(bool isFull)
    {
        if (runtimeLightMaterial == null) return;

        int index = isFull ? 2 : 0;

        runtimeLightMaterial.SetFloat(LightSelectionProp, index);

        if (lastColorIndex == 0 && index != 0)
        {
            runtimeLightMaterial.SetFloat(IntensityProp, lightIntensity);
        }
        else if (lastColorIndex != 0 && index == 0)
        {
            runtimeLightMaterial.SetFloat(IntensityProp, 0.0f);
        }

        lastColorIndex = index;
    }
    internal bool CanAcceptServer(ItemLoot item, ulong expected)
    {
        return isServer && isSpawned && expected != 0 && Occupancy.Version == expected &&
            !Occupancy.Occupant.Identity.HasValue && item.GetComponent<HullBreach_DrillItem>() && stationDrillSlot != null;
    }

    internal bool CanReleaseServer(ItemLoot item, ulong expected)
    {
        return isServer && expected != 0 && Occupancy.Version == expected && Occupancy.Occupant.Matches(item);
    }

    protected override void OnDespawned(bool asServer)
    {
        if (asServer)
        {
            var item = Occupancy.Occupant.Resolve<ItemLoot>(this);
            occupancy.value = new ItemSocketOccupancy { Version = ItemLoot.NextStateVersion(this) };
            slottedDrill = null;
            if (item && item.Possession.Location == ItemSharedLocation.Socket && item.Possession.Context.Matches(this))
                Destroy(item.gameObject);
        }
        base.OnDespawned(asServer);
    }

    internal void OccupyServer(ItemLoot item, ulong stamp)
    {
        if (!isServer) return;
        ItemIdentityHandle.TryCreate(item, out var handle);
        occupancy.value = new ItemSocketOccupancy { Occupant = handle, Version = stamp };
        slottedDrill = item.GetComponent<HullBreach_DrillItem>();
        var nt = item.GetComponent<NetworkTransform>();
        if (nt) { nt.RemoveOwnership(); nt.StopIgnoringParentChanges(); }
        item.transform.SetParent(stationDrillSlot);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        item.SetPhysicalState(ItemSharedLocation.Socket);
        item.SetVisible(true);
        UpdateStationState();
    }

    internal void ReleaseServer(ItemLoot item, ulong stamp)
    {
        if (!isServer || !Occupancy.Occupant.Matches(item)) return;
        occupancy.value = new ItemSocketOccupancy { Version = stamp };
        slottedDrill = null;
        UpdateStationState();

    }

    internal void RefreshPresentation() { ApplyOccupancy(true); }

    private void ApplyOccupancy(bool force = false)
    {
        var item = Occupancy.Occupant.Resolve<ItemLoot>(this);
        if (Occupancy.Occupant.Identity.HasValue && (!item || item.Possession.Location != ItemSharedLocation.Socket ||
            !item.Possession.Context.Matches(this))) return;
        if (!force && slottedDrill == (item ? item.GetComponent<HullBreach_DrillItem>() : null)) return;
        slottedDrill = item ? item.GetComponent<HullBreach_DrillItem>() : null;
        if (item)
        {
            item.SetPhysicalState(ItemSharedLocation.Socket);
            item.transform.SetParent(stationDrillSlot);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.SetVisible(true);
        }
        UpdateStationState();
    }

}
