using UnityEngine;

public static class SaveManager
{
    public static int GetMaxUnlockedLevelID()
    {
        return PlayerPrefs.GetInt("MaxUnlockedLevelID", 1);
    }

    public static void UnlockLevel(int levelID)
    {
        int currentMax = GetMaxUnlockedLevelID();

        if (levelID > currentMax)
        {
            PlayerPrefs.SetInt("MaxUnlockedLevelID", levelID);
            PlayerPrefs.Save();
            Debug.Log($"Yeni seviye açıldı! Artık Level {levelID} oynanabilir.");
        }
    }
}