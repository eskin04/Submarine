using UnityEngine;
using TMPro;
using DG.Tweening;

public class Thermal_EngineerPanel : MonoBehaviour
{
    [Header("Manager Reference")]
    public Thermal_StationManager manager;

    public TMP_Text timerText;

    [Header("Meshes")]
    public Transform needleMesh;
    public Transform sliderConeMesh;

    [Header("Thermometers (Heat Visuals)")]
    public Transform frontThermometerNeedle;
    public Transform backThermometerNeedle;

    [Header("Thermometer Settings")]
    public float thermoMinAngle = 0f;
    public float thermoMaxAngle = 270f;
    public Vector3 thermoRotationAxis = Vector3.right;

    [Header("Gauge Settings")]
    public float minAngle = 90f;
    public float maxAngle = -90f;
    public Vector3 rotationAxis = Vector3.forward;

    [Header("Warning Lights")]
    public Thermal_WarningLight frontWarningLight;
    public Thermal_WarningLight backWarningLight;

    private float targetClientPressure = 0f;
    private float targetFrontHeat = 0f;
    private float targetBackHeat = 0f;
    private bool isGameEnded = false;

    public void UpdateDashboardData(int frontHeat, int backHeat, int timer, float pressure)
    {

        if (timerText != null)
        {
            int minutes = timer / 60;
            int seconds = timer % 60;
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        targetClientPressure = pressure;
        targetFrontHeat = frontHeat;
        targetBackHeat = backHeat;

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
        isGameEnded = true;
        if (frontWarningLight != null) frontWarningLight.SetStationEndState(isWin);
        if (backWarningLight != null) backWarningLight.SetStationEndState(isWin);
        if (isWin)
        {
            PlayCoolingDownAnimation();
        }
    }

    private void PlayCoolingDownAnimation()
    {

        float targetHeatAngle = Mathf.Lerp(thermoMinAngle, thermoMaxAngle, 10f / 100f);
        Quaternion targetHeatRot = Quaternion.AngleAxis(targetHeatAngle, thermoRotationAxis);

        if (frontThermometerNeedle != null)
        {
            frontThermometerNeedle.DOLocalRotateQuaternion(targetHeatRot, 3f).SetEase(Ease.InOutSine);
        }

        if (backThermometerNeedle != null)
        {
            backThermometerNeedle.DOLocalRotateQuaternion(targetHeatRot, 3f).SetEase(Ease.InOutSine);
        }
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
        if (manager == null || isGameEnded) return;

        float pressureToLerp = manager.isServer ? manager.currentPressure : targetClientPressure;
        SmoothNeedleMovement(pressureToLerp);

        float lerpSpeed = manager.isServer ? 30f : 15f;

        SmoothThermometerMovement(frontThermometerNeedle, targetFrontHeat, lerpSpeed);
        SmoothThermometerMovement(backThermometerNeedle, targetBackHeat, lerpSpeed);
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

    private void SmoothThermometerMovement(Transform needle, float targetHeat, float speed)
    {
        if (needle != null)
        {
            float targetAngle = Mathf.Lerp(thermoMinAngle, thermoMaxAngle, targetHeat / 100f);

            Quaternion targetRot = Quaternion.AngleAxis(targetAngle, thermoRotationAxis);

            needle.localRotation = Quaternion.Lerp(needle.localRotation, targetRot, speed * Time.deltaTime);
        }
    }
}