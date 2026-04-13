using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class FMODEmitter : MonoBehaviour
{
    private EventInstance _instance;
    private System.Action<FMODEmitter> _onPlaybackFinished;

    public void Play(EventReference eventRef, System.Action<FMODEmitter> onFinished)
    {
        _onPlaybackFinished = onFinished;
        _instance = RuntimeManager.CreateInstance(eventRef);

        RuntimeManager.AttachInstanceToGameObject(_instance, gameObject);

        _instance.start();
    }

    public void StopSound()
    {
        _instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        _instance.release();

        _onPlaybackFinished?.Invoke(this);
    }

    public void SetParameter(string name, float value)
    {
        _instance.setParameterByName(name, value);
    }
}