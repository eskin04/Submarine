using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Thermal_BottleneckButton : MonoBehaviour
{
    public Thermal_StationManager manager;

    public ThermalValveType myLocation;
    public float pressDistance = 0.0005f;

    public int colorIndex;

    public Transform buttonMesh;
    private Vector3 initialPos;

    private void Start()
    {
        if (buttonMesh != null) initialPos = buttonMesh.localPosition;
    }

    private void OnMouseDown()
    {
        if (buttonMesh != null)
        {
            buttonMesh.DOKill();
            buttonMesh.localPosition = initialPos;
            buttonMesh.DOPunchPosition(Vector3.left * pressDistance, 0.2f, 1, 0.5f);
        }
        if (manager == null || !manager.isStationBroken || !manager.isBottleneckActive) return;



        // manager.SubmitBottleneckCodeRPC(colorIndex, myLocation);
    }
}