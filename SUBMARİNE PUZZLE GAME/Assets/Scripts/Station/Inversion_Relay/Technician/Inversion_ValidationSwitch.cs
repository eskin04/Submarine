using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Inversion_ValidationSwitch : MonoBehaviour
{
    [Header("References")]
    public Inversion_Relay_StationManager stationManager;
    public Transform switchMesh;

    [Header("Animation Settings")]
    public Vector3 upRotation = new Vector3(0, 0, 0);
    public Vector3 downRotation = new Vector3(90f, 0, 0);
    public float pullDuration = 0.3f;
    public float snapBackDuration = 0.15f;
    private Interactable interactable;

    private bool isPulled = false;

    private void Start()
    {
        interactable = GetComponent<Interactable>();

    }

    public void PullSwitch()
    {
        if (!stationManager.isRoundActive.value ||
             stationManager.isTesting.value ||
             isPulled)
            return;
        interactable.SetInteractable(false);
        isPulled = true;

        if (switchMesh != null)
        {
            switchMesh.DOKill();
            switchMesh.DOLocalRotate(downRotation, pullDuration).SetEase(Ease.OutBack);
        }

        stationManager.PullSwitchRPC();
    }

    public void SnapBack()
    {
        if (switchMesh != null)
        {
            switchMesh.DOKill();
            switchMesh.DOLocalRotate(upRotation, snapBackDuration).SetEase(Ease.InElastic).SetDelay(1f).OnComplete(() =>
            {
                isPulled = false;
                interactable.SetInteractable(true);
            });
        }
        else
        {
            isPulled = false;
            interactable.SetInteractable(true);
        }

    }


}