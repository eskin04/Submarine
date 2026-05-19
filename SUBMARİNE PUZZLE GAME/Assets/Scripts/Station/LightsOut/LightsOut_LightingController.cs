using UnityEngine;
using DG.Tweening;

public class LightsOut_LightingController : MonoBehaviour
{
    [Header("DOTween Settings")]
    public float fadeDuration = 0.3f;
    public Ease fadeEase = Ease.InOutQuad;

    private float originalAmbientIntensity;
    private float originalReflectionIntensity;

    private Tween ambientTween;
    private Tween reflectionTween;

    private void Start()
    {
        originalAmbientIntensity = RenderSettings.ambientIntensity;
        originalReflectionIntensity = RenderSettings.reflectionIntensity;
    }

    private void OnEnable()
    {
        LightsOut_StationManager.OnPowerStatusChanged += HandlePowerStatus;
    }

    private void OnDisable()
    {
        LightsOut_StationManager.OnPowerStatusChanged -= HandlePowerStatus;

        KillActiveTweens();
    }

    private void HandlePowerStatus(bool isPowered)
    {
        KillActiveTweens();

        float targetAmbient = isPowered ? originalAmbientIntensity : 0f;
        float targetReflection = isPowered ? originalReflectionIntensity : 0f;

        ambientTween = DOTween.To(() => RenderSettings.ambientIntensity,
                                   x => RenderSettings.ambientIntensity = x,
                                   targetAmbient,
                                   fadeDuration)
                               .SetEase(fadeEase);

        reflectionTween = DOTween.To(() => RenderSettings.reflectionIntensity,
                                      x => RenderSettings.reflectionIntensity = x,
                                      targetReflection,
                                      fadeDuration)
                                  .SetEase(fadeEase);
    }

    private void KillActiveTweens()
    {
        if (ambientTween != null && ambientTween.IsActive()) ambientTween.Kill();
        if (reflectionTween != null && reflectionTween.IsActive()) reflectionTween.Kill();
    }
}