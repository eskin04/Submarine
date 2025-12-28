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
    public AudioClip connectClip;
    public AudioClip disconnectClip;

    [Tooltip("Kanal doluyken basılırsa çalacak 'Meşgul/Hata' sesi")]
    public AudioClip busyErrorClip;

    private bool isLoggedIn = false;
    private bool isTransmitting = false;
    private bool isChannelBusy = false;

    private string currentChannelName;


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


    void Update()
    {
        if (isLoggedIn)
        {
            CheckChannelStatus();

            if (Input.GetKeyDown(pushToTalkKey))
            {
                if (isChannelBusy && !isTransmitting)
                {
                    PlayBusySound();
                    Debug.Log("KANAL MEŞGUL! Şu an başkası konuşuyor.");
                }
                else
                {
                    StartTransmission();
                }
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
                if (!participant.IsSelf && participant.SpeechDetected)
                {
                    isChannelBusy = true;
                    return;
                }
            }
        }
    }


    void StartTransmission()
    {
        if (radioSFXSource != null && connectClip != null)
        {
            radioSFXSource.PlayOneShot(connectClip);
        }

        VivoxService.Instance.UnmuteInputDevice();
        isTransmitting = true;
        Debug.Log("Telsiz AÇIK (Konuşuluyor...)");
    }

    void StopTransmission()
    {
        if (radioSFXSource != null && disconnectClip != null)
        {
            radioSFXSource.PlayOneShot(disconnectClip);
        }

        VivoxService.Instance.MuteInputDevice();
        isTransmitting = false;
        Debug.Log("Telsiz KAPALI");
    }

    void PlayBusySound()
    {
        if (radioSFXSource != null && busyErrorClip != null)
        {
            if (!radioSFXSource.isPlaying)
                radioSFXSource.PlayOneShot(busyErrorClip, 0.5f);
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