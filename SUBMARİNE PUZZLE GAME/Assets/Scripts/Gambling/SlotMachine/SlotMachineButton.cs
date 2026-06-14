using UnityEngine;
using DG.Tweening;

public enum BetButtonType
{
    Increase,
    Decrease
}

[RequireComponent(typeof(Collider))]
public class SlotMachineButton : MonoBehaviour
{
    [Header("References")]
    public SlotMachineBackend backend;
    public SlotMachineFrontend frontend;

    [Header("Button Settings")]
    public BetButtonType buttonType;

    [Header("Animation Settings")]
    public Vector3 pressOffset = new Vector3(0f, -0.05f, 0f);
    public float animDuration = 0.1f;

    private Vector3 _originalLocalPos;

    private bool _isSpinInteractable = true;
    private bool _isBetLimitInteractable = true;

    private void Start()
    {
        _originalLocalPos = transform.localPosition;

        EvaluateBetLimit(backend.minBet);
    }

    void OnEnable()
    {
        frontend.OnInteract += HandleSpinInteract;

        backend.OnBetChanged += EvaluateBetLimit;
    }

    void OnDisable()
    {
        frontend.OnInteract -= HandleSpinInteract;
        backend.OnBetChanged -= EvaluateBetLimit;
    }

    private void HandleSpinInteract(bool isInteract)
    {
        _isSpinInteractable = isInteract;
        UpdateHighlightState();
    }

    private void EvaluateBetLimit(int currentBet)
    {
        if (buttonType == BetButtonType.Increase)
        {
            _isBetLimitInteractable = (currentBet + backend.betStep <= backend.maxBet);
        }
        else if (buttonType == BetButtonType.Decrease)
        {
            _isBetLimitInteractable = (currentBet - backend.betStep >= backend.minBet);
        }

        UpdateHighlightState();
    }

    private void UpdateHighlightState()
    {
        bool finalInteractableState = _isSpinInteractable && _isBetLimitInteractable;

        if (HighlightManager.Instance != null)
        {
            HighlightManager.Instance.SetInteractableState(transform.gameObject, finalInteractableState);
        }
    }

    private void OnMouseDown()
    {
        if (!_isSpinInteractable || !_isBetLimitInteractable) return;

        // Ses: Buton tıklama (Click/Clack) sesi burada çalınacak.

        transform.DOLocalMove(_originalLocalPos + pressOffset, animDuration)
            .OnComplete(() => transform.DOLocalMove(_originalLocalPos, animDuration));

        if (buttonType == BetButtonType.Increase)
        {
            backend.IncreaseBet();
        }
        else if (buttonType == BetButtonType.Decrease)
        {
            backend.DecreaseBet();
        }
    }
}