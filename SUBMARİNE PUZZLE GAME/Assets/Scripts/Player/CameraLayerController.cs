using UnityEngine;
using System;

[RequireComponent(typeof(Camera))]
public class CameraLayerController : MonoBehaviour
{
    public static Action OnInteractionStarted;
    public static Action OnInteractionEnded;

    [Header("Layer Ignore Lists")]
    public LayerMask ignoreNormalCameraLayer;
    public LayerMask ignoreInteractCameraLayer;

    private Camera _mainCam;
    private int _hiddenMask;
    private int _interactCameraLayerMask;

    void Awake()
    {
        _mainCam = GetComponent<Camera>();
        int defaultMask = _mainCam.cullingMask;
        _hiddenMask = defaultMask & ~ignoreNormalCameraLayer.value;
        _interactCameraLayerMask = defaultMask & ~ignoreInteractCameraLayer.value;

        SetNormalCulling();

        OnInteractionStarted += SetInteractCulling;
        OnInteractionEnded += SetNormalCulling;
    }

    private void OnDestroy()
    {
        OnInteractionStarted -= SetInteractCulling;
        OnInteractionEnded -= SetNormalCulling;
    }

    private void SetNormalCulling() => _mainCam.cullingMask = _hiddenMask;
    private void SetInteractCulling() => _mainCam.cullingMask = _interactCameraLayerMask;
}