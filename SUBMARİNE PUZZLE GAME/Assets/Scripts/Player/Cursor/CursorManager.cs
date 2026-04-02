using System;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Header("Cursor Textures")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
    [SerializeField] private Vector2 cursorHoverHotspot = Vector2.zero;

    [Header("Raycast Settings (Global)")]
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float raycastDistance = 10f;

    public static LayerMask InteractableLayer { get; private set; }
    public static float RaycastDistance { get; private set; }

    public static Action OnInteractionStarted;
    public static Action OnInteractionEnded;
    public static Action<bool> OnHoverStateChanged;

    private void Awake()
    {
        InteractableLayer = interactableLayer;
        RaycastDistance = raycastDistance;
    }

    private void OnEnable()
    {
        OnInteractionStarted += HandleInteractionStarted;
        OnInteractionEnded += HandleInteractionEnded;
        OnHoverStateChanged += HandleHoverStateChanged;
    }

    private void OnDisable()
    {
        OnInteractionStarted -= HandleInteractionStarted;
        OnInteractionEnded -= HandleInteractionEnded;
        OnHoverStateChanged -= HandleHoverStateChanged;
    }


    private void HandleInteractionStarted()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Cursor.SetCursor(defaultCursor, cursorHotspot, CursorMode.Auto);
    }

    private void HandleInteractionEnded()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void HandleHoverStateChanged(bool isHovering)
    {
        Texture2D tex = isHovering ? hoverCursor : defaultCursor;
        Vector2 hotspot = isHovering ? cursorHoverHotspot : cursorHotspot;
        Cursor.SetCursor(tex, hotspot, CursorMode.Auto);
    }
}