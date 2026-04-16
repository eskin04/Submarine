using UnityEngine;
using TMPro;
using PurrNet;
using DG.Tweening;

public class EngineerLockDown_Numpad : NetworkBehaviour
{
    [Header("References")]
    public TMP_Text displayScreen;
    public EngineerLockDown_StationManager stationManager;

    [Header("3 Step Status Lights")]
    public MeshRenderer[] stepLights;
    public float lightIntensity = 5.0f;

    [Header("Audio Settings")]
    public AudioEventChannelSO _channel;
    public FMODUnity.EventReference buttonSound;
    public FMODUnity.EventReference correctSound;
    public FMODUnity.EventReference wrongSound;
    private Material[] runtimeMaterials = new Material[3];

    private static readonly int LightSelectionProp = Shader.PropertyToID("_ColorIndex");
    private static readonly int IntensityProp = Shader.PropertyToID("_LightIntensity");

    private string currentInput = "";
    private int maxDigits = 3;

    private void Awake()
    {
        for (int i = 0; i < stepLights.Length; i++)
        {
            if (stepLights[i] != null)
                runtimeMaterials[i] = stepLights[i].materials[0];
        }
    }

    private void OnEnable()
    {
        EngineerLockDown_StationManager.OnOverrideStateChanged += HandleStateChanged;
        EngineerLockDown_StationManager.OnOverrideStepCompleted += LightUpGreen;
        EngineerLockDown_StationManager.OnOverrideFailed += ResetAllLightsToRed;
        EngineerLockDown_StationManager.OnOverrideFailed += PlayWrongSound;
    }

    private void OnDisable()
    {
        EngineerLockDown_StationManager.OnOverrideStateChanged -= HandleStateChanged;
        EngineerLockDown_StationManager.OnOverrideStepCompleted -= LightUpGreen;
        EngineerLockDown_StationManager.OnOverrideFailed -= ResetAllLightsToRed;
        EngineerLockDown_StationManager.OnOverrideFailed -= PlayWrongSound;
    }

    public void OnNumberPressed(int number)
    {
        if (stationManager != null && stationManager.currentOverrideStep.value >= 3) return;
        if (currentInput.Length < maxDigits)
        {
            currentInput += number.ToString();
            UpdateDisplay();
            displayScreen.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f);
            PlayButtonSound();

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
        if (string.IsNullOrEmpty(currentInput)) return;

        int enteredValue = int.Parse(currentInput);
        stationManager.SubmitOverrideEntryRPC(enteredValue);

        OnClearPressed();
    }

    private void UpdateDisplay()
    {
        if (displayScreen != null) displayScreen.text = currentInput;
    }

    private void HandleStateChanged(EngineerLockDownStationState state)
    {
        if (state == EngineerLockDownStationState.Active) ResetAllLightsToRed();
    }

    private void ResetAllLightsToRed()
    {
        displayScreen.transform.DOShakePosition(0.5f, 5f);

        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            if (runtimeMaterials[i] != null)
            {
                runtimeMaterials[i].SetFloat(LightSelectionProp, 1);
                runtimeMaterials[i].SetFloat(IntensityProp, lightIntensity);
            }
        }
    }

    private void LightUpGreen(int stepIndex)
    {
        if (stepIndex >= 0 && stepIndex < runtimeMaterials.Length)
        {
            if (runtimeMaterials[stepIndex] != null)
            {
                runtimeMaterials[stepIndex].SetFloat(LightSelectionProp, 2);
                runtimeMaterials[stepIndex].SetFloat(IntensityProp, lightIntensity);
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

    private void PlayWrongSound()
    {
        if (_channel != null && !wrongSound.IsNull)
        {
            AudioEventPayload payload = new AudioEventPayload(wrongSound, this.transform.position);
            _channel.RaiseEvent(payload);
        }
    }
}