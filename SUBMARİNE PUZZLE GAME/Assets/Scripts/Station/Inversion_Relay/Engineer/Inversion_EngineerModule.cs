using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings; // Dil takibi için eklendi

public class Inversion_EngineerModule : MonoBehaviour
{
    [Header("References")]
    public Inversion_Relay_StationManager stationManager;
    public TextMeshProUGUI[] ruleTexts = new TextMeshProUGUI[5];

    [Header("Glitch Effect")]
    public float currentGlitchIntensity = 0f;

    private EngineerRule[] currentRules = new EngineerRule[5];
    private string glitchCharacters = "!@#$%^&*()_+{}|:<>?~";

    private LocalizedString[] uiRuleStrings = new LocalizedString[5];

    private void Awake()
    {
        for (int i = 0; i < 5; i++)
        {
            uiRuleStrings[i] = new LocalizedString();

            int index = i;
            uiRuleStrings[i].StringChanged += (translatedText) =>
            {
                if (ruleTexts[index] != null)
                {
                    ruleTexts[index].text = translatedText;
                }
            };
        }

        // Dil değişimini dinliyoruz
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(Locale newLocale)
    {
        if (currentRules != null && currentRules.Length > 0)
        {
            UpdateRuleDisplay();
        }
    }

    public void SetupRules(EngineerRule[] rules)
    {
        currentRules = rules;
        UpdateRuleDisplay();
    }

    public void SetGlitchIntensity(float intensity)
    {
        if (Mathf.Abs(currentGlitchIntensity - intensity) > 0.05f)
        {
            currentGlitchIntensity = Mathf.Clamp01(intensity);
            UpdateRuleDisplay();
        }
    }

    private void UpdateRuleDisplay()
    {
        for (int i = 0; i < 5; i++)
        {
            if (ruleTexts[i] == null) continue;

            string finalStateStr;

            if (currentRules[i].IsCorrupted)
            {
                string cleanText = LocalizationHelper.GetTranslatedText("UI_General", "inv_data_corrupted");

                finalStateStr = $"<color=red>{cleanText}</color>";
            }
            else
            {
                string stateKey = $"inv_state_{currentRules[i].TargetState.ToString().ToLower()}";
                string translatedState = LocalizationHelper.GetTranslatedText("UI_General", stateKey);

                if (currentGlitchIntensity > 0f)
                {
                    translatedState = ScrambleString(translatedState, currentGlitchIntensity);
                }

                finalStateStr = translatedState;
            }

            var args = new Dictionary<string, string>
            {
                { "Letter", currentRules[i].Letter.ToString() },
                { "State", finalStateStr }
            };

            uiRuleStrings[i].Arguments = new object[] { args };
            uiRuleStrings[i].TableReference = "UI_General";
            uiRuleStrings[i].TableEntryReference = "inv_pipe_rule";
            uiRuleStrings[i].RefreshString();
        }
    }

    private string ScrambleString(string original, float intensity)
    {
        int maxLengthVariation = Mathf.RoundToInt(intensity * 4f);

        int randomOffset = Random.Range(-maxLengthVariation, maxLengthVariation + 1);
        int targetLength = original.Length + randomOffset;

        targetLength = Mathf.Max(2, targetLength);

        char[] scrambled = new char[targetLength];

        for (int i = 0; i < targetLength; i++)
        {
            if (i < original.Length && Random.value >= intensity)
            {
                scrambled[i] = original[i];
            }
            else
            {
                scrambled[i] = glitchCharacters[Random.Range(0, glitchCharacters.Length)];
            }
        }

        return new string(scrambled);
    }
}