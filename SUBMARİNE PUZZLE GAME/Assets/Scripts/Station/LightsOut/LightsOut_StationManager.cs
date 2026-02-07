using UnityEngine;
using System.Collections.Generic;
using PurrNet;
using System.Linq;

public class LightsOut_StationManager : NetworkBehaviour
{
    public static event System.Action<bool> OnPowerStatusChanged;
    [Header("Game State")]
    public List<CableData> allCables = new List<CableData>();
    public List<SwitchData> allSwitches = new List<SwitchData>();

    public List<WireColor> correctSolutionSequence = new List<WireColor>();
    public List<WireColor> playerInputSequence = new List<WireColor>();

    [Header("Settings")]
    public bool isRoundActive = false;
    public List<LightsOut_Switch> switchButtons = new List<LightsOut_Switch>();

    [Header("References")]
    public LightsOut_TechnicianUI technicianUI;
    public LightsOut_EngineerUI engineerUI;
    public LightsOut_Lever lever;
    public StationController stationController;

    private bool isCorrectSequence = false;


    public void StartNewRound()
    {
        if (!isServer) return;

        GeneratePuzzle();

    }



    private void GeneratePuzzle()
    {
        allCables.Clear();
        allSwitches.Clear();
        correctSolutionSequence.Clear();

        // Renk havuzu
        List<WireColor> colors = new List<WireColor>()
        {
            WireColor.Yellow, WireColor.Green, WireColor.Blue, WireColor.Red
        };

        List<int> availablePorts = new List<int>() { 0, 1, 2, 3 };
        List<WireColor> randomOutputs = new List<WireColor>(colors);
        Shuffle(randomOutputs);

        List<int> solutionPorts = new List<int>(availablePorts);
        Shuffle(solutionPorts);

        for (int i = 0; i < 4; i++)
        {
            CableData cable = new CableData();
            cable.cableID = i;
            cable.physicalColor = colors[i];

            cable.outputLightColor = randomOutputs[i];

            cable.correctPortIndex = solutionPorts[i];

            cable.currentPortIndex = -1;

            allCables.Add(cable);
        }

        List<WireColor> switchLabels = new List<WireColor>(colors);
        Shuffle(switchLabels);

        for (int i = 0; i < 4; i++)
        {
            SwitchData sw = new SwitchData();
            sw.switchIndex = i;
            sw.labelColor = switchLabels[i];
            sw.isOn = false;
            allSwitches.Add(sw);
        }


        for (int portIndex = 0; portIndex < 4; portIndex++)
        {
            CableData correctCable = allCables.Find(c => c.correctPortIndex == portIndex);

            correctSolutionSequence.Add(correctCable.outputLightColor);
        }

        isRoundActive = true;
        RpcSyncPuzzle(allCables);
        CalculateAndSyncEngineerLights();
        RpcSyncSwitches(allSwitches);
        RpcSetGlobalLights(false);

    }

    [ObserversRpc]
    private void RpcSetGlobalLights(bool lightsOn)
    {

        OnPowerStatusChanged?.Invoke(lightsOn);


    }

    [ObserversRpc]
    private void RpcSyncPuzzle(List<CableData> cableData)
    {

        if (technicianUI != null)
        {
            technicianUI.HandlePuzzleSync(cableData);
        }
    }




    [ServerRpc(requireOwnership: false)]
    public void ConnectCableRPC(int cableID, int targetPortIndex)
    {
        if (!isRoundActive) return;

        if (targetPortIndex < 0 || targetPortIndex > 3) return;

        CableData incomingCable = allCables.Find(c => c.cableID == cableID);
        if (incomingCable != null)
        {
            CableData existingCable = allCables.Find(c => c.currentPortIndex == targetPortIndex);

            if (existingCable != null && existingCable != incomingCable)
            {
                existingCable.currentPortIndex = -1;
                RpcUpdateCableState(existingCable.cableID, -1);
            }

            incomingCable.currentPortIndex = targetPortIndex;

            RpcUpdateCableState(cableID, targetPortIndex);
            CalculateAndSyncEngineerLights();
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void UnplugCableRPC(int cableID)
    {
        if (!isRoundActive) return;

        CableData cable = allCables.Find(c => c.cableID == cableID);
        if (cable != null)
        {
            if (cable.currentPortIndex != -1)
            {
                cable.currentPortIndex = -1;

                RpcUpdateCableState(cableID, -1);
                CalculateAndSyncEngineerLights();
            }
        }
    }

    [ObserversRpc]
    private void RpcUpdateCableState(int cableID, int portIndex)
    {
        if (technicianUI != null)
        {
            technicianUI.UpdateVisuals(cableID, portIndex);
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }


    private void CalculateAndSyncEngineerLights()
    {
        int totalPluggedCables = 0;
        int correctConnectionCount = 0;

        foreach (var cable in allCables)
        {
            if (cable.currentPortIndex != -1)
            {
                totalPluggedCables++;

                if (cable.correctPortIndex == cable.currentPortIndex)
                {
                    correctConnectionCount++;
                }
            }
        }

        List<StatusLightState> currentStates = new List<StatusLightState>();

        if (totalPluggedCables < 4)
        {
            for (int i = 0; i < 4; i++)
            {
                currentStates.Add(StatusLightState.Yellow);
            }
        }
        else
        {
            for (int lightIndex = 0; lightIndex < 4; lightIndex++)
            {
                if (lightIndex < correctConnectionCount)
                {
                    currentStates.Add(StatusLightState.Green);
                }
                else
                {
                    currentStates.Add(StatusLightState.Red);
                }
            }
        }

        RpcSyncEngineerLights(currentStates);
    }

    [ObserversRpc]
    private void RpcSyncEngineerLights(List<StatusLightState> states)
    {
        if (engineerUI != null)
        {
            engineerUI.UpdateLights(states);
        }
    }


    [ServerRpc(requireOwnership: false)]
    public void RegisterSwitchPressRPC(WireColor pressedColor)
    {
        playerInputSequence.Add(pressedColor);
    }

    [ServerRpc(requireOwnership: false)]
    public void PullLeverActionRPC()
    {
        CheckSequenceSolution();
    }

    private void CheckSequenceSolution()
    {
        if (!isServer) return;


        if (playerInputSequence.Count != 4)
        {
            ResetPuzzlePenalty();
            RpcResetLever();
            return;
        }


        isCorrectSequence = playerInputSequence.SequenceEqual(correctSolutionSequence);

        if (isCorrectSequence)
        {
            if (stationController != null)
            {
                stationController.SetReparied();
            }
            RpcSetGlobalLights(true);
        }
        else
        {
            ResetPuzzlePenalty();
            RpcResetLever();
        }
    }

    [ObserversRpc]
    private void RpcResetLever()
    {
        if (lever != null)
        {
            lever.ResetLever();
        }
    }

    private void ResetPuzzlePenalty()
    {
        playerInputSequence.Clear();

        GeneratePuzzle();

    }



    [ObserversRpc]
    private void RpcSyncSwitches(List<SwitchData> switchData)
    {


        foreach (var data in switchData)
        {
            foreach (var btn in switchButtons)
            {
                if (btn.buttonID == data.switchIndex)
                {
                    btn.Setup(data.labelColor);
                }
            }
        }
    }




    [ContextMenu("TEST: Start Round (Generate Puzzle)")]
    public void Test_StartPuzzle()
    {
        GeneratePuzzle();
    }


}