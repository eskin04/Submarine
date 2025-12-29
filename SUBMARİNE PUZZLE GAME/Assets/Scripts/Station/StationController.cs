using System;
using System.Collections.Generic;
using PurrNet;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class StationController : NetworkBehaviour
{
    public Action<StationState, StationController> StateChanged;

    [SerializeField] private StationType stationType;
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
}
