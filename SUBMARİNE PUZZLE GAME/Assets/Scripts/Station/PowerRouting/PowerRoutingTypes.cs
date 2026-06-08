using System;
using System.Collections.Generic;

public enum LightColor
{
    Red,
    Purple,
    Yellow,
    Green
}

public enum SwitchState
{
    Down,
    Up
}

public struct PowerRoutingPuzzleData
{
    public int[] TechDigits;
    public int[] EngDigits;
    public LightColor[] LightSequence;

    public SwitchState[] ExpectedSmallSwitches;
    public Dictionary<LightColor, int> ExpectedColorSwitches;
}