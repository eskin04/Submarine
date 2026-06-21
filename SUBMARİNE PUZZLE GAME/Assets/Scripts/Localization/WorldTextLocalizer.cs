using UnityEngine;
using TMPro;
using UnityEngine.Localization;

[RequireComponent(typeof(TextMeshPro))]
public class WorldTextLocalizer : MonoBehaviour
{
    private TextMeshPro worldText;

    public LocalizedString localizedString;

    private void Awake()
    {
        worldText = GetComponent<TextMeshPro>();
    }

    private void OnEnable()
    {
        localizedString.StringChanged += OnTranslatedTextChanged;
    }

    private void OnDisable()
    {
        localizedString.StringChanged -= OnTranslatedTextChanged;
    }

    private void OnTranslatedTextChanged(string translatedText)
    {
        if (worldText != null)
        {
            worldText.text = translatedText;
        }
    }
}