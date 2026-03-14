using UnityEngine;
using DG.Tweening;

public class EngineerLockDown_DoorSwitch : MonoBehaviour
{
    [Header("References")]
    public EngineerLockDown_StationManager overrideManager;

    [Header("Visuals (Opsiyonel)")]
    public Transform switchHandle;
    public Vector3 pulledRotation = new Vector3(90, 0, 0);
    public float pullAnimTime = 0.3f;

    private Vector3 originalRotation;
    private bool isAnimating = false;

    private void Awake()
    {
        if (switchHandle != null)
            originalRotation = switchHandle.localEulerAngles;
    }




    public void OnClicked()
    {
        if (isAnimating || overrideManager == null) return;

        overrideManager.RequestEngineerDoorOpenRPC();

        if (switchHandle != null)
        {
            isAnimating = true;
            switchHandle.DOLocalRotate(pulledRotation, pullAnimTime).OnComplete(() =>
            {
                switchHandle.DOLocalRotate(originalRotation, pullAnimTime).SetDelay(0.5f).OnComplete(() =>
                {
                    isAnimating = false;
                });
            });
        }
    }


}