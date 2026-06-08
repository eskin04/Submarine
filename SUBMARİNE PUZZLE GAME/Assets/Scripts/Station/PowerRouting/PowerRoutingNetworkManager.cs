using PurrNet;
using UnityEngine;
using System.Collections.Generic;

public class PowerRoutingNetworkManager : NetworkBehaviour
{
    public StationController stationController;
    private PowerRoutingCore _coreLogic;
    private PowerRoutingPuzzleData _currentPuzzle;

    private SwitchState[] _currentSmallSwitches = new SwitchState[4];
    private Dictionary<LightColor, int> _currentColorSwitches = new Dictionary<LightColor, int>()
    {
        { LightColor.Red, 0 },
        { LightColor.Purple, 0 },
        { LightColor.Yellow, 0 },
        { LightColor.Green, 0 }
    };

    // İstasyon Durumu (0: Kapalı, 1: Aktif, 2: Çözüldü)
    public readonly SyncVar<ushort> CurrentState = new(0);

    public event System.Action<int[], int[], LightColor[]> OnPuzzleStarted;
    public event System.Action OnPuzzleSolved;
    public event System.Action OnPuzzleFailed;

    private void Awake()
    {
        _coreLogic = new PowerRoutingCore();
    }

    public void StartStation()
    {
        if (!isServer) return;
        if (CurrentState.value == 1) return;

        _currentPuzzle = _coreLogic.GeneratePuzzle();

        for (int i = 0; i < 4; i++) _currentSmallSwitches[i] = SwitchState.Up;

        _currentColorSwitches[LightColor.Red] = 0;
        _currentColorSwitches[LightColor.Purple] = 0;
        _currentColorSwitches[LightColor.Yellow] = 0;
        _currentColorSwitches[LightColor.Green] = 0;

        CurrentState.value = 1;

        StartStationObserversRpc(
            _currentPuzzle.TechDigits,
            _currentPuzzle.EngDigits,
            _currentPuzzle.LightSequence
        );
    }

    [ObserversRpc]
    private void StartStationObserversRpc(int[] techDigits, int[] engDigits, LightColor[] lightSequence)
    {
        OnPuzzleStarted?.Invoke(techDigits, engDigits, lightSequence);
    }

    [ServerRpc(requireOwnership: false)]
    public void UpdateSmallSwitchServerRpc(int switchIndex, SwitchState newState)
    {
        if (CurrentState.value != 1 || switchIndex < 0 || switchIndex >= 4) return;
        _currentSmallSwitches[switchIndex] = newState;
    }

    [ServerRpc(requireOwnership: false)]
    public void UpdateColorSwitchServerRpc(LightColor color, bool isAdded)
    {
        if (CurrentState.value != 1) return;

        if (isAdded) _currentColorSwitches[color]++;
        else _currentColorSwitches[color]--;

        if (_currentColorSwitches[color] < 0) _currentColorSwitches[color] = 0;
    }

    [ServerRpc(requireOwnership: false)]
    public void SubmitSolutionServerRpc()
    {
        if (CurrentState.value != 1) return;

        bool isCorrect = _coreLogic.ValidateSolution(
            _currentPuzzle,
            _currentSmallSwitches,
            _currentColorSwitches
        );

        if (isCorrect)
        {
            CurrentState.value = 2;
            PuzzleSolvedObserversRpc();
            stationController.SetReparied();
            Debug.Log("[PowerRouting] İstasyon Çözüldü!");
        }
        else
        {
            PuzzleFailedObserversRpc();
            stationController.ReportRepairMistake();
            Debug.LogWarning("[PowerRouting] İstasyon Hata Verdi! Şalterler veya miktarlar yanlış. İstasyon sıfırlanıyor...");

            StartCoroutine(RestartStationRoutine());
        }
    }

    private System.Collections.IEnumerator RestartStationRoutine()
    {
        CurrentState.value = 0;

        yield return new WaitForSeconds(1.5f);

        StartStation();
    }

    [ObserversRpc]
    private void PuzzleSolvedObserversRpc()
    {
        OnPuzzleSolved?.Invoke();
    }

    [ObserversRpc]
    private void PuzzleFailedObserversRpc()
    {
        OnPuzzleFailed?.Invoke();
    }

    // ==========================================

    [ContextMenu("Test 1: Start Station")]
    private void TestStartStation()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Testleri Play Modundayken çalıştırın!");
            return;
        }

        StartStation();

        Debug.Log("--- İSTASYON BULMACASI OLUŞTURULDU ---");
        Debug.Log($"Teknisyen Rakamları: {_currentPuzzle.TechDigits[0]} {_currentPuzzle.TechDigits[1]} {_currentPuzzle.TechDigits[2]} {_currentPuzzle.TechDigits[3]}");
        Debug.Log($"Mühendis Rakamları: {_currentPuzzle.EngDigits[0]} {_currentPuzzle.EngDigits[1]} {_currentPuzzle.EngDigits[2]} {_currentPuzzle.EngDigits[3]}");
        Debug.Log($"Işık Sırası: {_currentPuzzle.LightSequence[0]}, {_currentPuzzle.LightSequence[1]}, {_currentPuzzle.LightSequence[2]}, {_currentPuzzle.LightSequence[3]}");

        Debug.Log("BEKLENEN CEVAPLAR:");
        Debug.Log("Minik Şalterler: " + string.Join(", ", _currentPuzzle.ExpectedSmallSwitches));
        Debug.Log($"Renk Şalterleri (Miktar): Kırmızı:{_currentPuzzle.ExpectedColorSwitches[LightColor.Red]} Mor:{_currentPuzzle.ExpectedColorSwitches[LightColor.Purple]} Sarı:{_currentPuzzle.ExpectedColorSwitches[LightColor.Yellow]} Yeşil:{_currentPuzzle.ExpectedColorSwitches[LightColor.Green]}");
    }

}