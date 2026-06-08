using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using System.Threading.Tasks;
using FMODUnity;
using UnityEngine.SceneManagement;

public class RadioVoiceManager : MonoBehaviour
{
    public static RadioVoiceManager Instance { get; private set; }
    public event System.Action<bool> OnRadioStateChanged;
    public AudioSource vivoxAudioSource { get; private set; }
    public event System.Action<bool> OnReceivingTransmission;

    [Header("Malfunction Settings")]
    public bool isRadioBroken = false;
    public EventReference brokenStaticEvent;
    private FMODEmitter _activeStaticEmitter;

    public event System.Action<bool> OnRadioBrokenStateChanged;

    private bool wasAnyoneElseTalking = false;
    public KeyCode pushToTalkKey = KeyCode.Q;
    public AudioEventChannelSO _channel;
    public EventReference connectEvent;
    public EventReference disconnectEvent;
    private bool isLoggedIn = false;
    private bool isConnectingToVivox = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        vivoxAudioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        InvokeRepeating(nameof(MonitorIncomingTransmissions), 1f, 0.05f);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_activeStaticEmitter != null)
        {
            _activeStaticEmitter.StopSound();
            _activeStaticEmitter = null;
        }

    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        if (isRadioBroken)
        {
            Debug.Log($"<color=cyan>[Telsiz]</color> Yeni sahne yüklendi ({scene.name}). Telsiz durumu sıfırlanıyor...");
            SetRadioBrokenState(false);
        }
    }

    private void MonitorIncomingTransmissions()
    {
        if (!isLoggedIn) return;

        bool isTalkingNow = IsAnyoneElseTalking();

        if (isTalkingNow != wasAnyoneElseTalking)
        {
            wasAnyoneElseTalking = isTalkingNow;
            OnReceivingTransmission?.Invoke(isTalkingNow);
        }
    }
    public void SetRadioBrokenState(bool isBroken)
    {
        if (isRadioBroken == isBroken) return;

        isRadioBroken = isBroken;
        OnRadioBrokenStateChanged?.Invoke(isBroken);

        if (isRadioBroken)
        {
            if (vivoxAudioSource != null) vivoxAudioSource.volume = 0f;

            VivoxService.Instance.MuteInputDevice();
            if (_activeStaticEmitter == null && !brokenStaticEvent.IsNull)
            {
                _activeStaticEmitter = AudioManager.Instance.PlayLoopingOrAttachedSound(brokenStaticEvent, this.transform);
                _activeStaticEmitter.SetPaused(true);
            }

            OnRadioStateChanged?.Invoke(false);

            Debug.Log("<color=red>[Telsiz]</color> Telsiz BOZULDU! İletişim koptu.");
        }
        else
        {
            if (vivoxAudioSource != null) vivoxAudioSource.volume = 1f;

            if (_activeStaticEmitter != null)
            {
                _activeStaticEmitter.StopSound();
                _activeStaticEmitter = null;
            }

            Debug.Log("<color=green>[Telsiz]</color> Telsiz TAMİR EDİLDİ! İletişim sağlandı.");
        }
    }

    public async void StartLobbyVoice(string channelName)
    {
        if (isConnectingToVivox)
        {
            return;
        }

        try
        {
            isConnectingToVivox = true;

            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                try
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                catch (Unity.Services.Authentication.AuthenticationException e)
                {
                    if (e.Message.Contains("already signing in"))
                    {
                        Debug.LogWarning("<color=orange>[Telsiz]</color> PurrNet zaten giriş yapıyor, telsiz bağlantısı için bekleniyor...");

                        while (!AuthenticationService.Instance.IsSignedIn)
                        {
                            await Task.Delay(100);
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            if (VivoxService.Instance.IsLoggedIn)
            {
                await JoinRadioChannel(channelName);
            }
            else
            {
                await InitializeVivoxAsync(channelName);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Vivox Başlatma Hatası: " + e.Message);
        }
        finally
        {
            isConnectingToVivox = false;
        }
    }

    private async Task InitializeVivoxAsync(string channelName)
    {
        try
        {
            await VivoxService.Instance.InitializeAsync();
        }
        catch { }

        try
        {
            LoginOptions options = new LoginOptions();
            options.DisplayName = "Operator_" + Random.Range(100, 999);
            await VivoxService.Instance.LoginAsync(options);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("<color=orange>[Vivox]</color> Login Uyarısı (Göz ardı edilebilir): " + e.Message);
        }
        await JoinRadioChannel(channelName);
    }

    private async Task JoinRadioChannel(string channelName)
    {
        if (VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
        {
            Debug.Log($"<color=cyan>[Telsiz]</color> Zaten {channelName} frekansına bağlıyız.");
            isLoggedIn = true;
            return;
        }

        try
        {
            VivoxService.Instance.MuteInputDevice();
            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);

            isLoggedIn = VivoxService.Instance.IsLoggedIn;
            Debug.Log($"<color=green>[Vivox]</color> {channelName} frekansına başarıyla bağlanıldı.");
        }
        catch (System.Exception e)
        {
            if (e.Message.Contains("already an active channel") || e.Message.Contains("already"))
            {
                Debug.LogWarning($"<color=orange>[Vivox]</color> Kanal kıl payı çakışması önlendi. Zaten {channelName} kanalındayız.");
                isLoggedIn = true;
            }
            else
            {
                Debug.LogError("Vivox Kanal Hatası: " + e.Message);
            }
        }
    }
    public async void LeaveVoiceChannel()
    {
        if (isLoggedIn)
        {
            await VivoxService.Instance.LogoutAsync();
            isLoggedIn = false;
            OnRadioStateChanged?.Invoke(false);
            Debug.Log("<color=red>[Vivox]</color> Lobiden ayrılınca ses bağlantısı kesildi.");
        }
    }

    public bool IsAnyoneElseTalking()
    {
        if (VivoxService.Instance == null || !VivoxService.Instance.IsLoggedIn) return false;

        foreach (var channelParticipantsList in VivoxService.Instance.ActiveChannels.Values)
        {
            foreach (var participant in channelParticipantsList)
            {
                if (!participant.IsSelf && participant.SpeechDetected)
                {
                    return true;
                }
            }
        }

        return false;
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
        OnRadioStateChanged?.Invoke(true);
        if (isRadioBroken)
        {
            if (_activeStaticEmitter != null)
            {
                _activeStaticEmitter.SetPaused(false);
            }
            Debug.Log("<color=orange>[Telsiz]</color> Telsiz bozuk! Mandala basıldı ama sadece cızırtı duyuluyor.");
            return;
        }
        if (!connectEvent.IsNull)
        {
            PlaySound(connectEvent);
        }

        VivoxService.Instance.UnmuteInputDevice();

        if (vivoxAudioSource != null)
        {
            vivoxAudioSource.mute = true;
        }
        Debug.Log($"<color=yellow>[Vivox]</color> Telsiz Tuşuna Basıldı! Mikrofon Sessizde mi?: {VivoxService.Instance.IsInputDeviceMuted}");
    }

    void StopTransmission()
    {
        OnRadioStateChanged?.Invoke(false);

        if (isRadioBroken)
        {
            if (_activeStaticEmitter != null)
            {
                _activeStaticEmitter.SetPaused(true);
            }
            return;
        }
        if (!disconnectEvent.IsNull)
        {
            PlaySound(disconnectEvent);
        }

        VivoxService.Instance.MuteInputDevice();

        if (vivoxAudioSource != null)
        {
            vivoxAudioSource.mute = false;
        }
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

    [ContextMenu("Force broken (for Testing)")]
    public void ForceBrokenState()
    {
        SetRadioBrokenState(true);
    }

    [ContextMenu("Force fixed (for Testing)")]
    public void ForceFixedState()
    {
        SetRadioBrokenState(false);
    }
}