using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using System.Linq;

public class Magnetic_StationManager : NetworkBehaviour
{
    [Header("Live State (SyncVars)")]
    public SyncVar<bool> isRoundActive = new SyncVar<bool>(false);

    // Kanalların ilerleme durumu (0 = CH1, 1 = CH2, 2 = CH3, 3 = Tamamlandı)
    public SyncVar<int> techCurrentChannel = new SyncVar<int>(0);
    public SyncVar<int> engCurrentChannel = new SyncVar<int>(0);

    // Telsize girilmesi gereken final frekans (Örn: "44.757hz")
    public SyncVar<string> targetRadioFrequency = new SyncVar<string>("");

    [Header("Backend Data (Server Only)")]
    // Sunucuda tutulan, 3 kanallık bulmaca verisi
    private ChannelData[] activeChannels = new ChannelData[3];
    private int[] symbolValues = new int[10]; // 0-9 arası sembollerin sayısal karşılıkları

    [Header("References")]
    public StationController stationController; // Ceza ve başarı durumları için ana controller

    // Client'ların state değişimlerini dinlemesi için eventler (Frontend scriptleri bunlara abone olacak)
    public event System.Action OnPuzzleGenerated;
    public event System.Action<int> OnTechChannelAdvanced;
    public event System.Action<int> OnEngChannelAdvanced;

    public void StartNewRound()
    {
        if (!isServer) return;

        GeneratePuzzle();

        isRoundActive.value = true;
        techCurrentChannel.value = 0;
        engCurrentChannel.value = 0;
    }

    private void GeneratePuzzle()
    {
        // 1. Sembol havuzundan 10 tane rastgele sembol seçilir ve rakamlara atanır[cite: 93, 94].
        List<int> digits = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Shuffle(digits);
        for (int i = 0; i < 10; i++) symbolValues[i] = digits[i];

        // 2. 1-1-2 hariç tüm dalga frekansları havuzu[cite: 95].
        List<WaveConfig> allPossibleWaves = new List<WaveConfig>();
        for (int a = 1; a <= 6; a++)
        {
            for (int f = 1; f <= 6; f++)
            {
                for (int p = 1; p <= 3; p++)
                {
                    if (a == 1 && f == 1 && p == 2) continue;
                    allPossibleWaves.Add(new WaveConfig { amplitude = a, frequency = f, phase = p });
                }
            }
        }
        Shuffle(allPossibleWaves);

        // 3. Kanallara atanacak 3 farklı sembol ID'sini seçiyoruz (0'dan 9'a kadar indeks)[cite: 109].
        List<int> poolForSymbols = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Shuffle(poolForSymbols);
        int s1_id = poolForSymbols[0]; int s1_val = symbolValues[s1_id]; // [cite: 110]
        int s2_id = poolForSymbols[1]; int s2_val = symbolValues[s2_id]; // [cite: 111]
        int s3_id = poolForSymbols[2]; int s3_val = symbolValues[s3_id]; // [cite: 112]

        // 4. Core Algoritmayı çağırarak X, Y, Z değişkenlerini ve formatlanmış Denklem verilerini üretiyoruz[cite: 96].
        EquationData eq1, eq2, eq3;
        int x, y, z;

        Magnetic_EquationGenerator.GenerateEquations(
            s1_id, s1_val, s2_id, s2_val, s3_id, s3_val,
            out eq1, out eq2, out eq3,
            out x, out y, out z
        );

        // 5. Belirlenen semboller, denklemler ve dalgalar kanallara aktarılır[cite: 99].
        activeChannels[0] = new ChannelData { targetWave = allPossibleWaves[0], symbolID = s1_id, equation = eq1 };
        activeChannels[1] = new ChannelData { targetWave = allPossibleWaves[1], symbolID = s2_id, equation = eq2 };
        activeChannels[2] = new ChannelData { targetWave = allPossibleWaves[2], symbolID = s3_id, equation = eq3 };

        // 6. X, Y, Z değişkenlerine göre radyoya girilmesi gereken final frekansı belirlenir[cite: 100].
        targetRadioFrequency.value = $"{x}{x}.{y}{z}{z}hz"; //[cite: 71, 74].

        // Tüm verileri Client'lara gönder
        RpcSendPuzzleToClients(activeChannels[0], activeChannels[1], activeChannels[2]);
    }

    [ObserversRpc]
    private void RpcSendPuzzleToClients(ChannelData ch1, ChannelData ch2, ChannelData ch3)
    {
        activeChannels[0] = ch1;
        activeChannels[1] = ch2;
        activeChannels[2] = ch3;

        OnPuzzleGenerated?.Invoke();
    }

    // ==========================================
    // CLIENT -> SERVER RPC PROTOKOLLERİ
    // ==========================================

