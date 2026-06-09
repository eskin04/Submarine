using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Magnetic_SymbolBoard : MonoBehaviour
{
    [Header("Referanslar")]
    public Magnetic_StationManager stationManager;
    public Magnetic_SymbolDatabase symbolDatabase; // Merkezi veritabanımız

    [Header("UI Slotları (0'dan 9'a kadar)")]
    public Image[] symbolImages = new Image[10];

    private void OnEnable()
    {
        if (stationManager != null)
            stationManager.OnSymbolMappingReceived += UpdateBoardVisuals;
    }

    private void OnDisable()
    {
        if (stationManager != null)
            stationManager.OnSymbolMappingReceived -= UpdateBoardVisuals;
    }

    private void UpdateBoardVisuals(int[] currentMapping)
    {
        // Panodaki her bir slot (i), 0'dan 9'a kadar olan sabit bir sayıyı temsil eder.
        for (int i = 0; i < 10; i++)
        {


            // 2. Bu sabit sayıya (i) karşılık gelen Sembol ID'sini buluyoruz
            int matchingSymbolID = -1;
            for (int j = 0; j < currentMapping.Length; j++)
            {
                if (currentMapping[j] == i)
                {
                    matchingSymbolID = j;
                    break;
                }
            }

            // 3. Bulduğumuz sembolü o slotun Image bileşenine atıyoruz
            if (matchingSymbolID != -1 && symbolImages[i] != null && symbolDatabase != null)
            {
                symbolImages[i].sprite = symbolDatabase.GetSymbol(matchingSymbolID);
            }
        }
    }
}