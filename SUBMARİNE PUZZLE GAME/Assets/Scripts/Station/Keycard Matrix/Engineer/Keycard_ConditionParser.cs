using UnityEngine;

public static class Keycard_ConditionParser
{
    public static string ParseToEnglish(ConditionData cond)
    {
        string mustBe = cond.IsPositive ? "MUST BE" : "MUST NOT BE";
        string trait = GetTraitString(cond);

        switch (cond.TemplateType)
        {
            case ConditionTemplateType.SpecificSocketTrait:
                return $"If this card is in the system, the card on SOCKET {cond.TargetSocket1 + 1} {mustBe} {trait}.";

            case ConditionTemplateType.GlobalTraitPresence:
                return $"If this card is in the system, there {mustBe} a {trait} card present.";

            case ConditionTemplateType.RelativeDirectionTrait:
                string dir1 = cond.Direction == RelativeDirection.Left ? "LEFT" : "RIGHT";
                return $"If this card is in the system, the card to its {dir1} {mustBe} {trait}.";

            case ConditionTemplateType.ForbiddenSockets:
                return $"This card must NOT be placed on SOCKET {cond.TargetSocket1 + 1} or SOCKET {cond.TargetSocket2 + 1}.";

            case ConditionTemplateType.RelativeSharedCategory:
                string dir2 = cond.Direction == RelativeDirection.Left ? "LEFT" : "RIGHT";
                string category = GetCategoryString(cond.TargetCategory);
                return $"If this card is in the system, it must share the same {category} with the card to its {dir2}.";

            case ConditionTemplateType.ExactGlobalTraitCount:
                string plural = cond.TargetCount > 1 ? "cards" : "card";
                return $"If this card is in the system, there must be exactly {cond.TargetCount} {trait} {plural} present.";

            default:
                return "ERR_NO_DATA_FOUND";
        }
    }

    private static string GetTraitString(ConditionData cond)
    {
        switch (cond.TargetCategory)
        {
            case PropertyCategory.Color: return cond.TargetColor.ToString().ToUpper();
            case PropertyCategory.Type:
                if (cond.TargetType == CardType.Crack1) return "broken in the middle";
                if (cond.TargetType == CardType.Crack2) return "broken edge";
                if (cond.TargetType == CardType.Crack3) return "hole in the middle";
                if (cond.TargetType == CardType.Crack4) return "hole on top";
                return cond.TargetType.ToString().ToUpper();
            case PropertyCategory.Detail: return cond.TargetDetail.ToString().ToUpper();
            default: return "";
        }
    }

    private static string GetCategoryString(PropertyCategory category)
    {
        return category.ToString().ToUpper();
    }
}