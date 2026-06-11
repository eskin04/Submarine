
[System.Serializable]
public struct WaveConfig
{
    public int amplitude;
    public int frequency;
    public int phase;

    public bool Equals(WaveConfig other) =>
        amplitude == other.amplitude && frequency == other.frequency && phase == other.phase;
}

[System.Serializable]
public struct EquationData
{
    public string displayString;
    public int targetAnswer;
}

[System.Serializable]
public struct ChannelData
{
    public WaveConfig targetWave;
    public int symbolID;
    public EquationData equation;
}