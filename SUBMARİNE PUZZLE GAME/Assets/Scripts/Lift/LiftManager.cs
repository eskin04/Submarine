using UnityEngine;
using DG.Tweening;
using PurrNet;
using System;
using FMODUnity;
using PurrLobby;

public class LiftManager : NetworkBehaviour
{
    public static Action<Transform, float> OnDropItemToLıft;
    public static Action<bool> OnItemInElevator;


    [System.Serializable]
    public struct LiftButtonData
    {
        public LiftButton button;
        public int targetFloorIndex;
        public InteractionIndicator indicator;
    }

    [Header("Lift Setup")]
    [SerializeField] private LiftButtonData[] allLiftButtons;
    [SerializeField] private GameObject lift;
    [SerializeField] private LiftDoor[] liftDoors;
    [SerializeField] private Light liftLight;

    [Header("Settings")]
    [SerializeField] private float liftSpeed = 2f;
    [SerializeField] private float xPosRange = 1f;
    [SerializeField] private float liftUpPosition = 1f;
    [SerializeField] private float liftDownPosition = 0f;
    [SerializeField] private bool isDisable;

    [Header("Audio Settings")]
    [SerializeField] private AudioEventChannelSO sfxChannel;
    [SerializeField] private EventReference liftMoveSound;
    [SerializeField] private EventReference liftArriveSound;

    [Header("Tutorial Indicators")]
    [SerializeField] private InteractionIndicator[] liftFrameIndicators;

    private FMODEmitter _activeMoveEmitter;

    private int currentFloorIndex = 0;
    private bool isElevatorTutorialLocked = false;
    private bool isButtonTutorialLocked = false;
    private bool isLiftTravelPending;
    private Coroutine settlingWait;

    void Awake()
    {
        InventoryManager.OnEquipChange += ToggleInteractLift;
        OnItemInElevator += HandleLightState;
        TutorialQuestView.OnTaskUnlockedLocal += HandleTutorialTaskUnlocked;

    }

    void Start()
    {
        if (InstanceHandler.TryGetInstance<TutorialQuestView>(out _))
        {
            isElevatorTutorialLocked = true;
            isButtonTutorialLocked = true;
        }
        if (isDisable)
        {
            SetAllButtonsInteractability(false);
        }
        else
        {
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
        }

        liftDoors[currentFloorIndex].ToggleDoor(true);
        float targetY = currentFloorIndex == 0 ? liftDownPosition : liftUpPosition;
        lift.transform.localPosition = new Vector3(lift.transform.localPosition.x, targetY, lift.transform.localPosition.z);

        if (!isDisable) UpdateButtonInteractability(currentFloorIndex);
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
        OnItemInElevator -= HandleLightState;
        TutorialQuestView.OnTaskUnlockedLocal -= HandleTutorialTaskUnlocked;

        if (_activeMoveEmitter != null)
        {
            _activeMoveEmitter.StopSound();
            _activeMoveEmitter = null;
        }
    }

    private void HandleTutorialTaskUnlocked(TutorialAction actionType)
    {
        if (actionType == TutorialAction.UseElevator)
        {
            isElevatorTutorialLocked = false;
            bool hasItem = InventoryManager.LocalPlayer != null && InventoryManager.LocalPlayer.GetCurrentHeldObject() != null;
            ToggleInteractLift(hasItem);

            if (liftFrameIndicators != null && liftFrameIndicators.Length > currentFloorIndex)
                liftFrameIndicators[currentFloorIndex]?.Show();
        }
        else if (actionType == TutorialAction.PickUpItem)
        {
            isElevatorTutorialLocked = false;
            bool hasItem = InventoryManager.LocalPlayer != null && InventoryManager.LocalPlayer.GetCurrentHeldObject() != null;
            ToggleInteractLift(hasItem);
        }
        else if (actionType == TutorialAction.SendElevatorItem)
        {
            isButtonTutorialLocked = false;
            UpdateButtonInteractability(currentFloorIndex);

            foreach (var btn in allLiftButtons)
            {
                if (btn.targetFloorIndex != currentFloorIndex) btn.indicator?.Show();
            }
        }
    }

    private void HandleLightState(bool _)
    {
        // Transfers publish after the server commits possession and parenting.
        // Replica hierarchies may still show the removed item when the RPC arrives.
        if (isServer) RpcApplyLightState(HasLiftCargo());
    }

    private bool HasLiftCargo()
    {
        foreach (var item in lift.GetComponentsInChildren<ItemLoot>(true))
            if (item.isSpawned && item.Possession.Location == ItemSharedLocation.World &&
                item.transform.parent == lift.transform) return true;
        return false;
    }

    private void LateUpdate()
    {
        // Despawn/teardown can remove cargo without the pickup transfer event.
        if (!isServer) return;
        bool occupied = HasLiftCargo();
        if (liftLight.enabled != occupied) RpcApplyLightState(occupied);
    }

    [ObserversRpc(runLocally: true)]
    private void RpcApplyLightState(bool isInLift)
    {
        if (liftLight.enabled == isInLift)
            return;
        if (isInLift)
        {
            liftLight.enabled = true;

            if (!isButtonTutorialLocked)
            {
                foreach (var btn in allLiftButtons)
                {
                    if (btn.targetFloorIndex != currentFloorIndex) btn.indicator?.Show();
                }
            }
        }
        else
        {
            liftLight.enabled = false;
            if (liftFrameIndicators != null && liftFrameIndicators.Length > currentFloorIndex)
                liftFrameIndicators[currentFloorIndex]?.Hide();
        }
    }

