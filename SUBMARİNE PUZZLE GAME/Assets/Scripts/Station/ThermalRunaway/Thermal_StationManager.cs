using UnityEngine;
using PurrNet;
using System.Collections.Generic;

public class Thermal_StationManager : NetworkBehaviour
{
    [Header("References")]
    public StationController stationController;
    public Thermal_EngineerPanel engineerPanel;
    public Thermal_PhysicalSwitch physicalSwitch;
    public Thermal_TechnicianPanel frontTechnicianPanel;
    public Thermal_TechnicianPanel backTechnicianPanel;

    [Header("Station Status")]
    public bool isStationBroken = false;
    public bool hasEngineerInteracted = false;
    public float roundTimer = 50f;

    [Header("Heat")]
    public float frontHeat = 25f;
    public float backHeat = 25f;

    public float baseHeatIncreaseRate = 3f;
    private readonly float[] possibleHeatRates = { 1f, 1.5f, 2.0f, 2.5f };

    [Header("Engineer Data")]
    public ThermalValveType activeCoolingValve = ThermalValveType.Common;
    [Range(5f, 95f)]
    public float engineerSliderPosition = 50f;
    public float sliderConeWidth = 10f;

    [Header("Needle & Cooling Zones")]
    [Range(0f, 110f)]
    public float currentPressure = 0f;
    public List<NeedleZoneData> coolingZones = new List<NeedleZoneData>();


    [Header("Pumping")]
    public float currentEmin = 0.4f;
    public float currentEmax = 1.4f;
    public float currentPumpMultiplier = 1f;
    public float needleRiseSpeed = 5f;
    public float baseNeedleDropSpeed = 2f;

    [Header("Bottleneck System")]
    public bool isBottleneckActive = false;
    public float bottleneckCooldownTimer = 0f;
    private List<int> currentBottleneckSequence = new List<int>();
    private int currentSequenceIndex = 0;

    [Header("Network")]
    public float networkTickRate = 0.05f;

    private float networkSyncTimer = 0f;
    private float lastPumpTime = 0f;
    private float lastInterval = 2f;

    private int lastSentFront = -1;
    private int lastSentBack = -1;
    private int lastSentTime = -1;
    private float lastSentPressure = -1f;
    private ThermalValveType bottleneckLocation;

    public void StartThermalRunaway()
    {
        if (!isServer) return;


        RPCSetBrokenStation(true);
        SelectRandomHeatIncreaseRate();
        SelectRandomRhythm();



        Debug.Log($"<color=cyan>[RİTİM]</color> İstasyon Bozuldu! Emin={currentEmin}, Emax={currentEmax}, Çarpan=x{currentPumpMultiplier}");
    }

    [ObserversRpc(runLocally: true)]
    private void RPCSetBrokenStation(bool value)
    {
        isStationBroken = value;
    }


    private void SelectRandomRhythm()
    {
        int rhythmRoll = Random.Range(0, 100);
        if (rhythmRoll < 40) { currentEmin = 0.2f; currentEmax = 1.2f; currentPumpMultiplier = 1.0f; }
        else if (rhythmRoll < 70) { currentEmin = 0.4f; currentEmax = 1.4f; currentPumpMultiplier = 1.5f; }
        else { currentEmin = 0.8f; currentEmax = 1.8f; currentPumpMultiplier = 2.0f; }

        lastPumpTime = Time.time;
        lastInterval = currentEmax;
    }

    private void SelectRandomHeatIncreaseRate()
    {
        baseHeatIncreaseRate = possibleHeatRates[Random.Range(0, possibleHeatRates.Length)];
    }

    private void Update()
    {
        if (!isServer || !isStationBroken || !hasEngineerInteracted) return;

        HandleTimerAndHeat();
        HandleNeedlePhysics();

        networkSyncTimer += Time.deltaTime;
        if (networkSyncTimer >= networkTickRate)
        {
            networkSyncTimer = 0f;
            CheckAndSyncContinuousData();
        }

        if (bottleneckCooldownTimer > 0f)
        {
            bottleneckCooldownTimer -= Time.deltaTime;
        }
    }



    private void HandleTimerAndHeat()
    {
        roundTimer -= Time.deltaTime;

        float frontMultiplier = 1f;
        float backMultiplier = 1f;

        if (activeCoolingValve == ThermalValveType.Front) { frontMultiplier = 0.8f; backMultiplier = 1.2f; }
        else if (activeCoolingValve == ThermalValveType.Back) { frontMultiplier = 1.2f; backMultiplier = 0.8f; }

        frontHeat += baseHeatIncreaseRate * frontMultiplier * Time.deltaTime;
        backHeat += baseHeatIncreaseRate * backMultiplier * Time.deltaTime;

        if (frontHeat >= 100f || backHeat >= 100f) LoseStation();
        else if (roundTimer <= 0f) WinStation();
    }

