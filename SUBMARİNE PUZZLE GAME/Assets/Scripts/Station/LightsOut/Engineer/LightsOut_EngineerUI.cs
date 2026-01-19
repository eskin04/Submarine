using UnityEngine;
using System.Collections.Generic;
using PurrNet;

public class LightsOut_EngineerUI : NetworkBehaviour
{
    [Header("References")]
    public List<LightsOut_StatusLight> statusLights;

    public void UpdateLights(List<StatusLightState> states)
    {
        for (int i = 0; i < statusLights.Count; i++)
        {
            if (i < states.Count)
            {
                statusLights[i].SetState(states[i]);
            }
        }
    }
}