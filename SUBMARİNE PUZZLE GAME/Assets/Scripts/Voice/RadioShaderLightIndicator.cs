using UnityEngine;
using DG.Tweening;

public class RadioShaderLightIndicator : MonoBehaviour
{
    [Header("References")]
    public MeshRenderer meshRenderer;
    public int materialIndex = 0;

    [Header("Light Settings")]
    public float maxLightIntensity = 5f;

    public float animationDuration = 0.25f;

    private static readonly int IntensityProp = Shader.PropertyToID("_LightIntensity");
    private Material runtimeMaterial;
    private float currentIntensity = 0f;
    private Tween lightTween;

    void Awake()
    {
        if (meshRenderer != null)
        {
            runtimeMaterial = meshRenderer.materials[materialIndex];
            runtimeMaterial.SetFloat(IntensityProp, 0f);
        }
    }

    void OnEnable()
    {
        if (RadioVoiceManager.Instance != null)
        {
            RadioVoiceManager.Instance.OnReceivingTransmission += HandleTransmissionState;

            HandleTransmissionState(RadioVoiceManager.Instance.IsAnyoneElseTalking());
        }
    }

    void OnDisable()
    {
        if (RadioVoiceManager.Instance != null)
        {
            RadioVoiceManager.Instance.OnReceivingTransmission -= HandleTransmissionState;
        }
        lightTween?.Kill();
    }

    private void HandleTransmissionState(bool isReceiving)
    {
        if (runtimeMaterial == null) return;

        float targetIntensity = isReceiving ? maxLightIntensity : 0f;

        lightTween?.Kill();

        lightTween = DOTween.To(() => currentIntensity, x =>
        {
            currentIntensity = x;
            runtimeMaterial.SetFloat(IntensityProp, currentIntensity);
        }, targetIntensity, animationDuration)
        .SetEase(isReceiving ? Ease.OutBack : Ease.InOutQuad);
    }
}