using UnityEngine;
using DG.Tweening;

public class LiftDoor : MonoBehaviour
{
    [SerializeField] private Vector3 openPosition;

    private Vector3 closedPosition;
    private bool isOpen = false;

    void Start()
    {
        closedPosition = transform.localPosition;
    }

    public void ToggleDoor(bool open)
    {
        if (open && !isOpen)
        {
            transform.DOLocalMove(openPosition, .5f).SetEase(Ease.InOutSine);
            isOpen = true;
        }
        else if (!open && isOpen)
        {
            transform.DOLocalMove(closedPosition, .5f).SetEase(Ease.InOutSine);
            isOpen = false;
        }
    }
}
