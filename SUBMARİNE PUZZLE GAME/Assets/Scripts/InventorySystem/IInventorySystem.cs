using UnityEngine;

public interface IInventoryItem
{
    void OnEquip();
    void OnUnequip();
    void OnDrop();
}

public interface ILiftInteractable
{
    Transform GetTransform();
}