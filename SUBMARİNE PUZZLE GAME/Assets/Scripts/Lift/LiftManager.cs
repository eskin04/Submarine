using UnityEngine;
using DG.Tweening;
using PurrNet;
using System;

public class LiftManager : NetworkBehaviour
{
    public static Action<Transform, float> OnDropItemToLıft;
    [SerializeField] private LiftButton[] liftButtons;
    [SerializeField] private GameObject lift;
    [SerializeField] private float liftyPosOffset = 1f;
    [SerializeField] private float liftSpeed = 2f;
    [SerializeField] private float xPosRange = 1f;
    private Interactable currentActiveButton;


    void Awake()
    {
        InventoryManager.OnEquipChange += ToggleInteractLift;
    }

    void Start()
    {

        foreach (var button in liftButtons)
        {
            button.OnLiftButtonPressed += HandleLiftButtonPressed;
            if (!button.GetComponent<Interactable>().CanInteract())
            {
                currentActiveButton = button.GetComponent<Interactable>();
                lift.transform.position = new Vector3(lift.transform.position.x, button.transform.position.y + liftyPosOffset, lift.transform.position.z);
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
        float liftTime = Mathf.Abs(lift.transform.position.y - (liftButtons[floorIndex].transform.position.y + liftyPosOffset)) / liftSpeed;
        lift.transform.DOMoveY(liftButtons[floorIndex].transform.position.y + liftyPosOffset, liftTime).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            currentActiveButton.SetInteractable(true);
            currentActiveButton = liftButtons[floorIndex].GetComponent<Interactable>();
        });
    }

    private void ToggleInteractLift(bool isEquipped)
    {
        lift.GetComponent<Interactable>().SetInteractable(isEquipped);
    }

    public void LiftInteract()
    {
        OnDropItemToLıft(lift.transform, xPosRange);
        lift.GetComponent<Interactable>().StopInteract();
    }
}
