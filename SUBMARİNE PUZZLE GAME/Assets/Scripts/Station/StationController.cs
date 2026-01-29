using System;
using PurrNet;
using UnityEngine;
using UnityEngine.Events;

public class StationController : NetworkBehaviour
{
    public Action<StationState, StationController> StateChanged;

    [SerializeField] private UtilityEventData utilityEventData;

    public StationType stationType;
    public StationTier stationTier;

    public float mistakeWaterPenalty = 3.0f;
    public float mistakeStressPenalty = 10.0f;
    public float repairStressReward = 15.0f;
    public float timeoutStressPenalty = 20.0f;

    [SerializeField] private SyncVar<StationState> stationState = new SyncVar<StationState>(StationState.Default);
    [SerializeField] private UnityEvent onStart;


    private Interactable[] interactables;

    void Awake()
    {
        stationState.onChanged += OnStateChanged;
        interactables = GetComponentsInChildren<Interactable>();
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        SetInteractable(false);

        if (isServer)
        {
            if (stationType == StationType.Utility)
            {
                GlobalEvents.OnRegisterUtilityStation?.Invoke(this, utilityEventData);
            }
            else if (stationType == StationType.Main)
            {
                GlobalEvents.OnRegisterMainStation?.Invoke(this);
            }
        }
    }



    private void OnStateChanged(StationState newState)
    {
        UpdateVisual(newState);
        StateChanged?.Invoke(newState, this);

        if (isServer)
        {
            GlobalEvents.OnStationStatusChanged?.Invoke(this, newState);
        }
    }

    private void SetInteractable(bool value)
    {
        foreach (var interactable in interactables)
        {
            interactable.SetInteractable(value);
        }
    }



    private void UpdateVisual(StationState state)
    {
        switch (state)
        {
            case StationState.Operational:
                SetInteractable(false);
                break;
            case StationState.Broken:
                SetInteractable(true);
                break;
            case StationState.Destroyed:
                StopInteraction();
                break;
            case StationState.Reparied:
                SetInteractable(false);
                break;
        }
    }

    public void StartStation()
    {
        onStart?.Invoke();
    }

    public StationState GetCurrentState()
    {
        return stationState;
    }

    public void SetOperational()
    {

        stationState.value = StationState.Operational;
    }

    public void SetBroken()
    {
        stationState.value = StationState.Broken;
        StartStation();
    }

    public void SetDestroyed()
    {
        stationState.value = StationState.Destroyed;

    }

    public void StopInteraction()
    {
        foreach (var interactable in interactables)
        {
            if (interactable.IsInteracting())
                interactable.GetComponent<ModuleInteraction>().StopInteract();
        }
        SetInteractable(false);

    }

    [ServerRpc(requireOwnership: false)]
    public void SetReparied()
    {
        if (stationState.value == StationState.Broken)
        {
            GlobalEvents.OnReduceStress?.Invoke(repairStressReward);
            stationState.value = StationState.Reparied;
            if (utilityEventData == null)
            {
                GlobalEvents.OnShowSystemMessage?.Invoke(gameObject.name, false);

            }
            else
            {
                string stationName = utilityEventData.targetStatName.ToString();
                GlobalEvents.OnShowSystemMessage?.Invoke(stationName, false);

            }


            Debug.Log($"<color=green>{gameObject.name} TAMİR EDİLDİ!</color>");
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void ReportRepairMistake()
    {
        GlobalEvents.OnAddFloodPenalty?.Invoke(mistakeWaterPenalty);
        GlobalEvents.OnAddStress?.Invoke(mistakeStressPenalty);

        Debug.Log($"<color=red>{gameObject.name} HATA YAPILDI!</color>");
    }

    [ServerRpc(requireOwnership: false)]
    public void AvoidMistake()
    {
        GlobalEvents.OnAddFloodPenalty?.Invoke(mistakeWaterPenalty / 2);
        GlobalEvents.OnAddStress?.Invoke(mistakeStressPenalty / 2);

        Debug.Log($"<color=red>{gameObject.name} HATA YAPILDI!</color>");
    }

    [ServerRpc(requireOwnership: false)]
    public void ReportTimeOutFailure()
    {
        GlobalEvents.OnAddStress?.Invoke(timeoutStressPenalty);

        Debug.Log($"<color=red>{gameObject.name} SÜRE DOLDU!</color>");
    }
}
