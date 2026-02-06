using UnityEngine;
using UnityEngine.Events;
using DG.Tweening; // Animasyon için

[RequireComponent(typeof(BoxCollider))]
public class RoE_PhysicalButton : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform movingPart;
    [SerializeField] private Vector3 pressDirection = new Vector3(0, -1, 0);
    [SerializeField] private float pressDistance = 0.02f;
    [SerializeField] private float pressDuration = 0.1f;


    [Header("Events")]
    public UnityEvent OnClick;

    public bool isInteractable = false;
    private bool isPressing = false;
    private Vector3 originalLocalPos;

    private void Start()
    {
        if (movingPart == null) movingPart = transform;
        originalLocalPos = movingPart.localPosition;

    }

    public void SetInteractable(bool state)
    {
        isInteractable = state;
    }



    private void OnMouseDown()
    {
        if (!isInteractable || isPressing) return;

        PressAnimation();
        OnClick?.Invoke();
    }

    private void PressAnimation()
    {
        isPressing = true;

        Sequence seq = DOTween.Sequence();

        Vector3 targetLocalPos = originalLocalPos + (pressDirection.normalized * pressDistance);

        seq.Append(movingPart.DOLocalMove(targetLocalPos, pressDuration).SetEase(Ease.OutQuad));

        seq.Append(movingPart.DOLocalMove(originalLocalPos, pressDuration).SetEase(Ease.OutQuad)); // Hafif yaylanma efekti

        seq.OnComplete(() => isPressing = false);
    }
}