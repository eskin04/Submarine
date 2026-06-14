using System.Collections.Generic;
using UnityEngine;

public class HighlightManager : MonoBehaviour
{
    public static HighlightManager Instance { get; private set; }

    [Header("Outline Settings")]
    [SerializeField] private LayerMask interactableMeshLayer;
    [SerializeField] private float normalOutlineWidth = 2f;
    [SerializeField] private float hoverOutlineWidth = 4f;
    [SerializeField] private Color outlineColor = Color.yellow;

    private List<Outline> currentActiveOutlines = new List<Outline>();
    private Outline currentHoveredOutline;
    private Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    public void ActivateModuleHighlights(Transform moduleRoot)
    {
        currentActiveOutlines.Clear();

        Collider[] allChildren = moduleRoot.GetComponentsInChildren<Collider>(true);

        foreach (Collider child in allChildren)
        {
            if (((1 << child.gameObject.layer) & interactableMeshLayer) != 0)
            {
                if (child.GetComponent<NoOutline>() != null)
                {
                    continue;
                }
                Outline outline = child.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = child.gameObject.AddComponent<Outline>();
                    outline.OutlineMode = Outline.Mode.OutlineVisible;
                    outline.OutlineColor = outlineColor;
                }

                outline.enabled = true;
                outline.OutlineWidth = normalOutlineWidth;

                currentActiveOutlines.Add(outline);
            }
        }
    }


    public void DeactivateModuleHighlights()
    {
        foreach (Outline outline in currentActiveOutlines)
        {
            if (outline != null) outline.enabled = false;
        }

        currentActiveOutlines.Clear();
        currentHoveredOutline = null;
    }


    public void SetHoveredObject(Transform hitTransform)
    {
        Outline hitOutline = hitTransform != null ? hitTransform.GetComponent<Outline>() : null;

        if (currentHoveredOutline != hitOutline)
        {
            if (currentHoveredOutline != null)
                currentHoveredOutline.OutlineWidth = normalOutlineWidth;

            currentHoveredOutline = hitOutline;

            if (currentHoveredOutline != null)
                currentHoveredOutline.OutlineWidth = hoverOutlineWidth;
        }
    }


    // ==========================================
    public void SetInteractableState(GameObject targetObj, bool isInteractable)
    {
        if (targetObj == null) return;

        Outline outline = targetObj.GetComponent<Outline>();

        if (isInteractable)
        {
            if (originalLayers.ContainsKey(targetObj))
            {
                targetObj.layer = originalLayers[targetObj];
            }

            if (outline == null)
            {
                outline = targetObj.AddComponent<Outline>();
                outline.OutlineMode = Outline.Mode.OutlineVisible;
                outline.OutlineColor = outlineColor;
            }

            outline.enabled = true;
            outline.OutlineWidth = normalOutlineWidth;

            if (!currentActiveOutlines.Contains(outline))
            {
                currentActiveOutlines.Add(outline);
            }
        }
        else
        {
            if (!originalLayers.ContainsKey(targetObj))
            {
                originalLayers[targetObj] = targetObj.layer;
            }

            targetObj.layer = 0;

            if (outline != null)
            {
                outline.enabled = false;

                if (currentHoveredOutline == outline)
                {
                    currentHoveredOutline = null;
                }
            }
        }
    }
}