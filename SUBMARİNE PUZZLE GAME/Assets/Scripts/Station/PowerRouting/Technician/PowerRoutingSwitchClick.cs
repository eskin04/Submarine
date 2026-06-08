using UnityEngine;
using DG.Tweening;

public enum PowerRoutingSwitchType
{
    SmallSwitch,
    ColorSwitch
}

public class PowerRoutingSwitchClick : MonoBehaviour
{
    [Header("Main Controller")]
    [SerializeField] private PowerRoutingTechnicianVisuals _technicianVisuals;

    [Header("Switch Settings")]
    public PowerRoutingSwitchType switchType;
    public int switchIndex;
    public LightColor switchColor;

    [Header("Animation Settings")]
    public Transform switchTransform;
    [SerializeField] private float _animDuration = 0.2f;
    [SerializeField] private Vector3 _upRotation = new Vector3(-30f, 0, 0);
    [SerializeField] private Vector3 _downRotation = new Vector3(30f, 0, 0);

    private bool _isDown = false;

    public void ResetToUp()
    {
        _isDown = false;
        switchTransform.localEulerAngles = _upRotation;
    }

    public void BounceUp()
    {
        _isDown = false;
        switchTransform.DOLocalRotate(_upRotation, 0.15f).SetEase(Ease.OutBounce);
    }

    private void OnMouseDown()
    {
        if (_technicianVisuals == null || !_technicianVisuals.IsStationActive()) return;

        _isDown = !_isDown;
        Vector3 targetRot = _isDown ? _downRotation : _upRotation;

        switchTransform.DOLocalRotate(targetRot, _animDuration).SetEase(Ease.OutBack);
        // FMOD: RuntimeManager.PlayOneShot("event:/Station/Switch_Click", switchTransform.position);

        if (switchType == PowerRoutingSwitchType.SmallSwitch)
        {
            SwitchState state = _isDown ? SwitchState.Down : SwitchState.Up;
            _technicianVisuals.NotifySmallSwitchChanged(switchIndex, state);
        }
        else if (switchType == PowerRoutingSwitchType.ColorSwitch)
        {
            _technicianVisuals.NotifyColorSwitchChanged(switchColor, switchIndex, _isDown);
        }
    }
}