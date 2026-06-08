using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Inversion_CalibrationKnob : MonoBehaviour
{
    [Header("References")]
    public Inversion_EngineerModule engineerModule;
    public Inversion_CalibrationGauge gauge;
    public Transform knobMesh;

    [Header("Knob Settings")]
    public bool invertRotation = false;
    public float inputForceMultiplier = 0.05f;

    private float currentVisualAngle = 0f;
    private float previousMouseAngle;
    private Vector2 knobScreenPos;


    private void OnMouseDown()
    {
        if (engineerModule.stationManager != null && !engineerModule.stationManager.isRoundActive.value) return;

        knobScreenPos = Camera.main.WorldToScreenPoint(knobMesh.position);

        Vector2 mouseDir = (Vector2)Input.mousePosition - knobScreenPos;

        previousMouseAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
    }

    private void OnMouseDrag()
    {
        if (engineerModule.stationManager != null && !engineerModule.stationManager.isRoundActive.value) return;

        Vector2 mouseDir = (Vector2)Input.mousePosition - knobScreenPos;
        float currentMouseAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;

        float deltaAngle = Mathf.DeltaAngle(previousMouseAngle, currentMouseAngle);

        if (invertRotation) deltaAngle = -deltaAngle;

        previousMouseAngle = currentMouseAngle;


        currentVisualAngle += deltaAngle;
        if (knobMesh != null)
        {
            knobMesh.localEulerAngles = new Vector3(0, 0, -currentVisualAngle);
        }

        if (gauge != null)
        {
            gauge.ApplyPlayerInput(-deltaAngle * inputForceMultiplier);
        }
    }
}