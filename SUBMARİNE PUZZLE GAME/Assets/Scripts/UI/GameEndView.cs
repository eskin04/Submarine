using PurrNet;
using TMPro;
using UnityEngine;

public class GameEndView : View
{
    [SerializeField] private TMP_Text resultText;

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<GameEndView>();
    }

    public override void OnShow()
    {

    }

    public override void OnHide()
    {

    }

    public void SetResultText(ushort isWin)
    {
        string text = isWin == 1 ? "Level Completed!" : "Game Over!";
        if (resultText != null)
        {
            resultText.text = text;
        }
    }
}
