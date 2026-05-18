using UnityEngine;
using System.Collections.Generic;
using PurrNet;
using System.Linq;
using System.Collections;
using System;



public class Inversion_Relay_StationManager : NetworkBehaviour
{
    [Header("Backend Data (Server Only)")]
    public InversionPhase currentPhase;
    public PipeLetter[] pipeAssignments = new PipeLetter[5];
    public EngineerRule[] displayedRules = new EngineerRule[5];

    [Header("Live State")]
    public SyncVar<bool> isRoundActive = new SyncVar<bool>(false);
    public ValveState[] currentValveStates = new ValveState[5] { ValveState.Neutral, ValveState.Neutral, ValveState.Neutral, ValveState.Neutral, ValveState.Neutral };
    public SyncVar<bool> isTesting = new SyncVar<bool>(false);
    [Range(0, 5)] public int corruptedDataCount = 2;
    public float testDurationPerPipe = 3.0f;

    [Header("References")]
    public StationController stationController;
    public Inversion_EngineerModule engineerModule;
    public Inversion_TechnicianModule technicianModule;
    public Inversion_ValidationSwitch validationSwitch;
    public Inversion_TestButton testButton;

    private Coroutine activeTesterRoutine;

    public void StartNewRound()
    {
        if (!isServer) return;
        GeneratePuzzle();
    }

    private void GeneratePuzzle()
    {
        currentPhase = UnityEngine.Random.value > 0.5f ? InversionPhase.Normal : InversionPhase.Inverted;

        List<PipeLetter> availableLetters = new List<PipeLetter> { PipeLetter.A, PipeLetter.B, PipeLetter.D, PipeLetter.E };
        Shuffle(availableLetters);

        pipeAssignments[0] = availableLetters[0];
        pipeAssignments[1] = availableLetters[1];
        pipeAssignments[2] = PipeLetter.C;
        pipeAssignments[3] = availableLetters[2];
        pipeAssignments[4] = availableLetters[3];

        List<EngineerRule> tempRules = new List<EngineerRule>();
        Array valveStates = Enum.GetValues(typeof(ValveState));

        foreach (PipeLetter letter in Enum.GetValues(typeof(PipeLetter)))
        {
            ValveState randomTarget = (ValveState)valveStates.GetValue(UnityEngine.Random.Range(0, valveStates.Length));
            tempRules.Add(new EngineerRule { Letter = letter, TargetState = randomTarget, IsCorrupted = false });
        }

        Shuffle(tempRules);

        List<int> ruleIndices = new List<int> { 0, 1, 2, 3, 4 };
        Shuffle(ruleIndices);

        int safeCorruptedCount = Mathf.Clamp(corruptedDataCount, 0, 5);

        for (int i = 0; i < safeCorruptedCount; i++)
        {
            EngineerRule currentRule = tempRules[ruleIndices[i]];
            currentRule.IsCorrupted = true;
            tempRules[ruleIndices[i]] = currentRule;
        }

        displayedRules = tempRules.ToArray();

        for (int i = 0; i < 5; i++) currentValveStates[i] = ValveState.Neutral;

        isRoundActive.value = true;
        isTesting.value = false;
        PipeLetter[] rLetters = new PipeLetter[5];
        ValveState[] rStates = new ValveState[5];
        bool[] rCorrupts = new bool[5];

        for (int i = 0; i < 5; i++)
        {
            rLetters[i] = displayedRules[i].Letter;
            rStates[i] = displayedRules[i].TargetState;
            rCorrupts[i] = displayedRules[i].IsCorrupted;
        }

        RpcSyncPuzzle(pipeAssignments, rLetters, rStates, rCorrupts);
    }

    [ObserversRpc]
    private void RpcSyncPuzzle(PipeLetter[] syncedPipes, PipeLetter[] rLetters, ValveState[] rStates, bool[] rCorrupts)
    {
        EngineerRule[] syncedRules = new EngineerRule[5];
        for (int i = 0; i < 5; i++)
        {
            syncedRules[i] = new EngineerRule
            {
                Letter = rLetters[i],
                TargetState = rStates[i],
                IsCorrupted = rCorrupts[i]
            };
        }
        if (technicianModule != null)
        {
            technicianModule.SetupPipes(syncedPipes);
        }

        if (engineerModule != null) engineerModule.SetupRules(syncedRules);

        Debug.Log("İstasyon verileri istemcilere ulaştı ve kurallar ağ üzerinden başarıyla oluşturuldu.");
    }