    private void HandleNeedlePhysics()
    {
        float timeSincePump = Time.time - lastPumpTime;
        float activeInterval = Mathf.Max(lastInterval, timeSincePump);

        float t = (activeInterval - currentEmax) / (currentEmin - currentEmax);
        float targetPressure = t * 100f;

        if (currentPressure < targetPressure)
        {
            currentPressure = Mathf.Lerp(currentPressure, targetPressure, needleRiseSpeed * Time.deltaTime);
        }
        else
        {
            float dropMultiplier = 1f + timeSincePump / currentEmax * 2f;
            currentPressure = Mathf.Lerp(currentPressure, targetPressure, baseNeedleDropSpeed * dropMultiplier * Time.deltaTime);
        }

        currentPressure = Mathf.Max(0f, currentPressure);
    }

    private void CheckAndSyncContinuousData()
    {
        int cFront = Mathf.RoundToInt(frontHeat);
        int cBack = Mathf.RoundToInt(backHeat);
        int cTime = Mathf.CeilToInt(roundTimer);

        bool hasChanges = cFront != lastSentFront || cBack != lastSentBack || cTime != lastSentTime || Mathf.Abs(currentPressure - lastSentPressure) >= 0.5f;

        if (hasChanges)
        {
            lastSentFront = cFront;
            lastSentBack = cBack;
            lastSentTime = cTime;
            lastSentPressure = currentPressure;

            RpcSyncDashboard(cFront, cBack, cTime, currentPressure);
        }
    }



    [ServerRpc(requireOwnership: false)]
    public void EngineerInteractedRPC()
    {
        if (!isStationBroken) return;
        if (!hasEngineerInteracted)
            RPCSetEngineerInteracted();
    }

    [ObserversRpc(runLocally: true)]
    private void RPCSetEngineerInteracted(bool value = true)
    {
        hasEngineerInteracted = value;
    }

    [ServerRpc(requireOwnership: false)]
    public void SwitchValveDirectionRPC(ThermalValveType targetState)
    {
        if (!isStationBroken) return;

        activeCoolingValve = targetState;
        RpcUpdateActiveValve(targetState);
    }

    [ObserversRpc]
    public void RpcUpdateActiveValve(ThermalValveType newActiveValve)
    {
        activeCoolingValve = newActiveValve;
    }

    [ServerRpc(requireOwnership: false)]
    public void UpdateSliderPositionRPC(float newPosition)
    {
        if (!isStationBroken) return;
        engineerSliderPosition = Mathf.Clamp(newPosition, 5f, 95f);
    }

    [ServerRpc(requireOwnership: false)]
    public void PumpValveRPC(ThermalValveType pumpedValve)
    {
        if (!isStationBroken || !hasEngineerInteracted || isBottleneckActive) return;

        float interval = Time.time - lastPumpTime;
        lastInterval = interval;
        lastPumpTime = Time.time;

        if (pumpedValve == activeCoolingValve && activeCoolingValve != ThermalValveType.Common)
        {
            if (interval < currentEmin || currentPressure > 100f)
            {
                Debug.Log($"<color=red>[TEKNİSYEN]</color> OVERKILL! Soğutma boşa gitti.");
                currentPressure = 105f;
                CheckForBottleneck(currentPressure);
                return;
            }

            if (CheckForBottleneck(currentPressure)) return;

            float halfCone = sliderConeWidth / 2f;
            float minBound = engineerSliderPosition - halfCone;
            float maxBound = engineerSliderPosition + halfCone;

            if (currentPressure >= minBound && currentPressure <= maxBound)
            {
                float baseCooling = GetCoolingAmountFromPressure(currentPressure);
                float actualCooling = baseCooling * currentPumpMultiplier;

                if (actualCooling > 0)
                {
                    if (pumpedValve == ThermalValveType.Front)
                    {
                        frontHeat = Mathf.Max(10f, frontHeat - actualCooling);
                    }
                    else
                    {
                        backHeat = Mathf.Max(10f, backHeat - actualCooling);
                    }
                }
            }
        }
    }

    private float GetCoolingAmountFromPressure(float pressure)
    {
        foreach (var zone in coolingZones)
        {
            if (pressure >= zone.minPressure && pressure <= zone.maxPressure)
                return zone.coolingAmount;
        }
        return 0f;
    }

    private bool CheckForBottleneck(float pressureToCheck)
    {
        if (bottleneckCooldownTimer > 0f) return false;

        float chance = GetBottleneckChanceFromPressure(pressureToCheck);
        int roll = Random.Range(0, 100);
        Debug.Log($"<color=magenta>[DARBOĞAZ]</color> Şans Kontrolü: {roll} < {chance} ? {(roll < chance ? "DARBOĞAZ TETİKLENDİ!" : "Kurtuldunuz.")}");
        if (roll < chance)
        {
            TriggerBottleneck();
            return true;
        }
        return false;
    }

