using StarterAssets;
using UnityEngine;
using DG.Tweening;
using PurrNet;
using System.Collections.Generic;

[System.Serializable]
public struct ModulePromptData
{
    public string promptId;
    public string keyText;
    public string actionText;
    public Sprite icon;
}

public class ModuleInteraction : MonoBehaviour
{
    [SerializeField] private float animDuration = .5f;
    [SerializeField] private Ease animEase = Ease.InOutSine;
    [SerializeField] private Vector3 initialPositionOffset = new Vector3(0, 0, -1);
    [SerializeField] private Vector3 initialRotationOffset = Vector3.zero;
    [SerializeField] private bool isUnlockCursor = true;
    [SerializeField] private bool isMoveable = false;
    [Header("Prompt Settings")]

    [SerializeField] private List<ModulePromptData> modulePrompts = new List<ModulePromptData>();
    private const string DEFAULT_EXIT_PROMPT_ID = "module_default_exit";


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
    private bool isInteracting = false;

    private bool isHoveringMesh = false;
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
        if (ınteractable.IsInteracting() && Input.GetKeyDown(KeyCode.Mouse1) && isInteracting)
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
            InstanceHandler.GetInstance<MainGameView>().SetInteractionVisibility(false);
            playerController.enabled = false;
            SetInteractPosition();
            CameraLayerController.OnInteractionStarted?.Invoke();
            ShowPrompts();
            if (rb != null) rb.isKinematic = true;
            if (colliderObject != null) colliderObject.enabled = false;
            if (isUnlockCursor)
            {

                CursorManager.OnInteractionStarted?.Invoke();
                isHoveringMesh = false;
                HighlightManager.Instance.ActivateModuleHighlights(transform);

            }
        }
    }

    private void SetInteractPosition()
    {
        if (isMoveable)
        {

            transform.parent = playerCameraTransform;
            transform.DOLocalMove(initialPositionOffset, animDuration).SetEase(animEase).OnComplete(() =>
            {
                isInteracting = true;
            });
            transform.DOLocalRotate(initialRotationOffset, animDuration).SetEase(animEase);

        }
        else
        {
            playerInteractCameraTransform.parent = transform;
            playerInteractCameraTransform.localPosition = initialPositionOffset;
            playerInteractCameraTransform.localEulerAngles = initialRotationOffset;
            playerInteractCameraTransform.gameObject.SetActive(true);
            isInteracting = true;
        }
    }


    public void StopInteract()
    {
        if (playerCameraTransform != null)
        {
            InstanceHandler.GetInstance<MainGameView>().SetInteractionVisibility(true);
            ınteractable.StopInteract();
            playerController.enabled = true;
            SetCameraPositionBack();
            CameraLayerController.OnInteractionEnded?.Invoke();
            HidePrompts();
            if (rb != null) rb.isKinematic = false;
            if (colliderObject != null) colliderObject.enabled = true;
            if (isUnlockCursor)
            {

                HighlightManager.Instance.DeactivateModuleHighlights();
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
            transform.DOMove(originalPosition, animDuration).SetEase(animEase).OnComplete(() =>
            {
                isInteracting = false;
            });
            transform.DORotateQuaternion(originalRotation, animDuration).SetEase(animEase);
        }
        else
        {
            playerInteractCameraTransform.parent = playerOriginalParent;
            playerInteractCameraTransform.gameObject.SetActive(false);
            isInteracting = false;
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
                HighlightManager.Instance.SetHoveredObject(hit.transform);
                isHoveringMesh = true;

            }
        }
        else
        {
            if (isHoveringMesh)
            {
                CursorManager.OnHoverStateChanged?.Invoke(false);
                HighlightManager.Instance.SetHoveredObject(null);
                isHoveringMesh = false;
            }
        }
    }

    private void ShowPrompts()
    {
        if (InstanceHandler.TryGetInstance<PromptView>(out var promptView))
        {
            promptView.AddPrompt(DEFAULT_EXIT_PROMPT_ID, "Right Click", "Exit", PromptGroup.Module);

            foreach (var prompt in modulePrompts)
            {
                promptView.AddPrompt(prompt.promptId, prompt.keyText, prompt.actionText, PromptGroup.Module, prompt.icon);
            }
        }
    }

    private void HidePrompts()
    {
        if (InstanceHandler.TryGetInstance<PromptView>(out var promptView))
        {
            promptView.RemovePromptsByGroup(PromptGroup.Module);
        }
    }

}
