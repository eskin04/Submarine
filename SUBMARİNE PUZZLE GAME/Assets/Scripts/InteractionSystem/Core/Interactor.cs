using System;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField] private float interactionRange = 3f;
    public static RaycastHit? CurrentLookHit { get; private set; }
    public static Action<IInteractable> OnInteractableChanged;
    public static Action<IInteractable> OnInteract;
    private IInteractable currentInteractable;
    private List<KeyCode> interactKeys = new List<KeyCode>();


    void Update()
    {

        CheckForInteractable();

        if (IsInteractKeyDown() && currentInteractable != null && currentInteractable.CanInteract())
        {
            currentInteractable.Interact();
            OnInteract?.Invoke(currentInteractable);
        }
    }

    private bool IsInteractKeyDown()
    {
        foreach (KeyCode key in interactKeys)
        {
            if (Input.GetKeyDown(key))
            {
                return true;

            }
        }
        return false;
    }



    private void CheckForInteractable()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            CurrentLookHit = hit;
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract())
            {
                if (currentInteractable != interactable)
                {
                    if (currentInteractable != null)
                    {
                        currentInteractable?.OnLoseFocus();
                        InstanceHandler.GetInstance<GameViewManager>().HideView<InteractionView>();

                    }
                    currentInteractable = interactable;
                    currentInteractable?.OnFocus();
                    InstanceHandler.GetInstance<GameViewManager>().ShowView<InteractionView>(hideOthers: false);
                    interactKeys = currentInteractable.InteractKeys;
                    OnInteractableChanged?.Invoke(currentInteractable);
                }
                return;
            }
        }
        else
        {
            CurrentLookHit = null;
        }

        if (currentInteractable != null)
        {
            currentInteractable?.OnLoseFocus();
            InstanceHandler.GetInstance<GameViewManager>().HideView<InteractionView>();
            currentInteractable = null;
            OnInteractableChanged?.Invoke(null);
        }
    }


}
