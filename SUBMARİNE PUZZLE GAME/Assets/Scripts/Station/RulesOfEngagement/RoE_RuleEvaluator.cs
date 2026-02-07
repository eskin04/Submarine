using UnityEngine;

public static class RoE_RuleEvaluator
{
    public static bool ShouldShoot(RoE_ObjectData objData, RoE_RuleData currentRule)
    {
        var cats = objData.categories;

        switch (currentRule.logicType)
        {

            case RuleLogicType.Shoot_Only_Mechanical:
                return cats.Contains(ObjectCategory.Mechanical);

            case RuleLogicType.Shoot_Only_Biological:
                return cats.Contains(ObjectCategory.Biological);

            case RuleLogicType.Shoot_Only_Glowing:
                return cats.Contains(ObjectCategory.Glowing);

            case RuleLogicType.Shoot_All_Explosive:
                return cats.Contains(ObjectCategory.Explosive);



            case RuleLogicType.Shoot_All_Except_Valuable:
                return !cats.Contains(ObjectCategory.Valuable);

            case RuleLogicType.Shoot_All_Except_Biological:
                return !cats.Contains(ObjectCategory.Biological);

            case RuleLogicType.Shoot_All_Except_Mechanical:
                return !cats.Contains(ObjectCategory.Mechanical);

            case RuleLogicType.Shoot_Glowing_Except_Valuable:
                return cats.Contains(ObjectCategory.Glowing) && !cats.Contains(ObjectCategory.Valuable);



            case RuleLogicType.Shoot_Biological_AND_Glowing:
                return cats.Contains(ObjectCategory.Biological) && cats.Contains(ObjectCategory.Glowing);

            case RuleLogicType.Shoot_Explosive_AND_Mechanical:
                return cats.Contains(ObjectCategory.Explosive) && cats.Contains(ObjectCategory.Mechanical);



            case RuleLogicType.Shoot_Biological_OR_Glowing:
                return cats.Contains(ObjectCategory.Biological) || cats.Contains(ObjectCategory.Glowing);

            case RuleLogicType.Shoot_Explosive_OR_Glowing:
                return cats.Contains(ObjectCategory.Explosive) || cats.Contains(ObjectCategory.Glowing);

            case RuleLogicType.Shoot_Mechanical_OR_Explosive:
                return cats.Contains(ObjectCategory.Mechanical) || cats.Contains(ObjectCategory.Explosive);



            case RuleLogicType.Shoot_Only_Single_Category:
                return cats.Count == 1;

            case RuleLogicType.Shoot_Only_Double_Category:
                return cats.Count == 2;


            default:
                return false;
        }
    }
}