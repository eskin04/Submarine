using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using DG.Tweening;
using FMODUnity;
using UnityEngine.UI;

public class Handbook : NetworkBehaviour, IInventoryItem
{
    [SerializeField] private KeyCode operateKey = KeyCode.Mouse0;
    [SerializeField] private Transform book, sideCover, topCover;
    [SerializeField] private CanvasGroup bookGroup;
    [SerializeField] private Vector3 bookOpenPosition;
    [SerializeField] private Vector3 bookOpenRotation;
    private bool isEquipped = false;
    private bool isOperate = false;
    public bool IsOperate => isOperate;
    private bool isOperating = false;
    public bool IsOperating => isOperating;
    public float flipDuration = 0.5f;


    [Header("Audio Settings")]
    [SerializeField] private AudioEventChannelSO _channel;
    [SerializeField] private EventReference _openSound;
    [SerializeField] private EventReference _closeSound;
    private bool canOperate = true;
    private Vector3 initialRotation;
    private Transform inspectPosition;
    private Transform originalParent;

    void Awake()
    {
        initialRotation = book.localEulerAngles;
        originalParent = book.parent;

    }

    public void SetInspectPosition(Transform inspectPosition)
    {
        this.inspectPosition = inspectPosition;
    }
    public void OnDrop()
    {
        isEquipped = false;
        isOperate = false;
        ToggleBookAnim();
    }

    public void OnEquip()
    {
        isEquipped = true;

    }

    public void OnUnequip()
    {
        isEquipped = false;
        isOperate = false;
        ToggleBookAnim();

    }

    void LateUpdate()
    {
        if (!isEquipped || !canOperate) return;

        if (isOwner && Input.GetKeyDown(operateKey) && !isOperating)
        {
            isOperate = !isOperate;
            isOperating = true;
            ToggleBookAnim();
            ToggleSound();
        }
    }

    private void ToggleBookAnim()
    {
        InventoryManager invManager = GetComponentInParent<InventoryManager>();
        if (isOperate)
        {
            book.SetParent(inspectPosition);
            topCover.DOLocalRotate(Vector3.zero, flipDuration);
            sideCover.DOLocalRotate(new Vector3(-180, 0, 0), flipDuration);
            book.DOLocalMove(bookOpenPosition, flipDuration).OnComplete(() => isOperating = false);
            book.DOLocalRotate(bookOpenRotation, flipDuration);
            bookGroup.alpha = 1f;
            bookGroup.interactable = true;
            bookGroup.blocksRaycasts = true;
            if (invManager != null) invManager.IsScrollLocked = true;

        }
        else
        {
            book.SetParent(originalParent);
            topCover.DOLocalRotate(new Vector3(90, 0, 0), flipDuration);
            sideCover.DOLocalRotate(new Vector3(-90, 0, 0), flipDuration);
            book.DOLocalMove(Vector3.zero, flipDuration).OnComplete(() => isOperating = false);
            book.DOLocalRotate(initialRotation, flipDuration);
            bookGroup.alpha = 0f;
            bookGroup.interactable = false;
            bookGroup.blocksRaycasts = false;
            if (invManager != null) invManager.IsScrollLocked = false;

        }
    }

    private void ToggleSound()
    {
        if (isOperate)
        {
            PlaySound(_openSound);
        }
        else
        {
            PlaySound(_closeSound);
        }
    }

    private void PlaySound(EventReference sound)
    {
        if (_channel != null && !sound.IsNull)
        {
            AudioEventPayload payload = new AudioEventPayload(sound, transform.position);
            _channel.RaiseEvent(payload);
        }
    }

    public void CanOperate(bool canOperate)
    {
        this.canOperate = canOperate;
    }
}
