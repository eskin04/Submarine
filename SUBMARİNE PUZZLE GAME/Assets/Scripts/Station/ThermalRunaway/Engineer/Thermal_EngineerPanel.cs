using UnityEngine;
using TMPro;

public class Thermal_EngineerPanel : MonoBehaviour
{
    [Header("Manager Reference")]
    public Thermal_StationManager manager;

    [Header("Texts")]
    public TMP_Text frontHeatText;
    public TMP_Text backHeatText;
    public TMP_Text timerText;

    [Header("Meshes")]
    public Transform needleMesh;
    public Transform sliderConeMesh;

    [Header("Gauge Settings")]
    public float minAngle = 90f;
    public float maxAngle = -90f;
    public Vector3 rotationAxis = Vector3.forward;

    [Header("Warning Lights")]
    public Thermal_WarningLight frontWarningLight;
    public Thermal_WarningLight backWarningLight;

    private float targetClientPressure = 0f;

    public void UpdateDashboardData(int frontHeat, int backHeat, int timer, float pressure)
    {
        if (frontHeatText != null) frontHeatText.text = $"%{frontHeat}";
        if (backHeatText != null) backHeatText.text = $"%{backHeat}";
        if (timerText != null) timerText.text = $"{timer}s";

        targetClientPressure = pressure;

        if (frontWarningLight != null) frontWarningLight.UpdateHeat(frontHeat);
        if (backWarningLight != null) backWarningLight.UpdateHeat(backHeat);
    }

    public void SetBottleneckState(bool isActive)
    {
        if (frontWarningLight != null) frontWarningLight.SetBottleneckActive(isActive);
        if (backWarningLight != null) backWarningLight.SetBottleneckActive(isActive);
    }

    public void TriggerBottleneckError()
    {
        if (frontWarningLight != null) frontWarningLight.TriggerErrorSequence();
        if (backWarningLight != null) backWarningLight.TriggerErrorSequence();
    }

    public void SetStationEndState(bool isWin)
    {
        if (frontWarningLight != null) frontWarningLight.SetStationEndState(isWin);
        if (backWarningLight != null) backWarningLight.SetStationEndState(isWin);
    }



    public void UpdateSliderVisual(float newPos)
    {
        if (sliderConeMesh != null)
        {
            float coneAngle = Mathf.Lerp(minAngle, maxAngle, newPos / 100f);
            sliderConeMesh.localRotation = Quaternion.AngleAxis(coneAngle, rotationAxis);
        }
    }

    private void Update()
    {
        if (manager == null) return;

        float pressureToLerp = manager.isServer ? manager.currentPressure : targetClientPressure;
        SmoothNeedleMovement(pressureToLerp);
    }

    private void SmoothNeedleMovement(float targetPressure)
    {
        if (needleMesh != null)
        {
            float targetAngle = Mathf.Lerp(minAngle, maxAngle, targetPressure / 100f);
            Quaternion currentRot = needleMesh.localRotation;
            Quaternion targetRot = Quaternion.AngleAxis(targetAngle, rotationAxis);

            float lerpSpeed = manager.isServer ? 30f : 15f;
            needleMesh.localRotation = Quaternion.Lerp(currentRot, targetRot, lerpSpeed * Time.deltaTime);
        }
    }
}