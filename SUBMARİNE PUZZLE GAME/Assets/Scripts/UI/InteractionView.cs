using TMPro;
using UnityEngine;

public class InteractionView : View
{
    [SerializeField] private TMP_Text interactionText;

    void Awake()
    {
        Interactor.OnInteractableChanged += UpdateInteractionText;
    }

    private void OnDestroy()
    {
        Interactor.OnInteractableChanged -= UpdateInteractionText;
    }

    public void UpdateInteractionText(IInteractable ınteractable)
    {
        if (ınteractable == null)
        {
            interactionText.text = "";
            return;
        }
        string interactableName = ınteractable.DisplayName;
        string keyHintText = $"[{ınteractable.InteractKeys[0]}]";
        interactionText.text = $"{keyHintText} {interactableName}";
    }

    public override void OnShow()
    {

    }

    public override void OnHide()
    {
        interactionText.text = "";
    }
}
