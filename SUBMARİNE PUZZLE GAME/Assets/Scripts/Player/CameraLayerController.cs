using UnityEngine;
using Cinemachine;

public class CameraLayerController : MonoBehaviour
{
    public LayerMask interactCameraLayer;

    public LayerMask stationUILayer;

    private Camera _mainCam;
    private int _defaultMask;
    private int _hiddenMask;
    private CinemachineBrain _brain;

    void Awake()
    {
        _mainCam = GetComponent<Camera>();
        _brain = GetComponent<CinemachineBrain>();

        _defaultMask = _mainCam.cullingMask;

        _hiddenMask = _defaultMask & ~stationUILayer.value;

        _mainCam.cullingMask = _hiddenMask;
    }

    void Update()
    {
        if (_brain == null || _brain.ActiveVirtualCamera == null) return;

        int activeCamLayerMask = 1 << _brain.ActiveVirtualCamera.VirtualCameraGameObject.layer;

        if ((activeCamLayerMask & interactCameraLayer.value) != 0)
        {
            _mainCam.cullingMask = _defaultMask;
        }
        else
        {
            _mainCam.cullingMask = _hiddenMask;
        }
    }
}