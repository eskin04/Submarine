using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class RoE_HandbookItem : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI typeText2;
    public TextMeshProUGUI typeText3;

    public void Setup(string objName, List<ObjectCategory> shuffledCats)
    {
        nameText.text = objName;

        typeText.text = shuffledCats.Count > 0 ? shuffledCats[0].ToString() : "";
        typeText2.text = shuffledCats.Count > 1 ? shuffledCats[1].ToString() : "";

        if (typeText3 != null)
            typeText3.text = shuffledCats.Count > 2 ? shuffledCats[2].ToString() : "";
    }
}