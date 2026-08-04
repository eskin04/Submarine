using UnityEngine;
using PurrNet;
using DG.Tweening;
using UnityEngine.UI;

public enum DrillControlMode { SmoothFollow, RestAndSnap }

public class HullBreach_DrillItem : NetworkBehaviour, IInventoryItem
{
    [Header("References")]
    public Transform drillVisual;

    [Header("Minigame Settings")]
    public DrillControlMode controlMode = DrillControlMode.SmoothFollow;
    public float followSpeed = 15f;
    public Vector3 angledRotation = new Vector3(45f, 0f, 45f);
    public Vector3 straightRotation = Vector3.zero;
    public float animSpeed = 0.15f;
    public ParticleSystem weldSparks;
    public float zOffsetFromPlate = 0.1f;
    public Vector3 minigameScale = new Vector3(0.5f, 0.5f, 0.5f);
    private Vector3 originalLocalScale;

    [Header("Rest & Snap Mode Settings")]
    public Vector3 restLocalPosition = new Vector3(0.3f, -0.3f, 0.3f);
    private HullBreach_WeldPoint activeWeldPoint;

    [Header("Charge Settings")]
    public float maxCharge = 100f;
    public float currentCharge = 100f;
    public float chargePerScrew = 12.5f;
    public Slider drillUIChargeBar;

    private bool isEquipped = false;
    private bool isInMinigame = false;
    private Transform currentPlate;
    private Plane platePlane;
    private Camera mainCam;

    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private bool isDrilling = false;

    private void Awake()
    {
        if (drillVisual != null)
        {
            originalLocalPos = drillVisual.localPosition;
            originalLocalRot = drillVisual.localRotation;
            originalLocalScale = drillVisual.localScale;
        }

        if (drillUIChargeBar != null)
        {
            drillUIChargeBar.minValue = 0f;
            drillUIChargeBar.maxValue = maxCharge;
            drillUIChargeBar.value = currentCharge;
        }
    }

    #region IINVENTORYITEM GEREKSİNİMLERİ
    public void OnEquip()
    {
        isEquipped = true;
    }

    public void OnUnequip()
    {
        isEquipped = false;
        if (isInMinigame) StopMinigame();
        CmdSyncCharge(currentCharge);
    }

    public void OnDrop()
    {
        isEquipped = false;
        if (isInMinigame) StopMinigame();
        CmdSyncCharge(currentCharge);
    }

    public void CanOperate(bool canOperate) { }
    #endregion


    public void StartMinigame(Transform plateTransform)
    {
        if (!isEquipped || drillVisual == null) return;

        isInMinigame = true;
        currentPlate = plateTransform;
        mainCam = Camera.main;

        platePlane = new Plane(currentPlate.forward, currentPlate.position);

        drillVisual.DOKill();
        drillVisual.localRotation = Quaternion.Euler(angledRotation);
        drillVisual.DOScale(minigameScale, 0.3f);

        if (controlMode == DrillControlMode.RestAndSnap)
        {
            drillVisual.DOLocalMove(restLocalPosition, 0.3f);
        }
    }

    public void StopMinigame()
    {
        isInMinigame = false;
        isDrilling = false;
        activeWeldPoint = null;
        Cursor.visible = true;
        if (weldSparks != null && weldSparks.isPlaying) weldSparks.Stop();

        if (drillVisual != null)
        {
            drillVisual.DOKill();
            drillVisual.DOLocalMove(originalLocalPos, 0.3f);
            drillVisual.DOLocalRotateQuaternion(originalLocalRot, 0.3f);
            drillVisual.DOScale(originalLocalScale, 0.3f);
        }
    }


    private void Update()
    {
        if (!isInMinigame || mainCam == null || !isOwner) return;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        if (controlMode == DrillControlMode.SmoothFollow)
        {
            if (platePlane.Raycast(ray, out float enterDistance))
            {
                Vector3 targetHitPoint = ray.GetPoint(enterDistance);
                targetHitPoint += platePlane.normal * zOffsetFromPlate;
                drillVisual.position = Vector3.Lerp(drillVisual.position, targetHitPoint, Time.deltaTime * followSpeed);
            }
        }
        else if (controlMode == DrillControlMode.RestAndSnap)
        {
            if (isDrilling && activeWeldPoint != null)
            {
                Vector3 targetPos = activeWeldPoint.transform.position + (platePlane.normal * zOffsetFromPlate);
                drillVisual.position = Vector3.Lerp(drillVisual.position, targetPos, Time.deltaTime * followSpeed);
            }
            else
            {
                drillVisual.localPosition = Vector3.Lerp(drillVisual.localPosition, restLocalPosition, Time.deltaTime * followSpeed);
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (currentCharge <= 0)
            {
                if (isDrilling) StopDrillingAnim();
                return;
            }
            CheckAndDrill(ray);
        }
        else
        {
            if (isDrilling) StopDrillingAnim();
        }
    }

    private void CheckAndDrill(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            HullBreach_WeldPoint wp = hit.collider.GetComponent<HullBreach_WeldPoint>();

            if (wp != null && !wp.isWelded)
            {
                activeWeldPoint = wp;

                if (!isDrilling)
                {
                    Cursor.visible = false;
                    isDrilling = true;
                    drillVisual.DOKill();
                    drillVisual.DOLocalRotate(straightRotation, animSpeed);
                    if (weldSparks != null) weldSparks.Play();
                }

                float drainRate = chargePerScrew / wp.requiredWeldTime;
                currentCharge -= drainRate * Time.deltaTime;

                if (currentCharge < 0) currentCharge = 0;

                UpdateDrillVisuals();
                wp.ApplyWeld(Time.deltaTime);
                return;
            }
        }

        if (isDrilling) StopDrillingAnim();
    }

    public void UpdateDrillVisuals()
    {
        if (drillUIChargeBar != null)
        {
            drillUIChargeBar.value = currentCharge;
        }
    }

    private void StopDrillingAnim()
    {
        isDrilling = false;
        activeWeldPoint = null;

        Cursor.visible = true;
        if (weldSparks != null) weldSparks.Stop();

        drillVisual.DOKill();
        drillVisual.DOLocalRotate(angledRotation, animSpeed);
    }


    [ServerRpc(requireOwnership: false)]
    private void CmdSyncCharge(float newCharge)
    {
        RpcSyncCharge(newCharge);
    }

    [ObserversRpc(runLocally: true)]
    private void RpcSyncCharge(float newCharge)
    {
        currentCharge = newCharge;
        UpdateDrillVisuals();
    }
}