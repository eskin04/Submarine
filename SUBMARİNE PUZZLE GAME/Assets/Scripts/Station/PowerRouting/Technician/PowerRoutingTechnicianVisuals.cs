using UnityEngine;
using TMPro;

public class PowerRoutingTechnicianVisuals : MonoBehaviour
{
    [Header("Network Reference")]
    [SerializeField] private PowerRoutingNetworkManager _networkManager;

    [Header("3D Panel Text Meshes")]
    [SerializeField] private TextMeshPro[] _techDigitTexts;

    [Header("Switch Controllers")]
    [SerializeField] private PowerRoutingSwitchClick[] _smallSwitches;
    [SerializeField] private PowerRoutingSwitchClick[] _redSwitches;
    [SerializeField] private PowerRoutingSwitchClick[] _purpleSwitches;
    [SerializeField] private PowerRoutingSwitchClick[] _yellowSwitches;
    [SerializeField] private PowerRoutingSwitchClick[] _greenSwitches;
    [SerializeField] private PowerRoutingSubmitLever _submitLever;

    private void OnEnable()
    {
        _networkManager.OnPuzzleStarted += HandlePuzzleStarted;
        _networkManager.OnPuzzleFailed += HandlePuzzleFailed;
    }

    private void OnDisable()
    {
        _networkManager.OnPuzzleStarted -= HandlePuzzleStarted;
        _networkManager.OnPuzzleFailed -= HandlePuzzleFailed;
    }

    public bool IsStationActive()
    {
        return _networkManager.CurrentState.value == 1;
    }

    private void HandlePuzzleStarted(int[] techDigits, int[] engDigits, LightColor[] lightSequence)
    {
        for (int i = 0; i < 4; i++)
        {
            _techDigitTexts[i].text = techDigits[i].ToString();
        }

        ResetAllSwitches(_smallSwitches);
        ResetAllSwitches(_redSwitches);
        ResetAllSwitches(_purpleSwitches);
        ResetAllSwitches(_yellowSwitches);
        ResetAllSwitches(_greenSwitches);

        if (_submitLever != null) _submitLever.ResetLever();
    }

    private void HandlePuzzleFailed()
    {
        BounceAllSwitches(_smallSwitches);
        BounceAllSwitches(_redSwitches);
        BounceAllSwitches(_purpleSwitches);
        BounceAllSwitches(_yellowSwitches);
        BounceAllSwitches(_greenSwitches);
    }

    // ==========================================
    private void ResetAllSwitches(PowerRoutingSwitchClick[] switches)
    {
        foreach (var sw in switches) sw.ResetToUp();
    }

    private void BounceAllSwitches(PowerRoutingSwitchClick[] switches)
    {
        foreach (var sw in switches) sw.BounceUp();
    }

    // ==========================================
    public void NotifySmallSwitchChanged(int index, SwitchState state)
    {
        _networkManager.UpdateSmallSwitchServerRpc(index, state);
    }

    public void NotifyColorSwitchChanged(LightColor color, int index, bool isDown)
    {
        _networkManager.UpdateColorSwitchServerRpc(color, isDown);
    }

    public void NotifySubmitLeverPulled()
    {
        _networkManager.SubmitSolutionServerRpc();
    }
}