    [ServerRpc(requireOwnership: false)]
    public void TechnicianChangeValveRPC(int pipeIndex, ValveState newState)
    {
        if (!isRoundActive.value || isTesting.value) return;
        if (pipeIndex < 0 || pipeIndex > 4) return;

        currentValveStates[pipeIndex] = newState;

    }




    [ServerRpc(requireOwnership: false)]
    public void PressTestButtonRPC()
    {
        if (!isRoundActive.value || isTesting.value) return;

        isTesting.value = true;
        if (activeTesterRoutine != null) StopCoroutine(activeTesterRoutine);

        activeTesterRoutine = StartCoroutine(TesterProcessRoutine());
    }

    private IEnumerator TesterProcessRoutine()
    {
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(testDurationPerPipe);

            bool isCorrect = CheckPipeIndex(i);

            if (isCorrect)
            {
                RpcUpdateTestLight(i, true);
            }
            else
            {
                RpcUpdateTestLight(i, false);
                break;
            }
        }

        isTesting.value = false;
        activeTesterRoutine = null;
        RpcTestComplete();
    }

    [ObserversRpc]
    private void RpcTestComplete()
    {
        if (testButton != null)
        {
            testButton.OnTestComplete();
        }
        technicianModule.ResetPipeLights();
    }

    [ObserversRpc]
    private void RpcUpdateTestLight(int pipeIndex, bool isSuccess)
    {
        if (technicianModule != null)
        {
            technicianModule.UpdatePipeLight(pipeIndex, isSuccess);
        }

    }


    [ServerRpc(requireOwnership: false)]
    public void PullSwitchRPC()
    {
        if (!isRoundActive.value || isTesting.value) return;

        CheckSequenceSolution();
    }

    private void CheckSequenceSolution()
    {
        if (!isServer) return;

        bool isCorrectSequence = ValidateEntireStation();

        if (isCorrectSequence)
        {
            RpcStationResolved(true);
            isRoundActive.value = false;
            stationController.SetReparied();
        }
        else
        {
            RpcStationResolved(false);
            stationController.ReportRepairMistake();
        }
    }


    [ObserversRpc]
    private void RpcStationResolved(bool isSuccess)
    {
        if (isSuccess)
        {
            Debug.Log("<color=green>BAŞARILI! INVERSION RELAY ÇÖZÜLDÜ.</color>");
        }
        else
        {
            Debug.Log("<color=red>HATA! YANLIŞ VANA KONUMLARI. ŞALTER GERİ ATTI!</color>");
            if (validationSwitch != null)
            {
                validationSwitch.SnapBack();
            }
        }
    }


    private bool ValidateEntireStation()
    {
        for (int i = 0; i < 5; i++)
        {
            if (!CheckPipeIndex(i)) return false;
        }
        return true;
    }

    private bool CheckPipeIndex(int pipeIndex)
    {
        int checkIndex = pipeIndex;

        if (currentPhase == InversionPhase.Inverted)
        {
            checkIndex = 4 - pipeIndex;
        }

        PipeLetter letterAtPipe = pipeAssignments[checkIndex];
        ValveState expectedState = displayedRules.First(r => r.Letter == letterAtPipe).TargetState;

        if (currentPhase == InversionPhase.Inverted)
        {
            if (expectedState == ValveState.Fill) expectedState = ValveState.Empty;
            else if (expectedState == ValveState.Empty) expectedState = ValveState.Fill;
        }

        return currentValveStates[pipeIndex] == expectedState;
    }

    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1) { n--; int k = UnityEngine.Random.Range(0, n + 1); T value = list[k]; list[k] = list[n]; list[n] = value; }
    }

    [ContextMenu("Test 1: Oyunu Başlat (Generate)")]
    public void Test_StartPuzzle()
    {
        StartNewRound();
    }


}