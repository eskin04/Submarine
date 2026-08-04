using UnityEngine;
using DG.Tweening;

public class HullBreach_PlateModule : MonoBehaviour
{
    [Header("References")]
    public Transform drillVisual;

    [Header("Settings")]
    public float drillFollowSpeed = 15f;
    public Vector3 angledRotation = new Vector3(45f, 0f, 45f);
    public Vector3 straightRotation = Vector3.zero;
    public float animSpeed = 0.15f;

    private Camera mainCam;
    private bool isInteracting = false;
    private Plane platePlane;
    private bool isDrilling = false;

    public void OnModuleEnter()
    {
        isInteracting = true;
        mainCam = Camera.main;

        platePlane = new Plane(transform.forward, transform.position);

        drillVisual.gameObject.SetActive(true);
        drillVisual.localRotation = Quaternion.Euler(angledRotation);

    }

    public void OnModuleExit()
    {
        isInteracting = false;
        drillVisual.gameObject.SetActive(false);

    }

    private void Update()
    {
        if (!isInteracting || mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (platePlane.Raycast(ray, out float enterDistance))
        {
            Vector3 hitPoint = ray.GetPoint(enterDistance);
            drillVisual.position = Vector3.Lerp(drillVisual.position, hitPoint, Time.deltaTime * drillFollowSpeed);
        }

        if (Input.GetMouseButton(0))
        {
            CheckAndDrill(ray);
        }
        else if (isDrilling)
        {
            isDrilling = false;
            drillVisual.DOKill(); // Mevcut animasyonu durdur
            drillVisual.DOLocalRotate(angledRotation, animSpeed);

        }
    }

    private void CheckAndDrill(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            HullBreach_WeldPoint wp = hit.collider.GetComponent<HullBreach_WeldPoint>();

            if (wp != null && !wp.isWelded)
            {
                if (!isDrilling)
                {
                    isDrilling = true;
                    drillVisual.DOKill();
                    drillVisual.DOLocalRotate(straightRotation, animSpeed);

                }

                wp.ApplyWeld(Time.deltaTime);
                return;
            }
        }

        if (isDrilling)
        {
            isDrilling = false;
            drillVisual.DOKill();
            drillVisual.DOLocalRotate(angledRotation, animSpeed);
        }
    }
}