using UnityEngine;
using FMODUnity;

public struct AudioEventPayload
{
    public EventReference EventReference;
    public Vector3 Position;

    // FMOD RTPC (Real-Time Parameter Control) için opsiyonel veriler
    public string ParameterName;
    public float ParameterValue;

    public AudioEventPayload(EventReference eventRef, Vector3 position, string paramName = "", float paramValue = 0f)
    {
        EventReference = eventRef;
        Position = position;
        ParameterName = paramName;
        ParameterValue = paramValue;
    }
}