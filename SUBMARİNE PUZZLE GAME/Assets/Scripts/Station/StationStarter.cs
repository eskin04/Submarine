using UnityEngine;
using UnityEngine.Events;

public class StationStarter : MonoBehaviour
{
    [SerializeField] private UnityEvent onStart;

    void Awake()
    {
        MainGameState.startGame += HandleStationStarting;
    }

    void OnDestroy()
    {
        MainGameState.startGame -= HandleStationStarting;
    }

    private void HandleStationStarting()
    {
        onStart?.Invoke();
    }

}
