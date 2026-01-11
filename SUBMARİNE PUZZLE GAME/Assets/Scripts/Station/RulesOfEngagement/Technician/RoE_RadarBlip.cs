using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class RoE_RadarBlip : MonoBehaviour
{
    [Header("References")]
    public Button btn;
    public Image blipImage;
    public TextMeshProUGUI codeText;

    [Header("Visual Settings")]
    public Color defaultColor = Color.red;
    public Color selectedColor = Color.green;
    public float defaultScale = 1.0f;
    public float selectedScale = 1.5f;

    private string myCodeName;
    private RoE_TechnicianUI uiManager;

    public void Setup(string codeName, RoE_TechnicianUI manager)
    {
        myCodeName = codeName;
        uiManager = manager;

        if (codeText) codeText.text = codeName;
        if (blipImage == null) blipImage = gameObject.GetComponent<Image>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        uiManager.OnThreatSelected(myCodeName);
    }

    public void SetSelectionState(bool isSelected)
    {
        if (blipImage != null)
        {
            blipImage.color = isSelected ? selectedColor : defaultColor;

            float scale = isSelected ? selectedScale : defaultScale;
            transform.DOScale(scale, .5f);
        }
    }
}