using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Interactable : MonoBehaviour, IInteractable
{
    [SerializeField] private string displayName = "Interact";
    [SerializeField] private List<KeyCode> interactKeys = new List<KeyCode>() { KeyCode.E };
    [SerializeField] private bool isInteractable = true;
    [SerializeField] private bool canDisableInteraction = true;
    [SerializeField] private UnityEvent onInteract;
    [SerializeField] private UnityEvent onStopInteract;
    [SerializeField] private bool CanOutlined = true;

    private bool isInteracting = false;

    private Outline outline;

    void Awake()
    {
        if (!CanOutlined) return;
        outline = gameObject.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineVisible;
        outline.OutlineColor = Color.yellow;
        outline.OutlineWidth = 3f;
        outline.enabled = false;
    }

    void OnDestroy()
    {
        if (CanOutlined && outline != null)
        {
            outline.enabled = false;
            Destroy(outline);
        }
    }
    public string DisplayName => displayName;

    public List<KeyCode> InteractKeys => interactKeys;
    public bool CanDisableInteraction() => canDisableInteraction;

    public bool CanInteract() => isInteractable;
    public void SetInteractable(bool value) => isInteractable = value;

    public bool IsInteracting() => isInteracting;
    public void Interact()
    {
        isInteracting = true;
        onInteract?.Invoke();

    }

    public void StopInteract()
    {
        isInteracting = false;
        onStopInteract?.Invoke();
    }

    public void OnFocus()
    {
        if (CanOutlined && outline != null)
            outline.enabled = true;
    }

    public void OnLoseFocus()
    {
        if (CanOutlined && outline != null)
            outline.enabled = false;
    }

    public void SetDisplayName(string newName) => displayName = newName;
}
