using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;


public class RoE_TechnicianUI : MonoBehaviour
{
    [Header("References")]
    public RoE_StationManager stationManager;
    public RoE_ThreatManager threatManager;
    public RectTransform radarCenter;
    public GameObject blipPrefab;

    [Header("Settings")]
    public float radarRadius = 250f;
    public float maxGameDistance = 1000f;
    public float minAngleSpacing = 20f;
    public float selectionCooldown = 5.0f;

    [Header("Info Panel")]
    public RoE_PhysicalButton shootButton;
    public RoE_PhysicalButton passButton;
    public RoE_PhysicalButton evadeButton;
    public GameObject evadeGlassCover;
    public TMP_Text feedbackText;

    [Header("Sonar Settings")]
    public Material radarMaterial;
    public string shaderProperty = "_ScanAngle";
    public float sweepSpeed = 100f;
    public float beamWidth = 20f;

    public float visualOffset = 0f;


    private float currentSweepAngle = 0f;

    private Material activeMaterial;
    private string currentSelectedCode = "";
    private Coroutine displayCoroutine = null;
    private RoE_RadarBlip currentVisualBlip = null;
    private float nextSelectionTime = 0f;


    private Dictionary<string, GameObject> activeBlips = new Dictionary<string, GameObject>();
    private Dictionary<string, float> blipAngles = new Dictionary<string, float>();

    private void Start()
    {

        activeMaterial = radarMaterial;

    }
    private void Update()
    {

        if (stationManager == null || !stationManager.GetSimulateRunning()) return;

        UpdateRadarBlips();
        UpdateSelectionPanel();
        ProcessSonarSweep();

    }



    private void ProcessSonarSweep()
    {
        currentSweepAngle += sweepSpeed * Time.deltaTime;
        currentSweepAngle %= 360f;

        if (activeMaterial != null)
        {
            activeMaterial.SetFloat(shaderProperty, currentSweepAngle + visualOffset);
        }

        CheckBlipsInBeam();
    }

    private void CheckBlipsInBeam()
    {
        foreach (var kvp in activeBlips)
        {
            if (kvp.Value == null) continue;

            RectTransform blipRect = kvp.Value.GetComponent<RectTransform>();

            Vector2 direction = blipRect.anchoredPosition;
            float blipAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            float logicAngle = currentSweepAngle;

            float diff = Mathf.DeltaAngle(logicAngle, blipAngle);

            if (Mathf.Abs(diff) < beamWidth / 2f)
            {
                var blipScript = kvp.Value.GetComponent<RoE_RadarBlip>();
                if (blipScript) blipScript.Ping();
            }
        }
    }



    public string GetCurrentSelectedCode()
    {
        return currentSelectedCode;
    }

    private void UpdateRadarBlips()
    {
        foreach (var threat in threatManager.activeThreats)
        {
            if (threat.isDestroyed)
            {
                if (activeBlips.ContainsKey(threat.displayName))
                {
                    GameObject objToDestroy = activeBlips[threat.displayName];
                    if (currentVisualBlip != null && currentVisualBlip.gameObject == objToDestroy)
                    {
                        currentVisualBlip = null;
                        currentSelectedCode = "";
                        UpdateSelectionPanel();
                    }

                    Destroy(objToDestroy);
                    activeBlips.Remove(threat.displayName);
                    blipAngles.Remove(threat.displayName);
                }
                continue;
            }

            if (!activeBlips.ContainsKey(threat.displayName))
            {
                GameObject newBlip = Instantiate(blipPrefab, radarCenter);
                newBlip.GetComponent<RoE_RadarBlip>().Setup(threat.displayName, this);

                activeBlips.Add(threat.displayName, newBlip);

                float smartAngle = GetValidAngle();
                blipAngles.Add(threat.displayName, smartAngle);

            }

            float ratio = Mathf.Clamp01(threat.currentDistance / maxGameDistance);

            float distanceInPixels = ratio * radarRadius;

            float angle = blipAngles[threat.displayName];
            Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.up;

            activeBlips[threat.displayName].transform.localPosition = direction * distanceInPixels;
        }
    }

