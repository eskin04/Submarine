using UnityEngine;
using DG.Tweening;

public class Thermal_WarningLight : MonoBehaviour
{
    public enum WarningState { Off, Flashing, Solid, Error, Won, Lost }
    private WarningState currentState = WarningState.Off;

    [Header("Shader")]
    public MeshRenderer meshRenderer;
    public float maxIntensity = 5.0f;

    public int shaderColorIndex = 1;

    public int winColorIndex = 2;

    [Header("Settings")]
    public float flashSpeed = 0.5f;
    public float errorFadeOutTime = 0.5f;

    private Material runtimeMaterial;
    private static readonly int LightSelectionProp = Shader.PropertyToID("_ColorIndex");
    private static readonly int IntensityProp = Shader.PropertyToID("_LightIntensity");

    private bool isBottleneck = false;
    private float currentHeat = 0f;

    private void Awake()
    {
        if (meshRenderer != null)
        {
            runtimeMaterial = meshRenderer.materials[0];
            ResetLight();
        }
    }

    public void UpdateHeat(float heat)
    {
        currentHeat = heat;

        if (currentState == WarningState.Error || currentState == WarningState.Won || currentState == WarningState.Lost) return;

        EvaluateState();
    }

    public void SetBottleneckActive(bool active)
    {
        isBottleneck = active;
        if (currentState == WarningState.Won || currentState == WarningState.Lost) return;
        EvaluateState();
    }

    public void TriggerErrorSequence()
    {
        if (currentState == WarningState.Won || currentState == WarningState.Lost) return;
        ChangeState(WarningState.Error);
    }

    public void SetStationEndState(bool isWin)
    {
        ChangeState(isWin ? WarningState.Won : WarningState.Lost);
    }

    public void ResetLight()
    {
        isBottleneck = false;
        currentHeat = 0f;
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(LightSelectionProp, shaderColorIndex);
        }
        ChangeState(WarningState.Off);
    }

    private void EvaluateState()
    {
        if (currentState == WarningState.Won || currentState == WarningState.Lost) return;

        WarningState targetState = WarningState.Off;

        if (isBottleneck) targetState = WarningState.Solid;
        else if (currentHeat >= 75f) targetState = WarningState.Flashing;

        if (targetState == currentState) return;

        ChangeState(targetState);
    }

    private void ChangeState(WarningState newState)
    {
        currentState = newState;

        DOTween.Kill(this);

        switch (newState)
        {
            case WarningState.Off:
                SetIntensity(0f);
                break;

            case WarningState.Solid:
                SetIntensity(maxIntensity);
                break;

            case WarningState.Flashing:
                float flashVal = 0f;
                DOTween.To(() => flashVal, x => { flashVal = x; SetIntensity(flashVal); }, maxIntensity, flashSpeed)
                       .SetLoops(-1, LoopType.Yoyo)
                       .SetEase(Ease.InOutSine)
                       .SetTarget(this);
                break;

            case WarningState.Error:
                float errVal = maxIntensity;
                DOTween.To(() => errVal, x => { errVal = x; SetIntensity(errVal); }, 0f, errorFadeOutTime)
                       .SetEase(Ease.OutQuad)
                       .SetTarget(this)
                       .OnComplete(() =>
                       {
                           DOVirtual.DelayedCall(0.2f, () =>
                           {
                               if (currentState == WarningState.Error)
                               {
                                   currentState = WarningState.Off;
                                   EvaluateState();
                               }
                           }).SetTarget(this);
                       });
                break;

            case WarningState.Won:
                if (runtimeMaterial != null) runtimeMaterial.SetFloat(LightSelectionProp, winColorIndex);
                SetIntensity(maxIntensity);
                break;

            case WarningState.Lost:
                if (runtimeMaterial != null) runtimeMaterial.SetFloat(LightSelectionProp, shaderColorIndex);
                SetIntensity(maxIntensity);
                break;
        }
    }

    private void SetIntensity(float val)
    {
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(IntensityProp, val);
        }
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
}