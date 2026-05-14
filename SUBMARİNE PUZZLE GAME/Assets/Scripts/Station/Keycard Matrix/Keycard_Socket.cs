using UnityEngine;
using PurrNet;

public enum SocketType { Dispenser, Technician, Engineer, Tester }

[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(Collider))]
public class Keycard_Socket : NetworkBehaviour
{
    public SocketType type;
    public int socketIndex;
    public Keycard_StationManager stationManager;

    public Keycard_Item slottedCard;

    private Interactable interactable;
    private Collider socketCollider;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        socketCollider = GetComponent<Collider>();
        InitializeSocket();
    }

    public void InitializeSocket(Keycard_Item preSlottedCard = null)
    {
        slottedCard = preSlottedCard;
        UpdateSocketState();
    }

    private void Update()
    {
        if (!isServer) return;

        if (slottedCard != null)
        {
            if (slottedCard.transform.parent != this.transform || !slottedCard.gameObject.activeSelf)
            {
                ServerHandleCardRemoved();
            }
        }
    }
    public void HandleInteraction()
    {
        if (stationManager == null || !stationManager.isRoundActive)
            return;

        if (slottedCard == null)
        {
            TryInsertCard();
        }
    }

    private void TryInsertCard()
    {
        Debug.Log($"<color=cyan>[Keycard Socket] {type} Socket {socketIndex} ile etkileşim kuruldu. Kart takılmaya çalışılıyor...</color>");
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return;
        interactable.StopInteract();
        GameObject heldObj = inv.GetCurrentHeldObject();
        if (heldObj == null) return;

        Keycard_Item keycard = heldObj.GetComponent<Keycard_Item>();
        if (keycard != null)
        {
            inv.ExtractCurrentHeldItem();
            CmdPlaceCardInSocket(keycard.gameObject, keycard.myData.CardID);
        }
    }


    private void ServerHandleCardRemoved()
    {
        slottedCard = null;

        if (type == SocketType.Engineer && stationManager != null)
            stationManager.EngineerRemoveCardRPC();
        else if (type == SocketType.Technician && stationManager != null)
            stationManager.TechnicianRemoveCardRPC(socketIndex);
        else if (type == SocketType.Tester && stationManager != null)
            stationManager.TesterRemoveCardRPC(socketIndex);

        RpcClearSocket();
    }


    [ServerRpc(requireOwnership: false)]
    private void CmdPlaceCardInSocket(GameObject cardObj, int cardID)
    {
        RpcPlaceCardInSocket(cardObj);

        if (type == SocketType.Engineer && stationManager != null)
            stationManager.EngineerInsertCardRPC(cardID);
        else if (type == SocketType.Technician && stationManager != null)
            stationManager.TechnicianInsertCardRPC(cardID, socketIndex);
        else if (type == SocketType.Tester && stationManager != null)
            stationManager.TesterInsertCardRPC(cardID, socketIndex);
    }

    [ObserversRpc]
    private void RpcPlaceCardInSocket(GameObject cardObj)
    {
        slottedCard = cardObj.GetComponent<Keycard_Item>();
        interactable.StopInteract();

        cardObj.transform.SetParent(this.transform);
        cardObj.transform.localPosition = Vector3.zero;
        cardObj.transform.localRotation = Quaternion.identity;
        cardObj.GetComponent<Collider>().enabled = true;
        cardObj.SetActive(true);
        Rigidbody rb = cardObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        UpdateSocketState();
    }

    [ObserversRpc]
    private void RpcClearSocket()
    {
        slottedCard = null;
        UpdateSocketState();

    }

    private void UpdateSocketState()
    {
        if (interactable == null || socketCollider == null) return;

        if (slottedCard != null)
        {
            interactable.SetInteractable(false);
            socketCollider.enabled = false;
        }
        else
        {
            interactable.SetInteractable(true);
            socketCollider.enabled = true;
        }
    }
}