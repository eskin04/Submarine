using System;
using PurrNet;
using UnityEngine;
using UnityEngine.Events;

public class StationController : NetworkBehaviour
{
    public Action<StationState, StationController> StateChanged;
    public StationType stationType;
    public StationTier stationTier;
    [SerializeField] private SyncVar<StationState> stationState = new SyncVar<StationState>(StationState.Default);
    [SerializeField] private UnityEvent onStart;


    private Interactable[] interactables;

    void Awake()
    {
        stationState.onChangedWithOld += OnStateChanged;
        interactables = GetComponentsInChildren<Interactable>();
    }

    private void OnStateChanged(StationState oldState, StationState newState)
    {
        UpdateVisual(newState);
        StateChanged.Invoke(newState, this);
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
    }

    public void SetDestroyed()
    {
        stationState.value = StationState.Destroyed;
    }

    [ServerRpc(requireOwnership: false)]
    public void SetReparied()
    {
        if (stationState.value == StationState.Broken)
            stationState.value = StationState.Reparied;
    }

    [ServerRpc(requireOwnership: false)]
    public void ReportRepairMistake(float penaltyAmount)
    {
        if (InstanceHandler.TryGetInstance<FloodManager>(out FloodManager instance))
            instance.AddPenalty(penaltyAmount);
    }
}
