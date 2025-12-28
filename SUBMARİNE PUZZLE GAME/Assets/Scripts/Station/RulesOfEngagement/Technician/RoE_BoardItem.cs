using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class RoE_BoardItem : MonoBehaviour
{
    public Image objectImage;
    public TMP_Text objectName;
    public Image[] symbolSlots;

    public void Setup(RoE_ObjectData data, List<DecryptionSymbol> symbols)
    {
        if (data.polaroidImage)
            objectImage.sprite = data.polaroidImage;

        objectName.text = data.objectName;


        for (int i = 0; i < symbolSlots.Length; i++)
        {
            if (i < symbols.Count)
            {
                symbolSlots[i].sprite = symbols[i].icon;

                switch (symbols[i].color)
                {
                    case DecryptionSymbol.Color.Red: symbolSlots[i].color = Color.red; break;
                    case DecryptionSymbol.Color.Green: symbolSlots[i].color = Color.green; break;
                    case DecryptionSymbol.Color.Blue: symbolSlots[i].color = Color.blue; break;
                }

                symbolSlots[i].enabled = true;
            }
            else
            {
                symbolSlots[i].enabled = false;
            }
        }
    }
}