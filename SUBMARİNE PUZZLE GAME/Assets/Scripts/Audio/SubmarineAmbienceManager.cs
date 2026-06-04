using UnityEngine;
using FMODUnity;

public class SubmarineAmbienceManager : MonoBehaviour
{
    [Header("Audio Settings")]
    public EventReference submarineAmbience;

    private FMODEmitter _ambienceEmitter;
    private void Start()
    {
        PlayAmbience();
    }

    private void PlayAmbience()
    {
        if (!submarineAmbience.IsNull)
        {
            _ambienceEmitter = AudioManager.Instance.PlayLoopingOrAttachedSound(submarineAmbience, this.transform);
        }
        else
        {
            Debug.LogWarning("[AmbienceManager] Ambiyans sesi atanmamış!");
        }
    }

    private void OnDestroy()
    {
        if (_ambienceEmitter != null)
        {
            _ambienceEmitter.StopSound();
            _ambienceEmitter = null;
        }
    }
}