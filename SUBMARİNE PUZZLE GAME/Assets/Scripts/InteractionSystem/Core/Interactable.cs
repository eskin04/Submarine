using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour, IInteractable
{
    [SerializeField] private string displayName = "Interact";
    [SerializeField] private bool isInteractable = true;
    [SerializeField] private UnityEvent onInteract;

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

    public bool CanInteract() => isInteractable;


    public void Interact()
    {
        onInteract?.Invoke();
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
