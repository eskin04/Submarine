using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using PurrNet;

public enum AlchemicalSymbol
{
    Water, Earth, Air,
    Poison, Acid,
    Elixir, Gold
}

[Serializable]
public struct SymbolConfig
{
    public AlchemicalSymbol symbol;
    public int weight;
    public bool isPenalty;
    [Tooltip("Yatırılan bahis miktarı ile çarpılacak oran (Örn: 1.5, 2.0, 10.0)")]
    public float payoutMultiplier;
}

public class SlotMachineBackend : NetworkBehaviour
{
    public event Action OnSpinStarted;
    public event Action<AlchemicalSymbol[]> OnSpinCompleted;
    public event Action<AlchemicalSymbol[]> OnSpinCalculated;
    public event Action<int, bool> OnWin;
    public event Action<int, bool> OnPenalty;
    public event Action OnLoss;
    public event Action<int> OnBetChanged;

    [Header("Bet Settings")]
    public int minBet = 10;
    public int maxBet = 100;
    public int betStep = 10;

    [Range(0.1f, 1f)]
    public float partialMatchMultiplier = 0.5f;

    [Header("Symbol Configurations")]
    public List<SymbolConfig> symbolConfigs;

    private int currentBet;
    private const int ReelCount = 3;
    private int _totalWeight = 0;

    private bool _isSpinning = false;
    private bool _hasPendingResult = false;
    private AlchemicalSymbol _pendingSymbol;
    private bool _pendingIsMatch3;
    private bool _pendingIsMatch2;

    private void Awake()
    {
        currentBet = minBet;

        foreach (var config in symbolConfigs)
        {
            _totalWeight += config.weight;
        }
    }

    public void IncreaseBet()
    {
        if (currentBet + betStep <= maxBet)
        {
            currentBet += betStep;
            OnBetChanged?.Invoke(currentBet);
        }
    }

    public void DecreaseBet()
    {
        if (currentBet - betStep >= minBet)
        {
            currentBet -= betStep;
            OnBetChanged?.Invoke(currentBet);
        }
    }

    public void TrySpin()
    {
        if (_isSpinning) return;

        _isSpinning = true;

        UpdateWaterLevelServerRpc(currentBet);

        OnSpinStarted?.Invoke();
        CalculateSpinResult();
    }

    private void CalculateSpinResult()
    {
        AlchemicalSymbol[] result = new AlchemicalSymbol[ReelCount];
        for (int i = 0; i < ReelCount; i++) result[i] = GetRandomWeightedSymbol();

        EvaluateResult(result);

        OnSpinCalculated?.Invoke(result);
    }

    private AlchemicalSymbol GetRandomWeightedSymbol()
    {
        int randomValue = Random.Range(0, _totalWeight);
        int currentSum = 0;

        foreach (var config in symbolConfigs)
        {
            currentSum += config.weight;
            if (randomValue < currentSum)
            {
                return config.symbol;
            }
        }

        return symbolConfigs[0].symbol;
    }

    private void EvaluateResult(AlchemicalSymbol[] result)
    {
        _pendingIsMatch3 = (result[0] == result[1]) && (result[1] == result[2]);
        _pendingIsMatch2 = false;
        _pendingSymbol = result[0];

        if (!_pendingIsMatch3)
        {
            if (result[0] == result[1] || result[0] == result[2]) { _pendingIsMatch2 = true; _pendingSymbol = result[0]; }
            else if (result[1] == result[2]) { _pendingIsMatch2 = true; _pendingSymbol = result[1]; }
        }

        _hasPendingResult = true;
    }

    public void FinalizeSpin()
    {
        if (!_hasPendingResult) return;

        if (_pendingIsMatch3 || _pendingIsMatch2)
        {
            ProcessMatch(_pendingSymbol, _pendingIsMatch3);
        }
        else
        {
            OnLoss?.Invoke();
        }

        _hasPendingResult = false;
        _isSpinning = false;
    }

    private void ProcessMatch(AlchemicalSymbol symbol, bool isMatch3)
    {
        SymbolConfig config = symbolConfigs.Find(x => x.symbol == symbol);

        float baseAmount = currentBet * config.payoutMultiplier;
        int finalAmount = Mathf.RoundToInt(isMatch3 ? baseAmount : (baseAmount * partialMatchMultiplier));

        if (config.isPenalty)
        {
            UpdateWaterLevelServerRpc(finalAmount);
            OnPenalty?.Invoke(finalAmount, isMatch3);
        }
        else
        {
            UpdateWaterLevelServerRpc(-finalAmount);
            OnWin?.Invoke(finalAmount, isMatch3);
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void UpdateWaterLevelServerRpc(float fillAmount)
    {
        Debug.Log($"[Slot Server] Su seviyesi güncelleniyor. Miktar: {fillAmount}");
        GlobalEvents.OnAddFloodPenalty?.Invoke(fillAmount);
    }

    [ContextMenu("Test Spin")]
    private void TestSpin()
    {
        TrySpin();
    }

    [ContextMenu("Test Increase Bet")]
    private void TestIncreaseBet()
    {
        IncreaseBet();
    }

    [ContextMenu("Test Decrease Bet")]
    private void TestDecreaseBet()
    {
        DecreaseBet();
    }

}