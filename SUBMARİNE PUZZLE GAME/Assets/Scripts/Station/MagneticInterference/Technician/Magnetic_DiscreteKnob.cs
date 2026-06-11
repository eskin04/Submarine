using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Magnetic_DiscreteKnob : MonoBehaviour
{
    public enum KnobType { Amplitude, Frequency, Phase }

    [Header("References")]
    public Magnetic_WaveOscilloscope oscilloscope;
    public Transform knobMesh;

    [Header("Knob Settings")]
    public KnobType type;
    public bool invertRotation = false;

    public float snapThresholdAngle = 60f;

    public float turnSensitivity = 1.0f;

    public float visualStepAngle = 60f;

    private float accumulatedTension = 0f;
    private float currentLockedVisualAngle = 0f;
    private float previousMouseAngle;
    private Vector2 knobScreenPos;
    private bool isDragging = false;


    public void InitializePosition(int startingValue)
    {
        int stepIndex = startingValue - 1;
        if (type == KnobType.Phase)
        {
            stepIndex = startingValue;
        }

        currentLockedVisualAngle = stepIndex * visualStepAngle;

        if (knobMesh != null)
        {
            knobMesh.localEulerAngles = new Vector3(0, 0, -currentLockedVisualAngle);
        }

        accumulatedTension = 0f;
    }

    private void OnMouseDown()
    {
        if (oscilloscope == null) return;

        isDragging = true;
        knobScreenPos = Camera.main.WorldToScreenPoint(knobMesh.position);

        Vector2 mouseDir = (Vector2)Input.mousePosition - knobScreenPos;
        previousMouseAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;

        // Todo : Fmod
    }

    private void OnMouseDrag()
    {
        if (!isDragging || oscilloscope == null) return;

        Vector2 mouseDir = (Vector2)Input.mousePosition - knobScreenPos;
        float currentMouseAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;

        float deltaAngle = Mathf.DeltaAngle(previousMouseAngle, currentMouseAngle);
        if (invertRotation) deltaAngle = -deltaAngle;

        previousMouseAngle = currentMouseAngle;

        accumulatedTension += deltaAngle * turnSensitivity;

        float strainVisual = Mathf.Clamp(accumulatedTension * 0.25f, -15f, 15f);
        knobMesh.localEulerAngles = new Vector3(0, 0, -(currentLockedVisualAngle + strainVisual));

        if (Mathf.Abs(accumulatedTension) >= snapThresholdAngle)
        {
            int direction = (int)Mathf.Sign(accumulatedTension);
            PerformSnap(direction);
        }
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        if (accumulatedTension != 0)
        {
            accumulatedTension = 0f;

            knobMesh.DOKill();
            knobMesh.DOLocalRotate(new Vector3(0, 0, -currentLockedVisualAngle), 0.3f)
                    .SetEase(Ease.OutElastic, 1.5f, 0.5f);
        }
    }

    private void PerformSnap(int direction)
    {
        accumulatedTension = 0f;

        currentLockedVisualAngle += direction * visualStepAngle;
        direction = -direction;
        switch (type)
        {
            case KnobType.Amplitude:
                oscilloscope.ChangeAmplitude(direction);
                break;
            case KnobType.Frequency:
                oscilloscope.ChangeFrequency(direction);
                break;
            case KnobType.Phase:
                oscilloscope.ChangePhase(direction);
                break;
        }

        knobMesh.DOKill();
        knobMesh.DOLocalRotate(new Vector3(0, 0, -currentLockedVisualAngle), 0.15f)
                .SetEase(Ease.OutBack, 2.0f);

        // RuntimeManager.PlayOneShot("event:/Interactables/Heavy_Knob_Snap");
    }
}