    private float GetValidAngle()
    {
        int maxAttempts = 100;

        for (int i = 0; i < maxAttempts; i++)
        {
            float candidateAngle = Random.Range(0f, 360f);
            bool isTooClose = false;

            foreach (float existingAngle in blipAngles.Values)
            {
                float difference = Mathf.Abs(Mathf.DeltaAngle(candidateAngle, existingAngle));

                if (difference < minAngleSpacing)
                {
                    isTooClose = true;
                    break;
                }
            }

            if (!isTooClose)
            {
                return candidateAngle;
            }
        }

        return Random.Range(0f, 360f);
    }

    public void OnThreatSelected(string codeName)
    {
        if (currentSelectedCode == codeName) return;

        if (Time.time < nextSelectionTime)
        {
            float remaining = nextSelectionTime - Time.time;
            Debug.LogWarning($"<color=orange>SİNYAL KARIŞIKLIĞI! {remaining:F1} sn bekle.</color>");

            return;
        }
        nextSelectionTime = Time.time + selectionCooldown;

        if (currentVisualBlip != null)
        {
            currentVisualBlip.SetSelectionState(false);
        }

        if (activeBlips.ContainsKey(codeName))
        {
            GameObject blipObj = activeBlips[codeName];
            RoE_RadarBlip blipScript = blipObj.GetComponent<RoE_RadarBlip>();

            if (blipScript != null)
            {
                blipScript.SetSelectionState(true);

                currentVisualBlip = blipScript;
            }
        }

        currentSelectedCode = codeName;

        UpdateSelectionPanel();

        if (stationManager != null)
        {
            var threatList = threatManager.activeThreats;
            int index = threatList.FindIndex(t => t.displayName == codeName);

            if (index != -1)
            {
                stationManager.SelectThreatRPC(index);
            }
        }

    }

    public void UpdateFeedBack(string feedback, Color color)
    {
        if (displayCoroutine != null) StopCoroutine(displayCoroutine);
        feedbackText.text = feedback;
        feedbackText.color = color;
        displayCoroutine = StartCoroutine(ClearFeedBack());
    }

    private IEnumerator ClearFeedBack()
    {

        yield return new WaitForSeconds(5);
        feedbackText.text = "";
    }

    public void OnClick_SubmitAction(int actionIndex)
    {
        if (string.IsNullOrEmpty(currentSelectedCode)) return;
        var threatList = threatManager.activeThreats;
        int index = threatList.FindIndex(t => t.displayName == currentSelectedCode);
        if (index != -1)
        {
            Roe_PlayerAction action = (Roe_PlayerAction)actionIndex;
            stationManager.SubmitActionRPC(index, action);
            Debug.Log(action);
        }

        currentSelectedCode = "";
        UpdateSelectionPanel();
    }



    private void UpdateSelectionPanel()
    {
        bool hasSelection = !string.IsNullOrEmpty(currentSelectedCode);

        if (shootButton) shootButton.SetInteractable(hasSelection);
        if (passButton) passButton.SetInteractable(hasSelection);


        if (evadeButton != null)
        {
            bool canEvade = false;

            if (!string.IsNullOrEmpty(currentSelectedCode))
            {
                var threat = threatManager.GetThreat(currentSelectedCode);
                if (threat != null && !threat.isDestroyed && threat.currentDistance <= 100f)
                {
                    canEvade = true;
                }
            }

            evadeButton.SetInteractable(canEvade);

            if (evadeGlassCover) evadeGlassCover.SetActive(!canEvade);
        }
    }

    public void ForceClearInterface()
    {
        foreach (var kvp in activeBlips)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }

        activeBlips.Clear();
        blipAngles.Clear();

        currentVisualBlip = null;
        currentSelectedCode = "";

        UpdateSelectionPanel();
    }

    private void OnDrawGizmos()
    {
        if (radarCenter != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(radarCenter.position, 10f * transform.lossyScale.x);

            Gizmos.color = Color.red;
            float worldRadius = radarRadius * transform.lossyScale.x;
            Gizmos.DrawWireSphere(radarCenter.position, worldRadius);
        }
    }
}