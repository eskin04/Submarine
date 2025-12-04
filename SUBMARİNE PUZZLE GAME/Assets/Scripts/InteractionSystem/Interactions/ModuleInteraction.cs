using StarterAssets;
using UnityEngine;
using DG.Tweening;

public class ModuleInteraction : MonoBehaviour
{
    [SerializeField] private float animDuration = .5f;
    [SerializeField] private Ease animEase = Ease.InOutSine;
    [SerializeField] private Vector3 initialPositionOffset = new Vector3(0, 0, -1);
    [SerializeField] private Vector3 initialRotationOffset = Vector3.zero;
    [SerializeField] private bool isUnlockCursor = true;
    private FirstPersonController playerController;
    private Transform playerCameraTransform;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    private Interactable interactable;
    private Rigidbody rb;
    private Collider colliderObject;

    void Awake()
    {
        PlayerInventory.OnAssignController += HandlePlayerController;
        interactable = GetComponent<Interactable>();
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;
        rb = GetComponent<Rigidbody>();
        colliderObject = GetComponent<Collider>();

    }
    void OnDestroy()
    {
        PlayerInventory.OnAssignController -= HandlePlayerController;
    }

    private void HandlePlayerController(FirstPersonController controller, Transform camera)
    {
        playerController = controller;
        playerCameraTransform = camera;
    }



    public void InteractWithModule()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
            transform.parent = playerCameraTransform;
            transform.DOLocalMove(initialPositionOffset, animDuration).SetEase(animEase);
            transform.DOLocalRotate(initialRotationOffset, animDuration).SetEase(animEase);
            if (rb != null) rb.isKinematic = true;
            if (colliderObject != null) colliderObject.enabled = false;
            if (isUnlockCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    public void StopInteractingWithModule()
    {
        if (playerController != null)
        {
            playerController.enabled = true;
            transform.parent = originalParent;
            transform.DOMove(originalPosition, animDuration).SetEase(animEase);
            transform.DORotateQuaternion(originalRotation, animDuration).SetEase(animEase);
            if (rb != null) rb.isKinematic = false;
            if (colliderObject != null) colliderObject.enabled = true;
            if (isUnlockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
