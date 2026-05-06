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
    public bool isRoundActive = false;
    public int[] technicianSockets = new int[4] { -1, -1, -1, -1 };
    public int engineerSocket = -1;
    public bool isButtonCoverOpen = false;


    public void StartNewRound()
    {
        if (!isServer) return;
        GeneratePuzzle();
    }

    private void GeneratePuzzle()
    {
        Debug.Log("<color=cyan>[Keycard Backend] Yeni istasyon bulmacası üretiliyor...</color>");

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

        isRoundActive = true;
        RpcSyncPuzzle(shuffledCards);

        Debug.Log($"<color=green>[Keycard Backend] Üretim Tamam! Doğru Çözüm ID'leri: {correctSolutionSequence[0]} - {correctSolutionSequence[1]} - {correctSolutionSequence[2]} - {correctSolutionSequence[3]}</color>");
    }

    [ObserversRpc]
    private void RpcSyncPuzzle(List<CardData> syncedCards)
    {
        Debug.Log($"[Keycard Network] Müşterilere {syncedCards.Count} adet kart ulaştı.");
        Debug.Log("[Keycard Network] Mühendis ekranı 'Waiting for input' durumuna geçti.");
    }


    [ServerRpc(requireOwnership: false)]
    public void TechnicianInsertCardRPC(int cardID, int socketIndex)
    {
        if (!isRoundActive) return;
        if (socketIndex < 0 || socketIndex > 3) return;

        RemoveCardFromAnySocket(cardID);

        technicianSockets[socketIndex] = cardID;
        Debug.Log($"[Teknisyen] Kart (ID: {cardID}) -> {socketIndex}. Sokete takıldı.");

        CheckTechnicianSocketsFull();
    }

    [ServerRpc(requireOwnership: false)]
    public void TechnicianRemoveCardRPC(int socketIndex)
    {
        if (!isRoundActive) return;
        if (socketIndex < 0 || socketIndex > 3) return;

        int removedCardID = technicianSockets[socketIndex];
        technicianSockets[socketIndex] = -1;

        Debug.Log($"[Teknisyen] {socketIndex}. Soketteki kart (ID: {removedCardID}) çıkartıldı.");

        CheckTechnicianSocketsFull();
    }

    private void CheckTechnicianSocketsFull()
    {
        bool isFull = !technicianSockets.Contains(-1);
        if (isFull && !isButtonCoverOpen)
        {
            isButtonCoverOpen = true;
            Debug.Log("<color=yellow>[Teknisyen] 4 soket doldu! Test butonunun kapağı AÇILDI.</color>");
        }
        else if (!isFull && isButtonCoverOpen)
        {
            isButtonCoverOpen = false;
            Debug.Log("<color=yellow>[Teknisyen] Soketlerden biri boşaldı. Test butonunun kapağı KAPANDI.</color>");
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void EngineerInsertCardRPC(int cardID)
    {
        if (!isRoundActive) return;

        RemoveCardFromAnySocket(cardID);
        engineerSocket = cardID;

        CardData insertedCard = allCards.Find(c => c.CardID == cardID);

        Debug.Log($"<color=orange>[Mühendis] Info Bilgisayarına Kart (ID: {cardID}) takıldı. Koşul okunuyor...</color>");
        RpcUpdateEngineerSocket(cardID, insertedCard.Condition);
    }

    [ServerRpc(requireOwnership: false)]
    public void EngineerRemoveCardRPC()
    {
        if (!isRoundActive) return;

        int removedID = engineerSocket;
        engineerSocket = -1;

        Debug.Log($"<color=orange>[Mühendis] Kart (ID: {removedID}) bilgisayardan çıkarıldı.</color>");
        RpcUpdateEngineerSocket(-1, new ConditionData());
    }

    [ObserversRpc]
    private void RpcUpdateEngineerSocket(int cardID, ConditionData condition)
    {
        if (cardID == -1)
        {
            Debug.Log("[Mühendis UI] Ekran: 'Waiting for input...'");
        }
        else
        {
            string conditionText = $"Şablon: {condition.TemplateType} | Hedef Renk: {condition.TargetColor} | Hedef Tür: {condition.TargetType} | Yön: {condition.Direction}";
            Debug.Log($"[Mühendis UI] Ekranda Yazan Koşul: {conditionText}");
        }
    }



    [ServerRpc(requireOwnership: false)]
    public void PressTestButtonRPC()
    {
        if (!isButtonCoverOpen)
        {
            Debug.LogWarning("[Hata] Kapak kapalıyken butona basılamaz!");
            return;
        }

        Debug.Log("[Sistem] Test butonuna basıldı. Kombinasyon kontrol ediliyor...");
        CheckSequenceSolution();
    }

    private void CheckSequenceSolution()
    {
        if (!isServer) return;

        bool isCorrectSequence = technicianSockets.SequenceEqual(correctSolutionSequence);

        if (isCorrectSequence)
        {
            RpcStationResolved(true);
            isRoundActive = false;
        }
        else
        {
            RpcStationResolved(false);
            StartCoroutine(ResetSocketsAfterDelay());
        }
    }

    [ObserversRpc]
    private void RpcStationResolved(bool isSuccess)
    {
        if (isSuccess)
        {
            Debug.Log("<color=green>==========================================</color>");
            Debug.Log("<color=green>BAŞARILI! KEYCARD İSTASYONU ÇÖZÜLDÜ.</color>");
            Debug.Log("<color=green>==========================================</color>");
        }
        else
        {
            Debug.Log("<color=red>==========================================</color>");
            Debug.Log("<color=red>HATA! YANLIŞ KOMBİNASYON. CEZA UYGULANIYOR (Su Seviyesi x2)</color>");
            Debug.Log("<color=red>==========================================</color>");
        }
    }

    private IEnumerator ResetSocketsAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        Debug.Log("[Sistem] Hatalı deneme sonrası kartlar geri atılıyor (Soft Reset)...");

        for (int i = 0; i < 4; i++)
        {
            if (technicianSockets[i] != -1)
            {
                TechnicianRemoveCardRPC(i);
            }
        }
    }

    private void RemoveCardFromAnySocket(int cardID)
    {
        for (int i = 0; i < 4; i++)
        {
            if (technicianSockets[i] == cardID)
            {
                technicianSockets[i] = -1;
                Debug.Log($"[Sistem] Kart {cardID} takılmadan önce Teknisyenin {i}. soketinden çıkarıldı.");
            }
        }

        if (engineerSocket == cardID)
        {
            engineerSocket = -1;
            Debug.Log($"[Sistem] Kart {cardID} takılmadan önce Mühendis bilgisayarından çıkarıldı.");
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1) { n--; int k = Random.Range(0, n + 1); T value = list[k]; list[k] = list[n]; list[n] = value; }
    }

    // ==========================================
    // TEST FONKSİYONLARI
    // ==========================================

    [ContextMenu("Test 1: Oyunu Başlat (Generate)")]
    public void Test_StartPuzzle()
    {
        StartNewRound();
    }

    [ContextMenu("Test 2: Doğru Çözümü Otomatik Gir ve Test Et")]
    public void Test_AutoSolveCorrectly()
    {
        if (!isRoundActive) { Debug.LogWarning("Önce Test 1 ile oyunu başlatın!"); return; }

        Debug.Log("<b>--- Test: Doğru Kartlar Takılıyor ---</b>");
        for (int i = 0; i < 4; i++)
        {
            TechnicianInsertCardRPC(correctSolutionSequence[i], i);
        }
        PressTestButtonRPC();
    }

    [ContextMenu("Test 3: Yanlış Çözüm Gir (Ceza Testi)")]
    public void Test_InsertWrongSequence()
    {
        if (!isRoundActive) { Debug.LogWarning("Önce Test 1 ile oyunu başlatın!"); return; }

        Debug.Log("<b>--- Test: Yanlış Kartlar Takılıyor ---</b>");
        for (int i = 0; i < 4; i++)
        {
            TechnicianInsertCardRPC(correctSolutionSequence[3 - i], i);
        }
        PressTestButtonRPC();
    }

    [ContextMenu("Test 4: Mühendis İlk Kartı Okusun")]
    public void Test_EngineerReadCard()
    {
        if (!isRoundActive) { Debug.LogWarning("Önce Test 1 ile oyunu başlatın!"); return; }

        EngineerInsertCardRPC(allCards[0].CardID);
    }
}