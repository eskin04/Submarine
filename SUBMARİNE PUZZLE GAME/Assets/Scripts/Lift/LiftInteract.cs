using System;
using UnityEngine;

public class LiftInteract : MonoBehaviour
{
    public Action OnLiftInteract;
    public void Interact()
    {
        OnLiftInteract?.Invoke();
    }
}
