using UnityEngine;
using DG.Tweening;
using FMODUnity;

public class EngineerLockDown_DoorSwitch : MonoBehaviour
{
    [Header("References")]
    public EngineerLockDown_StationManager overrideManager;

    [Header("Visuals (Opsiyonel)")]
    public Transform switchHandle;
    public Vector3 pulledRotation = new Vector3(90, 0, 0);
    public float pullAnimTime = 0.3f;
    public float closeAnimTime = 0.1f;
    public float stayOpenTime = 5.0f;

    [Header("Audio Settings")]
    [SerializeField] private AudioEventChannelSO _channel;
    [SerializeField] private EventReference _switchSound;

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

        overrideManager.RequestEngineerDoorOpenRPC(stayOpenTime);

        if (switchHandle != null)
        {
            isAnimating = true;
            // sadece x ekseninde dönecek şekilde hedef rotasyonu oluştur
            Vector3 targetRotation = new Vector3(pulledRotation.x, originalRotation.y, originalRotation.z);
            switchHandle.DOLocalRotate(targetRotation, pullAnimTime).OnComplete(() =>
            {
                switchHandle.DOLocalRotate(originalRotation, closeAnimTime).SetDelay(stayOpenTime + 1f).OnComplete(() =>
                {
                    isAnimating = false;
                });
            });
            PlaySwitchSound();
        }
    }

    private void PlaySwitchSound()
    {
        if (_channel != null && !_switchSound.IsNull)
        {
            AudioEventPayload payload = new AudioEventPayload(_switchSound, transform.position);
            _channel.RaiseEvent(payload);
        }
    }


}