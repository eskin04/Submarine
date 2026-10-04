using StarterAssets;
using UnityEngine;
using DG.Tweening;
using PurrNet;
using System;
using System.Collections.Generic;
using System.Collections;
using Cinemachine;

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
    private const string DEFAULT_INTERACT_PROMPT_ID = "module_default_interact";

    [Header("Tutorial Settings")]
    [SerializeField] private bool isNotebookModule = false;

    private enum InteractionPhase { Idle, Entering, Active, Returning }
    private InteractionPhase phase;
    private static ModuleInteraction currentModule;
    private bool isEnding;
    private bool terminalRequested;
    private bool playerStateRestored;
    private Tween transitionTween;
    private Coroutine cameraReturn;
    private CinemachineBrain capturedBrain;
    private FirstPersonController playerController;
    private Transform playerCameraTransform;
    private Transform playerInteractCameraTransform;
    private PlayerInventory capturedPlayer;
    private InventoryManager capturedInventory;
    private FirstPersonController capturedController;
    private Transform capturedInteractCamera;
    private bool controllerWasEnabled;
    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform playerOriginalParent;
    private Vector3 cameraLocalPosition;
    private Quaternion cameraLocalRotation;
    private Vector3 cameraLocalScale;
    private bool cameraWasActive;
    private Rigidbody rb;
    private Collider colliderObject;
    private bool wasKinematic;
    private bool colliderWasEnabled;
    private IInteractable ınteractable;
    private Camera mainCam;
    private int cameraMask;
    private CursorLockMode cursorLock;
    private bool cursorWasVisible;
    private bool cameraSignalStarted;
    private bool cursorSignalStarted;
    private bool isHoveringMesh;

    internal bool HasInteraction => phase == InteractionPhase.Entering || phase == InteractionPhase.Active;
    internal bool IsTerminalCleanup => terminalRequested || !isActiveAndEnabled;
    internal event Action OnTerminalInteractionEnded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliderObject = GetComponent<Collider>();
        ınteractable = GetComponent<IInteractable>();
    }

    private void OnEnable()
    {
        PlayerInventory.OnAssignController += HandlePlayerController;
        PlayerInventory.OnControllerInvalidated += HandleControllerInvalidated;
        HandlePlayerController(PlayerInventory.LocalPlayerController,
            PlayerInventory.LocalPlayerCamera, PlayerInventory.LocalInteractCamera);
    }

    private void OnDisable()
    {
        EndInteraction(true);
        UnbindController();
    }

    private void OnDestroy()
    {
        EndInteraction(true);
        UnbindController();
    }

    private void UnbindController()
    {
        PlayerInventory.OnAssignController -= HandlePlayerController;
        PlayerInventory.OnControllerInvalidated -= HandleControllerInvalidated;
        playerController = null;
        playerCameraTransform = null;
        playerInteractCameraTransform = null;
    }

    private void HandlePlayerController(FirstPersonController controller, Transform camera, Transform interactCamera)
    {
        if (playerController != controller || playerCameraTransform != camera || playerInteractCameraTransform != interactCamera)
            EndInteraction(true);
        playerController = controller;
        playerCameraTransform = camera;
        playerInteractCameraTransform = interactCamera;
    }

    private void HandleControllerInvalidated(PlayerInventory player)
    {
        if (ReferenceEquals(capturedPlayer, player)) EndInteraction(true);
        if (ReferenceEquals(PlayerInventory.LocalBinding, player))
        {
            playerController = null;
            playerCameraTransform = null;
            playerInteractCameraTransform = null;
        }
    }

    internal static void EndInteractionForPlayer(PlayerInventory player)
    {
        if (currentModule && ReferenceEquals(currentModule.capturedPlayer, player))
            currentModule.EndInteraction(true);
    }

    private void Update()
    {
        if (!HasInteraction && phase != InteractionPhase.Returning) return;
        if (!playerController || !playerCameraTransform || !playerInteractCameraTransform || !capturedPlayer)
        {
            EndInteraction(true);
            return;
        }
        if (!HasInteraction) return;
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (isNotebookModule && InstanceHandler.TryGetInstance<TutorialQuestView>(out var questView) &&
                questView && questView.ShouldBlockAction(TutorialAction.CloseNotebook)) return;
            StopInteract();
        }
        if (HasInteraction && isUnlockCursor) HandleCursorHover();
    }

    public void Interact()
    {
        if (!isActiveAndEnabled || isEnding || HasInteraction) return;
        if (currentModule && currentModule != this && currentModule.isEnding) return;
        if (phase == InteractionPhase.Returning) EndInteraction(true);
        if (currentModule && currentModule != this) currentModule.EndInteraction(true);
        HandlePlayerController(PlayerInventory.LocalPlayerController,
            PlayerInventory.LocalPlayerCamera, PlayerInventory.LocalInteractCamera);
        if (!playerController || !playerCameraTransform || !playerInteractCameraTransform || !PlayerInventory.LocalBinding)
            return;

        capturedPlayer = PlayerInventory.LocalBinding;
        capturedInventory = capturedPlayer.GetComponent<InventoryManager>();
        capturedController = playerController;
        capturedInteractCamera = playerInteractCameraTransform;
        controllerWasEnabled = playerController.enabled;
        originalParent = transform.parent;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        playerOriginalParent = playerInteractCameraTransform.parent;
        cameraLocalPosition = playerInteractCameraTransform.localPosition;
        cameraLocalRotation = playerInteractCameraTransform.localRotation;
        cameraLocalScale = playerInteractCameraTransform.localScale;
        cameraWasActive = playerInteractCameraTransform.gameObject.activeSelf;
        wasKinematic = rb && rb.isKinematic;
        colliderWasEnabled = colliderObject && colliderObject.enabled;
        mainCam = Camera.main;
        capturedBrain = mainCam ? mainCam.GetComponent<CinemachineBrain>() : null;
        if (mainCam) cameraMask = mainCam.cullingMask;
        cursorLock = Cursor.lockState;
        cursorWasVisible = Cursor.visible;
        currentModule = this;
        phase = InteractionPhase.Entering;
        terminalRequested = false;
        playerStateRestored = false;

        if (InstanceHandler.TryGetInstance<MainGameView>(out var view) && view) view.SetInteractionVisibility(false);
        if (!HasInteraction) return;
        playerController.enabled = false;
        if (rb) rb.isKinematic = true;
        if (colliderObject) colliderObject.enabled = false;
        SetInteractPosition();
        if (!HasInteraction) return;
        cameraSignalStarted = true;
        CameraLayerController.OnInteractionStarted?.Invoke();
        if (!HasInteraction) return;
        ShowPrompts();
        if (isUnlockCursor)
        {
            cursorSignalStarted = true;
            CursorManager.OnInteractionStarted?.Invoke();
            if (!HasInteraction) return;
            isHoveringMesh = false;
            if (HighlightManager.Instance) HighlightManager.Instance.ActivateModuleHighlights(transform);
        }
    }

    private void SetInteractPosition()
    {
        CancelTransition();
        if (isMoveable)
        {
            transform.SetParent(playerCameraTransform, true);
            transitionTween = OwnTransition(DOTween.Sequence()
                .Join(transform.DOLocalMove(initialPositionOffset, animDuration).SetEase(animEase))
                .Join(transform.DOLocalRotate(initialRotationOffset, animDuration).SetEase(animEase))
                .OnComplete(() => { transitionTween = null; phase = InteractionPhase.Active; }));
        }
        else
        {
            playerInteractCameraTransform.SetParent(transform, true);
            playerInteractCameraTransform.localPosition = initialPositionOffset;
            playerInteractCameraTransform.localEulerAngles = initialRotationOffset;
            phase = InteractionPhase.Active;
            playerInteractCameraTransform.gameObject.SetActive(true);
        }
    }

    public void StopInteract() => EndInteraction(false);
    internal void InterruptInteraction() => EndInteraction(true);

    private void EndInteraction(bool terminal)
    {
        if (isEnding)
        {
            terminalRequested |= terminal;
            return;
        }
        if (phase == InteractionPhase.Idle)
        {
            if (terminal) OnTerminalInteractionEnded?.Invoke();
            return;
        }
        if (phase == InteractionPhase.Returning && !terminal) return;

        isEnding = true;
        terminalRequested = terminal;
        bool notifyStop = HasInteraction;
        CancelTransition();
        phase = InteractionPhase.Returning;
        try
        {
            if (notifyStop && ınteractable is UnityEngine.Object target && target && ınteractable.IsInteracting())
                ınteractable.StopInteract();
        }
        finally
        {
            try
            {
                if (isMoveable && !terminalRequested && isActiveAndEnabled)
                {
                    transform.SetParent(originalParent ? originalParent : null, true);
                    transitionTween = OwnTransition(DOTween.Sequence()
                        .Join(transform.DOMove(originalPosition, animDuration).SetEase(animEase))
                        .Join(transform.DORotateQuaternion(originalRotation, animDuration).SetEase(animEase))
                        .OnComplete(FinishReturn));
                }
                else if (!terminalRequested && isActiveAndEnabled && capturedInteractCamera &&
                    !cameraWasActive && capturedBrain && capturedBrain.isActiveAndEnabled)
                {
                    // Cinemachine blends from this outgoing pose. Moving it home first collapses the blend.
                    capturedInteractCamera.gameObject.SetActive(false);
                    if (!terminalRequested) cameraReturn = StartCoroutine(WaitForCameraReturn());
                    else FinishReturn();
                }
                else
                {
                    if (isMoveable)
                    {
                        transform.SetParent(originalParent ? originalParent : null, true);
                        transform.SetPositionAndRotation(originalPosition, originalRotation);
                    }
                    FinishReturn();
                }
            }
            finally { isEnding = false; }
        }
    }

    private void RestorePlayerState()
    {
        if (capturedController) capturedController.enabled = controllerWasEnabled;
        if (!isMoveable && capturedInteractCamera)
        {
            capturedInteractCamera.SetParent(playerOriginalParent ? playerOriginalParent : null, false);
            capturedInteractCamera.localPosition = cameraLocalPosition;
            capturedInteractCamera.localRotation = cameraLocalRotation;
            capturedInteractCamera.localScale = cameraLocalScale;
            capturedInteractCamera.gameObject.SetActive(cameraWasActive);
        }
        if (cameraSignalStarted)
        {
            cameraSignalStarted = false;
            CameraLayerController.OnInteractionEnded?.Invoke();
            if (mainCam) mainCam.cullingMask = cameraMask;
        }
        if (capturedInventory) capturedInventory.EndModuleInteraction(ınteractable);
        if (rb) rb.isKinematic = wasKinematic;
        if (colliderObject) colliderObject.enabled = colliderWasEnabled;
        if (InstanceHandler.TryGetInstance<MainGameView>(out var view) && view) view.SetInteractionVisibility(true);
        HidePrompts();
        if (cursorSignalStarted)
        {
            cursorSignalStarted = false;
            if (HighlightManager.Instance) HighlightManager.Instance.DeactivateModuleHighlights();
            CursorManager.OnInteractionEnded?.Invoke();
            bool settingsOpen = InstanceHandler.TryGetInstance<GameViewManager>(out var views) && views && views.IsViewActive<SettingsView>();
            Cursor.lockState = settingsOpen ? CursorLockMode.None : cursorLock;
            Cursor.visible = settingsOpen || cursorWasVisible;
        }
        isHoveringMesh = false;
    }

    private void CancelTransition()
    {
        if (cameraReturn != null) StopCoroutine(cameraReturn);
        cameraReturn = null;
        transitionTween?.Kill(false);
        transitionTween = null;
    }

    private Tween OwnTransition(Tween tween)
    {
        return tween.OnKill(() =>
        {
            if (ReferenceEquals(transitionTween, tween)) transitionTween = null;
        });
    }

    private void FinishReturn()
    {
        transitionTween = null;
        cameraReturn = null;
        bool wasEnding = isEnding;
        isEnding = true;
        try
        {
            if (!playerStateRestored)
            {
                playerStateRestored = true;
                RestorePlayerState();
            }
            if (terminalRequested) OnTerminalInteractionEnded?.Invoke();
        }
        finally
        {
            phase = InteractionPhase.Idle;
            if (currentModule == this) currentModule = null;
            capturedPlayer = null;
            capturedInventory = null;
            capturedController = null;
            capturedInteractCamera = null;
            capturedBrain = null;
            isEnding = wasEnding;
        }
    }

    private IEnumerator WaitForCameraReturn()
    {
        // Wait for the brain to select the player camera and finish its existing blend.
        yield return null;
        while (capturedBrain && capturedBrain.isActiveAndEnabled &&
            (capturedBrain.IsBlending || (capturedInteractCamera &&
                capturedBrain.ActiveVirtualCamera?.VirtualCameraGameObject == capturedInteractCamera.gameObject)))
            yield return null;
        FinishReturn();
    }

    private void HandleCursorHover()
    {
        if (!mainCam) return;
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, CursorManager.RaycastDistance, CursorManager.InteractableLayer))
        {
            if (!isHoveringMesh)
            {
                CursorManager.OnHoverStateChanged?.Invoke(true);
                if (HighlightManager.Instance) HighlightManager.Instance.SetHoveredObject(hit.transform);
                isHoveringMesh = true;
            }
        }
        else if (isHoveringMesh)
        {
            CursorManager.OnHoverStateChanged?.Invoke(false);
            if (HighlightManager.Instance) HighlightManager.Instance.SetHoveredObject(null);
            isHoveringMesh = false;
        }
    }

    private void ShowPrompts()
    {
        if (InstanceHandler.TryGetInstance<PromptView>(out var promptView) && promptView)
        {
            promptView.AddPrompt(DEFAULT_EXIT_PROMPT_ID, "Right Click", "Exit", PromptGroup.Module);
            if (isUnlockCursor) promptView.AddPrompt(DEFAULT_INTERACT_PROMPT_ID, "Left Click", "Interact", PromptGroup.Module);
            foreach (var prompt in modulePrompts)
                promptView.AddPrompt(prompt.promptId, prompt.keyText, prompt.actionText, PromptGroup.Module, prompt.icon);
        }
    }

    private void HidePrompts()
    {
        if (InstanceHandler.TryGetInstance<PromptView>(out var promptView) && promptView)
        {
            promptView.RemovePrompt(DEFAULT_EXIT_PROMPT_ID);
            if (isUnlockCursor) promptView.RemovePrompt(DEFAULT_INTERACT_PROMPT_ID);
            foreach (var prompt in modulePrompts) promptView.RemovePrompt(prompt.promptId);
        }
    }
}
