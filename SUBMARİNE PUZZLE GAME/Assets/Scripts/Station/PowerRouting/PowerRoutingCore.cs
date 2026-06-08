using System;
using System.Collections.Generic;

public class PowerRoutingCore
{
    private Random _random = new Random();


    public PowerRoutingPuzzleData GeneratePuzzle()
    {
        var data = new PowerRoutingPuzzleData
        {
            TechDigits = new int[4],
            EngDigits = new int[4],
            LightSequence = new LightColor[4],
            ExpectedSmallSwitches = new SwitchState[4],
            ExpectedColorSwitches = new Dictionary<LightColor, int>
            {
                { LightColor.Red, 0 },
                { LightColor.Purple, 0 },
                { LightColor.Yellow, 0 },
                { LightColor.Green, 0 }
            }
        };

        for (int i = 0; i < 4; i++)
        {
            data.TechDigits[i] = _random.Next(0, 10);
            data.EngDigits[i] = _random.Next(0, 10);

            LightColor randomColor = (LightColor)_random.Next(0, 4);
            data.LightSequence[i] = randomColor;

            data.ExpectedColorSwitches[randomColor]++;
        }

        for (int i = 0; i < 4; i++)
        {
            data.ExpectedSmallSwitches[i] = CalculateExpectedSwitchState(
                data.TechDigits[i],
                data.EngDigits[i],
                data.LightSequence[i]
            );
        }

        return data;
    }

    private SwitchState CalculateExpectedSwitchState(int techDigit, int engDigit, LightColor color)
    {
        switch (color)
        {
            case LightColor.Red:
                int diff = Math.Abs(techDigit - engDigit);
                return diff >= 5 ? SwitchState.Up : SwitchState.Down;

            case LightColor.Purple:
                int sumPurple = techDigit + engDigit;
                return (sumPurple % 2 == 0) ? SwitchState.Up : SwitchState.Down;

            case LightColor.Yellow:
                int sumYellow = techDigit + engDigit;
                return sumYellow >= 10 ? SwitchState.Up : SwitchState.Down;

            case LightColor.Green:
                return engDigit >= techDigit ? SwitchState.Up : SwitchState.Down;

            default:
                return SwitchState.Up;
        }
    }

    public bool ValidateSolution(
        PowerRoutingPuzzleData currentPuzzle,
        SwitchState[] playerSmallSwitches,
        Dictionary<LightColor, int> playerColorSwitchCounts)
    {
        for (int i = 0; i < 4; i++)
        {
            if (playerSmallSwitches[i] != currentPuzzle.ExpectedSmallSwitches[i])
            {
                return false;
            }
        }

        foreach (var kvp in currentPuzzle.ExpectedColorSwitches)
        {
            LightColor color = kvp.Key;
            int expectedCount = kvp.Value;

            playerColorSwitchCounts.TryGetValue(color, out int playerCount);

            if (playerCount != expectedCount)
            {
                return false;
            }
        }

        return true;
    }
}