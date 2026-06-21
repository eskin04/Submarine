using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class RoE_EngineerDisplay : MonoBehaviour
{
    [Header("References")]
    public Image[] symbolImages;
    public RoE_StationManager stationManager;
    public TextMeshProUGUI ruleText;
    public TextMeshProUGUI targetCodeText;
    public GameObject bgPanel;

    private string currentDisplayedCode = "";

    private LocalizedString uiRuleString = new LocalizedString();
    private LocalizedString uiTargetCodeString = new LocalizedString { TableReference = "UI_General" };

    private RoE_RuleData activeRuleData;
    private bool activeIsShoot;
    private ObjectCategory activeCatX;
    private ObjectCategory activeCatY;


    private void OnEnable()
    {
        // Event'e baştan abone oluyoruz
        uiRuleString.StringChanged += OnTranslatedRuleReady;
        uiTargetCodeString.StringChanged += OnTranslatedTargetCodeReady;
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        uiRuleString.StringChanged -= OnTranslatedRuleReady;
        uiTargetCodeString.StringChanged -= OnTranslatedTargetCodeReady;
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    private void OnTranslatedTargetCodeReady(string translatedText)
    {
        if (targetCodeText != null)
        {
            targetCodeText.text = translatedText;
        }
    }

    private void OnLanguageChanged(Locale newLocale)
    {

        if (activeRuleData != null)
        {
            UpdateRuleDisplay(activeRuleData, activeIsShoot, activeCatX, activeCatY);
        }
    }

    void Start()
    {
        SetDisplayActive(false);
    }

    public void StartDisplaySequence(List<int> symbolIndices, string codeName)
    {
        currentDisplayedCode = codeName;
        uiTargetCodeString.TableEntryReference = codeName;


        DisplayAllSymbols(symbolIndices);
    }

    private void DisplayAllSymbols(List<int> indices)
    {
        for (int i = 0; i < symbolImages.Length; i++)
        {
            symbolImages[i].enabled = false;
        }

        for (int i = 0; i < indices.Count; i++)
        {
            if (i >= symbolImages.Length) break;

            int index = indices[i];

            if (index >= 0 && index < stationManager.availableSymbols.Count)
            {
                var symbolData = stationManager.availableSymbols[index];

                symbolImages[i].sprite = symbolData.icon;
                symbolImages[i].enabled = true;
            }
        }
    }

    public void OnTargetDestroyed(string destroyedCodeName)
    {
        if (currentDisplayedCode == destroyedCodeName)
        {

            for (int i = 0; i < symbolImages.Length; i++)
            {
                symbolImages[i].enabled = false;
            }

            if (targetCodeText != null)
            {
                uiTargetCodeString.TableEntryReference = "Signal Lost";
                targetCodeText.color = Color.red;
            }

            currentDisplayedCode = "";
        }
    }

    public void UpdateRuleDisplay(RoE_RuleData ruleData, bool isShoot, ObjectCategory catX, ObjectCategory catY)
    {
        activeRuleData = ruleData;
        activeIsShoot = isShoot;
        activeCatX = catX;
        activeCatY = catY;
        string actionKey = isShoot ? "Shoot" : "Pass";
        string translatedAction = LocalizationHelper.GetTranslatedText("UI_General", actionKey);

        string xKey = catX.ToString();
        string translatedX = LocalizationHelper.GetTranslatedText("UI_General", xKey);

        string yKey = catY.ToString();
        string translatedY = LocalizationHelper.GetTranslatedText("UI_General", yKey);

        var ruleArguments = new Dictionary<string, string>
        {
            { "ACTION", translatedAction },
            { "X", translatedX },
            { "Y", translatedY }
        };

        uiRuleString.Arguments = new object[] { ruleArguments };

        uiRuleString.TableReference = ruleData.localizedRuleDescription.TableReference;
        uiRuleString.TableEntryReference = ruleData.localizedRuleDescription.TableEntryReference;

        uiRuleString.RefreshString();
    }

    private void OnTranslatedRuleReady(string finalRuleText)
    {
        if (ruleText != null)
        {
            ruleText.text = finalRuleText;
        }
    }

    public void SetDisplayActive(bool isActive)
    {
        if (bgPanel != null)
        {
            bgPanel.SetActive(isActive);
        }
    }
}