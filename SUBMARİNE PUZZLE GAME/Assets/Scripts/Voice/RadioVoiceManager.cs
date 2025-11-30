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
            options.DisplayName = "Operator_" + Random.Range(100, 999); // Rastgele isim

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


        Channel3DProperties properties = new Channel3DProperties();

        VivoxService.Instance.MuteInputDevice();

        await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);

        Debug.Log($"Telsiz Kanalına Katılındı: {channelName}");
    }

    void Update()
    {
        if (VivoxService.Instance.IsLoggedIn)
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

    void OnApplicationQuit()
    {
        if (VivoxService.Instance.IsLoggedIn)
        {

            VivoxService.Instance.LogoutAsync();
        }

    }

    void StartTransmission()
    {
        if (radioSFXSource != null && connectClip != null)
        {
            radioSFXSource.PlayOneShot(connectClip);
        }

        VivoxService.Instance.UnmuteInputDevice();
        Debug.Log("Telsiz AÇIK (Konuşuluyor...)");
    }

    void StopTransmission()
    {
        if (radioSFXSource != null && disconnectClip != null)
        {
            radioSFXSource.PlayOneShot(disconnectClip);
        }

        VivoxService.Instance.MuteInputDevice();
        Debug.Log("Telsiz KAPALI");
    }
}