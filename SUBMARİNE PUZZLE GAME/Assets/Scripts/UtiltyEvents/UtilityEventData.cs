using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUtilityEvent", menuName = "Station/Utility Event Data")]
public class UtilityEventData : ScriptableObject
{
    [Header("Settings")]
    public float baseWeight = 10f;
    public StatType targetStatName;
    public int threshold;
    public float weightMultiplier;

    public enum ConditionType { GreaterThan, LessThan, Equals }
    public ConditionType condition = ConditionType.GreaterThan;

    public float GetCurrentWeight(GameStatistics stats)
    {
        float finalWeight = baseWeight;

        if (stats != null)
        {

            finalWeight *= Evaluate(stats);

        }
        return Mathf.Max(0, finalWeight);
    }



    public float Evaluate(GameStatistics stats)
    {
        if (stats == null) return 1f;

        int currentValue = stats.GetStat(targetStatName);
        bool isMet = false;

        switch (condition)
        {
            case ConditionType.GreaterThan: isMet = currentValue > threshold; break;
            case ConditionType.LessThan: isMet = currentValue < threshold; break;
            case ConditionType.Equals: isMet = currentValue == threshold; break;
        }

        return isMet ? weightMultiplier : 1f;
    }
}