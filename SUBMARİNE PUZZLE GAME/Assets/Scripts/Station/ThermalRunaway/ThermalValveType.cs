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