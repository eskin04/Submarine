using UnityEngine;
using TMPro;
using DG.Tweening;
using PurrNet;

public class EventUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text infoText;

    [Header("Settings")]
    [SerializeField] private float speedPerCharacter = 0.05f;
    [SerializeField] private float displayDuration = 3.0f;

    [Header("Colors")]
    [SerializeField] private Color failureColor = Color.red;
    [SerializeField] private Color repairColor = Color.green;

    private Sequence currentSequence;

    private void OnEnable()
    {
        GlobalEvents.OnShowSystemMessage += PlayTypewriterEffect;
    }

    private void OnDisable()
    {
        GlobalEvents.OnShowSystemMessage -= PlayTypewriterEffect;
        currentSequence?.Kill();
    }



    public void PlayTypewriterEffect(string stationName, bool isFailure)
    {
        currentSequence?.Kill();

        string prefix = isFailure ? "SYSTEM FAILURE" : "SYSTEM RESTORED";
        string fullMessage = $"{prefix}: {stationName.ToUpper()}";

        if (infoText)
        {
            infoText.text = "";
            infoText.color = isFailure ? failureColor : repairColor;
        }

        InstanceHandler.GetInstance<GameViewManager>().ShowView<InfoView>(hideOthers: false);


        float typingDuration = fullMessage.Length * speedPerCharacter;
        currentSequence = DOTween.Sequence();

        int charCount = 0;
        Tween typeWriter = DOTween.To(() => charCount, x => charCount = x, fullMessage.Length, typingDuration)
            .OnUpdate(() => infoText.text = fullMessage[..charCount])
            .SetEase(Ease.Linear);

        currentSequence.Append(typeWriter);
        currentSequence.AppendInterval(displayDuration);

        currentSequence.OnComplete(() =>
        {
            InstanceHandler.GetInstance<GameViewManager>().HideView<InfoView>();

            if (infoText) infoText.text = "";
        });
    }
}