using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Inversion_ValvePhysicalSwitch : MonoBehaviour
{
    [Header("References")]
    public Inversion_TechnicianModule technicianModule;
    public int pipeIndex;
    public Transform valveMesh;

    [Header("Valve Rotations")]
    public Vector3 emptyRotation = new Vector3(0f, 0f, -90f);
    public Vector3 neutralRotation = new Vector3(0f, 0f, 0f);
    public Vector3 fillRotation = new Vector3(0f, 0f, 90f);

    public float animationDuration = 0.2f;
    public float dragThreshold = 50f;
    public bool invertDragDirection = false;

    private Vector3 startMousePos;
    private bool hasSwitchedThisDrag = false;
    private ValveState stateAtDragStart;
    public ValveState currentState = ValveState.Neutral;

    private void OnMouseDown()
    {
        if (technicianModule == null ||
            !technicianModule.stationManager.isRoundActive.value ||
            technicianModule.stationManager.isTesting.value)
            return;

        startMousePos = Input.mousePosition;
        stateAtDragStart = currentState;
        hasSwitchedThisDrag = false;

        if (valveMesh != null) valveMesh.DOKill();
    }

    private void OnMouseDrag()
    {
        if (technicianModule == null ||
            !technicianModule.stationManager.isRoundActive.value ||
            technicianModule.stationManager.isTesting.value ||
            hasSwitchedThisDrag)
            return;

        float deltaX = Input.mousePosition.x - startMousePos.x;
        if (invertDragDirection) deltaX = -deltaX;

        UpdateDragVisuals(deltaX);

        if (stateAtDragStart == ValveState.Neutral)
        {
            if (deltaX < -dragThreshold) TrySwitch(ValveState.Empty);
            else if (deltaX > dragThreshold) TrySwitch(ValveState.Fill);
        }
        else if (stateAtDragStart == ValveState.Empty)
        {
            if (deltaX > dragThreshold) TrySwitch(ValveState.Neutral);
        }
        else if (stateAtDragStart == ValveState.Fill)
        {
            if (deltaX < -dragThreshold) TrySwitch(ValveState.Neutral);
        }
    }

    private void OnMouseUp()
    {
        if (technicianModule == null ||
            !technicianModule.stationManager.isRoundActive.value ||
            technicianModule.stationManager.isTesting.value)
            return;

        if (!hasSwitchedThisDrag)
        {
            UpdateSwitchVisuals(stateAtDragStart);
        }
    }

    private void UpdateDragVisuals(float deltaX)
    {
        if (valveMesh == null) return;

        float clampedDelta = deltaX;
        if (stateAtDragStart == ValveState.Empty) clampedDelta = Mathf.Clamp(deltaX, 0, dragThreshold);
        else if (stateAtDragStart == ValveState.Fill) clampedDelta = Mathf.Clamp(deltaX, -dragThreshold, 0);
        else clampedDelta = Mathf.Clamp(deltaX, -dragThreshold, dragThreshold);

        Vector3 baseRot = GetRotationForState(stateAtDragStart);
        float targetZ = baseRot.z;

        if (clampedDelta > 0)
        {
            float nextZ = (stateAtDragStart == ValveState.Empty) ? neutralRotation.z : fillRotation.z;
            targetZ = Mathf.Lerp(baseRot.z, nextZ, clampedDelta / dragThreshold);
        }
        else if (clampedDelta < 0)
        {
            float prevZ = (stateAtDragStart == ValveState.Fill) ? neutralRotation.z : emptyRotation.z;
            targetZ = Mathf.Lerp(baseRot.z, prevZ, -clampedDelta / dragThreshold);
        }

        valveMesh.localEulerAngles = new Vector3(baseRot.x, baseRot.y, targetZ);
    }

    private void TrySwitch(ValveState targetState)
    {
        hasSwitchedThisDrag = true;

        UpdateSwitchVisuals(targetState);
        technicianModule.OnValveDragged(pipeIndex, targetState);
    }

    public void UpdateSwitchVisuals(ValveState newState)
    {
        if (valveMesh == null) return;
        currentState = newState;

        Vector3 targetRot = GetRotationForState(newState);

        valveMesh.DOKill();
        valveMesh.DOLocalRotate(targetRot, animationDuration).SetEase(Ease.OutBack);
    }

    private Vector3 GetRotationForState(ValveState state)
    {
        if (state == ValveState.Empty) return emptyRotation;
        if (state == ValveState.Fill) return fillRotation;
        return neutralRotation;
    }
}