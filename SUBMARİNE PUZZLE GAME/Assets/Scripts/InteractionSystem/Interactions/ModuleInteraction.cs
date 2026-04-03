using StarterAssets;
using UnityEngine;
using DG.Tweening;
using PurrNet;

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
    private Transform playerInteractCameraTransform;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform playerOriginalParent;
    private Transform originalParent;
    private Rigidbody rb;
    private Collider colliderObject;
    private IInteractable ınteractable;

    private bool isHoveringMesh = false;
    private Camera activeCam;
    private Camera mainCam;

    void Awake()
    {
        PlayerInventory.OnAssignController += HandlePlayerController;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;
        rb = GetComponent<Rigidbody>();
        colliderObject = GetComponent<Collider>();
        ınteractable = GetComponent<IInteractable>();
        mainCam = Camera.main;

    }
    void OnDestroy()
    {
        PlayerInventory.OnAssignController -= HandlePlayerController;
    }

    private void HandlePlayerController(FirstPersonController controller, Transform camera, Transform interactCamera)
    {
        playerController = controller;
        playerCameraTransform = camera;
        playerInteractCameraTransform = interactCamera;
        playerOriginalParent = playerInteractCameraTransform.parent;

    }

    private void Update()
    {
        if (ınteractable.IsInteracting() && Input.GetKeyDown(KeyCode.Mouse1))
        {
            StopInteract();
        }
        if (ınteractable.IsInteracting() && isUnlockCursor)
        {
            HandleCursorHover();
        }
    }



    public void Interact()
    {
        if (playerCameraTransform != null)
        {
            InstanceHandler.GetInstance<GameViewManager>().HideView<MainGameView>();
            playerController.enabled = false;
            SetInteractPosition();
            if (rb != null) rb.isKinematic = true;
            if (colliderObject != null) colliderObject.enabled = false;
            if (isUnlockCursor)
            {

                CursorManager.OnInteractionStarted?.Invoke();
                isHoveringMesh = false;

                activeCam = isMoveable ? playerCameraTransform.GetComponent<Camera>() : playerInteractCameraTransform.GetComponent<Camera>();

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
            playerInteractCameraTransform.parent = transform;
            playerInteractCameraTransform.localPosition = initialPositionOffset;
            playerInteractCameraTransform.localEulerAngles = initialRotationOffset;
            playerInteractCameraTransform.gameObject.SetActive(true);
        }
    }


    public void StopInteract()
    {
        if (playerCameraTransform != null)
        {
            InstanceHandler.GetInstance<GameViewManager>().ShowView<MainGameView>(hideOthers: false);

            ınteractable.StopInteract();
            playerController.enabled = true;
            SetCameraPositionBack();
            if (rb != null) rb.isKinematic = false;
            if (colliderObject != null) colliderObject.enabled = true;
            if (isUnlockCursor)
            {


                CursorManager.OnInteractionEnded?.Invoke();
                isHoveringMesh = false;
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
            playerInteractCameraTransform.parent = playerOriginalParent;
            playerInteractCameraTransform.gameObject.SetActive(false);
        }
    }

    private void HandleCursorHover()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, CursorManager.RaycastDistance, CursorManager.InteractableLayer))
        {
            if (!isHoveringMesh)
            {
                CursorManager.OnHoverStateChanged?.Invoke(true);
                isHoveringMesh = true;
            }
        }
        else
        {
            if (isHoveringMesh)
            {
                CursorManager.OnHoverStateChanged?.Invoke(false);
                isHoveringMesh = false;
            }
        }
    }

}
