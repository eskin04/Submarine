using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public Sprite icon;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;
}