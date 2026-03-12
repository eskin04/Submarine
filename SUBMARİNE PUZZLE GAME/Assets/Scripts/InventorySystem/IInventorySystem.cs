using UnityEngine;

public interface IInventoryItem
{
    void OnEquip();
    void OnUnequip();
    void OnDrop();
    void CanOperate(bool canOperate);
}

public interface ILiftInteractable
{
    Transform GetTransform();
}