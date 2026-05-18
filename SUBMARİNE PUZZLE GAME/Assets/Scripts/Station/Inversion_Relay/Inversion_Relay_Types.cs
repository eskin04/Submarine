using System;

public enum InversionPhase
{
    Normal,
    Inverted
}

public enum ValveState
{
    Empty = -1,
    Neutral = 0,
    Fill = 1
}

public enum PipeLetter
{
    A, B, C, D, E
}

[Serializable]
public struct EngineerRule
{
    public PipeLetter Letter;
    public ValveState TargetState;
    public bool IsCorrupted;
}