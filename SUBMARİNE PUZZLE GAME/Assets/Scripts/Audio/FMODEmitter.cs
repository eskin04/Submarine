using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class FMODEmitter : MonoBehaviour
{
    private EventInstance _instance;
    private System.Action<FMODEmitter> _onPlaybackFinished;

    /// <summary>
    /// Sesi başlatır ve havuz yöneticisine "işim bitince sana haber vereceğim" der.
    /// </summary>
    public void Play(EventReference eventRef, System.Action<FMODEmitter> onFinished)
    {
        _onPlaybackFinished = onFinished;
        _instance = RuntimeManager.CreateInstance(eventRef);

        // KRİTİK: Sesin bu GameObject'i takip etmesini sağlar (Hareket eden objeler için)
        RuntimeManager.AttachInstanceToGameObject(_instance, gameObject);

        _instance.start();
    }

    /// <summary>
    /// PurrNet veya lokal bir tetikleyici sesi durdurmak istediğinde çağrılır
    /// </summary>
    public void StopSound()
    {
        // Sesi yavaşça kısarak (Fade-out) durdur
        _instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        _instance.release();

        // İşimiz bitti, havuza geri dönebiliriz mesajını gönder
        _onPlaybackFinished?.Invoke(this);
    }

    /// <summary>
    /// Sürekli çalan sese anlık parametre göndermek için (Örn: Su basıncı artıyor)
    /// </summary>
    public void SetParameter(string name, float value)
    {
        _instance.setParameterByName(name, value);
    }
}