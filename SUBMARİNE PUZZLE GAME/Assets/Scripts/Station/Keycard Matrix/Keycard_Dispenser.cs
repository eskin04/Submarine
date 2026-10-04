using System.Collections.Generic;
using UnityEngine;
using PurrNet;
using DG.Tweening;

public class Keycard_Dispenser : NetworkBehaviour
{
    [Header("Referances")]
    public GameObject keycardPrefab;

    public Transform[] cardSlots;

    [Header("Animation Settings")]
    public float animDuration = 0.4f;
    public float delayBetweenCards = 0.2f;
    public Ease animEase = Ease.OutBack;

    public void DispenseCards(List<CardData> cardsToSpawn)
    {
        if (!isServer) return;

        for (int i = 0; i < cardsToSpawn.Count; i++)
        {
            if (i >= cardSlots.Length)
            {
                Debug.LogWarning("[Dispenser] Yeterli yuva yok, fazla kartlar spawn edilemedi!");
                break;
            }

            Transform targetSlot = cardSlots[i];

            GameObject newCardObj = Instantiate(keycardPrefab, targetSlot.position, targetSlot.rotation);

            newCardObj.transform.SetParent(targetSlot);

            Keycard_Item itemScript = newCardObj.GetComponent<Keycard_Item>();
            if (itemScript != null)
            {
                itemScript.InitializeCard(cardsToSpawn[i]);
            }

            Rigidbody rb = newCardObj.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            // Socket/held cards disable NT. Never spawn their networked root at
            // zero scale: the host-only tween would leave observers invisible.
            newCardObj.transform.localScale = Vector3.one;
            if (newCardObj.transform.childCount > 0)
            {
                // The existing prefab has one visual-model child. Animate that
                // child locally, leaving identity, collider and shared scale stable.
                Transform visual = newCardObj.transform.GetChild(0);
                Vector3 visualScale = visual.localScale;
                visual.localScale = Vector3.zero;
                visual.DOScale(visualScale, animDuration)
                    .SetDelay(i * delayBetweenCards)
                    .SetEase(animEase);
            }

            Keycard_Socket socketScript = targetSlot.GetComponent<Keycard_Socket>();
            RpcInitializeSocket(socketScript, itemScript);
        }
    }

    [ObserversRpc(runLocally: true)]
    private void RpcInitializeSocket(Keycard_Socket socket, Keycard_Item item)
    {
        if (socket != null)
        {
            socket.InitializeSocket(item);
        }
    }
}
