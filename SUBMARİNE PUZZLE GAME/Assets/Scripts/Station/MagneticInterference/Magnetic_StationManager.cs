using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using System.Linq;

public class Magnetic_StationManager : NetworkBehaviour
{
    [Header("Live State (SyncVars)")]
    public SyncVar<bool> isRoundActive = new SyncVar<bool>(false);

    public SyncVar<int> techCurrentChannel = new SyncVar<int>(0);
    public SyncVar<int> engCurrentChannel = new SyncVar<int>(0);

    public SyncVar<string> targetRadioFrequency = new SyncVar<string>("");
    public SyncVar<string> radioFrequencyFormat = new SyncVar<string>("");

    [Header("Backend Data (Server Only)")]
    private ChannelData[] activeChannels = new ChannelData[3];
    private int[] symbolValues = new int[10];

    [Header("References")]
    public StationController stationController;
    public event System.Action OnPuzzleGenerated;
    public event System.Action<int> OnTechChannelAdvanced;
    public event System.Action<int> OnEngChannelAdvanced;
    public event System.Action<int[]> OnSymbolMappingReceived;
    public event System.Action<string> OnFrequencyFormatReceived;

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
        List<int> digits = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Shuffle(digits);
        for (int i = 0; i < 10; i++) symbolValues[i] = digits[i];

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

        List<int> poolForSymbols = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Shuffle(poolForSymbols);
        int s1_id = poolForSymbols[0]; int s1_val = symbolValues[s1_id];
        int s2_id = poolForSymbols[1]; int s2_val = symbolValues[s2_id];
        int s3_id = poolForSymbols[2]; int s3_val = symbolValues[s3_id];

        EquationData eq1, eq2, eq3;
        int x, y, z;

        Magnetic_EquationGenerator.GenerateEquations(
            s1_id, s1_val, s2_id, s2_val, s3_id, s3_val,
            out eq1, out eq2, out eq3,
            out x, out y, out z
        );

        activeChannels[0] = new ChannelData { targetWave = allPossibleWaves[0], symbolID = s1_id, equation = eq1 };
        activeChannels[1] = new ChannelData { targetWave = allPossibleWaves[1], symbolID = s2_id, equation = eq2 };
        activeChannels[2] = new ChannelData { targetWave = allPossibleWaves[2], symbolID = s3_id, equation = eq3 };

        string[] formatTemplates = new string[]
         {
            "XX.YZZ", "XY.ZZX", "ZY.XXY", "YZ.ZXX",
            "ZX.YXY", "YX.ZYZ", "ZZ.XYY", "YY.ZXX",
            "XZ.YYZ", "ZY.XZZ", "XY.YZZ", "ZX.XYZ",
            "YZ.XYX", "ZY.YXX", "XX.ZYY"
         };

        string selectedFormat = formatTemplates[Random.Range(0, formatTemplates.Length)];

        string formatString = selectedFormat + " hz";
        radioFrequencyFormat.value = formatString;

        string finalFreq = selectedFormat.Replace("X", x.ToString())
                                         .Replace("Y", y.ToString())
                                         .Replace("Z", z.ToString());

        targetRadioFrequency.value = finalFreq + "hz";

        RpcSendPuzzleToClients(activeChannels[0], activeChannels[1], activeChannels[2], symbolValues, formatString);
    }

    [ObserversRpc]
    private void RpcSendPuzzleToClients(ChannelData ch1, ChannelData ch2, ChannelData ch3, int[] mapping, string formatStr)
    {
        RadioVoiceManager.Instance.SetRadioBrokenState(true);

        activeChannels[0] = ch1;
        activeChannels[1] = ch2;
        activeChannels[2] = ch3;

        OnPuzzleGenerated?.Invoke();
        OnSymbolMappingReceived?.Invoke(mapping);
        OnFrequencyFormatReceived?.Invoke(formatStr);
    }
    // ==========================================
    // CLIENT -> SERVER RPC 
    // ==========================================

    [ServerRpc(requireOwnership: false)]
    public void SubmitWaveRPC(int amplitude, int frequency, int phase)
    {
        if (!isRoundActive.value || techCurrentChannel.value >= 3) return;

        WaveConfig submittedWave = new WaveConfig { amplitude = amplitude, frequency = frequency, phase = phase };
        WaveConfig targetWave = activeChannels[techCurrentChannel.value].targetWave;

        if (submittedWave.Equals(targetWave))
        {
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
            engCurrentChannel.value++;
            RpcEngChannelSuccess(engCurrentChannel.value);
        }
        else
        {
            stationController.ReportRepairMistake();
            RpcStationMistake("Mühendis yanlış bir hesaplama yaptı!");
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void SubmitRadioFrequencyRPC(string inputFreq)
    {
        if (!isRoundActive.value) return;
        Debug.Log($"<color=magenta>Final frekans denemesi: {inputFreq}</color>");

        if (inputFreq == targetRadioFrequency.value)
        {
            isRoundActive.value = false;
            stationController.SetReparied();
            RpcStationResolved(true);
        }
        else
        {
            stationController.ReportRepairMistake(2);
            RpcStationMistake("Yanlış radyo frekansı girildi!");
        }
    }

    // ==========================================
    // SERVER -> CLIENT RPC
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
    }

    [ObserversRpc]
    private void RpcStationResolved(bool isSuccess)
    {
        Debug.Log("<color=green>BAŞARILI! MAGNETIC İSTASYONU ÇÖZÜLDÜ.</color>");
        RadioVoiceManager.Instance.SetRadioBrokenState(false);

    }

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

    public ChannelData GetChannelData(int channelIndex)
    {
        if (channelIndex >= 0 && channelIndex < 3)
            return activeChannels[channelIndex];

        return default;
    }


    [ContextMenu("Start New Round")]
    private void Test_StartNewRound()
    {

        StartNewRound();
    }


}