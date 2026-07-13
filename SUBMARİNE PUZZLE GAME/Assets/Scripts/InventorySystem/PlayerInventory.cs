using System;
using PurrNet;
using StarterAssets;
using Unity.Mathematics;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] private Transform handPosition;
    [SerializeField] private Transform inspectPosition;
    [SerializeField] private Transform dropPosition;
    [SerializeField] private Transform cameraPosition;
    [SerializeField] private Transform interactCameraPosition;

    public Transform HandPosition => handPosition;
    public Transform DropPosition => dropPosition;
    public Transform InspectPosition => inspectPosition;
    public Transform InteractCameraTrans => interactCameraPosition;
    public static FirstPersonController LocalPlayerController { get; private set; }
    public static Transform LocalPlayerCamera { get; private set; }
    public static Transform LocalInteractCamera { get; private set; }



    public static Action<FirstPersonController, Transform, Transform> OnAssignController;
    private FirstPersonController playerController;



    protected override void OnSpawned()
    {
        if (isOwner)
        {

            playerController = GetComponent<FirstPersonController>();
            OnAssignController?.Invoke(playerController, cameraPosition, interactCameraPosition);
            LocalPlayerController = playerController;
            LocalPlayerCamera = cameraPosition;
            LocalInteractCamera = interactCameraPosition;

        }
    }




}