    private float GetBottleneckChanceFromPressure(float pressure)
    {
        if (pressure >= 100f) return 65f;

        foreach (var zone in coolingZones)
        {
            if (pressure >= zone.minPressure && pressure <= zone.maxPressure)
                return zone.bottleneckChance;
        }
        return 0f;
    }

    private void TriggerBottleneck()
    {
        isBottleneckActive = true;
        currentSequenceIndex = 0;
        currentBottleneckSequence.Clear();
        bottleneckLocation = activeCoolingValve;
        int length = Random.Range(3, 7);
        for (int i = 0; i < length; i++)
        {
            currentBottleneckSequence.Add(Random.Range(0, 3));
        }

        Debug.Log($"<color=yellow>!!! {bottleneckLocation} VANASINDA DARBOĞAZ OLUŞTU !!!</color>");

        RpcOnBottleneckTriggered(currentBottleneckSequence.ToArray(), bottleneckLocation);
    }

    [ServerRpc(requireOwnership: false)]
    public void SubmitBottleneckCodeRPC(int colorCode, ThermalValveType buttonLocation)
    {
        if (!isBottleneckActive || !isStationBroken) return;
        if (buttonLocation != bottleneckLocation) return;
        if (currentBottleneckSequence[currentSequenceIndex] == colorCode)
        {
            currentSequenceIndex++;
            Debug.Log($"<color=cyan>[MÜHENDİS]</color> Doğru Tuş! ({currentSequenceIndex}/{currentBottleneckSequence.Count})");

            if (currentSequenceIndex >= currentBottleneckSequence.Count)
            {
                isBottleneckActive = false;
                bottleneckCooldownTimer = 5f;
                Debug.Log("<color=lime>!!! DARBOĞAZ ÇÖZÜLDÜ !!! 5sn Koruma Aktif.</color>");
                RpcOnBottleneckSolved();
            }
        }
        else
        {
            currentSequenceIndex = 0;
            Debug.Log("<color=red>[MÜHENDİS]</color> YANLIŞ TUŞ! Şifre sıfırlandı.");
            stationController.ReportRepairMistake();
            RpcOnBottleneckFailed();
        }
    }


    [ObserversRpc]
    private void RpcOnBottleneckTriggered(int[] sequence, ThermalValveType location)
    {
        isBottleneckActive = true;

        if (frontTechnicianPanel != null) frontTechnicianPanel.HandleBottleneckTrigger(sequence, location);
        if (backTechnicianPanel != null) backTechnicianPanel.HandleBottleneckTrigger(sequence, location);

        if (engineerPanel != null) engineerPanel.SetBottleneckState(true);
    }

    [ObserversRpc]
    private void RpcOnBottleneckFailed()
    {
        if (engineerPanel != null) engineerPanel.TriggerBottleneckError();
    }

    [ObserversRpc]
    private void RpcOnBottleneckSolved()
    {
        isBottleneckActive = false;
        if (frontTechnicianPanel != null) frontTechnicianPanel.StopSequence();
        if (backTechnicianPanel != null) backTechnicianPanel.StopSequence();

        if (engineerPanel != null) engineerPanel.SetBottleneckState(false);
    }




    [ObserversRpc]
    private void RpcSyncDashboard(int fHeat, int bHeat, int time, float pressure)
    {
        if (!isServer)
        {
            frontHeat = fHeat;
            backHeat = bHeat;
            roundTimer = time;
            currentPressure = pressure;
        }

        if (engineerPanel != null)
        {
            engineerPanel.UpdateDashboardData(fHeat, bHeat, time, pressure);
        }
    }

    private void WinStation()
    {
        stationController.SetReparied();
        RPCSetBrokenStation(false);
        RPCSetEngineerInteracted(false);
        frontHeat = 10f;
        backHeat = 10f;
        Debug.Log("<color=lime>!!! İSTASYON ÇÖZÜLDÜ !!!</color>");
        RpcSyncDashboard(10, 10, 0, 0);
        RpcOnStationEnded(true);
    }

    private void LoseStation()
    {
        stationController.ReportRepairMistake();
        RPCSetBrokenStation(false);
        RPCSetEngineerInteracted(false);
        Debug.Log("<color=red>!!! İSTASYON PATLADI !!!</color>");
        RpcOnStationEnded(false);
    }

    [ObserversRpc]
    private void RpcOnStationEnded(bool isWin)
    {
        if (engineerPanel != null)
        {
            engineerPanel.SetStationEndState(isWin);
        }

        backTechnicianPanel.StopSequence();
        frontTechnicianPanel.StopSequence();
    }

    [ContextMenu("TEST: BreakStation")]
    public void Test_BreakStation() { StartThermalRunaway(); }
}