using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using System.Threading.Tasks;
using FMODUnity;

public class RadioVoiceManager : MonoBehaviour
{
    public static RadioVoiceManager Instance { get; private set; }

    [Header("Telsiz Ayarları")]
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
        StartLobbyVoice();
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
    }

    void StopTransmission()
    {
        if (!disconnectEvent.IsNull)
        {
            PlaySound(disconnectEvent);
        }

        VivoxService.Instance.MuteInputDevice();
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