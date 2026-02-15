using UnityEngine;
using System.Collections.Generic;
using PurrNet;
using DG.Tweening;

public class LightsOut_TechnicianUI : NetworkBehaviour
{
    [Header("Managers")]
    public LightsOut_StationManager stationManager;

    [Header("3D Containers")]
    public List<Transform> startSlots;

    public List<Transform> wireAnchors;


    [Header("Prefabs & Lists")]
    public GameObject cablePrefab;
    public List<LightsOut_Port> ports;
    public GameObject parentObject;
    public BoxCollider dragLimitBox;
    public Transform doorTransform;

    private List<LightsOut_Cable> createdCables = new List<LightsOut_Cable>();
    private List<CableData> localPuzzleData = new List<CableData>();

    public void HandlePuzzleSync(List<CableData> puzzleData)
    {
        InitializeCables(puzzleData);

        foreach (var data in puzzleData)
        {
            UpdateVisuals(data.cableID, data.currentPortIndex);
        }
    }

    private void InitializeCables(List<CableData> data)
    {
        localPuzzleData = new List<CableData>(data);
        foreach (var cable in createdCables)
        {
            if (cable != null) Destroy(cable.gameObject);
        }
        createdCables.Clear();

        foreach (var port in ports)
        {
            port.TurnOffLight();
        }



        foreach (var d in data)
        {
            if (d.cableID >= startSlots.Count) continue;
            Transform targetSlot = startSlots[d.cableID];

            GameObject newCable = Instantiate(cablePrefab, parentObject.transform);

            newCable.transform.position = targetSlot.position;

            LightsOut_WireVisual wireVis = newCable.GetComponentInChildren<LightsOut_WireVisual>();

            if (wireVis != null)
            {
                if (d.cableID < wireAnchors.Count)
                    wireVis.endPoint = wireAnchors[d.cableID];

                wireVis.SetColor(GetColorEnum(d.physicalColor));
            }

            LightsOut_Cable script = newCable.GetComponent<LightsOut_Cable>();
            if (script != null)
            {
                script.Setup(d.cableID, d.physicalColor, this, dragLimitBox);
                createdCables.Add(script);
            }
        }
    }

    public void OnCableDropped(LightsOut_Cable cable, LightsOut_Port port)
    {
        cable.SnapToPort(port);

        if (stationManager != null)
        {
            stationManager.ConnectCableRPC(cable.cableID, port.portID);
        }
    }

    public void OnCableDisconnected(LightsOut_Cable cable)
    {
        if (stationManager != null)
        {
            stationManager.ConnectCableRPC(cable.cableID, -1);
        }
    }

    public void UpdateVisuals(int cableID, int portID)
    {
        LightsOut_Cable cable = createdCables.Find(c => c.cableID == cableID);
        if (cable == null) return;

        if (portID == -1)
        {
            cable.ResetPosition();
        }
        else
        {
            LightsOut_Port port = ports.Find(p => p.portID == portID);
            if (port != null)
            {
                cable.SnapToPort(port);

                var cableData = localPuzzleData.Find(c => c.cableID == cableID);

                if (cableData != null)
                {
                    port.SetLightColor(cableData.outputLightColor);
                }
            }
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

    public void ToggleDoor(bool open)
    {
        if (doorTransform == null) return;
        Vector3 targetRotation = open ? new Vector3(-90, 0, 170) : new Vector3(-90, 0, 0);


        doorTransform.DOLocalRotate(targetRotation, 1f).SetEase(Ease.InOutSine);


    }
}