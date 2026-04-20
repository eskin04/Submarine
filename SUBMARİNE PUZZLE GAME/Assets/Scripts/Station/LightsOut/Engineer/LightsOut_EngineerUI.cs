using UnityEngine;
using System.Collections.Generic;
using PurrNet;
using DG.Tweening;
using FMODUnity;

public class LightsOut_EngineerUI : NetworkBehaviour
{
    [Header("References")]
    public List<LightsOut_StatusLight> statusLights;
    [Header("Meshes")]
    public Transform generatorMesh;
    public float rotationDuration = 1f;
    public Vector3 rotationAmount = new Vector3(0, 0, 360);

    [Header("Audio Settings")]
    public EventReference generatorLoopSound;
    private FMODEmitter _activeGeneratorEmitter;

    private Tween rotationTween;

    private void Start()
    {
        if (generatorMesh != null)
        {
            rotationTween = generatorMesh.DORotate(rotationAmount, rotationDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }
    }

    private void OnEnable()
    {
        LightsOut_StationManager.OnPowerStatusChanged += HandlePowerStatus;
        MainGameState.startGame += ResumeGeneratorSound;
    }

    protected override void OnDestroy()
    {
        LightsOut_StationManager.OnPowerStatusChanged -= HandlePowerStatus;
        MainGameState.startGame -= ResumeGeneratorSound;
        if (rotationTween != null) rotationTween.Kill();
        if (_activeGeneratorEmitter != null)
        {
            _activeGeneratorEmitter.StopSound();
            _activeGeneratorEmitter = null;
        }
    }

    private void HandlePowerStatus(bool isPowered)
    {
        if (rotationTween == null) return;

        if (isPowered)
        {
            rotationTween.Play();
            ResumeGeneratorSound();
        }
        else
        {
            rotationTween.Pause();
            PauseGeneratorSound();
        }
    }

    private void ResumeGeneratorSound()
    {
        if (generatorLoopSound.IsNull) return;

        if (_activeGeneratorEmitter != null)
        {
            _activeGeneratorEmitter.SetPaused(false);
        }
        else
        {
            Transform targetTransform = generatorMesh != null ? generatorMesh : this.transform;
            _activeGeneratorEmitter = AudioManager.Instance.PlayLoopingOrAttachedSound(generatorLoopSound, targetTransform);
        }
    }

    private void PauseGeneratorSound()
    {
        if (_activeGeneratorEmitter != null)
        {
            _activeGeneratorEmitter.SetPaused(true);
        }
    }

    public void UpdateLights(List<StatusLightState> states)
    {
        for (int i = 0; i < statusLights.Count; i++)
        {
            if (i < states.Count)
            {
                statusLights[i].SetState(states[i]);
            }
        }
    }
}