using System;
using PurrNet;
using UnityEngine;

public class StationController : NetworkBehaviour
{
    public Action<StationState, StationController> StateChanged;

    [SerializeField] private StationType stationType;
    [SerializeField] private SyncVar<StationState> stationState = new SyncVar<StationState>(StationState.Default);
    [SerializeField] private Material material;
    private Interactable interactable;

    void Awake()
    {
        stationState.onChangedWithOld += OnStateChanged;
        interactable = GetComponent<Interactable>();
    }

    private void OnStateChanged(StationState oldState, StationState newState)
    {
        UpdateVisual(newState);
        StateChanged.Invoke(newState, this);
    }



    private void UpdateVisual(StationState state)
    {
        switch (state)
        {
            case StationState.Operational:
                material.color = Color.blue;
                interactable.SetInteractable(false);
                break;
            case StationState.Broken:
                material.color = Color.red;
                interactable.SetInteractable(true);

                break;
            case StationState.Destroyed:
                material.color = Color.black;
                interactable.SetInteractable(false);
                break;
        }
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
