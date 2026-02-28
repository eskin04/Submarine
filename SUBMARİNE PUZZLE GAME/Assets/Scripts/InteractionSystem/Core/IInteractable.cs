using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    Transform transform { get; }
    string DisplayName { get; }
    List<KeyCode> InteractKeys { get; }
    bool CanInteract();
    bool IsInteracting();
    void StopInteract();
    void Interact();
    void OnFocus();
    void OnLoseFocus();

}
