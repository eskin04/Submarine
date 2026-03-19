using UnityEngine;
using PurrNet;
using System.Collections.Generic;

public enum LockdownColor
{
    Purple,
    Red,
    Blue,
    Green,
    Brown,
    Yellow,
    White,
    Pink,
}

public enum LockDownStationState
{
    Idle,
    Active,
    Solved
}



public enum RegionID
{
    T1, T2, T3, T4, T5, T6, T7, T8,
    M1, M2, M3, M4, M5, M6, M7, M8
}

[System.Serializable]
public struct LegendData
{
    public LockdownColor color;
    public RegionID assignedRegion;
}

[System.Serializable]
public struct SequenceData
{
    public LockdownColor color;
    public int targetNumber;
}

[System.Serializable]
public struct DigitConfig
{
    public int digits;
    public int count;
}

[System.Serializable]
public struct CodeVariation
{
    public string variationName;
    public List<DigitConfig> digitConfigs;
    public int techRegionCount;
    public int engRegionCount;
}