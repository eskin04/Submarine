using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

public class PowerRoutingEngineerVisuals : MonoBehaviour
{
    [Header("Network Reference")]
    [SerializeField] private PowerRoutingNetworkManager _networkManager;

    [Header("3D Panel Text Meshes")]
    [SerializeField] private TextMeshPro[] _engDigitTexts;

    [Header("Light Controllers")]
    [SerializeField] private PowerRoutingLightController[] _engineerLights;

    private int[] _currentEngDigits;
    private LightColor[] _currentLightSequence;

    private bool[] _isDigitRevealed = new bool[4];
    private Coroutine _lightLoopCoroutine;

    private void Awake()
    {
        foreach (var light in _engineerLights)
        {
            if (light != null) light.TurnOff();
        }

        foreach (var text in _engDigitTexts)
        {
            if (text != null) text.text = "";
        }
    }

    private void OnEnable()
    {
        _networkManager.OnPuzzleStarted += HandlePuzzleStarted;

        _networkManager.OnPuzzleSolved += StopLights;
        _networkManager.OnPuzzleFailed += StopLights;
    }

    private void OnDisable()
    {
        _networkManager.OnPuzzleStarted -= HandlePuzzleStarted;
        _networkManager.OnPuzzleSolved -= StopLights;
        _networkManager.OnPuzzleFailed -= StopLights;
    }

    private void HandlePuzzleStarted(int[] techDigits, int[] engDigits, LightColor[] lightSequence)
    {
        _currentEngDigits = engDigits;
        _currentLightSequence = lightSequence;

        StopLights();

        for (int i = 0; i < 4; i++)
        {
            _engDigitTexts[i].text = "";
            _engineerLights[i].TurnOff();
            _isDigitRevealed[i] = false;
        }
    }

    public void TriggerLightSequence()
    {
        if (_networkManager.CurrentState.value != 1 || _lightLoopCoroutine != null) return;

        _lightLoopCoroutine = StartCoroutine(LightSequenceRoutine());
    }

    private void StopLights()
    {
        if (_lightLoopCoroutine != null)
        {
            StopCoroutine(_lightLoopCoroutine);
            _lightLoopCoroutine = null;
        }

        foreach (var light in _engineerLights)
        {
            if (light != null) light.TurnOff();
        }
    }

    private IEnumerator LightSequenceRoutine()
    {
        while (true)
        {
            for (int i = 0; i < 4; i++)
            {
                _engineerLights[i].TurnOn(_currentLightSequence[i]);

                // FMOD: RuntimeManager.PlayOneShot("event:/Station/Light_TurnOn", _engineerLights[i].transform.position);

                if (!_isDigitRevealed[i])
                {
                    _isDigitRevealed[i] = true;
                    _engDigitTexts[i].text = _currentEngDigits[i].ToString();
                    _engDigitTexts[i].transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 1f);
                }

                yield return new WaitForSeconds(1.0f);

                _engineerLights[i].TurnOff();
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
}