using UnityEngine;
using System.Collections.Generic;

public class RoE_HandbookLoader : MonoBehaviour
{
    public RoE_StationManager stationManager;

    [Header("UI Settings")]
    public Transform contentParent;
    public GameObject itemPrefab;

    private void Start()
    {
        if (stationManager != null)
        {
            LoadHandbook(stationManager.allPossibleObjects);
        }
    }


    public void LoadHandbook(List<RoE_ObjectData> allObjects)
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var obj in allObjects)
        {
            GameObject newRow = Instantiate(itemPrefab, contentParent);

            RoE_HandbookItem itemScript = newRow.GetComponent<RoE_HandbookItem>();
            if (itemScript != null)
            {
                itemScript.Setup(obj);
            }
        }
    }
}