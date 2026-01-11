
public enum ThreatCodeName
{
    Alpha,
    Beta,
    Charlie,
    Delta,
}

public static class ThreatExtensions
{
    public static string ToDisplayString(this ThreatCodeName code)
    {
        return "AR-" + code.ToString();
    }
}



[System.Serializable]
public struct NetworkThreatData
{
    public int threatID;
    public int codeEnumIndex;
    public int realObjectIndex;
    public float startDistance;
    public float speed;
}

[System.Serializable]
public struct NetworkBoardData
{
    public int objectIndex;
    public int[] symbolIndices;
}