using System;
using PurrNet;
using StarterAssets;
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
    internal static PlayerInventory LocalBinding { get; private set; }

    public static Action<FirstPersonController, Transform, Transform> OnAssignController;
    internal static event Action<PlayerInventory> OnControllerInvalidated;

    private void OnEnable()
    {
        if (isSpawned) PublishController();
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        PublishController();
    }

    private void PublishController()
    {
        if (!isOwner || !isActiveAndEnabled) return;
        var controller = GetComponent<FirstPersonController>();
        if (!controller || !cameraPosition || !interactCameraPosition) return;
        if (LocalBinding == this && LocalPlayerController == controller &&
            LocalPlayerCamera == cameraPosition && LocalInteractCamera == interactCameraPosition) return;

        if (LocalBinding) LocalBinding.InvalidateController();
        LocalBinding = this;
        LocalPlayerController = controller;
        LocalPlayerCamera = cameraPosition;
        LocalInteractCamera = interactCameraPosition;
        OnAssignController?.Invoke(controller, cameraPosition, interactCameraPosition);
    }

    private void InvalidateController()
    {
        if (!ReferenceEquals(LocalBinding, this)) return;
        try
        {
            // End the captured interaction while its restoration targets are still available.
            OnControllerInvalidated?.Invoke(this);
        }
        finally
        {
            if (ReferenceEquals(LocalBinding, this))
            {
                LocalBinding = null;
                LocalPlayerController = null;
                LocalPlayerCamera = null;
                LocalInteractCamera = null;
            }
        }
    }

    private void OnDisable() => InvalidateController();

    protected override void OnDespawned()
    {
        InvalidateController();
        base.OnDespawned();
    }

    protected override void OnDestroy()
    {
        try { InvalidateController(); }
        finally { base.OnDestroy(); }
    }
}
