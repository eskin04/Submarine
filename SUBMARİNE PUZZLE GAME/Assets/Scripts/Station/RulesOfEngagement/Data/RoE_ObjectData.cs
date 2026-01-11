using UnityEngine;
using System.Collections.Generic;

public enum ObjectCategory
{
    Biological, // Biyolojik
    Mechanical, // Mekanik
    Glowing,    // Işıldayan
    Explosive,  // Patlayıcı
    Valuable    // Değerli
}

[CreateAssetMenu(fileName = "New_RoE_Object", menuName = "RoE/Object Data")]
public class RoE_ObjectData : ScriptableObject
{
    [Header("Identity")]
    public string objectName;
    public Sprite polaroidImage;

    [Header("Handbook Stats")]
    public List<ObjectCategory> categories;
}