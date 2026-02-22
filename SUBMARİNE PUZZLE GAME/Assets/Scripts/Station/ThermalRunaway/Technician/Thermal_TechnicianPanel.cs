using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Thermal_TechnicianPanel : MonoBehaviour
{
    public ThermalValveType myLocation;

    public Thermal_BottleneckLamp[] lamps = new Thermal_BottleneckLamp[3];

    public float showDuration = 0.6f;
    public float hideDuration = 0.2f;

    private Coroutine displayRoutine;
    private Dictionary<int, Thermal_BottleneckLamp> lampDict = new Dictionary<int, Thermal_BottleneckLamp>();


    void Awake()
    {
        foreach (var lamp in lamps)
        {
            if (!lampDict.ContainsKey(lamp.colorID))
            {
                lampDict.Add(lamp.colorID, lamp);
            }
        }
    }

    public void HandleBottleneckTrigger(int[] sequence, ThermalValveType bottleneckLocation)
    {
        if (myLocation != bottleneckLocation) return;

        if (displayRoutine != null) StopCoroutine(displayRoutine);
        displayRoutine = StartCoroutine(PlaySequenceLoop(sequence));
    }

    public void StopSequence()
    {
        if (displayRoutine != null) StopCoroutine(displayRoutine);
        TurnOffAllLamps();
    }

    private IEnumerator PlaySequenceLoop(int[] seq)
    {
        TurnOffAllLamps();

        while (true)
        {
            foreach (int colorIndex in seq)
            {
                if (lampDict.ContainsKey(colorIndex))
                {
                    lampDict[colorIndex].TurnOn();
                    yield return new WaitForSeconds(showDuration);

                    lampDict[colorIndex].TurnOff();
                    yield return new WaitForSeconds(hideDuration);
                }
            }

            yield return new WaitForSeconds(1.5f);
        }
    }

    private void TurnOffAllLamps()
    {
        foreach (var lamp in lampDict.Values)
        {
            lamp.TurnOff();
        }
    }
}