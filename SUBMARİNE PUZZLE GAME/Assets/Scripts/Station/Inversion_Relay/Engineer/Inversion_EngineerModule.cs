using UnityEngine;
using TMPro;

public class Inversion_EngineerModule : MonoBehaviour
{
    [Header("References")]
    public Inversion_Relay_StationManager stationManager;
    public TextMeshProUGUI[] ruleTexts = new TextMeshProUGUI[5];

    [Header("Glitch Effect")]
    [Tooltip("İbre Sweet Spot'tan ne kadar uzaksa bu değer o kadar artar (0 ile 1 arası)")]
    public float currentGlitchIntensity = 0f;

    private EngineerRule[] currentRules = new EngineerRule[5];
    private string glitchCharacters = "!@#$%^&*()_+{}|:<>?~";

    public void SetupRules(EngineerRule[] rules)
    {
        currentRules = rules;
        UpdateRuleDisplay();
    }

    public void SetGlitchIntensity(float intensity)
    {
        if (Mathf.Abs(currentGlitchIntensity - intensity) > 0.05f)
        {
            currentGlitchIntensity = Mathf.Clamp01(intensity);
            UpdateRuleDisplay();
        }
    }

    private void UpdateRuleDisplay()
    {
        for (int i = 0; i < 5; i++)
        {
            if (ruleTexts[i] == null) continue;

            if (currentRules[i].IsCorrupted)
            {
                ruleTexts[i].text = $"Pipe {currentRules[i].Letter} -> <color=red>DATA CORRUPTED</color>";
            }
            else
            {
                string targetStateStr = currentRules[i].TargetState.ToString();

                if (currentGlitchIntensity > 0f)
                {
                    targetStateStr = ScrambleString(targetStateStr, currentGlitchIntensity);
                }

                ruleTexts[i].text = $"Pipe {currentRules[i].Letter} -> {targetStateStr}";
            }
        }
    }

    private string ScrambleString(string original, float intensity)
    {
        int maxLengthVariation = Mathf.RoundToInt(intensity * 4f);

        int randomOffset = Random.Range(-maxLengthVariation, maxLengthVariation + 1);
        int targetLength = original.Length + randomOffset;

        targetLength = Mathf.Max(2, targetLength);

        char[] scrambled = new char[targetLength];

        for (int i = 0; i < targetLength; i++)
        {
            if (i < original.Length && Random.value >= intensity)
            {
                scrambled[i] = original[i];
            }
            else
            {
                scrambled[i] = glitchCharacters[Random.Range(0, glitchCharacters.Length)];
            }
        }

        return new string(scrambled);
    }
}