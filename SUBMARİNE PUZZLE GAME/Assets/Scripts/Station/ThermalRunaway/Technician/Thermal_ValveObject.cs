using UnityEngine;
using DG.Tweening; // DOTween kütüphanesi eklendi

[RequireComponent(typeof(Collider))]
public class Thermal_ValveObject : MonoBehaviour
{
    [Header("References")]
    public Thermal_StationManager manager;
    public ThermalValveType myValveType;
    public Transform buttonMesh;

    [Header("Settings")]
    public Vector3 pressAxis = Vector3.down;
    public float pressDepth = 0.05f;
    public float pressDuration = 0.15f;
    private Vector3 initialLocalPos;
    private Interactable ınteractable;

    private void Start()
    {
        if (buttonMesh != null)
        {
            initialLocalPos = buttonMesh.localPosition;
        }

        ınteractable = GetComponent<Interactable>();
    }


    public void InteractWithValve()
    {
        if (manager == null || !manager.isStationBroken)
        {
            return;
        }

        manager.PumpValveRPC(myValveType);

        if (buttonMesh != null)
        {
            buttonMesh.DOKill();
            buttonMesh.localPosition = initialLocalPos;

            buttonMesh.DOLocalMove(initialLocalPos + (pressAxis * pressDepth), pressDuration / 2f)
                      .SetEase(Ease.OutQuad)
                      .SetLoops(2, LoopType.Yoyo);
        }
        if (ınteractable.IsInteracting())
            ınteractable.StopInteract();
    }
}