    [ServerRpc(requireOwnership: false)]
    public void SubmitWaveRPC(int amplitude, int frequency, int phase)
    {
        if (!isRoundActive.value || techCurrentChannel.value >= 3) return;

        WaveConfig submittedWave = new WaveConfig { amplitude = amplitude, frequency = frequency, phase = phase };
        WaveConfig targetWave = activeChannels[techCurrentChannel.value].targetWave;

        if (submittedWave.Equals(targetWave))
        {
            // Doğru dalga! Teknisyenin kanalını atlat
            techCurrentChannel.value++;
            RpcTechChannelSuccess(techCurrentChannel.value);
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void SubmitEquationAnswerRPC(int numpadInput)
    {
        if (!isRoundActive.value || engCurrentChannel.value >= 3) return;

        int correctAnswer = activeChannels[engCurrentChannel.value].equation.targetAnswer;

        if (numpadInput == correctAnswer)
        {
            // Doğru cevap! Mühendisin kanalını atlat
            engCurrentChannel.value++;
            RpcEngChannelSuccess(engCurrentChannel.value);
        }
        else
        {
            // Yanlış Cevap! Ceza sistemi tetiklenir
            stationController.ReportRepairMistake();
            RpcStationMistake("Mühendis yanlış bir hesaplama yaptı!");
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void SubmitRadioFrequencyRPC(string inputFreq)
    {
        if (!isRoundActive.value || engCurrentChannel.value < 3 || techCurrentChannel.value < 3) return;

        if (inputFreq == targetRadioFrequency.value)
        {
            // İSTASYON BAŞARIYLA TAMAMLANDI!
            isRoundActive.value = false;
            stationController.SetReparied();
            RpcStationResolved(true);
        }
        else
        {
            // YANLIŞ FREKANS! Ceza
            stationController.ReportRepairMistake();
            RpcStationMistake("Yanlış radyo frekansı girildi!");
        }
    }

    // ==========================================
    // SERVER -> CLIENT BİLDİRİMLERİ (Görsel İşlemler İçin)
    // ==========================================

    [ObserversRpc]
    private void RpcTechChannelSuccess(int newChannelIndex)
    {
        OnTechChannelAdvanced?.Invoke(newChannelIndex);
    }

    [ObserversRpc]
    private void RpcEngChannelSuccess(int newChannelIndex)
    {
        OnEngChannelAdvanced?.Invoke(newChannelIndex);
    }

    [ObserversRpc]
    private void RpcStationMistake(string reason)
    {
        Debug.LogWarning($"<color=red>HATA! CEZA UYGULANIYOR:</color> {reason}");
        // Hata durumunda UI titremesi, kırmızı ışık yanması vs. burada tetiklenebilir.
    }

    [ObserversRpc]
    private void RpcStationResolved(bool isSuccess)
    {
        Debug.Log("<color=green>BAŞARILI! MAGNETIC İSTASYONU ÇÖZÜLDÜ.</color>");
        // Tamamlanma sesleri, ışıkların düzelmesi vs.
    }

    // Yardımcı Karıştırma Fonksiyonu
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // Frontend (UI) scriptlerinin veriyi okuyabilmesi için public bir Getter
    public ChannelData GetChannelData(int channelIndex)
    {
        if (channelIndex >= 0 && channelIndex < 3)
            return activeChannels[channelIndex];

        return default;
    }


#if UNITY_EDITOR
    [ContextMenu("Test/1. Start New Round")]
    private void Test_StartNewRound()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Testleri çalıştırabilmek için Play Mode'da olmalısınız!");
            return;
        }

        Debug.Log("<color=yellow>--- YENİ BULMACA ÜRETİLİYOR ---</color>");
        StartNewRound();
        Test_PrintPuzzleInfo();
    }

    [ContextMenu("Test/2. Print Puzzle Info")]
    private void Test_PrintPuzzleInfo()
    {
        if (!Application.isPlaying || !isRoundActive.value) return;

        for (int i = 0; i < 3; i++)
        {
            ChannelData data = GetChannelData(i);
            Debug.Log($"<b>[KANAL {i + 1}]</b> Dalga Hedefi: (A:{data.targetWave.amplitude}, F:{data.targetWave.frequency}, P:{data.targetWave.phase}) | " +
                      $"Sembol ID: S{data.symbolID} | " +
                      $"Denklem: {data.equation.displayString} | " +
                      $"Cevap: {data.equation.targetAnswer}");
        }

        Debug.Log($"<b>[FİNAL HEDEF]</b> Radyo Frekansı: {targetRadioFrequency.value}");
    }

    [ContextMenu("Test/3. Simulate Tech Correct Wave")]
    private void Test_SimulateTechCorrectWave()
    {
        if (!Application.isPlaying || !isRoundActive.value || techCurrentChannel.value >= 3) return;

        ChannelData currentData = GetChannelData(techCurrentChannel.value);
        Debug.Log($"<color=cyan>Teknisyen Doğru Dalgayı Gönderiyor (Kanal {techCurrentChannel.value + 1})</color>");
        SubmitWaveRPC(currentData.targetWave.amplitude, currentData.targetWave.frequency, currentData.targetWave.phase);
    }

    [ContextMenu("Test/4. Simulate Eng Correct Answer")]
    private void Test_SimulateEngCorrectAnswer()
    {
        if (!Application.isPlaying || !isRoundActive.value || engCurrentChannel.value >= 3) return;

        ChannelData currentData = GetChannelData(engCurrentChannel.value);
        Debug.Log($"<color=orange>Mühendis Doğru Denklem Cevabını Gönderiyor (Kanal {engCurrentChannel.value + 1}): {currentData.equation.targetAnswer}</color>");
        SubmitEquationAnswerRPC(currentData.equation.targetAnswer);
    }

    [ContextMenu("Test/5. Simulate Final Radio Frequency")]
    private void Test_SimulateFinalFrequency()
    {
        if (!Application.isPlaying || !isRoundActive.value) return;

        Debug.Log($"<color=green>Teknisyen Final Frekansı Gönderiyor: {targetRadioFrequency.value}</color>");
        SubmitRadioFrequencyRPC(targetRadioFrequency.value);
    }
#endif
}