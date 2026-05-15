using UnityEngine;
using DG.Tweening;
using PurrNet;
using System;

public class LiftManager : NetworkBehaviour
{
    public static Action<Transform, float> OnDropItemToLıft;


    [System.Serializable]
    public struct LiftButtonData
    {
        public LiftButton button;
        public int targetFloorIndex;
    }

    [Header("Lift Setup")]
    [SerializeField] private LiftButtonData[] allLiftButtons;
    [SerializeField] private GameObject lift;
    [SerializeField] private LiftDoor[] liftDoors;

    [Header("Settings")]
    [SerializeField] private float liftSpeed = 2f;
    [SerializeField] private float xPosRange = 1f;
    [SerializeField] private float liftUpPosition = 1f;
    [SerializeField] private float liftDownPosition = 0f;
    [SerializeField] private bool isDisable;

    private int currentFloorIndex = 0;

    void Awake()
    {
        InventoryManager.OnEquipChange += ToggleInteractLift;
    }

    void Start()
    {
        if (isDisable)
        {
            foreach (var buttonData in allLiftButtons)
            {
                if (buttonData.button != null)
                    buttonData.button.GetComponent<Interactable>().SetInteractable(false);
            }
            return;
        }

        foreach (var buttonData in allLiftButtons)
        {
            if (buttonData.button != null)
            {
                buttonData.button.OnLiftButtonPressed += HandleLiftButtonPressed;

                if (!buttonData.button.GetComponent<Interactable>().CanInteract())
                {
                    currentFloorIndex = buttonData.targetFloorIndex;
                }
            }
        }

        liftDoors[currentFloorIndex].ToggleDoor(true);
        float targetY = currentFloorIndex == 0 ? liftDownPosition : liftUpPosition;
        lift.transform.localPosition = new Vector3(lift.transform.localPosition.x, targetY, lift.transform.localPosition.z);

        UpdateButtonInteractability(currentFloorIndex);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        foreach (var buttonData in allLiftButtons)
        {
            if (buttonData.button != null)
                buttonData.button.OnLiftButtonPressed -= HandleLiftButtonPressed;
        }
        InventoryManager.OnEquipChange -= ToggleInteractLift;
    }

    [ObserversRpc(runLocally: true)]
    private void HandleLiftButtonPressed(int targetFloorIndex)
    {
        if (currentFloorIndex == targetFloorIndex) return;

        SetAllButtonsInteractability(false);
        liftDoors[currentFloorIndex].ToggleDoor(false);

        float targetY = targetFloorIndex == 0 ? liftDownPosition : liftUpPosition;

        lift.transform.DOLocalMoveY(targetY, liftSpeed).SetEase(Ease.InOutSine).SetDelay(0.3f).OnComplete(() =>
        {
            currentFloorIndex = targetFloorIndex;
            liftDoors[currentFloorIndex].ToggleDoor(true);

            UpdateButtonInteractability(currentFloorIndex);
        });
    }

    private void UpdateButtonInteractability(int currentFloor)
    {
        foreach (var buttonData in allLiftButtons)
        {
            if (buttonData.button != null)
            {
                Interactable interactable = buttonData.button.GetComponent<Interactable>();

                bool shouldBeInteractable = (buttonData.targetFloorIndex != currentFloor);
                interactable.SetInteractable(shouldBeInteractable);

                if (!shouldBeInteractable)
                {
                    interactable.StopInteract();
                }
            }
        }
    }

    private void SetAllButtonsInteractability(bool state)
    {
        foreach (var buttonData in allLiftButtons)
        {
            if (buttonData.button != null)
            {
                Interactable interactable = buttonData.button.GetComponent<Interactable>();
                interactable.SetInteractable(state);
                if (!state) interactable.StopInteract();
            }
        }
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