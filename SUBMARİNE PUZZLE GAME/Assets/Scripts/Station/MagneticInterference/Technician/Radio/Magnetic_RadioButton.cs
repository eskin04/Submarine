using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Magnetic_RadioButton : MonoBehaviour
{
    public Magnetic_RadioController radioController;
    public Transform buttonMesh;

    private Vector3 originalLocalPos;

    private void Awake()
    {
        if (buttonMesh != null)
            originalLocalPos = buttonMesh.localPosition;
    }

    private void OnMouseDown()
    {
        if (radioController == null) return;

        if (buttonMesh != null)
        {
            buttonMesh.DOKill();
            buttonMesh.localPosition = originalLocalPos;
            buttonMesh.DOPunchPosition(Vector3.back * 0.015f, 0.15f, 1, 0);
        }

        // RuntimeManager.PlayOneShot("event:/UI/Radio_Submit_Press");

        radioController.SubmitFrequency();
    }
}