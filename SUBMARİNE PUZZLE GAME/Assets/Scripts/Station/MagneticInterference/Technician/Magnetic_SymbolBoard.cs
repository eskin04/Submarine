using UnityEngine;
using UnityEngine.UI;

public class Magnetic_SymbolBoard : MonoBehaviour
{
    public Magnetic_StationManager stationManager;
    public Magnetic_SymbolDatabase symbolDatabase;

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
        for (int i = 0; i < 10; i++)
        {


            int matchingSymbolID = -1;
            for (int j = 0; j < currentMapping.Length; j++)
            {
                if (currentMapping[j] == i)
                {
                    matchingSymbolID = j;
                    break;
                }
            }

            if (matchingSymbolID != -1 && symbolImages[i] != null && symbolDatabase != null)
            {
                symbolImages[i].sprite = symbolDatabase.GetSymbol(matchingSymbolID);
            }
        }
    }
}