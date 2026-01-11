using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using System.Threading.Tasks;

public class RadioVoiceManager : MonoBehaviour
{
    [Header("Telsiz Ayarları")]
    public KeyCode pushToTalkKey = KeyCode.Q;

    [Header("Ses Efektleri")]
    public AudioSource radioSFXSource;
    public AudioClip connectClip;     // Konuşma Başlangıcı (Bip)
    public AudioClip disconnectClip;  // Konuşma Bitişi (Kşşt)

    [Header("Girişim (Interference) Ayarları")]
    [Tooltip("İki kişi aynı anda konuştuğunda ve biri sustuğunda çalacak cızırtı sesi")]
    public AudioClip interferenceEndClip;

    // Durum Değişkenleri
    private bool isLoggedIn = false;
    private bool isTransmitting = false; // Ben konuşuyor muyum?
    private bool isChannelBusy = false;  // Başkası konuşuyor mu?

    // Girişim durumunu takip etmek için
    private bool wasInterferenceActive = false;

    private string currentChannelName;

    // --- BAŞLATMA ---

    public async void StartLobbyVoice(string channelName = "GlobalOpsRadio")
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        await InitializeVivoxAsync(channelName);
    }

    private async Task InitializeVivoxAsync(string channelName)
    {
        try
        {
            await VivoxService.Instance.InitializeAsync();

            LoginOptions options = new LoginOptions();
            options.DisplayName = "Operator_" + Random.Range(100, 999);

            await VivoxService.Instance.LoginAsync(options);

            Debug.Log("Vivox Girişi Başarılı!");

            JoinRadioChannel(channelName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Vivox Hatası: " + e.Message);
        }
    }

    private async void JoinRadioChannel(string channelName)
    {
        currentChannelName = channelName;

        VivoxService.Instance.MuteInputDevice();

        await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);
        isLoggedIn = VivoxService.Instance.IsLoggedIn;

        Debug.Log($"Telsiz Kanalına Katılındı: {channelName}");
    }

    // --- GÜNCELLEME DÖNGÜSÜ ---

    void Update()
    {
        if (isLoggedIn)
        {
            // 1. Kanal Durumunu Kontrol Et (Başkası konuşuyor mu?)
            CheckChannelStatus();

            // 2. Girişim (Interference) Yönetimi - YENİ ÖZELLİK
            HandleInterference();

            // 3. Tuş Kontrolleri (Artık engelleme yok, herkes basabilir)
            if (Input.GetKeyDown(pushToTalkKey))
            {
                StartTransmission();
            }

            if (Input.GetKeyUp(pushToTalkKey))
            {
                if (isTransmitting)
                {
                    StopTransmission();
                }
            }
        }
    }

    // --- GİRİŞİM YÖNETİMİ (CORE LOGIC) ---
    void HandleInterference()
    {
        // Kesişim Kuralı: HEM ben konuşuyorum HEM DE başkası konuşuyor.
        bool isInterferenceNow = isTransmitting && isChannelBusy;

        if (isInterferenceNow)
        {
            // Çakışma var: Gelen sesi tamamen kapat (Birbirinizi duyamazsınız)
            VivoxService.Instance.SetOutputDeviceVolume(0);
        }
        else
        {
            // Çakışma yok: Sesi normal seviyeye getir (100)
            // Not: Sürekli 100 set etmek performans yemez, Vivox bunu optimize eder.
            VivoxService.Instance.SetOutputDeviceVolume(100);
        }

        // FEEDBACK MANTIĞI:
        // Durum değişimi: Az önce çakışma vardı -> Şimdi yok.
        if (wasInterferenceActive && !isInterferenceNow)
        {
            // Eğer çakışma bittiğinde BEN HALA KONUŞUYORSAM
            // (Yani karşı taraf sustu, yeşil çizgi bitti ama ben devam ediyorum)
            if (isTransmitting)
            {
                PlayInterferenceSound();
                Debug.Log("Girişim bitti, hat temizlendi.");
            }
        }

        // Durumu kaydet
        wasInterferenceActive = isInterferenceNow;
    }

    void CheckChannelStatus()
    {
        isChannelBusy = false;

        if (VivoxService.Instance.ActiveChannels == null || string.IsNullOrEmpty(currentChannelName))
        {
            return;
        }

        if (VivoxService.Instance.ActiveChannels.TryGetValue(currentChannelName, out var participants))
        {
            foreach (var participant in participants)
            {
                // Ben olmayan biri konuşuyorsa kanal doludur
                if (!participant.IsSelf && participant.SpeechDetected)
                {
                    isChannelBusy = true;
                    return;
                }
            }
        }
    }

    // --- İLETİM FONKSİYONLARI ---

    void StartTransmission()
    {
        if (radioSFXSource != null && connectClip != null)
        {
            radioSFXSource.PlayOneShot(connectClip);
        }

        VivoxService.Instance.UnmuteInputDevice();
        isTransmitting = true;
        // Debug.Log("Telsiz AÇIK");
    }

    void StopTransmission()
    {
        if (radioSFXSource != null && disconnectClip != null)
        {
            radioSFXSource.PlayOneShot(disconnectClip);
        }

        VivoxService.Instance.MuteInputDevice();
        isTransmitting = false;
        // Debug.Log("Telsiz KAPALI");
    }

    void PlayInterferenceSound()
    {
        // Çakışma bittiğinde çalacak kısa bir cızırtı
        if (radioSFXSource != null && interferenceEndClip != null)
        {
            radioSFXSource.PlayOneShot(interferenceEndClip, 0.5f);
        }
    }

    void OnApplicationQuit()
    {
        if (isLoggedIn)
        {
            VivoxService.Instance.LogoutAsync();
        }
    }
}