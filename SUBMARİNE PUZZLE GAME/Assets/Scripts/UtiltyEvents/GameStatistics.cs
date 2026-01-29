using System.Collections.Generic;
using UnityEngine;

public class GameStatistics : MonoBehaviour
{
    private Dictionary<StatType, int> stats = new Dictionary<StatType, int>();

    private void OnEnable()
    {
        GlobalEvents.OnModifyStat += ModifyStat;
    }

    private void OnDisable()
    {
        GlobalEvents.OnModifyStat -= ModifyStat;
    }

    private void ModifyStat(StatType statType, int amount)
    {
        if (statType == StatType.None) return;

        if (!stats.ContainsKey(statType))
        {
            stats[statType] = 0;
        }
        stats[statType] += amount;

        Debug.Log($"STAT GÜNCELLENDİ: {statType} = {stats[statType]}");
    }

    public int GetStat(StatType statName)
    {
        if (stats.ContainsKey(statName)) return stats[statName];
        return 0;
    }
}

public enum StatType
{
    None,
    LockDown,
    BlackOut,
    Magnetic,


}