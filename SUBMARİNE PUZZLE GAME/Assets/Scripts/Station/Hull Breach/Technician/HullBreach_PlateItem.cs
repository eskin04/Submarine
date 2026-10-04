using UnityEngine;
using PurrNet;
using System;

[RequireComponent(typeof(ItemLoot))]
public class HullBreach_PlateItem : NetworkBehaviour, IInventoryItem
{
    [Header("Settings")]
    public PlateMaterial plateMaterial;

    public Action<HullBreach_PlateItem> OnPlateTakenServer;

    private ItemLoot myLoot;
    private bool isLooted = false;

    private void Awake()
    {
        myLoot = GetComponent<ItemLoot>();
    }

    internal HullBreach_FoundryController SourceFoundry { get; set; }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        if (asServer) isLooted = false;
    }

    protected override void OnDespawned(bool asServer)
    {
        if (asServer && SourceFoundry) SourceFoundry.ForgetPlateServer(this);
        base.OnDespawned(asServer);
    }

    internal void NotifyPickupAcceptedServer()
    {
        if (!isServer || isLooted) return;
        if (SourceFoundry && !SourceFoundry.IsCurrentPlate(this)) return;
        isLooted = true;
        OnPlateTakenServer?.Invoke(this);
        SourceFoundry = null;
    }
    public void OnEquip()
    {
        if (InstanceHandler.TryGetInstance<PromptView>(out var promptView))
        {
            // promptView.AddPrompt("plate_info", "Çatlağa Yerleştir");
        }
    }

    public void OnUnequip()
    {
        if (InstanceHandler.TryGetInstance<PromptView>(out var promptView))
        {
            // promptView.RemovePrompt("plate_info");
        }
    }

    public void OnDrop()
    {
    }

    public void CanOperate(bool canOperate)
    {
    }
}
