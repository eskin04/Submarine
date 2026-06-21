using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
public class RoE_HandbookItem : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI typeText2;
    public TextMeshProUGUI typeText3;
    private LocalizedString uiLocalizeString = new LocalizedString();
    private List<ObjectCategory> currentCategories;


    void OnEnable()
    {
        uiLocalizeString.StringChanged += UpdateTranslatedName;
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        uiLocalizeString.StringChanged -= UpdateTranslatedName;
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(Locale newLocale)
    {
        UpdateCategoriesText();
    }

    public void Setup(RoE_ObjectData data, List<ObjectCategory> shuffledCats)
    {
        uiLocalizeString.TableReference = data.objectName.TableReference;
        uiLocalizeString.TableEntryReference = data.objectName.TableEntryReference;

        currentCategories = shuffledCats;
        UpdateCategoriesText();
        typeText.text = shuffledCats.Count > 0 ? shuffledCats[0].ToString() : "";
        typeText2.text = shuffledCats.Count > 1 ? shuffledCats[1].ToString() : "";

        if (typeText3 != null)
            typeText3.text = shuffledCats.Count > 2 ? shuffledCats[2].ToString() : "";
    }

    private void UpdateCategoriesText()
    {
        if (currentCategories == null) return;

        typeText.text = currentCategories.Count > 0 ? GetTranslatedCategory(currentCategories[0]) : "";
        typeText2.text = currentCategories.Count > 1 ? GetTranslatedCategory(currentCategories[1]) : "";

        if (typeText3 != null)
            typeText3.text = currentCategories.Count > 2 ? GetTranslatedCategory(currentCategories[2]) : "";
    }

    private string GetTranslatedCategory(ObjectCategory category)
    {

        return LocalizationHelper.GetTranslatedText("UI_General", category.ToString());
    }

    private void UpdateTranslatedName(string translatedText)
    {
        if (nameText != null)
        {
            nameText.text = translatedText;
        }
    }

}