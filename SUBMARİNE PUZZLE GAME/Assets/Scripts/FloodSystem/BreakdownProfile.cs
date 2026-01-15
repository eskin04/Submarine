using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBreakdownProfile", menuName = "FloodSystem/Breakdown Profile")]
public class BreakdownProfile : ScriptableObject
{
    [Header("Dalga Listesi")]
    public List<BreakdownWave> waves;
}

[System.Serializable]
public class BreakdownWave
{
    public string waveName;
    public float startTime;
    public float endTime;

    [Range(0, 100)] public float startProbability = 10f;
    [Range(0, 100)] public float endProbability = 100f;
}