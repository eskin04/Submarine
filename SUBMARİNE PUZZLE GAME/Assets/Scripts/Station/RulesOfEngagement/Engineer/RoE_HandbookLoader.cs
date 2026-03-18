using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RoE_HandbookLoader : MonoBehaviour
{

    [Header("UI Settings")]
    public Transform contentParent;
    public GameObject itemPrefab;

    public void LoadHandbook(List<RoE_ObjectData> roundObjects)
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        var shuffledObjects = roundObjects.OrderBy(x => Random.value).ToList();

        foreach (var obj in shuffledObjects)
        {
            GameObject newRow = Instantiate(itemPrefab, contentParent);
            RoE_HandbookItem itemScript = newRow.GetComponent<RoE_HandbookItem>();

            if (itemScript != null)
            {
                List<ObjectCategory> shuffledCats = obj.categories.OrderBy(x => Random.value).ToList();

                itemScript.Setup(obj.objectName, shuffledCats);
            }
        }
    }
}