    private void HandleLiftButtonPressed(int targetFloorIndex)
    {
        TutorialManager questOwner = null;
        int questIndex = 0;
        ulong entryId = 0;
        if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var view))
            view.TryGetQuestContext(out questOwner, out questIndex, out entryId);
        RpcHandleLiftButtonPressed(targetFloorIndex, questOwner, questIndex, entryId);
    }

    [ObserversRpc(runLocally: true)]
    private void RpcHandleLiftButtonPressed(int targetFloorIndex, TutorialManager questOwner, int questIndex, ulong entryId)
    {
        if (currentFloorIndex == targetFloorIndex || isLiftTravelPending) return;
        isLiftTravelPending = true;
        lift.GetComponent<Interactable>().SetInteractable(false);

        SetAllButtonsInteractability(false);
        liftDoors[currentFloorIndex].ToggleDoor(false);
        foreach (var btn in allLiftButtons) btn.indicator?.Hide();
        if (liftFrameIndicators != null && liftFrameIndicators.Length > currentFloorIndex)
            liftFrameIndicators[currentFloorIndex]?.Hide();

        if (PlayerStats.LocalInstance != null)
        {
            PlayerRole localRole = PlayerStats.LocalInstance.Role;
            bool isSender = (currentFloorIndex == 0 && localRole == PlayerRole.Technician) ||
                            (currentFloorIndex == 1 && localRole == PlayerRole.Engineer);

            if (isSender)
            {
                if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var view))
                {
                    view.OnActionPerformed(TutorialAction.SendElevatorItem, questOwner, questIndex, entryId);
                }
            }
        }

        if (isServer && TutorialInputManager.Instance != null)
        {
            PlayerRole targetRole = currentFloorIndex == 0 ? PlayerRole.Engineer : PlayerRole.Technician;
            TutorialInputManager.Instance.CompleteTaskForPlayerServerRpc(questOwner, questIndex, entryId,
                (int)targetRole, (int)TutorialAction.WaitForPartner);
        }

        if (isServer) settlingWait = StartCoroutine(WaitForSettledCargo(targetFloorIndex));
    }

    private System.Collections.IEnumerator WaitForSettledCargo(int targetFloorIndex)
    {
        while (isSpawned && isServer)
        {
            bool settled = true;
            foreach (var item in lift.GetComponentsInChildren<ItemLoot>())
                if (!item.TrySecureLiftTransport(lift.transform)) settled = false;
            if (settled)
            {
                settlingWait = null;
                RpcStartLiftTravel(targetFloorIndex);
                yield break;
            }
            yield return null;
        }
        settlingWait = null;
        isLiftTravelPending = false;
    }

    [ObserversRpc(runLocally: true)]
    private void RpcStartLiftTravel(int targetFloorIndex)
    {
        float targetY = targetFloorIndex == 0 ? liftDownPosition : liftUpPosition;
        StartLiftAudio();

        lift.transform.DOLocalMoveY(targetY, liftSpeed).SetEase(Ease.InOutSine).SetDelay(0.3f).OnComplete(() =>
        {
            currentFloorIndex = targetFloorIndex;
            isLiftTravelPending = false;
            HandleLiftArrival();

            liftDoors[currentFloorIndex].ToggleDoor(true);

            UpdateButtonInteractability(currentFloorIndex);
            ToggleInteractLift(InventoryManager.LocalPlayer != null && InventoryManager.LocalPlayer.GetCurrentHeldObject() != null);
        });
    }

    private void OnDisable()
    {
        CancelSettlingWait();
    }

    protected override void OnDespawned(bool asServer)
    {
        if (asServer) CancelSettlingWait();
        base.OnDespawned(asServer);
    }

    private void CancelSettlingWait()
    {
        if (settlingWait != null) StopCoroutine(settlingWait);
        settlingWait = null;
        isLiftTravelPending = false;
    }

    private void UpdateButtonInteractability(int currentFloor)
    {
        if (isButtonTutorialLocked)
        {
            SetAllButtonsInteractability(false);
            return;
        }
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
        if (isElevatorTutorialLocked)
        {
            lift.GetComponent<Interactable>().SetInteractable(false);
            return;
        }
        lift.GetComponent<Interactable>().SetInteractable(isEquipped && !isLiftTravelPending);
    }

    public void LiftInteract()
    {
        if (isLiftTravelPending) return;

        OnDropItemToLıft?.Invoke(lift.transform, xPosRange);
        lift.GetComponent<Interactable>().StopInteract();
        if (liftFrameIndicators != null && liftFrameIndicators.Length > currentFloorIndex)
            liftFrameIndicators[currentFloorIndex]?.Hide();
    }


    private void StartLiftAudio()
    {
        if (_activeMoveEmitter == null && !liftMoveSound.IsNull && lift != null)
        {
            _activeMoveEmitter = AudioManager.Instance.PlayLoopingOrAttachedSound(liftMoveSound, lift.transform);
        }
    }

    private void HandleLiftArrival()
    {
        if (_activeMoveEmitter != null)
        {
            _activeMoveEmitter.StopSound();
            _activeMoveEmitter = null;
        }

        if (sfxChannel != null && !liftArriveSound.IsNull && lift != null)
        {
            sfxChannel.RaiseEvent(new AudioEventPayload(liftArriveSound, lift.transform.position));
        }

        if (!isElevatorTutorialLocked)
        {
            if (liftFrameIndicators != null && liftFrameIndicators.Length > currentFloorIndex)
                liftFrameIndicators[currentFloorIndex]?.Show();
        }


    }
}
