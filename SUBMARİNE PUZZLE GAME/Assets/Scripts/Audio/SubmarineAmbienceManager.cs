using UnityEngine;
using FMODUnity;
using PurrNet;

public class SubmarineAmbienceManager : NetworkBehaviour
{
    [Header("Audio Settings")]
    public EventReference submarineAmbience;

    private FMODEmitter _ambienceEmitter;

    void OnEnable()
    {
        MainGameState.startGame += PlayAmbience;
    }

    void OnDisable()
    {
        MainGameState.startGame -= PlayAmbience;
    }

    [ObserversRpc(runLocally: true)]
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

    protected override void OnDestroy()
    {
        if (_ambienceEmitter != null)
        {
            _ambienceEmitter.StopSound();
            _ambienceEmitter = null;
        }
    }
}