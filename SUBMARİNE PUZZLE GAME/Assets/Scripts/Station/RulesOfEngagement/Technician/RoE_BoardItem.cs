using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Localization;

public class RoE_BoardItem : MonoBehaviour
{
    public Image objectImage;
    public TMP_Text objectName;
    public Image[] symbolSlots;
    private LocalizedString uiLocalizeString = new LocalizedString();

    void OnEnable()
    {
        uiLocalizeString.StringChanged += UpdateTranslatedName;
    }

    private void OnDestroy()
    {
        uiLocalizeString.StringChanged -= UpdateTranslatedName;
    }

    public void Setup(RoE_ObjectData data, List<DecryptionSymbol> symbols)
    {
        if (data.polaroidImage)
            objectImage.sprite = data.polaroidImage;


        uiLocalizeString.TableReference = data.objectName.TableReference;
        uiLocalizeString.TableEntryReference = data.objectName.TableEntryReference;

        for (int i = 0; i < symbolSlots.Length; i++)
        {
            if (i < symbols.Count)
            {
                symbolSlots[i].sprite = symbols[i].icon;
                symbolSlots[i].enabled = true;
            }
            else
            {
                symbolSlots[i].enabled = false;
            }
        }
    }

    private void UpdateTranslatedName(string translatedText)
    {
        if (objectName != null)
        {
            objectName.text = translatedText;
        }
    }


}