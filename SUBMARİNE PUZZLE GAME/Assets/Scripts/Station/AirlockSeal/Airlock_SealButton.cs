using UnityEngine;
using DG.Tweening;
using System;

[RequireComponent(typeof(Collider))]
public class Airlock_SealButton : MonoBehaviour
{
    [Header("Visual & Movement")]
    public Transform buttonMesh;
    public Vector3 pressOffset = new Vector3(0, -0.05f, 0);
    public float animationDuration = 0.1f;

    public event Action<bool> OnToggled;

    // Durum değişkenleri
    private bool isSealed = false;
    private bool _isLocked = false;

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

    private void Start()
    {
        if (buttonMesh == null) buttonMesh = transform;
        startLocalPosition = buttonMesh.localPosition;
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

        isSealed = !isSealed;

        UpdateButtonVisual();

        OnToggled?.Invoke(isSealed);

        // FMod
    }

    public void SetLocked(bool state)
    {
        isLocked = state;
    }

    public void ResetButton()
    {
        isSealed = false;
        UpdateButtonVisual();
    }

    private void UpdateButtonVisual()
    {
        Vector3 targetPos = isSealed ? (startLocalPosition + pressOffset) : startLocalPosition;
        buttonMesh.DOLocalMove(targetPos, animationDuration).SetEase(Ease.OutQuad);
    }
}