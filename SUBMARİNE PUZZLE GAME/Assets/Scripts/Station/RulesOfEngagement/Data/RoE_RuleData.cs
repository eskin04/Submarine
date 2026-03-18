using UnityEngine;

public enum RuleLogicType
{
    Pass_If_3rd_Object,                               // Seçilen 3. cisim ise pas geç.
    Pass_X,                                           // X cisimleri pas geç.
    Shoot_Y_In_Inner_Radar,                           // Radar iç merkezindeki Y cisimlerin hepsini vur.
    Shoot_Slowest,                                    // Cisimler arasında en yavaş yaklaşan cismi vur.
    Shoot_If_1st_Object,                              // Seçilen ilk cisim ise vur.
    Pass_Explosive_If_Water_Above_50,                 // Denizaltı su seviyesi %50’den yüksekse Patlayıcı cisimleri vurma.
    Shoot_Fastest_If_Multiple_And_Not_X,              // Radarda >1 cisim varsa, aralarındaki en hızlı cismi X değilse vur.
    Shoot_If_Faster_Than_3_And_Explosive_Or_Mechanical, // Cisim 3m/s’den hızlıysa ve Patlayıcı VEYA Mekanik ise vur.
    Shoot_X_ONLY_In_Inner_Radar,                      // X cisimleri SADECE radar iç merkezine girdiklerinde vur.
    Pass_If_Shares_Category_With_Previous,            // Önceki cisim ile aynı en az 1 kategorisi varsa pas geç.
    Shoot_Fastest_If_Water_Below_25,                  // Su seviyesi %25'in altındayken, ekranda bulunan en hızlı yaklaşan cismi vur.
    Shoot_X_If_Water_Below_25_Shoot_Y_If_Above_75,    // Su seviyesi %25'nin altında ise X vur, %75'nin üstünde ise Y vur.
    Shoot_Y_If_Previous_Passed_Was_X,                 // Pas geçilen önceki cisim X ise, sıradaki ilk Y cismi vur.
    Shoot_If_Shares_Category_With_Last_Two_And_Extra, // Önceki İKİ cisimle aynı kategoriye sahipse ve farklı bir kategorisi daha varsa vur.
    Pass_If_Previous_Destroyed_Was_X,                 // Vurulan önceki cisim X ise, sıradaki cismi pas geç.
    Shoot_Fastest_If_Not_X,                           // Cisimler arasında en hızlı cismi X değilse vur.
    Shoot_X_AND_Y,                                    // Cisim X VE Y ise vur.
    Shoot_X_OR_Y,                                     // Cisim X VEYA Y ise vur.
    Pass_Single_Category,                             // Tek kategorili cisimleri pas geç.
    Shoot_If_Slower_Than_4_And_Inner_Radar,           // Cisim 4m/s’den daha yavaşsa ve radar iç merkezindeyse vur.
    Shoot_X_If_Faster_Than_2,                         // 2m/s’den hızlı tüm X cisimleri vur.
    Shoot_Triple_Category                             // Üç kategorili cisimleri vur.
}

[CreateAssetMenu(fileName = "New_RoE_Rule", menuName = "RoE/Daily Rule")]
public class RoE_RuleData : ScriptableObject
{
    [TextArea]
    [Tooltip("Dinamik metinler için {X} ve {Y} kullanın.")]
    public string ruleDescription;

    public RuleLogicType logicType;

    [Tooltip("Radar iç merkezi varsayılan mesafesi (Örn: 50f)")]
    public float innerRadarDistanceThreshold = 50f;
}