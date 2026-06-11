using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Magnetic_KeypadButton : MonoBehaviour
{
    [Header("Settings")]
    public int numberValue;

    [Header("References")]
    public Magnetic_StationManager stationManager;
    public Magnetic_EngineerUI engineerUI;
    public Transform buttonMesh;

    private Vector3 originalLocalPos;

    private void Awake()
    {
        if (buttonMesh != null)
            originalLocalPos = buttonMesh.localPosition;
    }

    private void OnMouseDown()
    {
        if (stationManager == null || !stationManager.isRoundActive.value) return;

        if (engineerUI != null && !engineerUI.IsViewingActiveChannel())
        {
            return;
        }

        if (stationManager.engCurrentChannel.value >= 3) return;

        if (buttonMesh != null)
        {
            buttonMesh.DOKill();
            buttonMesh.localPosition = originalLocalPos;
            buttonMesh.DOPunchPosition(Vector3.back * 0.015f, 0.15f, 1, 0);
        }

        stationManager.SubmitEquationAnswerRPC(numberValue);
    }
}