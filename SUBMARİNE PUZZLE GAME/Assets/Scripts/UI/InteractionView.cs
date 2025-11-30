using TMPro;
using UnityEngine;

public class InteractionView : View
{
    [SerializeField] private TMP_Text interactionText;
    [SerializeField] private string keyHintText = "[E]";

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
