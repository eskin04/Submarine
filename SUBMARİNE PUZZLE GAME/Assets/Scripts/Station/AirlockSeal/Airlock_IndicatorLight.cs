using UnityEngine;
using DG.Tweening;
using System;

public class Airlock_IndicatorLight : MonoBehaviour
{
    [Header("Visuals")]
    public MeshRenderer meshRenderer;
    public float activeLightIntensity = 5.0f;
    public int activeColorIndex = 2;
    public int DisActiveColorIndex = 1;

    private Material runtimeMaterial;
    private Tween fadeTween;

    private static readonly int LightSelectionProp = Shader.PropertyToID("_ColorIndex");
    private static readonly int IntensityProp = Shader.PropertyToID("_LightIntensity");

    private void Awake()
    {
        if (meshRenderer != null)
        {
            runtimeMaterial = meshRenderer.materials[0];
        }
    }

    public void SetLightState(bool isOn)
    {
        fadeTween?.Kill();

        if (runtimeMaterial != null)
        {
            if (isOn)
            {
                runtimeMaterial.SetFloat(LightSelectionProp, activeColorIndex);
                runtimeMaterial.SetFloat(IntensityProp, activeLightIntensity);
            }
            else
            {
                runtimeMaterial.SetFloat(LightSelectionProp, DisActiveColorIndex);
                runtimeMaterial.SetFloat(IntensityProp, 0.0f);
            }
        }
    }

    public void PlayFadeOut(float duration, Action onComplete = null)
    {
        fadeTween?.Kill();

        if (runtimeMaterial != null)
        {
            fadeTween = runtimeMaterial.DOFloat(0f, IntensityProp, duration).SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    SetLightState(false);
                    onComplete?.Invoke();
                });
        }
    }

    public void ResetLight()
    {
        fadeTween?.Kill();
        SetLightState(false);
    }
}