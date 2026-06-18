using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
public enum LanguageType
{
    Turkish,
    English,
    Spanish
}

public class LocalizationManager : MonoBehaviour
{

    [SerializeField] private Button turkishButton;
    [SerializeField] private Button englishButton;

    private void Awake()
    {
        turkishButton.onClick.AddListener(() => SetLanguage(LanguageType.Turkish));
        englishButton.onClick.AddListener(() => SetLanguage(LanguageType.English));
    }
    public void SetLanguage(LanguageType languageType)
    {
        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.LocaleName.Equals(languageType.ToString()))
            {
                LocalizationSettings.SelectedLocale = locale;
                break;
            }
        }
    }
}
