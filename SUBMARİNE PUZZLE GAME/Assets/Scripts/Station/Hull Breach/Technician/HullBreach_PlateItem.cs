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

    private void OnEnable()
    {
        ItemLoot.OnLootAttempt += HandleLootAttempt;
    }

    private void OnDisable()
    {
        ItemLoot.OnLootAttempt -= HandleLootAttempt;
    }


    private void HandleLootAttempt(ItemLoot attemptedLoot)
    {
        if (attemptedLoot != myLoot || isLooted) return;

        isLooted = true;
        CmdNotifyTaken();
    }

    [ServerRpc(requireOwnership: false)]
    private void CmdNotifyTaken()
    {
        OnPlateTakenServer?.Invoke(this);
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