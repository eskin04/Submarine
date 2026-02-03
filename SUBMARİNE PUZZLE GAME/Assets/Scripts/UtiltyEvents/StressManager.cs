using System.Collections.Generic;
using System.Linq;
using PurrNet;
using UnityEngine;

public class StressManager : NetworkBehaviour
{
    [Header("Stress Settings")]
    [SerializeField] private float maxStress = 100f;
    [SerializeField] private float passiveStressRate = 5f;
    [SerializeField] private float passiveStressInterval = 10f;
    [SerializeField] private float diceCheckInterval = 15f;
    [SerializeField] private float eventCooldownDuration = 45f;
    [SerializeField] private float initialGracePeriod = 30f;

    [Header("Network Data")]
    public SyncVar<float> currentStress = new SyncVar<float>(0f);
    public SyncVar<string> currentEventName = new SyncVar<string>("");

    private List<UtilityEventWrapper> availableEvents = new List<UtilityEventWrapper>();

    private GameStatistics gameStats;

    private bool isActive = false;
    private bool isInCooldown = false;

    private float passiveTimer = 0f;
    private float diceTimer = 0f;
    private float cooldownTimer = 0f;

    protected override void OnEarlySpawn()
    {
        base.OnEarlySpawn();
        GlobalEvents.OnRegisterUtilityStation += RegisterStation;
        GlobalEvents.OnAddStress += IncreaseStress;
        GlobalEvents.OnReduceStress += DecreaseStress;
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        gameStats = GetComponent<GameStatistics>();
        MainGameState.startGame += StartStressManager;

    }

    private void StartStressManager()
    {
        Debug.Log("StressManager: Başlangıç Gecikmesi başladı.");
        Invoke(nameof(ActivateSystem), initialGracePeriod);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GlobalEvents.OnRegisterUtilityStation -= RegisterStation;
        GlobalEvents.OnAddStress -= IncreaseStress;
        GlobalEvents.OnReduceStress -= DecreaseStress;
        MainGameState.startGame -= StartStressManager;

    }


    private void RegisterStation(StationController station, UtilityEventData data)
    {
        if (!isServer) return;

        if (!availableEvents.Exists(x => x.station == station))
        {
            station.SetOperational();

            if (data == null)
            {
                data = ScriptableObject.CreateInstance<UtilityEventData>();
                data.baseWeight = 5f;
            }

            availableEvents.Add(new UtilityEventWrapper(station, data));
            Debug.Log($"StressManager: {data.targetStatName} eklendi. (Base: {data.baseWeight})");
        }
    }

    private void IncreaseStress(float amount)
    {
        if (!isServer || !isActive || isInCooldown) return;
        ModifyStress(amount);
    }

    private void DecreaseStress(float amount)
    {
        if (!isServer || !isActive) return;
        ModifyStress(-amount);
    }


    private void ActivateSystem()
    {
        isActive = true;
        Debug.Log("STRESS SYSTEM ONLINE");
    }

    private void Update()
    {
        if (!isServer || !isActive) return;

        if (isInCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                isInCooldown = false;
                currentStress.value = 0f;
                Debug.Log("Stress System: Cooldown bitti.");
            }
            return;
        }

        passiveTimer += Time.deltaTime;
        if (passiveTimer >= passiveStressInterval)
        {
            passiveTimer = 0f;
            ModifyStress(passiveStressRate);
        }

        diceTimer += Time.deltaTime;
        if (diceTimer >= diceCheckInterval)
        {
            diceTimer = 0f;
            CheckForEventTrigger();
        }

        if (currentStress.value >= maxStress)
        {
            TriggerRandomEvent();
        }
    }

    private void ModifyStress(float amount)
    {
        float target = currentStress.value + amount;
        currentStress.value = Mathf.Clamp(target, 0, maxStress);
    }

    private void CheckForEventTrigger()
    {
        if (availableEvents.Count == 0) return;

        float roll = Random.Range(0f, 100f);
        Debug.Log($"[ZAR] Atılan: {roll}, Stres: {currentStress.value}");

        if (roll < currentStress.value)
        {
            TriggerRandomEvent();
        }
    }

    private void TriggerRandomEvent()
    {
        var candidates = availableEvents.Where(x => x.station.GetCurrentState() == StationState.Operational).ToList();
        if (candidates.Count == 0) return;

        float totalWeight = 0f;
        foreach (var evt in candidates)
        {
            evt.CalculateCurrentWeight(gameStats);
            totalWeight += evt.currentWeight;
        }

        float randomPoint = Random.Range(0, totalWeight);
        float cursor = 0f;
        UtilityEventWrapper selectedEvent = null;

        foreach (var evt in candidates)
        {
            cursor += evt.currentWeight;
            if (randomPoint <= cursor)
            {
                selectedEvent = evt;
                Debug.Log($"[StressManager] Seçilen Event: {evt.eventData.targetStatName} (Ağırlık: {evt.currentWeight})");
                break;
            }
        }


        if (selectedEvent == null) selectedEvent = candidates.Last();
        StartEvent(selectedEvent);
    }

    private void StartEvent(UtilityEventWrapper evtWrapper)
    {
        evtWrapper.station.SetBroken();

        currentEventName.value = evtWrapper.eventData.targetStatName.ToString();

        isInCooldown = true;
        cooldownTimer = eventCooldownDuration;
        currentStress.value = 0f;
        passiveTimer = 0f;
        diceTimer = 0f;

        Debug.Log($"<color=red>EVENT TETİKLENDİ: {currentEventName.value}</color>");

        TriggerUIEffectRPC(currentEventName.value);
    }

    [ObserversRpc]
    private void TriggerUIEffectRPC(string eventName)
    {
        GlobalEvents.OnShowSystemMessage?.Invoke(eventName, true);
    }
}

[System.Serializable]
public class UtilityEventWrapper
{
    public StationController station;
    public UtilityEventData eventData;
    public float currentWeight;

    public UtilityEventWrapper(StationController station, UtilityEventData data)
    {
        this.station = station;
        this.eventData = data;
        this.currentWeight = data.baseWeight;
    }

    public void CalculateCurrentWeight(GameStatistics stats)
    {
        if (eventData != null) currentWeight = eventData.GetCurrentWeight(stats);
        else currentWeight = 5f;
    }
}