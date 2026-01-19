using UnityEngine;
using System.Collections.Generic;
using PurrNet;

public class LightsOut_TechnicianUI : NetworkBehaviour
{
    [Header("Managers")]
    public LightsOut_StationManager stationManager;

    [Header("UI Containers")]
    public List<Transform> startSlots;
    public List<Transform> wireAnchors;
    public Transform portContainer;
    public Transform dragLayer;

    [Header("Prefabs & Lists")]
    public GameObject cablePrefab;
    public List<LightsOut_Port> ports;

    private List<LightsOut_Cable> createdCables = new List<LightsOut_Cable>();
    private List<CableData> localPuzzleData = new List<CableData>();
    public float canvasScaleFactor = 1.0f;

    private void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvasScaleFactor = canvas.scaleFactor;

    }

    public void HandlePuzzleSync(List<CableData> puzzleData)
    {
        InitializeCables(puzzleData);

        foreach (var data in puzzleData)
        {
            UpdateVisuals(data.cableID, data.currentPortIndex);
        }
    }


    public void OnCableDropped(LightsOut_Cable cable, LightsOut_Port port)
    {
        cable.SnapToPort(port);

        stationManager.ConnectCableRPC(cable.cableID, port.portID);
    }

    public void OnCableDisconnected(LightsOut_Cable cable)
    {
        cable.ResetPosition();

        if (stationManager != null)
        {
            stationManager.UnplugCableRPC(cable.cableID);
        }
    }


    public void UpdateVisuals(int cableID, int portID)
    {
        var cable = createdCables.Find(c => c.cableID == cableID);
        if (cable == null) return;

        LightsOut_Port oldPort = cable.GetComponentInParent<LightsOut_Port>();

        if (oldPort != null)
        {
            oldPort.TurnOffLight();
        }


        var port = ports.Find(p => p.portID == portID);

        if (port != null)
        {
            cable.SnapToPort(port);

            var cableData = localPuzzleData.Find(c => c.cableID == cableID);
            if (cableData != null)
            {
                port.SetLightColor(cableData.outputLightColor);
            }
            else
            {
                Debug.LogWarning("Client: Kablo verisi yerel listede bulunamadı!");
            }
        }
        else
        {
            cable.ResetPosition();
        }
    }

    public void InitializeCables(List<CableData> data)
    {
        localPuzzleData = new List<CableData>(data);
        if (createdCables != null)
        {
            foreach (var cable in createdCables)
            {
                if (cable != null) Destroy(cable.gameObject);
            }
            createdCables.Clear();
        }

        foreach (var port in ports)
        {
            port.TurnOffLight();
        }

        foreach (var slot in startSlots)
        {
            foreach (Transform child in slot) Destroy(child.gameObject);
        }

        foreach (var d in data)
        {
            Transform targetSlot = startSlots[d.cableID];

            GameObject newCable = Instantiate(cablePrefab, targetSlot);
            newCable.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            LightsOut_WireVisual wireVis = newCable.GetComponentInChildren<LightsOut_WireVisual>();
            if (wireVis != null)
            {
                if (d.cableID < wireAnchors.Count)
                    wireVis.startPoint = wireAnchors[d.cableID];

                wireVis.SetColor(GetColorEnum(d.physicalColor));
            }

            LightsOut_Cable script = newCable.GetComponent<LightsOut_Cable>();
            script.Setup(d.cableID, d.physicalColor, this);
            createdCables.Add(script);
        }
    }

    private Color GetColorEnum(WireColor c)
    {
        switch (c)
        {
            case WireColor.Yellow: return Color.yellow;
            case WireColor.Green: return Color.green;
            case WireColor.Blue: return Color.blue;
            case WireColor.Red: return Color.red;
            default: return Color.white;
        }
    }
}