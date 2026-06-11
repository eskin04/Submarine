using UnityEngine;
using System.Collections;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class Magnetic_WaveOscilloscope : MonoBehaviour
{
    [Header("References")]
    public Magnetic_StationManager stationManager;
    public Renderer oscilloscopeScreen;
    private Material screenMaterial;

    [Header("Controls")]
    public Magnetic_DiscreteKnob amplitudeKnob;
    public Magnetic_DiscreteKnob frequencyKnob;
    public Magnetic_DiscreteKnob phaseKnob;

    public Magnetic_ChannelButton[] channelButtons;

    [Header("UI Elements")]
    public CanvasGroup symbolCanvasGroup;
    public Image symbolImage;
    public Magnetic_SymbolDatabase symbolDatabase;

    public TMP_Text channelNameText;
    public float uiTransitionDuration = 0.5f;

    [Header("Local Player State")]
    private int currentAmplitude = 1;
    private int currentFrequency = 1;
    private int currentPhase = 1;

    private WaveConfig currentTargetWave;
    private bool isWaveLocked = false;

    private int maxUnlockedChannel = 0;
    private int currentlyViewedChannel = 0;

    private readonly int targetFreqID = Shader.PropertyToID("_TargetFrequency");
    private readonly int targetAmpID = Shader.PropertyToID("_TargetAmplitude");
    private readonly int targetPhaseID = Shader.PropertyToID("_TargetPhase");

    private readonly int playerFreqID = Shader.PropertyToID("_PlayerFrequency");
    private readonly int playerAmpID = Shader.PropertyToID("_PlayerAmplitude");
    private readonly int playerPhaseID = Shader.PropertyToID("_PlayerPhase");

    private void Awake()
    {
        if (oscilloscopeScreen != null) screenMaterial = oscilloscopeScreen.material;
    }

    private void Start()
    {
        if (stationManager != null && stationManager.isRoundActive.value)
        {
            HandlePuzzleGenerated();
            ChangeViewedChannel(stationManager.techCurrentChannel.value);
        }
    }

    private void OnEnable()
    {
        if (stationManager != null)
        {
            stationManager.OnPuzzleGenerated += HandlePuzzleGenerated;
            stationManager.OnTechChannelAdvanced += HandleChannelAdvanced;
        }
    }

    private void OnDisable()
    {
        if (stationManager != null)
        {
            stationManager.OnPuzzleGenerated -= HandlePuzzleGenerated;
            stationManager.OnTechChannelAdvanced -= HandleChannelAdvanced;
        }
    }

    private void HandlePuzzleGenerated()
    {
        maxUnlockedChannel = 0;
        currentAmplitude = 1;
        currentFrequency = 1;
        currentPhase = 2;

        if (amplitudeKnob != null) amplitudeKnob.InitializePosition(currentAmplitude);
        if (frequencyKnob != null) frequencyKnob.InitializePosition(currentFrequency);
        if (phaseKnob != null) phaseKnob.InitializePosition(currentPhase);

        ChangeViewedChannel(0);
    }

    // ==========================================

    public void ChangeViewedChannel(int channelIndex)
    {
        if (channelIndex > maxUnlockedChannel) return;

        currentlyViewedChannel = channelIndex;

        if (channelNameText != null)
        {
            channelNameText.text = $"Channel {currentlyViewedChannel + 1}";
        }

        for (int i = 0; i < channelButtons.Length; i++)
        {
            if (channelButtons[i] != null)
            {
                bool isThisActive = (i == currentlyViewedChannel);
                bool isThisLocked = (i > maxUnlockedChannel);
                channelButtons[i].UpdateButtonState(isThisActive, isThisLocked);
            }
        }

        // 3. Ekran Gösterim Kararı
        if (currentlyViewedChannel < maxUnlockedChannel)
        {
            isWaveLocked = true;
            ShowSymbolScreen(currentlyViewedChannel, playFadeAnim: false);
        }
        else
        {
            isWaveLocked = false;
            ShowWaveScreen(currentlyViewedChannel);
        }
    }

    private void ShowSymbolScreen(int channelIndex, bool playFadeAnim)
    {
        ChannelData data = stationManager.GetChannelData(channelIndex);

        if (symbolImage != null && symbolDatabase != null)
        {
            symbolImage.sprite = symbolDatabase.GetSymbol(data.symbolID);
        }

        if (symbolCanvasGroup != null)
        {
            symbolCanvasGroup.DOKill();

            if (playFadeAnim)
            {
                symbolCanvasGroup.DOFade(1f, uiTransitionDuration);

            }
            else
            {
                symbolCanvasGroup.alpha = 1f;
            }
        }
    }

    private void ShowWaveScreen(int channelIndex)
    {
        LoadChannelTargetWave(channelIndex);
        UpdatePlayerShader();

        if (symbolCanvasGroup != null)
        {
            symbolCanvasGroup.DOKill();
            symbolCanvasGroup.alpha = 0f;
        }
    }

    private void LoadChannelTargetWave(int channelIndex)
    {
        ChannelData data = stationManager.GetChannelData(channelIndex);
        currentTargetWave = data.targetWave;

        if (screenMaterial != null)
        {
            screenMaterial.SetFloat(targetAmpID, currentTargetWave.amplitude);
            screenMaterial.SetFloat(targetFreqID, (currentTargetWave.frequency - 1f) * 10);
            screenMaterial.SetFloat(targetPhaseID, currentTargetWave.phase);
        }
    }

    // ==========================================

    public void ChangeAmplitude(int amount)
    {
        if (isWaveLocked) return;
        currentAmplitude += amount;
        if (currentAmplitude > 6) currentAmplitude = 1; else if (currentAmplitude < 1) currentAmplitude = 6;
        OnKnobTurned();
    }

    public void ChangeFrequency(int amount)
    {
        if (isWaveLocked) return;
        currentFrequency += amount;
        if (currentFrequency > 6) currentFrequency = 1; else if (currentFrequency < 1) currentFrequency = 6;
        OnKnobTurned();
    }

    public void ChangePhase(int amount)
    {
        if (isWaveLocked) return;
        currentPhase += amount;
        if (currentPhase > 3) currentPhase = 1; else if (currentPhase < 1) currentPhase = 3;
        OnKnobTurned();
    }

    private void OnKnobTurned()
    {
        UpdatePlayerShader();
        CheckIfWaveMatches();
    }

    private void UpdatePlayerShader()
    {
        if (screenMaterial != null)
        {
            screenMaterial.SetFloat(playerAmpID, currentAmplitude);
            screenMaterial.SetFloat(playerFreqID, (currentFrequency - 1f) * 10);
            screenMaterial.SetFloat(playerPhaseID, currentPhase);
        }
    }

    private void CheckIfWaveMatches()
    {
        if (currentAmplitude == currentTargetWave.amplitude &&
            currentFrequency == currentTargetWave.frequency &&
            currentPhase == currentTargetWave.phase)
        {
            isWaveLocked = true;

            ShowSymbolScreen(currentlyViewedChannel, playFadeAnim: true);

            stationManager.SubmitWaveRPC(currentAmplitude, currentFrequency, currentPhase);
        }
    }

    private void HandleChannelAdvanced(int newChannelIndex)
    {
        maxUnlockedChannel = newChannelIndex;

        if (newChannelIndex < 3)
        {
            for (int i = 0; i < channelButtons.Length; i++)
            {
                if (channelButtons[i] != null)
                {
                    bool isThisActive = (i == currentlyViewedChannel);
                    bool isThisLocked = (i > maxUnlockedChannel);
                    channelButtons[i].UpdateButtonState(isThisActive, isThisLocked);
                }
            }

            // if (currentlyViewedChannel == newChannelIndex - 1)
            // {
            //     StartCoroutine(AutoSwitchToNextChannel(newChannelIndex));
            // }
        }
    }

    private IEnumerator AutoSwitchToNextChannel(int newChannelIndex)
    {
        yield return new WaitForSeconds(1.5f);

        if (currentlyViewedChannel == newChannelIndex - 1)
        {
            ChangeViewedChannel(newChannelIndex);
        }
    }
}