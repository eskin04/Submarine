using UnityEngine;
using System.Collections.Generic;
using PurrNet;
using System.Linq;

public class Keycard_StationManager : NetworkBehaviour
{
    [Header("Backend Data (Read Only)")]
    public List<CardData> allCards = new List<CardData>();
    public List<int> correctSolutionSequence = new List<int>();

    [Header("Live State")]
    public SyncVar<bool> isRoundActive = new SyncVar<bool>(false);
    public int[] technicianSockets = new int[4] { -1, -1, -1, -1 };
    public int engineerSocket = -1;
    public bool isButtonCoverOpen = false;

    [Header("Dispensers")]
    public Keycard_Dispenser technicianDispenser;
    public Keycard_Dispenser engineerDispenser;

    [Header("References")]
    public Keycard_EngineerUI engineerUI;
    public StationController stationController;


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