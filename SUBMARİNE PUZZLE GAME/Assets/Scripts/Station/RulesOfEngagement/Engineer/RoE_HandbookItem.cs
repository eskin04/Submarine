using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoE_HandbookItem : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI typeText2;

    public void Setup(RoE_ObjectData data)
    {
        nameText.text = data.objectName;



        typeText.text = data.categories[0].ToString();
        if (data.categories.Count > 1)
        {
            typeText2.text = data.categories[1].ToString();
        }


    }
}