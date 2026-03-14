using UnityEngine;

public class EngineerLockDown_NumpadButton : MonoBehaviour
{
    [Header("References")]
    public EngineerLockDown_Numpad overrideNumpad;

    [Header("Settings")]
    public bool isNumberKey = true;
    public int numberValue = 0;

    public bool isSubmitKey = false;
    public bool isClearKey = false;

    public void OnClicked()
    {
        if (overrideNumpad == null) return;

        if (isNumberKey)
        {
            overrideNumpad.OnNumberPressed(numberValue);
        }
        else if (isSubmitKey)
        {
            overrideNumpad.OnSubmitPressed();
        }
        else if (isClearKey)
        {
            overrideNumpad.OnClearPressed();
        }
    }

    private void OnMouseDown()
    {
        OnClicked();
    }
}