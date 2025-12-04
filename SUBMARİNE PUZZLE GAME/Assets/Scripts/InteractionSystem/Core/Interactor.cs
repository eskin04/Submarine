using System;
using PurrNet;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField] private float interactionRange = 3f;
    public static Action<IInteractable> OnInteractableChanged;
    private IInteractable currentInteractable;
    private bool canInteracting = true;

    void Awake()
    {
        InventoryManager.OnItemEquipped += HandleInteractingState;
    }

    void OnDestroy()
    {
        InventoryManager.OnItemEquipped -= HandleInteractingState;
    }

    private void HandleInteractingState(bool isEquipped)
    {
        canInteracting = !isEquipped;
        if (!canInteracting)
        {
            StopInteract();
        }
    }
    void Update()
    {
        if (!canInteracting) return;
        if (currentInteractable != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape) && currentInteractable.IsInteract())
            {
                currentInteractable.StopInteract();
            }
            if (currentInteractable.IsInteract())
                return;
        }
        CheckForInteractable();
        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null && currentInteractable.CanInteract())
        {
            currentInteractable.Interact();
            StopInteract();
        }
    }

    private void StopInteract()
    {
        if (currentInteractable != null)
        {
            currentInteractable?.OnLoseFocus();
            InstanceHandler.GetInstance<GameViewManager>().HideView<InteractionView>();
        }
    }

    private void CheckForInteractable()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange))
        {
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
                    OnInteractableChanged?.Invoke(currentInteractable);
                }
                return;
            }
        }

        if (currentInteractable != null)
        {
            currentInteractable?.OnLoseFocus();
            InstanceHandler.GetInstance<GameViewManager>().HideView<InteractionView>();
            currentInteractable = null;
        }
    }


}
