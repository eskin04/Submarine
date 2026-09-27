using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using System.Collections.Generic;

[RequireComponent(typeof(TextMeshPro))]
public class WorldTextLocalizer : MonoBehaviour
{
    private TextMeshPro worldText;

    public LocalizedString localizedString;

    [Header("Smart String Parameters (Optional)")]
    public List<SmartArgument> smartArguments = new List<SmartArgument>();

    [System.Serializable]
    public struct SmartArgument
    {
        public string Key;
        public string Value;
    }

    private void Awake()
    {
        worldText = GetComponent<TextMeshPro>();

        if (smartArguments != null && smartArguments.Count > 0)
        {
            var argsDict = new Dictionary<string, string>();

            foreach (var arg in smartArguments)
            {
                if (!argsDict.ContainsKey(arg.Key))
                {
                    argsDict.Add(arg.Key, arg.Value);
                }
            }

            localizedString.Arguments = new object[] { argsDict };
        }
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