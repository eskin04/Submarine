using UnityEngine;
using TMPro;
using PurrNet;
using DG.Tweening;

public class SecurityLockdown_Numpad : NetworkBehaviour
{
    [Header("Settings")]
    public RegionID myRegion;
    public TMP_Text regionLabel;

    [Header("References")]
    public TMP_Text displayScreen;
    public SecurityLockdown_StationManager stationManager;

    [Header("Status Light")]
    public MeshRenderer statusLightRenderer;
    public float lightIntensity = 5.0f;
    private Material runtimeMaterial;

    [Header("Audio Settings")]
    public AudioEventChannelSO _channel;
    public FMODUnity.EventReference buttonSound;
    public FMODUnity.EventReference correctSound;

    private static readonly int LightSelectionProp = Shader.PropertyToID("_ColorIndex");
    private static readonly int IntensityProp = Shader.PropertyToID("_LightIntensity");

    private string currentInput = "";
    private Interactable ınteractable;

    private void Awake()
    {
        if (statusLightRenderer != null)
        {
            runtimeMaterial = statusLightRenderer.materials[0];
            TurnOffLight();
        }
        if (regionLabel != null)
        {
            regionLabel.text = $"{myRegion}";
        }
        ınteractable = GetComponent<Interactable>();
    }



    private void OnEnable()
    {
        SecurityLockdown_StationManager.OnStationFailed += ResetLightToRed;
        SecurityLockdown_StationManager.OnStateChanged += HandleStateChanged;
        SecurityLockdown_StationManager.OnRegionSolved += HandleRegionSolved;
        if (stationManager != null)
        {
            stationManager.areNumpadsActiveSync.onChanged += HandleNumpadActivation;
        }
    }

    private void OnDisable()
    {
        SecurityLockdown_StationManager.OnStationFailed -= ResetLightToRed;
        SecurityLockdown_StationManager.OnStateChanged -= HandleStateChanged;
        SecurityLockdown_StationManager.OnRegionSolved -= HandleRegionSolved;
        if (stationManager != null)
        {
            stationManager.areNumpadsActiveSync.onChanged -= HandleNumpadActivation;
        }
    }

    private void HandleNumpadActivation(bool areActive)
    {
        if (ınteractable != null)
        {
            ınteractable.SetInteractable(areActive);
        }
    }

    public void OnNumberPressed(int number)
    {
        if (stationManager == null || !stationManager.AreNumpadsActive) return;
        int limit = stationManager.currentVariationDigits.value;
        if (currentInput.Length < limit)
        {
            currentInput += number.ToString();
            UpdateDisplay();
            PlayButtonSound();


            displayScreen.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f);
        }
    }

    private void PlayButtonSound()
    {
        if (_channel != null && !buttonSound.IsNull)
        {
            AudioEventPayload payload = new AudioEventPayload(buttonSound, this.transform.position);
            _channel.RaiseEvent(payload);
        }
    }

    public void OnClearPressed()
    {
        currentInput = "";
        UpdateDisplay();
    }

    public void OnSubmitPressed()
    {
        if (stationManager == null || !stationManager.AreNumpadsActive) return;
        if (string.IsNullOrEmpty(currentInput)) return;

        int enteredValue = int.Parse(currentInput);
        stationManager.SubmitNumpadEntryRPC(myRegion, enteredValue);

        OnClearPressed();
    }

    private void UpdateDisplay()
    {
        if (displayScreen != null)
        {
            // int limit = stationManager.currentVariationDigits.value;
            // displayScreen.text = currentInput.PadLeft(limit, '_');
            displayScreen.text = currentInput;
        }
    }

    #region LIGHT CONTROL (SHADER)

    private void HandleStateChanged(LockDownStationState state)
    {
        if (state == LockDownStationState.Active)
        {
            ResetLightToRed();
        }
        if (state == LockDownStationState.Solved)
        {
            TurnOffLight();
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
                PlayCorrectSound();
            }
        }
    }

    private void PlayCorrectSound()
    {
        if (_channel != null && !correctSound.IsNull)
        {
            AudioEventPayload payload = new AudioEventPayload(correctSound, this.transform.position);
            _channel.RaiseEvent(payload);
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