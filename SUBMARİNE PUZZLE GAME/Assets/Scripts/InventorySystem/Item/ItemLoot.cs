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

    // Foundation only: legacy transfers do not read or update this state yet.
    private SyncVar<ItemPossession> possession = new SyncVar<ItemPossession>();
    public ItemPossession Possession => possession.value;
    public bool IsPossessionInitialized => isSpawned && possession.value.Version != 0;

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
