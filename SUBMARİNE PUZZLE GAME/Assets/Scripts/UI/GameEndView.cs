using PurrNet;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class GameEndView : View
{
    [SerializeField] private TMP_Text resultText;

    private LocalizedString localizedResultText = new LocalizedString { TableReference = "UI_General" };

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
        localizedResultText.StringChanged += OnTranslatedResultText;
    }

    private void OnDestroy()
    {
        localizedResultText.StringChanged -= OnTranslatedResultText;
        InstanceHandler.UnregisterInstance<GameEndView>();
    }

    private void OnTranslatedResultText(string translatedText)
    {
        if (resultText != null)
        {
            resultText.text = translatedText;
        }
    }

    public override void OnShow()
    {

    }

    public override void OnHide()
    {

    }

    public void SetResultText(ushort isWin)
    {
        string resultKey = isWin == 1 ? "game_end_win" : "game_end_lose";
        localizedResultText.TableEntryReference = resultKey;
    }
}
