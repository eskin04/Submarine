using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using DG.Tweening;

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
    private bool canOperate = true;
    private Vector3 initialRotation;

    void Awake()
    {
        initialRotation = book.localEulerAngles;
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
        }
    }

    private void ToggleBookAnim()
    {
        if (isOperate)
        {
            topCover.DOLocalRotate(Vector3.zero, .5f);
            sideCover.DOLocalRotate(new Vector3(-180, 0, 0), .5f);
            book.DOLocalMove(bookOpenPosition, .5f).OnComplete(() => isOperating = false);
            book.DOLocalRotate(bookOpenRotation, .5f);
            bookGroup.alpha = 1f;
            bookGroup.interactable = true;
            bookGroup.blocksRaycasts = true;


        }
        else
        {
            topCover.DOLocalRotate(new Vector3(90, 0, 0), .5f);
            sideCover.DOLocalRotate(new Vector3(-90, 0, 0), .5f);
            book.DOLocalMove(Vector3.zero, .5f).OnComplete(() => isOperating = false);
            book.DOLocalRotate(initialRotation, .5f);
            bookGroup.alpha = 0f;
            bookGroup.interactable = false;
            bookGroup.blocksRaycasts = false;

        }
    }

    public void CanOperate(bool canOperate)
    {
        this.canOperate = canOperate;
    }
}
