

public enum WireColor
{
    Yellow,
    Green,
    Blue,
    Red,
}

public enum StatusLightState
{
    Red,
    Yellow,
    Green
}

[System.Serializable]
public class CableData
{
    public int cableID;
    public WireColor physicalColor;

    public WireColor outputLightColor;

    public int correctPortIndex;
    public int currentPortIndex;
}

[System.Serializable]
public class SwitchData
{
    public int switchIndex;
    public WireColor labelColor;
    public bool isOn;
}