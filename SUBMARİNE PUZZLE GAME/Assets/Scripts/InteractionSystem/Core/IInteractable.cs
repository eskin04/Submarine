using UnityEngine;

public interface IInteractable
{
    Transform transform { get; }
    string DisplayName { get; }
    KeyCode InteractKey { get; }
    bool CanInteract();
    bool IsInteracting();
    void StopInteract();
    void Interact();
    void OnFocus();
    void OnLoseFocus();

}
