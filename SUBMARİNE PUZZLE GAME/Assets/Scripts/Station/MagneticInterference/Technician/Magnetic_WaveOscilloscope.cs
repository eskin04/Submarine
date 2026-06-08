using UnityEngine;
// using FMODUnity; // Sesler için eklenebilir
// using DG.Tweening; // Knob animasyonları için eklenebilir

public class Magnetic_WaveOscilloscope : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Sahnede bulunan ana Manager objesini buraya sürükleyin.")]
    public Magnetic_StationManager stationManager;

    [Tooltip("Shader'ın bulunduğu ekran (MeshRenderer).")]
    public Renderer oscilloscopeScreen;

    private Material screenMaterial;

    [Header("Local Player State")]
    // Teknisyenin başlangıçtaki dalga değerleri
    private int currentAmplitude = 1; // 1-6
    private int currentFrequency = 1; // 1-6
    private int currentPhase = 2;     // 1-3

    private WaveConfig currentTargetWave;
    private bool isWaveLocked = false; // Doğru dalga bulunduğunda knobları geçici kilitlemek için
    private int maxUnlockedChannel = 0; // Teknisyenin çözebildiği en yüksek kanal
    private int currentlyViewedChannel = 0; // Teknisyenin şu an fiziksel tuşlarla seçtiği kanal

    // Shader Property ID'leri (String arama maliyetinden kurtulmak için)
    private readonly int targetFreqID = Shader.PropertyToID("_TargetFrequency");
    private readonly int targetAmpID = Shader.PropertyToID("_TargetAmplitude");
    private readonly int targetPhaseID = Shader.PropertyToID("_TargetPhase");

    private readonly int playerFreqID = Shader.PropertyToID("_PlayerFrequency");
    private readonly int playerAmpID = Shader.PropertyToID("_PlayerAmplitude");
    private readonly int playerPhaseID = Shader.PropertyToID("_PlayerPhase");

    private void Awake()
    {
        // Çalışma anında materyalin bir kopyasını alıyoruz ki diğer objeler etkilenmesin
        if (oscilloscopeScreen != null)
        {
            screenMaterial = oscilloscopeScreen.material;

        }
    }

    private void OnEnable()
    {
        // Manager'daki eventlere abone oluyoruz
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
        isWaveLocked = false;

        // Başlangıç değerlerini sıfırla (1-1-1)
        currentAmplitude = 1;
        currentFrequency = 1;
        currentPhase = 2;
        UpdatePlayerShader();

        // CH1 verilerini yükle
        LoadChannelTargetWave(0);
    }

    private void HandleChannelAdvanced(int newChannelIndex)
    {
        maxUnlockedChannel = newChannelIndex;

        if (newChannelIndex < 3)
        {
            // Yeni kanal açıldığında otomatik olarak o kanalın görünümüne geç
            ChangeViewedChannel(newChannelIndex);
        }
    }

    public void ChangeViewedChannel(int channelIndex)
    {
        // Sadece kilidi açılmış kanallara veya aktif çözülen kanala bakılabilir
        if (channelIndex > maxUnlockedChannel) return;

        currentlyViewedChannel = channelIndex;
        LoadChannelTargetWave(currentlyViewedChannel);

        // Eğer dönüp bakılan kanal zaten çözülmüş bir kanalsa, knobları çevirmeyi engelle
        isWaveLocked = (currentlyViewedChannel < maxUnlockedChannel);
    }

    private void LoadChannelTargetWave(int channelIndex)
    {
        ChannelData data = stationManager.GetChannelData(channelIndex);
        currentTargetWave = data.targetWave;
        Debug.Log($"Viewing Channel {channelIndex + 1}: Target Wave - Amp: {currentTargetWave.amplitude}, Freq: {currentTargetWave.frequency}, Phase: {currentTargetWave.phase}");

        // Hedef dalgayı shader'a ilet (float olarak gönderilir)
        if (screenMaterial != null)
        {
            screenMaterial.SetFloat(targetAmpID, currentTargetWave.amplitude);
            screenMaterial.SetFloat(targetFreqID, (currentTargetWave.frequency - 1) * 10f);
            screenMaterial.SetFloat(targetPhaseID, currentTargetWave.phase);
        }

        isWaveLocked = false;
    }

    // ==========================================
    // ETKİLEŞİM METOTLARI (Fiziksel Knob'lardan Çağrılacak)
    // ==========================================

    // Örneğin Knob'u sağa çevirince ChangeAmplitude(1), sola çevirince ChangeAmplitude(-1) çağrılır
    public void ChangeAmplitude(int amount)
    {
        if (isWaveLocked) return;

        currentAmplitude += amount;

        // Sınırları aşarsa diğer taraftan başa sar (1 ile 6 arası)
        if (currentAmplitude > 6) currentAmplitude = 1;
        else if (currentAmplitude < 1) currentAmplitude = 6;

        OnKnobTurned();
    }

    public void ChangeFrequency(int amount)
    {
        if (isWaveLocked) return;

        currentFrequency += amount;

        // Sınırları aşarsa diğer taraftan başa sar (1 ile 6 arası)
        if (currentFrequency > 6) currentFrequency = 1;
        else if (currentFrequency < 1) currentFrequency = 6;

        OnKnobTurned();
    }

    public void ChangePhase(int amount)
    {
        if (isWaveLocked) return;

        currentPhase += amount;

        // Phase (Faz) sadece 1, 2 ve 3 değerlerini alıyor
        if (currentPhase > 3) currentPhase = 1;
        else if (currentPhase < 1) currentPhase = 3;

        OnKnobTurned();
    }

    private void OnKnobTurned()
    {
        // 1. FMOD ile klik sesi çalınabilir
        // RuntimeManager.PlayOneShot("event:/UI/Knob_Click");

        // 2. DOTween ile Knob'un fiziksel dönüş animasyonu tetiklenebilir (Burada veya etkileşim scriptinde)

        // 3. Shader güncellenir
        UpdatePlayerShader();

        // 4. Doğru kombinasyon mu diye kontrol edilir
        CheckIfWaveMatches();
    }

    private void UpdatePlayerShader()
    {
        if (screenMaterial != null)
        {
            screenMaterial.SetFloat(playerAmpID, currentAmplitude);
            screenMaterial.SetFloat(playerFreqID, (currentFrequency - 1) * 10f);
            screenMaterial.SetFloat(playerPhaseID, currentPhase);
        }
    }

    private void CheckIfWaveMatches()
    {
        if (currentAmplitude == currentTargetWave.amplitude &&
            currentFrequency == currentTargetWave.frequency &&
            currentPhase == currentTargetWave.phase)
        {
            // Dalga eşleşti! Artık knobları kilitleyip Server'a bildiriyoruz
            isWaveLocked = true;

            // FMOD ile başarı sinyali sesi çalınabilir
            // RuntimeManager.PlayOneShot("event:/UI/Wave_Match");

            // PurrNet üzerinden ServerRpc çağrısı (Ağ trafiği sadece oyuncu doğruyu bulduğunda yaşanır)
            stationManager.SubmitWaveRPC(currentAmplitude, currentFrequency, currentPhase);
        }
    }
}