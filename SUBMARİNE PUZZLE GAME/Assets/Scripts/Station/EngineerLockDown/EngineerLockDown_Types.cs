
public enum EngineerLockDownStationState
{
    Idle,
    Active,
    Solved
}

[System.Serializable]
public struct EngineerLockDownStepData
{
    public int techNumber;
    public int engNumber;
    public int expectedTotal;
}