using UnityEngine;

public enum RuleLogicType
{
    Shoot_Only_Mechanical,              // Sadece Mekanik
    Shoot_Only_Biological,              // Sadece Biyolojik
    Shoot_Only_Glowing,                 // Sadece Işıldayan
    Shoot_All_Except_Valuable,          // Değerli dışındaki her şey
    Shoot_Biological_AND_Glowing,       // Hem Biyolojik Hem Işıldayan
    Shoot_Biological_OR_Glowing,        // Biyolojik VEYA Işıldayan
    Shoot_All_Explosive,                // Tüm Patlayıcılar
    Shoot_Explosive_OR_Glowing,         // Patlayıcı VEYA Işıldayan
    Shoot_Mechanical_OR_Explosive,      // Mekanik VEYA Patlayıcı
    Shoot_Glowing_Except_Valuable,      // Değerli olmayan Işıldayanlar
    Shoot_All_Except_Biological,        // Biyolojik olmayan her şey
    Shoot_Only_Single_Category,         // Sadece tek kategorisi olanlar
    Shoot_Only_Double_Category,         // Sadece çift kategorisi olanlar
    Shoot_Explosive_AND_Mechanical,     // Hem Patlayıcı Hem Mekanik
    Shoot_All_Except_Mechanical         // Mekanik harici her şey
}

[CreateAssetMenu(fileName = "New_RoE_Rule", menuName = "RoE/Daily Rule")]
public class RoE_RuleData : ScriptableObject
{
    [TextArea]
    public string ruleDescription;
    public RuleLogicType logicType;
}