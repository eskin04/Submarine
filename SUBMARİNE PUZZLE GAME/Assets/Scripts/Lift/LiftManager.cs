using UnityEngine;
using DG.Tweening;
using PurrNet;
using System;
using System.Collections.Generic;

public class LiftManager : NetworkBehaviour
{
    public static Action<Transform, float> OnDropItemToLıft;
    [SerializeField] private LiftButton[] liftButtons;
    [SerializeField] private GameObject lift;
    [SerializeField] private LiftDoor[] liftDoors; // 0 is lower, 1 is upper
    [SerializeField] private float liftSpeed = 2f;
    [SerializeField] private float xPosRange = 1f;
    [SerializeField] private float liftUpPosition = 1f;
    [SerializeField] private float liftDownPosition = 0f;
    [SerializeField] private bool isDisable;
    private Interactable currentActiveButton;
    private int currentFloorIndex = 0;


    void Awake()
    {
        InventoryManager.OnEquipChange += ToggleInteractLift;

    }

    void Start()
    {
        if (isDisable)
        {
            List<Interactable> interactables = new List<Interactable>(GetComponentsInChildren<Interactable>());
            foreach (var interactable in interactables)
            {
                interactable.SetInteractable(false);
            }
            return;
        }

        foreach (var button in liftButtons)
        {
            button.OnLiftButtonPressed += HandleLiftButtonPressed;
            if (!button.GetComponent<Interactable>().CanInteract())
            {
                currentFloorIndex = Array.IndexOf(liftButtons, button);
                liftDoors[currentFloorIndex].ToggleDoor(true);
                currentActiveButton = button.GetComponent<Interactable>();
                float targetX = currentFloorIndex == 0 ? liftDownPosition : liftUpPosition;
                lift.transform.localPosition = new Vector3(targetX, lift.transform.localPosition.y, lift.transform.localPosition.z);
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        foreach (var button in liftButtons)
        {
            button.OnLiftButtonPressed -= HandleLiftButtonPressed;
        }
        InventoryManager.OnEquipChange -= ToggleInteractLift;

    }

    [ObserversRpc(runLocally: true)]
    private void HandleLiftButtonPressed(int floorIndex)
    {
        liftButtons[floorIndex].GetComponent<Interactable>().SetInteractable(false);
        liftButtons[floorIndex].GetComponent<Interactable>().StopInteract();
        liftDoors[currentFloorIndex].ToggleDoor(false);



        float targetX = floorIndex == 0 ? liftDownPosition : liftUpPosition;
        lift.transform.DOLocalMoveX(targetX, liftSpeed).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            currentActiveButton.SetInteractable(true);
            currentActiveButton = liftButtons[floorIndex].GetComponent<Interactable>();
            currentFloorIndex = floorIndex;
            liftDoors[currentFloorIndex].ToggleDoor(true);
        });


    }

    private void ToggleInteractLift(bool isEquipped)
    {
        lift.GetComponent<Interactable>().SetInteractable(isEquipped);
    }

    public void LiftInteract()
    {
        OnDropItemToLıft?.Invoke(lift.transform, xPosRange);
        lift.GetComponent<Interactable>().StopInteract();
    }
}
