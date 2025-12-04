using UnityEngine;

public interface IInteractable
{
    Transform transform { get; }
    string DisplayName { get; }
    bool CanInteract();
    bool IsInteract();
    void Interact();
    void StopInteract();
    void OnFocus();
    void OnLoseFocus();

}
