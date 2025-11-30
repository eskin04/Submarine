using System;
using PurrNet;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField] private float interactionRange = 3f;
    public static Action<IInteractable> OnInteractableChanged;
    private IInteractable currentInteractable;
    void Update()
    {
        CheckForInteractable();
        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null && currentInteractable.CanInteract())
        {
            currentInteractable.Interact();
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
