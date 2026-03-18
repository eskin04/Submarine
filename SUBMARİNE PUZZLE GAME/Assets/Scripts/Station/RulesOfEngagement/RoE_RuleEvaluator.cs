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
        ObjectCategory catY)
    {
        if (target == null || target.realIdentity == null || target.realIdentity.linkedObject == null) return false;

        var cats = target.realIdentity.linkedObject.categories;

        var activeThreats = threatManager.activeThreats.Where(t => !t.isDestroyed).ToList();
        ActiveThreat fastestThreat = activeThreats.OrderByDescending(t => t.approachSpeed).FirstOrDefault();
        ActiveThreat slowestThreat = activeThreats.OrderBy(t => t.approachSpeed).FirstOrDefault();

        bool isFastest = fastestThreat != null && fastestThreat == target;
        bool isSlowest = slowestThreat != null && slowestThreat == target;
        bool isInnerRadar = target.currentDistance <= currentRule.innerRadarDistanceThreshold;

        switch (currentRule.logicType)
        {
            case RuleLogicType.Pass_If_3rd_Object:
                return actionCount != 3;

            case RuleLogicType.Pass_X:
                return !cats.Contains(catX);

            case RuleLogicType.Shoot_Y_In_Inner_Radar:
                return cats.Contains(catY) && isInnerRadar;

            case RuleLogicType.Shoot_Slowest:
                return isSlowest;

            case RuleLogicType.Shoot_If_1st_Object:
                return actionCount == 1;

            case RuleLogicType.Pass_Explosive_If_Water_Above_50:
                if (waterLevel > 50f && cats.Contains(ObjectCategory.Explosive)) return false;
                return true;

            case RuleLogicType.Shoot_Fastest_If_Multiple_And_Not_X:
                if (activeThreats.Count > 1 && isFastest && !cats.Contains(catX)) return true;
                return false;

            case RuleLogicType.Shoot_If_Faster_Than_3_And_Explosive_Or_Mechanical:
                return target.approachSpeed > 3f &&
                      (cats.Contains(ObjectCategory.Explosive) || cats.Contains(ObjectCategory.Mechanical));

            case RuleLogicType.Shoot_X_ONLY_In_Inner_Radar:
                if (cats.Contains(catX)) return isInnerRadar;
                return false;

            case RuleLogicType.Pass_If_Shares_Category_With_Previous:
                if (lastTwoObjects.Count == 0) return true;
                bool sharesAny = cats.Intersect(lastTwoObjects.Last().categories).Any();
                return !sharesAny;

            case RuleLogicType.Shoot_Fastest_If_Water_Below_25:
                if (waterLevel < 25f) return isFastest;
                return false;

            case RuleLogicType.Shoot_X_If_Water_Below_25_Shoot_Y_If_Above_75:
                if (waterLevel < 25f) return cats.Contains(catX);
                if (waterLevel > 75f) return cats.Contains(catY);
                return false;

            case RuleLogicType.Shoot_Y_If_Previous_Passed_Was_X:
                if (previousPassed != null && previousPassed.categories.Contains(catX))
                {
                    return cats.Contains(catY);
                }
                return false;

            case RuleLogicType.Shoot_If_Shares_Category_With_Last_Two_And_Extra:
                if (lastTwoObjects.Count < 2) return false;
                bool sharesWithFirst = cats.Intersect(lastTwoObjects[0].categories).Any();
                bool sharesWithSecond = cats.Intersect(lastTwoObjects[1].categories).Any();
                return sharesWithFirst && sharesWithSecond && cats.Count > 1;

            case RuleLogicType.Pass_If_Previous_Destroyed_Was_X:
                if (previousDestroyed != null && previousDestroyed.categories.Contains(catX)) return false;
                return true;

            case RuleLogicType.Shoot_Fastest_If_Not_X:
                return isFastest && !cats.Contains(catX);

            case RuleLogicType.Shoot_X_AND_Y:
                return cats.Contains(catX) && cats.Contains(catY);

            case RuleLogicType.Shoot_X_OR_Y:
                return cats.Contains(catX) || cats.Contains(catY);

            case RuleLogicType.Pass_Single_Category:
                return cats.Count > 1;

            case RuleLogicType.Shoot_If_Slower_Than_4_And_Inner_Radar:
                return target.approachSpeed < 4f && isInnerRadar;

            case RuleLogicType.Shoot_X_If_Faster_Than_2:
                return target.approachSpeed > 2f && cats.Contains(catX);

            case RuleLogicType.Shoot_Triple_Category:
                return cats.Count == 3;

            default:
                return false;
        }
    }
}