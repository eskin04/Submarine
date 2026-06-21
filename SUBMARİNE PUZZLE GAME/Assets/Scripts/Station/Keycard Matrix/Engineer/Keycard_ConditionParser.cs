using System.Collections.Generic;
using UnityEngine;

public static class Keycard_ConditionParser
{
    // Artık string değil, UI'ın kullanacağı (Şablon Key'i, Argümanlar) ikilisini döndürüyoruz.
    public static (string templateKey, Dictionary<string, string> arguments) GetLocalizationData(ConditionData cond)
    {
        var args = new Dictionary<string, string>();
        string templateKey = "";

        // 1. Trait (Özellik) çeviri anahtarını al ve anında çevir
        string traitKey = GetTraitKey(cond);
        if (!string.IsNullOrEmpty(traitKey))
        {
            args.Add("Trait", LocalizationHelper.GetTranslatedText("UI_General", traitKey));
        }

        // 2. Enum durumuna göre doğru Şablon Key'ini ve sayısal argümanları ayarla
        switch (cond.TemplateType)
        {
            case ConditionTemplateType.SpecificSocketTrait:
                templateKey = cond.IsPositive ? "cond_socket_pos" : "cond_socket_neg";
                args.Add("Socket", (cond.TargetSocket1 + 1).ToString());
                break;

            case ConditionTemplateType.GlobalTraitPresence:
                templateKey = cond.IsPositive ? "cond_global_pos" : "cond_global_neg";
                break;

            case ConditionTemplateType.RelativeDirectionTrait:
                templateKey = cond.IsPositive ? "cond_relative_pos" : "cond_relative_neg";
                string dirKey = cond.Direction == RelativeDirection.Left ? "dir_left" : "dir_right";
                args.Add("Dir", LocalizationHelper.GetTranslatedText("UI_General", dirKey));
                break;

            case ConditionTemplateType.ForbiddenSockets:
                templateKey = "cond_forbidden";
                args.Add("Socket1", (cond.TargetSocket1 + 1).ToString());
                args.Add("Socket2", (cond.TargetSocket2 + 1).ToString());
                break;

            case ConditionTemplateType.RelativeSharedCategory:
                templateKey = "cond_shared_cat";
                string dir2Key = cond.Direction == RelativeDirection.Left ? "dir_left" : "dir_right";
                args.Add("Dir", LocalizationHelper.GetTranslatedText("UI_General", dir2Key));
                string catKey = $"cat_{cond.TargetCategory.ToString().ToLower()}";
                args.Add("Category", LocalizationHelper.GetTranslatedText("UI_General", catKey));
                break;

            case ConditionTemplateType.ExactGlobalTraitCount:
                templateKey = "cond_exact_count";
                args.Add("Count", cond.TargetCount.ToString());
                break;
        }

        return (templateKey, args);
    }

    // Enum'ları Google Sheets'teki karşılık gelen Key'lere dönüştürür
    private static string GetTraitKey(ConditionData cond)
    {
        switch (cond.TargetCategory)
        {
            case PropertyCategory.Color:
                return cond.TargetColor.ToString();
            case PropertyCategory.Type:
                return $"trait_type_{cond.TargetType.ToString().ToLower()}";
            case PropertyCategory.Detail:
                return $"trait_detail_{cond.TargetDetail.ToString().ToLower()}";
            default:
                return "";
        }
    }
}