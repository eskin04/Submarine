using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class Keycard_EngineerUI : MonoBehaviour
{
    [Header("Referanslar")]
    public TextMeshProUGUI infoText;
    private LocalizedString uiInfoString = new LocalizedString { TableReference = "UI_General" };
    private int activeCardID = -1;
    private ConditionData activeCondition;

    private void Awake()
    {
        uiInfoString.StringChanged += OnTranslatedTextReady;
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        uiInfoString.StringChanged -= OnTranslatedTextReady;
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(Locale newLocale)
    {
        UpdateSocketVisual(activeCardID, activeCondition);
    }

    private void OnTranslatedTextReady(string text)
    {
        if (infoText != null)
            infoText.text = text;
    }

    public void HandlePuzzleSync(List<CardData> syncedCards)
    {
        SetWaitingForInput();
    }

    public void SetWaitingForInput()
    {
        activeCardID = -1;

        uiInfoString.Arguments = null;
        uiInfoString.TableEntryReference = "ui_waiting_input";
        uiInfoString.RefreshString();
    }
    public void UpdateSocketVisual(int cardID, ConditionData condition)
    {
        activeCardID = cardID;
        activeCondition = condition;

        if (cardID == -1)
        {
            SetWaitingForInput();
            return;
        }

        var (templateKey, arguments) = Keycard_ConditionParser.GetLocalizationData(condition);

        uiInfoString.Arguments = new object[] { arguments };

        uiInfoString.TableEntryReference = templateKey;
        uiInfoString.RefreshString();
    }
}