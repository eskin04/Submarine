using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Thermal_BottleneckButton : MonoBehaviour
{
    public Thermal_StationManager manager;

    public ThermalValveType myLocation;

    public int colorIndex;

    public Transform buttonMesh;
    private Vector3 initialPos;

    private void Start()
    {
        if (buttonMesh != null) initialPos = buttonMesh.localPosition;
    }

    private void OnMouseDown()
    {
        if (manager == null || !manager.isStationBroken || !manager.isBottleneckActive) return;

        if (buttonMesh != null)
        {
            buttonMesh.DOKill();
            buttonMesh.localPosition = initialPos;
            buttonMesh.DOPunchPosition(Vector3.back * 0.02f, 0.2f, 1, 0.5f);
        }

        manager.SubmitBottleneckCodeRPC(colorIndex, myLocation);
    }
}