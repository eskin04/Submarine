using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using DG.Tweening;

public class Handbook : NetworkBehaviour, IInventoryItem
{
    [SerializeField] private KeyCode operateKey = KeyCode.Mouse0;
    [SerializeField] private Transform book, sideCover, topCover;
    [SerializeField] private GameObject bookUI;
    private bool amIOwner;
    private bool isEquipped = false;
    private bool isOperate = false;
    public bool IsOperate => isOperate;
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

    void Update()
    {
        if (!isEquipped) return;

        if (isOwner && Input.GetKeyDown(operateKey))
        {
            isOperate = !isOperate;
            ToggleBookAnim();
        }
    }

    private void ToggleBookAnim()
    {
        if (isOperate)
        {
            topCover.DOLocalRotate(Vector3.zero, .5f);
            sideCover.DOLocalRotate(new Vector3(-180, 0, 0), .5f);
            book.DOLocalMove(new Vector3(-.7f, .4f, -.6f), .5f);
            bookUI.SetActive(true);


        }
        else
        {
            topCover.DOLocalRotate(new Vector3(90, 0, 0), .5f);
            sideCover.DOLocalRotate(new Vector3(-90, 0, 0), .5f);
            book.DOLocalMove(Vector3.zero, .5f);
            bookUI.SetActive(false);

        }
    }


}
