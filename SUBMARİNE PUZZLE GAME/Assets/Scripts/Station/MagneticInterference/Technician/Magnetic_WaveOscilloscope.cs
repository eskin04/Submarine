using UnityEngine;
using System.Collections;
using TMPro;
using DG.Tweening;

public class Magnetic_WaveOscilloscope : MonoBehaviour
{
    [Header("References")]
    public Magnetic_StationManager stationManager;
    public Renderer oscilloscopeScreen;
    private Material screenMaterial;

    [Header("Fiziksel Knob Referanslari")]
    public Magnetic_DiscreteKnob amplitudeKnob;
    public Magnetic_DiscreteKnob frequencyKnob;
    public Magnetic_DiscreteKnob phaseKnob;

    [Header("Symbol UI Elements")]
    [Tooltip("Sembolün ve arka planının bulunduğu Canvas Group")]
    public CanvasGroup symbolCanvasGroup;
    [Tooltip("Sembolün yazdırılacağı Text objesi")]
    public TextMeshProUGUI symbolText;
    public float uiTransitionDuration = 0.5f;

    [Header("Local Player State")]
    private int currentAmplitude = 1;
    private int currentFrequency = 1;
    private int currentPhase = 1;

    private WaveConfig currentTargetWave;
    private bool isWaveLocked = false;

    // Kanal Gezinme State'leri
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
        currentPhase = 2; // PDF başlangıç kuralı 1-1-2

        if (amplitudeKnob != null) amplitudeKnob.InitializePosition(currentAmplitude);
        if (frequencyKnob != null) frequencyKnob.InitializePosition(currentFrequency);
        if (phaseKnob != null) phaseKnob.InitializePosition(currentPhase);

        ChangeViewedChannel(0); // CH1'i yükle ve sistemi başlat
    }

    // ==========================================
    // KANAL GEZİNME VE GÖRSEL YÖNETİM
    // ==========================================

    public void ChangeViewedChannel(int channelIndex)
    {
        // Kilitli bir kanala (geleceğe) bakılamaz
        if (channelIndex > maxUnlockedChannel)
        {
            // Hata Sesi çalınabilir (Bip!)
            return;
        }

        currentlyViewedChannel = channelIndex;

        if (currentlyViewedChannel < maxUnlockedChannel)
        {
            // Bu kanal zaten çözülmüş! Dalgayı kapat, sembolü göster. Knobları kilitle.
            isWaveLocked = true;
            ShowSymbolScreen(currentlyViewedChannel);
        }
        else
        {
            // Bu kanal şu an çözmeye çalıştığımız aktif kanal. Sembolü kapat, dalgayı göster.
            isWaveLocked = false;
            ShowWaveScreen(currentlyViewedChannel);
        }
    }

    private void ShowSymbolScreen(int channelIndex)
    {
        ChannelData data = stationManager.GetChannelData(channelIndex);

        if (symbolText != null)
        {
            // İleride kendi Alien veya özel fontunu atadığında bu string'i ona göre değiştirebilirsin
            symbolText.text = $"S{data.symbolID}";
        }

        // Sembol ekranını yavaşça görünür yap (Arka planındaki siyah panel osiloskobu gizler)
        if (symbolCanvasGroup != null) symbolCanvasGroup.DOFade(1f, uiTransitionDuration);
    }

    private void ShowWaveScreen(int channelIndex)
    {
        LoadChannelTargetWave(channelIndex);
        UpdatePlayerShader();

        // Sembolü gizle
        if (symbolCanvasGroup != null) symbolCanvasGroup.alpha = 0f;


    }

    private void LoadChannelTargetWave(int channelIndex)
    {
        ChannelData data = stationManager.GetChannelData(channelIndex);
        currentTargetWave = data.targetWave;

        if (screenMaterial != null)
        {
            screenMaterial.SetFloat(targetAmpID, currentTargetWave.amplitude);
            screenMaterial.SetFloat(targetFreqID, (currentTargetWave.frequency - 1f) * 10f);
            screenMaterial.SetFloat(targetPhaseID, currentTargetWave.phase);
        }
    }

    // ==========================================
    // ETKİLEŞİM VE BAŞARI KONTROLÜ
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
            screenMaterial.SetFloat(playerFreqID, (currentFrequency - 1f) * 10f);
            screenMaterial.SetFloat(playerPhaseID, currentPhase);
        }
    }

    private void CheckIfWaveMatches()
    {
        if (currentAmplitude == currentTargetWave.amplitude &&
            currentFrequency == currentTargetWave.frequency &&
            currentPhase == currentTargetWave.phase)
        {
            // 1. Dalga eşleştiği an oyuncuyu kilitliyoruz
            isWaveLocked = true;

            // 2. Bekleme olmaksızın anında sembolü ekranda gösteriyoruz (Başarı hissiyatı)
            ShowSymbolScreen(currentlyViewedChannel);

            // 3. Başarı sesi çalınabilir

            // 4. Server'a bildir
            stationManager.SubmitWaveRPC(currentAmplitude, currentFrequency, currentPhase);
        }
    }

    private void HandleChannelAdvanced(int newChannelIndex)
    {
        maxUnlockedChannel = newChannelIndex;

        if (newChannelIndex < 3)
        {
            // Eğer oyuncu başarının ardından hala çözdüğü ekrana bakıyorsa, sembolü beynine 
            // kazıması için 1.5 saniye verip sonra YENİ kanala otomatik geçiriyoruz.
            if (currentlyViewedChannel == newChannelIndex - 1)
            {
                StartCoroutine(AutoSwitchToNextChannel(newChannelIndex));
            }
        }
    }

    private IEnumerator AutoSwitchToNextChannel(int newChannelIndex)
    {
        yield return new WaitForSeconds(3f);

        // Eğer bu 1.5 saniyelik izleme süresinde oyuncu başka bir tuşa basmadıysa yeni kanala atla
        if (currentlyViewedChannel == newChannelIndex - 1)
        {
            ChangeViewedChannel(newChannelIndex);
        }
    }
}