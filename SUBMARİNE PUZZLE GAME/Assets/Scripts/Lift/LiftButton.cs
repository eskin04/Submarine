using System;
using UnityEngine;

public class LiftButton : MonoBehaviour
{
    public Action<int> OnLiftButtonPressed;
    [SerializeField] private int liftFloorIndex;



    public void PressButton()
    {
        OnLiftButtonPressed?.Invoke(liftFloorIndex);
    }


}
