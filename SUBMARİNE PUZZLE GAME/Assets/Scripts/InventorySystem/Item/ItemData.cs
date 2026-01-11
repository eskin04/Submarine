using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;        // UI'da görünecek resim
    public GameObject prefab;  // Yere atınca oluşacak 3D model
}