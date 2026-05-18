using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Inversion_CalibrationKnob : MonoBehaviour
{
    [Header("References")]
    public Inversion_EngineerModule engineerModule;
    public Inversion_CalibrationGauge gauge;
    public Transform knobMesh;

    [Header("Knob Settings")]
    public float rotationSensitivity = 1.5f;
    public float inputForceMultiplier = 0.2f;

    private Vector3 startMousePos;
    private float currentAngle = 0f;

    private void OnMouseDown()
    {
        if (engineerModule.stationManager != null && !engineerModule.stationManager.isRoundActive.value) return;
        startMousePos = Input.mousePosition;
    }

    private void OnMouseDrag()
    {
        if (engineerModule.stationManager != null && !engineerModule.stationManager.isRoundActive.value) return;

        float deltaX = Input.mousePosition.x - startMousePos.x;

        startMousePos = Input.mousePosition;

        currentAngle += deltaX * rotationSensitivity;
        if (knobMesh != null)
        {
            knobMesh.localEulerAngles = new Vector3(0, 0, -currentAngle);
        }

        if (gauge != null)
        {
            gauge.ApplyPlayerInput(deltaX * inputForceMultiplier);
        }
    }
}