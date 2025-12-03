using System;
using PurrNet;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] private Transform handPosition;
    [SerializeField] private Transform dropPosition;
    public static Action<Transform, Transform> OnSpawnPlayer;

    protected override void OnSpawned()
    {
        if (isOwner)
        {
            OnSpawnPlayer?.Invoke(handPosition, dropPosition);
        }
    }

    protected override void OnDespawned()
    {
        if (isOwner)
        {
            OnSpawnPlayer = null;
        }
    }


}
