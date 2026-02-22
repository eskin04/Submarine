using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Thermal_PhysicalSwitch : MonoBehaviour
{
    [Header("References")]
    public Thermal_StationManager manager;
    public Transform leverMesh;

    [Header("Valve Type Rotations")]
    public Vector3 frontRotation = new Vector3(0f, 0f, 45f);

    public Vector3 commonRotation = new Vector3(0f, 0f, 0f);

    public Vector3 backRotation = new Vector3(0f, 0f, -45f);

    public float animationDuration = 0.2f;

    public float dragThreshold = 50f;

    public bool invertDragDirection = false;

    private Vector3 startMousePos;
    private bool hasSwitchedThisDrag = false;
    private ThermalValveType stateAtDragStart;





    private void UpdateSwitchVisuals(ThermalValveType newState = ThermalValveType.Common)
    {
        if (leverMesh == null) return;

        Vector3 targetRot = commonRotation;
        if (newState == ThermalValveType.Front) targetRot = frontRotation;
        else if (newState == ThermalValveType.Back) targetRot = backRotation;

        leverMesh.DOKill();

        leverMesh.DOLocalRotate(targetRot, animationDuration).SetEase(Ease.OutBack);

    }


    private void OnMouseDown()
    {
        if (manager == null || !manager.isStationBroken) return;

        startMousePos = Input.mousePosition;
        stateAtDragStart = manager.activeCoolingValve;
        hasSwitchedThisDrag = false;
    }

    private void OnMouseDrag()
    {
        if (manager == null || !manager.isStationBroken || hasSwitchedThisDrag) return;

        float deltaX = Input.mousePosition.x - startMousePos.x;
        if (invertDragDirection) deltaX = -deltaX;


        if (stateAtDragStart == ThermalValveType.Common)
        {
            if (deltaX < -dragThreshold) TrySwitch(ThermalValveType.Front);
            else if (deltaX > dragThreshold) TrySwitch(ThermalValveType.Back);
        }
        else if (stateAtDragStart == ThermalValveType.Front)
        {
            if (deltaX > dragThreshold) TrySwitch(ThermalValveType.Common);
        }
        else if (stateAtDragStart == ThermalValveType.Back)
        {
            if (deltaX < -dragThreshold) TrySwitch(ThermalValveType.Common);
        }
    }

    private void TrySwitch(ThermalValveType targetState)
    {
        hasSwitchedThisDrag = true;
        manager.SwitchValveDirectionRPC(targetState);
        UpdateSwitchVisuals(targetState);
        //  PlaySwitchSound();
    }
}