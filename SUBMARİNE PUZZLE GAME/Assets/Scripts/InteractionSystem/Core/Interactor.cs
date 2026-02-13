using System;
using PurrNet;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField] private float interactionRange = 3f;
    public static RaycastHit? CurrentLookHit { get; private set; }
    public static Action<IInteractable> OnInteractableChanged;
    public static Action<IInteractable> OnInteract;
    private IInteractable currentInteractable;
    private KeyCode interactKey = KeyCode.E;


    void Update()
    {

        CheckForInteractable();
        if (Input.GetKeyDown(interactKey) && currentInteractable != null && currentInteractable.CanInteract())
        {
            currentInteractable.Interact();
            OnInteract?.Invoke(currentInteractable);
        }
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
                    interactKey = currentInteractable.InteractKey;
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
