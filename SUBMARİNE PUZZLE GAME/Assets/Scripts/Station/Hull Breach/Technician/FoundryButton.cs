using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class FoundryButton : MonoBehaviour
{
    [Header("References")]
    public HullBreach_FoundryController foundryController;

    [Header("Button Settings")]
    public PlateMaterial buttonMaterial;

    [Header("Animation Settings")]
    public Vector3 pressOffset = new Vector3(0f, -0.05f, 0f);
    public float animDuration = 0.1f;

    private Vector3 _originalLocalPos;
    private bool _isInteractable = true;

    private void Start()
    {
        _originalLocalPos = transform.localPosition;

        // Başlangıçta aktif başlat
        UpdateHighlightState(_isInteractable);
    }

    private void OnEnable()
    {
        if (foundryController != null)
        {
            foundryController.OnInteractableStateChanged += HandleInteractability;
        }
    }

    private void OnDisable()
    {
        if (foundryController != null)
        {
            foundryController.OnInteractableStateChanged -= HandleInteractability;
        }
    }

    private void HandleInteractability(bool canInteract)
    {
        _isInteractable = canInteract;
        UpdateHighlightState(_isInteractable);
    }

    private void UpdateHighlightState(bool state)
    {
        if (HighlightManager.Instance != null)
        {
            HighlightManager.Instance.SetInteractableState(transform.gameObject, state);
        }
    }

    private void OnMouseDown()
    {
        if (!_isInteractable || foundryController == null) return;

        // TODO: FMOD Buton tıklama sesi burada çalınacak.

        transform.DOLocalMove(_originalLocalPos + pressOffset, animDuration)
            .OnComplete(() => transform.DOLocalMove(_originalLocalPos, animDuration));

        foundryController.CmdStartPrinting(buttonMaterial);
    }
}