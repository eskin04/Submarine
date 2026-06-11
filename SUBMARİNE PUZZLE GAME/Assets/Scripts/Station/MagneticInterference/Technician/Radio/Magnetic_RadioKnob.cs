using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Magnetic_RadioKnob : MonoBehaviour
{
    public enum KnobMode { Continuous, Stepped }

    [Header("References")]
    public Magnetic_RadioController radioController;
    public Transform knobMesh;

    public KnobMode mode = KnobMode.Continuous;
    public bool invertRotation = false;

    public float frequencyChangeMultiplier = 0.01f;

    public float stepAngleThreshold = 15f;
    public float stepValue = 0.001f;

    private float currentVisualAngle = 0f;
    private float previousMouseAngle;
    private Vector2 knobScreenPos;

    private float accumulatedAngle = 0f;

    private void OnMouseDown()
    {
        if (radioController == null) return;

        knobScreenPos = Camera.main.WorldToScreenPoint(knobMesh.position);
        Vector2 mouseDir = (Vector2)Input.mousePosition - knobScreenPos;
        previousMouseAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
    }

    private void OnMouseDrag()
    {
        if (radioController == null) return;

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

        // =====================================

        if (mode == KnobMode.Continuous)
        {
            float frequencyDelta = deltaAngle * frequencyChangeMultiplier;
            radioController.AdjustFrequency(-frequencyDelta);
        }
        else if (mode == KnobMode.Stepped)
        {
            accumulatedAngle += deltaAngle;

            while (Mathf.Abs(accumulatedAngle) >= stepAngleThreshold)
            {
                int direction = (int)Mathf.Sign(accumulatedAngle);

                radioController.AdjustFrequency(-direction * stepValue);

                accumulatedAngle -= direction * stepAngleThreshold;

                // RuntimeManager.PlayOneShot("event:/UI/Small_Knob_Click");
            }
        }
    }
}