using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class RoE_RuleEvaluator
{
    public static bool ShouldShoot(
        ActiveThreat target,
        RoE_RuleData currentRule,
        RoE_ThreatManager threatManager,
        RoE_ObjectData previousDestroyed,
        RoE_ObjectData previousPassed,
        List<RoE_ObjectData> lastTwoObjects,
        int actionCount,
        float waterLevel,
        ObjectCategory catX,
        ObjectCategory catY,
        bool isRuleActionShoot)
    {
        if (target == null || target.realIdentity == null || target.realIdentity.linkedObject == null) return false;

        var cats = target.realIdentity.linkedObject.categories;

        var activeThreats = threatManager.activeThreats.Where(t => !t.isDestroyed).ToList();
        ActiveThreat fastestThreat = activeThreats.OrderByDescending(t => t.approachSpeed).FirstOrDefault();
        ActiveThreat slowestThreat = activeThreats.OrderBy(t => t.approachSpeed).FirstOrDefault();

        bool isFastest = fastestThreat != null && fastestThreat == target;
        bool isSlowest = slowestThreat != null && slowestThreat == target;
        bool isInnerRadar = target.currentDistance <= currentRule.innerRadarDistanceThreshold;

        bool isConditionMet = false;

        switch (currentRule.logicType)
        {
            case RuleLogicType.Pass_If_3rd_Object:
            case RuleLogicType.Shoot_If_1st_Object:
                isConditionMet = (currentRule.logicType == RuleLogicType.Pass_If_3rd_Object) ? (actionCount == 3) : (actionCount == 1);
                break;

            case RuleLogicType.Pass_X:
            case RuleLogicType.Shoot_X_If_Faster_Than_2:
                isConditionMet = cats.Contains(catX);
                if (currentRule.logicType == RuleLogicType.Shoot_X_If_Faster_Than_2)
                    isConditionMet = isConditionMet && (target.approachSpeed > 2f);
                break;

            case RuleLogicType.Shoot_Y_In_Inner_Radar:
                isConditionMet = cats.Contains(catY) && isInnerRadar;
                break;

            case RuleLogicType.Shoot_Slowest:
                isConditionMet = isSlowest;
                break;

            case RuleLogicType.Pass_Explosive_If_Water_Above_50:
                isConditionMet = (waterLevel > 50f && cats.Contains(ObjectCategory.Explosive));
                break;

            case RuleLogicType.Shoot_Fastest_If_Multiple_And_Not_X:
                isConditionMet = (activeThreats.Count > 1 && isFastest && !cats.Contains(catX));
                break;

            case RuleLogicType.Shoot_If_Faster_Than_3_And_Explosive_Or_Mechanical:
                isConditionMet = (target.approachSpeed > 3f && (cats.Contains(ObjectCategory.Explosive) || cats.Contains(ObjectCategory.Mechanical)));
                break;

            case RuleLogicType.Shoot_X_ONLY_In_Inner_Radar:
                isConditionMet = (cats.Contains(catX) && isInnerRadar);
                break;

            case RuleLogicType.Pass_If_Shares_Category_With_Previous:
                isConditionMet = (lastTwoObjects.Count > 0 && cats.Intersect(lastTwoObjects.Last().categories).Any());
                break;

            case RuleLogicType.Shoot_Fastest_If_Water_Below_25:
                isConditionMet = (waterLevel < 25f && isFastest);
                break;

            case RuleLogicType.Shoot_X_If_Water_Below_25_Shoot_Y_If_Above_75:
                isConditionMet = ((waterLevel < 25f && cats.Contains(catX)) || (waterLevel > 75f && cats.Contains(catY)));
                break;

            case RuleLogicType.Shoot_Y_If_Previous_Passed_Was_X:
                isConditionMet = (previousPassed != null && previousPassed.categories.Contains(catX) && cats.Contains(catY));
                break;

            case RuleLogicType.Shoot_If_Shares_Category_With_Last_Two_And_Extra:
                isConditionMet = (lastTwoObjects.Count >= 2 &&
                                  cats.Intersect(lastTwoObjects[0].categories).Any() &&
                                  cats.Intersect(lastTwoObjects[1].categories).Any() &&
                                  cats.Count > 1);
                break;

            case RuleLogicType.Pass_If_Previous_Destroyed_Was_X:
                isConditionMet = (previousDestroyed != null && previousDestroyed.categories.Contains(catX));
                break;

            case RuleLogicType.Shoot_Fastest_If_Not_X:
                isConditionMet = (isFastest && !cats.Contains(catX));
                break;

            case RuleLogicType.Shoot_X_AND_Y:
                isConditionMet = (cats.Contains(catX) && cats.Contains(catY));
                break;

            case RuleLogicType.Shoot_X_OR_Y:
                isConditionMet = (cats.Contains(catX) || cats.Contains(catY));
                break;

            case RuleLogicType.Pass_Single_Category:
                isConditionMet = (cats.Count == 1);
                break;

            case RuleLogicType.Shoot_If_Slower_Than_4_And_Inner_Radar:
                isConditionMet = (target.approachSpeed < 4f && isInnerRadar);
                break;

            case RuleLogicType.Shoot_Triple_Category:
                isConditionMet = (cats.Count == 3);
                break;
        }
        return isRuleActionShoot == isConditionMet;
    }
}