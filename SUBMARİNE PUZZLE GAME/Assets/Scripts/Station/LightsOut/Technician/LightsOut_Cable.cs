using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LightsOut_Cable : MonoBehaviour
{
    [Header("Identity")]
    public int cableID;
    public WireColor myPhysicalColor;

    [Header("References")]
    public LightsOut_TechnicianUI uiManager;

    private MeshRenderer meshRenderer;

    private bool isDragging = false;
    private Plane dragPlane;
    private LightsOut_Port currentConnectedPort;
    private BoxCollider movementBounds;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void Setup(int id, WireColor color, LightsOut_TechnicianUI manager, BoxCollider bounds)
    {
        cableID = id;
        myPhysicalColor = color;
        uiManager = manager;
        movementBounds = bounds;

        if (meshRenderer != null)
        {
            meshRenderer.material.color = GetColorValue(color);
        }
    }


    private void OnMouseDown()
    {
        if (uiManager == null) return;

        isDragging = true;

        if (currentConnectedPort != null)
        {
            currentConnectedPort.TurnOffLight();
            currentConnectedPort = null;
        }

        dragPlane = new Plane(-Camera.main.transform.forward, transform.position);
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (dragPlane.Raycast(ray, out float enterDist))
        {
            Vector3 hitPoint = ray.GetPoint(enterDist);
            if (movementBounds != null)
            {
                transform.position = movementBounds.ClosestPoint(hitPoint);
            }
            else
            {
                transform.position = hitPoint;
            }

        }
    }

    private void OnMouseUp()
    {
        isDragging = false;
        CheckDrop();
    }


    private void CheckDrop()
    {
        LightsOut_Port foundPort = null;
        float closestDist = float.MaxValue;
        float snapRange = 0.5f;

        foreach (var port in uiManager.ports)
        {
            float dist = Vector3.Distance(transform.position, port.snapPoint.position);

            if (dist < snapRange && dist < closestDist)
            {
                closestDist = dist;
                foundPort = port;
            }
        }

        if (foundPort != null)
        {
            uiManager.OnCableDropped(this, foundPort);
        }
        else
        {
            ResetPosition();
            uiManager.OnCableDisconnected(this);
        }
    }

    public void SnapToPort(LightsOut_Port port)
    {
        currentConnectedPort = port;
        transform.position = port.snapPoint.position;
    }

    public void ResetPosition()
    {
        currentConnectedPort = null;
        Transform myHomeSlot = uiManager.startSlots[cableID];
        transform.position = myHomeSlot.position;
    }

    private Color GetColorValue(WireColor c)
    {
        switch (c)
        {
            case WireColor.Yellow: return Color.yellow;
            case WireColor.Green: return Color.green;
            case WireColor.Blue: return Color.blue;
            case WireColor.Red: return Color.red;
            default: return Color.white;
        }
    }
}