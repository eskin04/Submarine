using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class LocalizationHelper
{
    public static string GetTranslatedText(string tableName, string key)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString(tableName, key);
    }
}