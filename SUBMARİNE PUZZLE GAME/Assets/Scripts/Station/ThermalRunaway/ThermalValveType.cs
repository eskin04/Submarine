public enum ThermalValveType
{
    Common,
    Front,
    Back
}

[System.Serializable]
public struct NeedleZoneData
{
    public string zoneName;
    public float minPressure;
    public float maxPressure;
    public float coolingAmount;
    public float bottleneckChance;
}

[System.Serializable]
public struct RhythmData
{
    public string rhythmName;

    public float weight;

    public float eMin;
    public float eMax;
    public float pumpMultiplier;
}