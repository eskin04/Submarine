using System;
using PurrLobby;
using PurrNet;
using StarterAssets;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] private Transform handPosition;
    [SerializeField] private Transform dropPosition;
    [SerializeField] private Transform cameraPosition;
    public static Action<Transform, Transform> OnSpawnPlayer;
    public static Action<FirstPersonController, Transform> OnAssignController;
    private FirstPersonController playerController;

    protected override void OnSpawned()
    {
        if (isOwner)
        {
            OnSpawnPlayer?.Invoke(handPosition, dropPosition);
            playerController = GetComponent<FirstPersonController>();
            OnAssignController?.Invoke(playerController, cameraPosition);

        }
    }

    protected override void OnDespawned()
    {
        if (isOwner)
        {
            OnSpawnPlayer = null;
            OnAssignController = null;
        }
    }


}
