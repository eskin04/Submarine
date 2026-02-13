using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Interactable : MonoBehaviour, IInteractable
{
    [SerializeField] private string displayName = "Interact";
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool isInteractable = true;
    [SerializeField] private UnityEvent onInteract;
    private bool isInteracting = false;

    private Outline outline;

    void Awake()
    {
        outline = gameObject.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineVisible;
        outline.OutlineColor = Color.white;
        outline.OutlineWidth = 3f;
        outline.enabled = false;
    }

    void OnDestroy()
    {
        outline.enabled = false;
        Destroy(outline);
    }
    public string DisplayName => displayName;

    public KeyCode InteractKey => interactKey;

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
    }

    public void OnFocus()
    {
        outline.enabled = true;
    }

    public void OnLoseFocus()
    {
        if (outline != null)
            outline.enabled = false;
    }
}
