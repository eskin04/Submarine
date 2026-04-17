using PurrNet;
using UnityEngine;
using System.Collections.Generic;

public class SpatialSyncNetworkManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private SpatialPatternDatabase patternDatabase;
    [SerializeField] private EngineerUIManager engineerUIManager;
    [SerializeField] private SSTechnicianUIManager sSTechnicianUIManager;
    [SerializeField] private StationController stationController;
    private SpatialSyncCore _coreLogic;

    public readonly SyncVar<ushort> CurrentState = new(0);
    public readonly SyncVar<ushort> CurrentPatternID = new(0);
    public readonly SyncVar<ushort> OffsetX = new(0);
    public readonly SyncVar<ushort> OffsetY = new(0);
    public readonly SyncVar<ushort> RefWorldX = new(0);
    public readonly SyncVar<ushort> RefWorldY = new(0);
    public readonly SyncVar<ushort> TargetWorldX = new(0);
    public readonly SyncVar<ushort> TargetWorldY = new(0);
    public readonly SyncVar<ushort> CurrentStep = new(0);

    private Dictionary<Vector2Int, List<Vector2Int>> _activeGraph;
    private Vector2Int _currentPos;
    private List<Vector2Int> _visitedNodes = new List<Vector2Int>();

    private Vector2Int _currentTechPos;
    private bool _techHasStarted = false;

    private void Awake()
    {
        _coreLogic = new SpatialSyncCore();
    }

    public void StartStation()
    {
        if (!isServer) return;
        ChangeStationStateServerRpc(1);
    }

    [ServerRpc]
    public void ChangeStationStateServerRpc(ushort newState)
    {
        CurrentState.value = newState;
        HandleStateLogic(newState);
    }

    private void HandleStateLogic(ushort state)
    {
        switch (state)
        {
            case 0: ResetStation(); break;
            case 1: GenerateNewPuzzle(); break;
            case 2: Debug.Log("Çözüldü!"); break;
            case 3: Debug.Log("Başarısız! Işıklar Kırmızı."); break;
        }
    }

    private void ResetStation()
    {
        if (!isServer) return;
        CurrentStep.value = 0;
        _visitedNodes.Clear();
        // ClearGridObserversRpc();
        _currentPos = new Vector2Int(RefWorldX.value, RefWorldY.value);
    }

    [ObserversRpc]
    private void ClearGridObserversRpc()
    {
        if (engineerUIManager != null)
        {
            engineerUIManager.ClearUI();
        }
    }

    private void GenerateNewPuzzle()
    {
        if (!isServer) return;

        bool isValid = false;
        int attempts = 0;

        while (!isValid && attempts < 50)
        {
            attempts++;
            ushort pId = (ushort)Random.Range(0, patternDatabase.PatternCount);

            if (patternDatabase.TryGetPattern(pId, out SpatialPattern pattern))
            {
                isValid = _coreLogic.TryLoadManualPattern(pattern, out Vector2Int offset, out _activeGraph, out Vector2Int refP, out Vector2Int targetP);
                _currentTechPos = refP;
                _techHasStarted = false;
                if (isValid)
                {
                    CurrentPatternID.value = pId;
                    OffsetX.value = (ushort)offset.x;
                    OffsetY.value = (ushort)offset.y;
                    RefWorldX.value = (ushort)refP.x;
                    RefWorldY.value = (ushort)refP.y;
                    TargetWorldX.value = (ushort)targetP.x;
                    TargetWorldY.value = (ushort)targetP.y;

                    CurrentStep.value = 0;
                    _currentPos = refP;
                    _visitedNodes = new List<Vector2Int> { refP };

                    BuildLocalGraphObserversRpc(pId, (ushort)offset.x, (ushort)offset.y, (ushort)refP.x, (ushort)refP.y, (ushort)targetP.x, (ushort)targetP.y);
                }
            }
        }

        if (!isValid) Debug.LogError("[SpatialSync] Veritabanındaki desenlerde çoklu çözüm bulunamadı!");
    }

    [ObserversRpc]
    private void BuildLocalGraphObserversRpc(ushort pId, ushort ox, ushort oy, ushort rx, ushort ry, ushort tx, ushort ty)
    {
        if (patternDatabase.TryGetPattern(pId, out SpatialPattern pattern))
        {
            _activeGraph = new Dictionary<Vector2Int, List<Vector2Int>>();
            Vector2Int offset = new Vector2Int(ox, oy);

            foreach (var branch in pattern.branches)
            {
                foreach (var node in branch.nodes)
                {
                    Vector2Int worldPos = node + offset;
                    if (!_activeGraph.ContainsKey(worldPos))
                        _activeGraph[worldPos] = new List<Vector2Int>();
                }
            }
            foreach (var branch in pattern.branches)
            {
                for (int i = 0; i < branch.nodes.Length - 1; i++)
                {
                    Vector2Int a = branch.nodes[i] + offset;
                    Vector2Int b = branch.nodes[i + 1] + offset;

                    if (!_activeGraph[a].Contains(b))
                    {
                        _activeGraph[a].Add(b);
                        _activeGraph[b].Add(a);
                    }
                }
            }

            if (engineerUIManager != null)
            {
                Vector2Int refP = new Vector2Int(rx, ry);

                engineerUIManager.DrawCircuit(_activeGraph, refP);
            }
            if (sSTechnicianUIManager != null)
            {
                Vector2Int targetP = new Vector2Int(tx, ty);
                sSTechnicianUIManager.SetTarget(targetP);
            }
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void SubmitCoordinateServerRpc(ushort inputX, ushort inputY)
    {
        if (CurrentState.value != 1 && CurrentState.value != 0) return;

        Vector2Int inputPos = new Vector2Int(inputX, inputY);
        Vector2Int referencePos = new Vector2Int(RefWorldX.value, RefWorldY.value);
        Vector2Int target = new Vector2Int(TargetWorldX.value, TargetWorldY.value);

        bool isCorrect = false;

        if (CurrentStep.value == 0)
        {
            if (inputPos == referencePos)
            {
                isCorrect = true;
            }
        }
        else
        {
            isCorrect = _coreLogic.ValidateCircuitInput(_activeGraph, _currentPos, inputPos, target, CurrentStep.value, _visitedNodes);
        }
        if (isCorrect)
        {
            Vector2Int previousPos = _currentPos;

            CurrentStep.value++;
            _currentPos = inputPos;
            _visitedNodes.Add(inputPos);

            CorrectStepObserversRpc((ushort)previousPos.x, (ushort)previousPos.y, (ushort)inputPos.x, (ushort)inputPos.y);

            if (CurrentStep.value >= SpatialSyncCore.REQUIRED_CLICKS)
            {
                ChangeStationStateServerRpc(2);
                PuzzleSolvedObserversRpc();
            }
        }
        else
        {
            ChangeStationStateServerRpc(0);
            WrongStepObserversRpc();
        }
    }

    [ObserversRpc]
    private void CorrectStepObserversRpc(ushort prevX, ushort prevY, ushort nextX, ushort nextY)
    {
        Vector2Int prev = new Vector2Int(prevX, prevY);
        Vector2Int next = new Vector2Int(nextX, nextY);


        if (sSTechnicianUIManager != null)
        {
            sSTechnicianUIManager.AddPathStep(prev, next);
        }

    }

    [ObserversRpc]
    private void WrongStepObserversRpc()
    {
        sSTechnicianUIManager.UpdateInputText("Fail!");
        sSTechnicianUIManager.SetTarget(new Vector2Int(TargetWorldX.value, TargetWorldY.value));
        stationController.ReportRepairMistake();
        // İleride buraya eklenebilecekler:
        // 1. Fmod ile "BZZZTT" hata sesi.
        // 2. Denizaltının o odasındaki kırmızı alarm ışıklarının yanıp sönmesi.
        // 3. Teknisyen UI'ının tamamen kırmızı renge dönmesi.
    }

    [ObserversRpc]
    private void PuzzleSolvedObserversRpc()
    {
        stationController.SetReparied();
        // İleride buraya eklenebilecekler:
        // 1. Fmod ile "Sistem Aktif" onay sesi.
        // 2. Teknisyenin ekranındaki tüm yolların yeşile dönmesi.
    }

    // Test
    [ContextMenu("Test Generate Puzzle")]
    private void TestGeneratePuzzle()
    {
        if (!isServer) return;
        ChangeStationStateServerRpc(1);
    }
}