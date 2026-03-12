using UnityEngine;

public class SecurityLockdown_NumpadButton : MonoBehaviour
{
    [Header("References")]
    public SecurityLockdown_Numpad mainNumpad;

    [Header("Settings")]
    public bool isNumberKey = true;
    public int numberValue = 0;

    public bool isSubmitKey = false;
    public bool isClearKey = false;

    public void OnClicked()
    {
        if (mainNumpad == null) return;

        if (isNumberKey)
        {
            mainNumpad.OnNumberPressed(numberValue);
        }
        else if (isSubmitKey)
        {
            mainNumpad.OnSubmitPressed();
        }
        else if (isClearKey)
        {
            mainNumpad.OnClearPressed();
        }
    }

    private void OnMouseDown()
    {
        OnClicked();
    }
}