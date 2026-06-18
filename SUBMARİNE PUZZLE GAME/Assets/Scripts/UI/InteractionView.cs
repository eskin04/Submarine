using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class InteractionView : View
{
    [SerializeField] private TMP_Text interactionText;
    private LocalizedString localizedName = new LocalizedString { TableReference = "UI_General" };

    private string currentKeyHint;

    void Awake()
    {
        Interactor.OnInteractableChanged += UpdateInteractionText;
        localizedName.StringChanged += OnTranslatedTextChanged;
    }

    private void OnDestroy()
    {
        Interactor.OnInteractableChanged -= UpdateInteractionText;
        localizedName.StringChanged -= OnTranslatedTextChanged;
    }

    private void UpdateInteractionText(IInteractable interactable)
    {
        if (interactable == null)
        {
            return;
        }
        currentKeyHint = $"[{interactable.InteractKeys[0]}]";
        localizedName.TableEntryReference = interactable.DisplayName;
    }

    private void OnTranslatedTextChanged(string translatedName)
    {

        interactionText.text = $"{currentKeyHint} {translatedName}";
    }

    public override void OnShow()
    {

    }

    public override void OnHide()
    {
    }
}
