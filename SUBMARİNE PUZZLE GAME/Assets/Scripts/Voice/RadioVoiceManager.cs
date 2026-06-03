using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using System.Threading.Tasks;
using FMODUnity;

public class RadioVoiceManager : MonoBehaviour
{
    public static RadioVoiceManager Instance { get; private set; }
    public event System.Action<bool> OnRadioStateChanged;

    public KeyCode pushToTalkKey = KeyCode.Q;
    public AudioEventChannelSO _channel;
    public EventReference connectEvent;
    public EventReference disconnectEvent;
    private bool isLoggedIn = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

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

            JoinRadioChannel(channelName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Vivox Hatası: " + e.Message);
        }
    }

    private async void JoinRadioChannel(string channelName)
    {
        VivoxService.Instance.MuteInputDevice();

        await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);
        isLoggedIn = VivoxService.Instance.IsLoggedIn;
        Debug.Log($"<color=green>[Vivox]</color> Aktif Kanal Sayısı: {VivoxService.Instance.ActiveChannels.Count}");
    }

    void Update()
    {
        if (isLoggedIn)
        {
            if (Input.GetKeyDown(pushToTalkKey))
            {
                StartTransmission();
            }

            if (Input.GetKeyUp(pushToTalkKey))
            {
                StopTransmission();
            }
        }
    }

    void StartTransmission()
    {
        if (!connectEvent.IsNull)
        {
            PlaySound(connectEvent);
        }

        VivoxService.Instance.UnmuteInputDevice();
        OnRadioStateChanged?.Invoke(true);
        Debug.Log($"<color=yellow>[Vivox]</color> Telsiz Tuşuna Basıldı! Mikrofon Sessizde mi?: {VivoxService.Instance.IsInputDeviceMuted}");
    }

    void StopTransmission()
    {
        if (!disconnectEvent.IsNull)
        {
            PlaySound(disconnectEvent);
        }

        VivoxService.Instance.MuteInputDevice();
        OnRadioStateChanged?.Invoke(false);
    }

    private void PlaySound(EventReference sound)
    {
        if (_channel != null && !sound.IsNull)
        {
            AudioEventPayload payload = new AudioEventPayload(sound, this.transform.position);
            _channel.RaiseEvent(payload);
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