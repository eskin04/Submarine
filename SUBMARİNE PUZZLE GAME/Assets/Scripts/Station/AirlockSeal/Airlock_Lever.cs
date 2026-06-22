using UnityEngine;
using DG.Tweening;
using TMPro;

[RequireComponent(typeof(Collider))]
public class Airlock_Lever : MonoBehaviour
{
    [Header("Lever Settings")]
    public bool isTechnicianLever = true;
    private int stepsCount = 10;

    [Header("Physical Movement")]
    public float stepDistance = 0.1f;

    public float maxDepthOffset = 0.05f;

    public float snapDuration = 0.05f;

    [Header("Rotation Setup (Z Axis Curve)")]
    public Vector3 topRotation = new Vector3(0, -90f, -15f);
    public Vector3 bottomRotation = new Vector3(0, -90f, 15f);

    [Header("UI Reference")]
    public TextMeshPro valueDisplay;

    [Header("Number Visuals Generator")]
    public GameObject numberPrefab;
    public Transform numbersContainer;
    public Vector3 numberOffset = new Vector3(0.5f, 0, 0);

    private int currentStepIndex = 0;

    public int LeverValue
    {
        get { return isTechnicianLever ? (currentStepIndex * 2) + 1 : (currentStepIndex * 2) + 2; }
    }

    private bool _isLocked = false;
    [HideInInspector]
    public bool isLocked
    {
        get { return _isLocked; }
        set
        {
            if (_isLocked != value)
            {
                _isLocked = value;
                UpdateHighlightState();
            }
        }
    }

    private Vector3 startLocalPosition;
    private Camera mainCamera;
    private float zDistanceToCamera;

    private float initialMouseLocalY;
    private int initialStepIndex;

    private void Start()
    {
        mainCamera = Camera.main;

        startLocalPosition = transform.localPosition;

        UpdateLeverVisuals(false);
    }

    private void UpdateHighlightState()
    {
        bool canBeHighlighted = !_isLocked;
        if (HighlightManager.Instance != null)
        {
            HighlightManager.Instance.SetInteractableState(transform.gameObject, canBeHighlighted);
        }
    }

    private void OnMouseDown()
    {
        if (isLocked) return;

        zDistanceToCamera = mainCamera.WorldToScreenPoint(transform.position).z;
        Vector3 mouseWorldPos = GetMouseWorldPosition();

        initialMouseLocalY = transform.parent.InverseTransformPoint(mouseWorldPos).y;
        initialStepIndex = currentStepIndex;
    }

    private void OnMouseDrag()
    {
        if (isLocked) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        float currentMouseLocalY = transform.parent.InverseTransformPoint(mouseWorldPos).y;

        float differenceY = currentMouseLocalY - initialMouseLocalY;

        int stepsMoved = Mathf.RoundToInt(-differenceY / stepDistance);
        int calculatedStep = Mathf.Clamp(initialStepIndex + stepsMoved, 0, stepsCount - 1);

        if (calculatedStep != currentStepIndex)
        {
            SetStep(calculatedStep);
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = zDistanceToCamera;
        return mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }

    public void SetStep(int newStepIndex)
    {
        currentStepIndex = Mathf.Clamp(newStepIndex, 0, stepsCount - 1);
        UpdateLeverVisuals(true);
    }

    public void ResetLever()
    {
        SetStep(0);
    }

    private void UpdateLeverVisuals(bool animate)
    {
        if (valueDisplay != null) valueDisplay.text = LeverValue.ToString();

        float t = (float)currentStepIndex / (stepsCount - 1);

        float curveMultiplier = Mathf.Pow((t - 0.5f) * 2f, 2f);
        float targetZ = startLocalPosition.z + (curveMultiplier * maxDepthOffset);

        float targetY = startLocalPosition.y - (currentStepIndex * stepDistance);

        Vector3 targetLocalPos = new Vector3(startLocalPosition.x, targetY, targetZ);

        Vector3 targetLocalRot = Vector3.Lerp(topRotation, bottomRotation, t);
        targetLocalRot.y = -90f;

        if (animate)
        {
            transform.DOLocalMove(targetLocalPos, snapDuration).SetEase(Ease.OutBack);
            transform.DOLocalRotate(targetLocalRot, snapDuration).SetEase(Ease.OutBack);
        }
        else
        {
            transform.localPosition = targetLocalPos;
            transform.localEulerAngles = targetLocalRot;
        }
    }

    public void GenerateNumbers()
    {
        if (numberPrefab == null || numbersContainer == null) return;

        for (int i = numbersContainer.childCount - 1; i >= 0; i--)
            DestroyImmediate(numbersContainer.GetChild(i).gameObject);

        Vector3 baseLocalPos = transform.localPosition;

        for (int i = 0; i < stepsCount; i++)
        {
            GameObject newNumObj = Instantiate(numberPrefab, numbersContainer);

            int displayValue = isTechnicianLever ? (i * 2) + 1 : (i * 2) + 2;
            newNumObj.name = $"Indicator_{displayValue}";

            float stepY = baseLocalPos.y - (i * stepDistance);
            newNumObj.transform.localPosition = new Vector3(baseLocalPos.x, stepY, baseLocalPos.z) + numberOffset;

            TextMeshPro tmp = newNumObj.GetComponent<TextMeshPro>();
            if (tmp != null) tmp.text = displayValue.ToString();
        }
    }
}