using StarterAssets;
using UnityEngine;
using DG.Tweening;
using System;

public class ModuleInteraction : MonoBehaviour
{
    [SerializeField] private float animDuration = .5f;
    [SerializeField] private Ease animEase = Ease.InOutSine;
    [SerializeField] private Vector3 initialPositionOffset = new Vector3(0, 0, -1);
    [SerializeField] private Vector3 initialRotationOffset = Vector3.zero;
    [SerializeField] private bool isUnlockCursor = true;
    [SerializeField] private bool isMoveable = false;

    private FirstPersonController playerController;
    private Transform playerCameraTransform;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Transform playerOriginalParent;
    private Transform originalParent;
    private Rigidbody rb;
    private Collider colliderObject;
    private float animDurationMultiplier = 1.5f;
    private IInteractable ınteractable;

    void Awake()
    {
        PlayerInventory.OnAssignController += HandlePlayerController;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;
        rb = GetComponent<Rigidbody>();
        colliderObject = GetComponent<Collider>();
        ınteractable = GetComponent<IInteractable>();

    }
    void OnDestroy()
    {
        PlayerInventory.OnAssignController -= HandlePlayerController;
    }

    private void HandlePlayerController(FirstPersonController controller, Transform camera)
    {
        playerController = controller;
        playerCameraTransform = camera;
        originalCameraPosition = playerCameraTransform.localPosition;
        originalCameraRotation = playerCameraTransform.localRotation;
        playerOriginalParent = playerCameraTransform.parent;

    }

    private void Update()
    {
        if (ınteractable.IsInteracting() && Input.GetKeyDown(KeyCode.Escape))
        {
            StopInteract();
        }
    }



    public void Interact()
    {
        if (playerCameraTransform != null)
        {
            playerController.enabled = false;
            SetInteractPosition();
            if (rb != null) rb.isKinematic = true;
            if (colliderObject != null) colliderObject.enabled = false;
            if (isUnlockCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    private void SetInteractPosition()
    {
        if (isMoveable)
        {

            transform.parent = playerCameraTransform;
            transform.DOLocalMove(initialPositionOffset, animDuration).SetEase(animEase);
            transform.DOLocalRotate(initialRotationOffset, animDuration).SetEase(animEase);

        }
        else
        {
            playerCameraTransform.parent = transform;
            playerCameraTransform.DOLocalMove(initialPositionOffset, animDuration * animDurationMultiplier).SetEase(animEase);
            playerCameraTransform.DOLocalRotate(initialRotationOffset, animDuration * animDurationMultiplier).SetEase(animEase);
        }
    }


    public void StopInteract()
    {
        if (playerCameraTransform != null)
        {
            ınteractable.StopInteract();
            playerController.enabled = true;
            SetCameraPositionBack();
            if (rb != null) rb.isKinematic = false;
            if (colliderObject != null) colliderObject.enabled = true;
            if (isUnlockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void SetCameraPositionBack()
    {
        if (isMoveable)
        {
            transform.parent = originalParent;
            transform.DOMove(originalPosition, animDuration).SetEase(animEase);
            transform.DORotateQuaternion(originalRotation, animDuration).SetEase(animEase);
        }
        else
        {
            playerCameraTransform.parent = playerOriginalParent;
            playerCameraTransform.DOLocalMove(originalCameraPosition, animDuration * animDurationMultiplier).SetEase(animEase);
            playerCameraTransform.DOLocalRotateQuaternion(originalCameraRotation, animDuration * animDurationMultiplier).SetEase(animEase);
        }
    }

}
