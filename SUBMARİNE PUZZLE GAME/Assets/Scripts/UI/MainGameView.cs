using PurrLobby;
using TMPro;
using UnityEngine;
using PurrNet;

public class MainGameView : View
{
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text waterLevelText;
    [SerializeField] private TMP_Text timerText;


    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<MainGameView>();
    }
    public override void OnHide()
    {

    }

    public override void OnShow()
    {

    }

    public void SetWaterLevelText(float currentWater, float maxWater)
    {
        waterLevelText.text = $"Water Level: {currentWater:F1} / {maxWater}";
    }

    public void SetRoleText(PlayerRole role)
    {
        if (roleText != null)
        {
            roleText.text = role.ToString();
        }
    }

    public void SetTimerText(string timeString)
    {
        if (timerText != null)
        {
            timerText.text = timeString;
        }
    }

}