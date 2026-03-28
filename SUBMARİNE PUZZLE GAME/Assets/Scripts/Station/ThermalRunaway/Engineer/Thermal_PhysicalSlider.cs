using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Thermal_PhysicalSlider : MonoBehaviour
{
    [Header("Manager Reference")]
    public Thermal_StationManager manager;
    public Thermal_EngineerPanel engineerPanel;
    public Interactable moduleInteractable;

    [Header("Settings")]
    public float minLocalY = -1f;
    public float maxLocalY = 1f;
    public bool invertDirection = false;

    private Vector3 offset;
    private float zCoord;


    private void Start()
    {
        if (manager != null)
        {
            float t = invertDirection
                ? Mathf.InverseLerp(95f, 5f, manager.engineerSliderPosition)
                : Mathf.InverseLerp(5f, 95f, manager.engineerSliderPosition);

            float startY = Mathf.Lerp(minLocalY, maxLocalY, t);

            Vector3 startPos = transform.localPosition;
            startPos.y = startY;
            transform.localPosition = startPos;
        }
    }

    private void OnMouseDown()
    {

        zCoord = Camera.main.WorldToScreenPoint(transform.position).z;
        offset = transform.position - GetMouseAsWorldPoint();
        Debug.Log("Slider'a tıklandı. Başlangıç offset'i: " + offset);
    }

    private void OnMouseDrag()
    {
        if (manager == null || !manager.isStationBroken) return;
        if (moduleInteractable != null && !moduleInteractable.IsInteracting()) return;


        Vector3 targetPos = GetMouseAsWorldPoint() + offset;

        Vector3 localPos = transform.parent.InverseTransformPoint(targetPos);

        localPos.x = transform.localPosition.x;
        localPos.z = transform.localPosition.z;

        localPos.y = Mathf.Clamp(localPos.y, minLocalY, maxLocalY);

        transform.localPosition = localPos;

        float t = Mathf.InverseLerp(minLocalY, maxLocalY, localPos.y);

        float managerValue = invertDirection
            ? Mathf.Lerp(95f, 5f, t)
            : Mathf.Lerp(5f, 95f, t);

        manager.UpdateSliderPositionRPC(managerValue);
        engineerPanel.UpdateSliderVisual(managerValue);
    }

    private Vector3 GetMouseAsWorldPoint()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = zCoord;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}