using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Thermal_ValveObject : MonoBehaviour
{
    [Header("References")]
    public Thermal_StationManager manager;
    public ThermalValveType myValveType;

    [Header("Lever Settings")]

    public Transform leverMesh;

    public Vector3 rotationAxis = Vector3.right;

    public float rotationAmount = 90f;

    public float animationDuration = 0.15f;

    private Quaternion initialLocalRotation;

    private void Start()
    {
        if (leverMesh != null)
        {
            initialLocalRotation = leverMesh.localRotation;
        }
    }

    public void InteractWithValve()
    {
        if (manager == null || !manager.isStationBroken)
        {
            return;
        }

        manager.PumpValveRPC(myValveType);

        if (leverMesh != null)
        {
            leverMesh.DOKill();
            leverMesh.localRotation = initialLocalRotation;

            Vector3 targetRotation = initialLocalRotation.eulerAngles + (rotationAxis * rotationAmount);

            leverMesh.DOLocalRotate(targetRotation, animationDuration / 2f)
                      .SetEase(Ease.OutQuad)
                      .SetLoops(2, LoopType.Yoyo);
        }
    }
}