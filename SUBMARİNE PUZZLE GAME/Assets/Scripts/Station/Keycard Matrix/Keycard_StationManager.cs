using UnityEngine;
using System.Collections.Generic;
using PurrNet;
using System.Linq;
using System.Collections;

public class Keycard_StationManager : NetworkBehaviour
{
    [Header("Backend Data (Read Only)")]
    public List<CardData> allCards = new List<CardData>();
    public List<int> correctSolutionSequence = new List<int>();

    [Header("Live State")]
    public SyncVar<bool> isRoundActive = new SyncVar<bool>(false);
    public int[] technicianSockets = new int[4] { -1, -1, -1, -1 };
    public int engineerSocket = -1;
    public int[] testerSockets = new int[2] { -1, -1 };
    public bool isButtonCoverOpen = false;
    public float progressDuration = 2.0f;

    [Header("Dispensers")]
    public Keycard_Dispenser technicianDispenser;
    public Keycard_Dispenser engineerDispenser;

    [Header("References")]
    public Keycard_EngineerUI engineerUI;
    public StationController stationController;
    public Keycard_TesterModule testerModule;

    private Coroutine activeTesterRoutine;


    public void StartNewRound()
    {
        if (!isServer) return;
        GeneratePuzzle();
    }

    private void GeneratePuzzle()
    {

        allCards.Clear();
        correctSolutionSequence.Clear();

        for (int i = 0; i < 4; i++) technicianSockets[i] = -1;
        testerSockets[0] = -1;
        testerSockets[1] = -1;
        engineerSocket = -1;
        isButtonCoverOpen = false;

        KeycardPuzzleGenerator generator = new KeycardPuzzleGenerator();
        CardData[] generatedCards = generator.GeneratePuzzle();

        for (int i = 0; i < 4; i++)
        {
            correctSolutionSequence.Add(generatedCards[i].CardID);
        }

        allCards = generatedCards.ToList();

        List<CardData> shuffledCards = new List<CardData>(allCards);
        Shuffle(shuffledCards);

        isRoundActive.value = true;
        RpcSyncPuzzle(shuffledCards);

        if (isServer)
        {
            List<CardData> techCards = shuffledCards.GetRange(0, 3);
            List<CardData> engCards = shuffledCards.GetRange(3, 3);

            if (technicianDispenser != null) technicianDispenser.DispenseCards(techCards);
            if (engineerDispenser != null) engineerDispenser.DispenseCards(engCards);
        }

    }

    [ObserversRpc]
    private void RpcSyncPuzzle(List<CardData> syncedCards)
    {
        if (engineerUI != null)
        {
            engineerUI.SetWaitingForInput();
        }
    }


    [ServerRpc(requireOwnership: false)]
    public void TechnicianInsertCardRPC(int cardID, int socketIndex)
    {
        if (!isRoundActive) return;
        if (socketIndex < 0 || socketIndex > 3) return;


        technicianSockets[socketIndex] = cardID;

        CheckTechnicianSocketsFull();
    }

    [ServerRpc(requireOwnership: false)]
    public void TechnicianRemoveCardRPC(int socketIndex)
    {
        if (!isRoundActive) return;
        if (socketIndex < 0 || socketIndex > 3) return;

        technicianSockets[socketIndex] = -1;


        CheckTechnicianSocketsFull();
    }

    private void CheckTechnicianSocketsFull()
    {
        bool isFull = !technicianSockets.Contains(-1);
        if (isFull && !isButtonCoverOpen)
        {
            isButtonCoverOpen = true;
        }
        else if (!isFull && isButtonCoverOpen)
        {
            isButtonCoverOpen = false;
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void EngineerInsertCardRPC(int cardID)
    {
        if (!isRoundActive) return;

        engineerSocket = cardID;

        CardData insertedCard = allCards.Find(c => c.CardID == cardID);

        RpcUpdateEngineerSocket(cardID, insertedCard.Condition);
    }

    [ServerRpc(requireOwnership: false)]
    public void EngineerRemoveCardRPC()
    {
        if (!isRoundActive) return;

        engineerSocket = -1;

        RpcUpdateEngineerSocket(-1, new ConditionData());
    }

    [ServerRpc(requireOwnership: false)]
    public void TesterInsertCardRPC(int cardID, int socketIndex)
    {
        if (!isRoundActive.value || socketIndex < 0 || socketIndex > 1) return;


        testerSockets[socketIndex] = cardID;
        EvaluateTesterModule();
    }

    [ServerRpc(requireOwnership: false)]
    public void TesterRemoveCardRPC(int socketIndex)
    {
        if (!isRoundActive.value || socketIndex < 0 || socketIndex > 1) return;

        testerSockets[socketIndex] = -1;
        EvaluateTesterModule();
    }

    private void EvaluateTesterModule()
    {
        int card1 = testerSockets[0];
        int card2 = testerSockets[1];

        if (activeTesterRoutine != null)
        {
            StopCoroutine(activeTesterRoutine);
            activeTesterRoutine = null;
            RpcSetTesterProgress(false, 0f);
        }

        if (card1 == -1 || card2 == -1)
        {
            RpcUpdateTesterLight(0);
            return;
        }

        activeTesterRoutine = StartCoroutine(TesterProcessRoutine(card1, card2));
    }

    private IEnumerator TesterProcessRoutine(int card1, int card2)
    {
        RpcUpdateTesterLight(4);
        RpcSetTesterProgress(true, progressDuration);

        yield return new WaitForSeconds(progressDuration);

        RpcSetTesterProgress(false, 0f);

        int index1 = correctSolutionSequence.IndexOf(card1);
        int index2 = correctSolutionSequence.IndexOf(card2);

        if (index1 == -1 || index2 == -1)
        {
            RpcUpdateTesterLight(1);
        }
        else if (Mathf.Abs(index1 - index2) == 1)
        {
            RpcUpdateTesterLight(2);
        }
        else
        {
            RpcUpdateTesterLight(1);
        }

        activeTesterRoutine = null;
    }

    [ObserversRpc]
    private void RpcUpdateTesterLight(int status)
    {
        if (testerModule != null)
        {
            testerModule.UpdateLightState(status);
        }
    }

    [ObserversRpc]
    private void RpcSetTesterProgress(bool isActive, float duration)
    {
        if (testerModule != null) testerModule.SetProgressUI(isActive, duration);
    }

    [ObserversRpc]
    private void RpcUpdateEngineerSocket(int cardID, ConditionData condition)
    {
        if (engineerUI != null)
        {
            engineerUI.UpdateSocketVisual(cardID, condition);
        }
    }



    [ServerRpc(requireOwnership: false)]
    public void PressTestButtonRPC()
    {
        if (!isButtonCoverOpen)
        {
            return;
        }

        CheckSequenceSolution();
    }

    private void CheckSequenceSolution()
    {
        if (!isServer) return;

        bool isCorrectSequence = technicianSockets.SequenceEqual(correctSolutionSequence);

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
            Debug.Log("<color=green>BAŞARILI! KEYCARD İSTASYONU ÇÖZÜLDÜ.</color>");
        }
        else
        {
            Debug.Log("<color=red>HATA! YANLIŞ KOMBİNASYON. CEZA UYGULANIYOR (Su Seviyesi x2)</color>");
        }
    }


    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1) { n--; int k = Random.Range(0, n + 1); T value = list[k]; list[k] = list[n]; list[n] = value; }
    }

    [ContextMenu("Test 1: Oyunu Başlat (Generate)")]
    public void Test_StartPuzzle()
    {
        StartNewRound();
    }


}