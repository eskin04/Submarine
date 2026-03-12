using UnityEngine;
using TMPro;
using PurrNet;
using DG.Tweening;

public class SecurityLockdown_Numpad : NetworkBehaviour
{
    [Header("Settings")]
    public RegionID myRegion;
    public int maxDigits = 5;

    [Header("References")]
    public TMP_Text displayScreen;
    public SecurityLockdown_StationManager stationManager;

    [Header("Status Light")]
    public MeshRenderer statusLightRenderer;
    public float lightIntensity = 5.0f;
    private Material runtimeMaterial;

    private static readonly int LightSelectionProp = Shader.PropertyToID("_ColorIndex");
    private static readonly int IntensityProp = Shader.PropertyToID("_LightIntensity");

    private string currentInput = "";

    private void Awake()
    {
        if (statusLightRenderer != null)
        {
            runtimeMaterial = statusLightRenderer.materials[0];
            TurnOffLight();
        }
    }


    private void OnEnable()
    {
        SecurityLockdown_StationManager.OnStationFailed += ResetLightToRed;
        SecurityLockdown_StationManager.OnSoftReset += ResetLightToRed;
        SecurityLockdown_StationManager.OnStateChanged += HandleStateChanged;
        SecurityLockdown_StationManager.OnRegionSolved += HandleRegionSolved;
    }

    private void OnDisable()
    {
        SecurityLockdown_StationManager.OnStationFailed -= ResetLightToRed;
        SecurityLockdown_StationManager.OnSoftReset -= ResetLightToRed;
        SecurityLockdown_StationManager.OnStateChanged -= HandleStateChanged;
        SecurityLockdown_StationManager.OnRegionSolved -= HandleRegionSolved;
    }

    public void OnNumberPressed(int number)
    {
        int limit = stationManager.currentVariationDigits.value;
        if (currentInput.Length < limit)
        {
            currentInput += number.ToString();
            UpdateDisplay();

            displayScreen.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f);
        }
    }

    public void OnClearPressed()
    {
        currentInput = "";
        UpdateDisplay();
    }

    public void OnSubmitPressed()
    {
        if (string.IsNullOrEmpty(currentInput)) return;

        int enteredValue = int.Parse(currentInput);
        stationManager.SubmitNumpadEntryRPC(myRegion, enteredValue);

        OnClearPressed();
    }

    private void UpdateDisplay()
    {
        if (displayScreen != null)
        {
            int limit = stationManager.currentVariationDigits.value;
            displayScreen.text = currentInput.PadLeft(limit, '-');
        }
    }

    #region LIGHT CONTROL (SHADER)

    private void HandleStateChanged(LockDownStationState state)
    {
        if (state == LockDownStationState.Active)
        {
            ResetLightToRed();
        }
    }

    private void ResetLightToRed()
    {
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(LightSelectionProp, 1);
            runtimeMaterial.SetFloat(IntensityProp, lightIntensity);
        }
    }

    private void HandleRegionSolved(RegionID solvedRegion)
    {
        if (solvedRegion == myRegion)
        {
            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetFloat(LightSelectionProp, 2);
                runtimeMaterial.SetFloat(IntensityProp, lightIntensity);
            }
        }
    }

    public void TurnOffLight()
    {
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(LightSelectionProp, 0);
            runtimeMaterial.SetFloat(IntensityProp, 0.0f);
        }
    }

    #endregion
}