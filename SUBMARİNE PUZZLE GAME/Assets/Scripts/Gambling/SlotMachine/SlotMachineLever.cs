using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class SlotMachineLever : MonoBehaviour
{
    [Header("References")]
    public SlotMachineBackend backend;
    public SlotMachineFrontend frontend;

    [Header("Animation Settings")]
    public Transform leverMesh;
    public Vector3 pullRotation = new Vector3(60f, 0f, 0f);
    [SerializeField] private Vector3 _leverDefaultRotation = new Vector3(-10f, 0, 90);
    public float animDuration = 0.3f;
    private Vector3 originalRot;

    private void Awake()
    {
        if (leverMesh != null)
        {
            leverMesh.localEulerAngles = _leverDefaultRotation;
        }
    }

    void OnEnable()
    {
        frontend.OnInteract += HandleInteract;
    }

    void OnDisable()
    {
        frontend.OnInteract -= HandleInteract;
    }

    private void HandleInteract(bool isInteract)
    {
        if (HighlightManager.Instance != null)
        {
            HighlightManager.Instance.SetInteractableState(transform.gameObject, isInteract);
        }
    }

    private void OnMouseDown()
    {
        if (!frontend.IsInteractable) return;


        // Ses: Kol çekilme (Lever Pull) sesi burada çalınacak.

        leverMesh.DOLocalRotate(pullRotation, animDuration)
            .OnComplete(() => leverMesh.DOLocalRotate(_leverDefaultRotation, animDuration).SetEase(Ease.OutBounce));

        backend.TrySpin();
    }
}