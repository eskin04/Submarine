using UnityEngine;
using Cinemachine;

public class CameraLayerController : MonoBehaviour
{
    [Header("Camera Detection")]
    public LayerMask interactCameraLayer;
    [Header("Layer Ignore Lists")]
    public LayerMask ignoreNormalCameraLayer;

    public LayerMask ignoreInteractCameraLayer;

    private Camera _mainCam;
    private int _defaultMask;
    private int _hiddenMask;
    private int _interactCameraLayerMask;
    private CinemachineBrain _brain;

    private int _currentMask;

    void Awake()
    {
        _mainCam = GetComponent<Camera>();
        _brain = GetComponent<CinemachineBrain>();

        _defaultMask = _mainCam.cullingMask;

        _hiddenMask = _defaultMask & ~ignoreNormalCameraLayer.value;

        _interactCameraLayerMask = _defaultMask & ~ignoreInteractCameraLayer.value;

        SetCullingMask(_hiddenMask);
    }

    void Update()
    {
        if (_brain == null || _brain.ActiveVirtualCamera == null) return;

        int activeCamLayerMask = 1 << _brain.ActiveVirtualCamera.VirtualCameraGameObject.layer;

        if ((activeCamLayerMask & interactCameraLayer.value) != 0)
        {
            SetCullingMask(_interactCameraLayerMask);
        }
        else
        {
            SetCullingMask(_hiddenMask);
        }
    }

    private void SetCullingMask(int newMask)
    {
        if (_currentMask != newMask)
        {
            _currentMask = newMask;
            _mainCam.cullingMask = _currentMask;
        }
    }
}