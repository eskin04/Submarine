using UnityEngine;

[System.Serializable]
public struct DriftRule
{
    public enum DriftDirection { Left = -1, Right = 1, Random = 0 }
    public DriftDirection direction;
    public float speed;
    public float duration;
}

public class Inversion_CalibrationGauge : MonoBehaviour
{
    [Header("References")]
    public Inversion_EngineerModule engineerModule;
    public Transform needleMesh;

    [Header("Table Data (Drift Rules)")]
    public DriftRule[] driftRules;

    [Header("Needle Visuals")]
    public float needleMaxAngle = 60f;
    public float needleSmoothness = 10f;

    [Header("Sweet Spot Settings")]
    public float perfectZone = 10f;
    public float maxError = 40f;

    private float currentError = 0f;

    private DriftRule currentRule;
    private float currentRuleTimer = 0f;

    private void Update()
    {
        if (engineerModule.stationManager == null || !engineerModule.stationManager.isRoundActive.value) return;

        currentRuleTimer -= Time.deltaTime;
        if (currentRuleTimer <= 0f)
        {
            PickRandomRule();
        }

        float directionMultiplier = (float)currentRule.direction;
        currentError += currentRule.speed * directionMultiplier * Time.deltaTime;

        currentError = Mathf.Clamp(currentError, -maxError, maxError);

        UpdateNeedleVisual(currentError);
        CalculateAndApplyGlitch(currentError);
    }

    public void ApplyPlayerInput(float inputForce)
    {
        currentError += inputForce;
        currentError = Mathf.Clamp(currentError, -maxError, maxError);
    }

    private void PickRandomRule()
    {
        if (driftRules.Length == 0) return;
        int randomIndex = Random.Range(0, driftRules.Length);
        currentRule = driftRules[randomIndex];
        currentRuleTimer = currentRule.duration;
        Debug.Log($"Yeni Kural: Yön={currentRule.direction}, Hız={currentRule.speed}, Süre={currentRule.duration}");
        if (currentRule.direction == DriftRule.DriftDirection.Random)
        {
            currentRule.direction = (Random.value < 0.5f) ? DriftRule.DriftDirection.Left : DriftRule.DriftDirection.Right;
        }
    }

    private void UpdateNeedleVisual(float errorAmount)
    {
        if (needleMesh == null) return;

        float targetAngle = errorAmount / maxError * needleMaxAngle;

        Quaternion targetRotation = Quaternion.Euler(targetAngle, 0, 0);
        needleMesh.localRotation = Quaternion.Slerp(needleMesh.localRotation, targetRotation, Time.deltaTime * needleSmoothness);
    }

    private void CalculateAndApplyGlitch(float errorAmount)
    {
        if (engineerModule == null) return;

        float absoluteError = Mathf.Abs(errorAmount);

        if (absoluteError <= perfectZone)
        {
            engineerModule.SetGlitchIntensity(0f);
        }
        else
        {
            float glitchRange = maxError - perfectZone;
            float currentGlitchAmount = absoluteError - perfectZone;
            float glitchPercent = Mathf.Clamp01(currentGlitchAmount / glitchRange);
            engineerModule.SetGlitchIntensity(glitchPercent);
        }
    }
}