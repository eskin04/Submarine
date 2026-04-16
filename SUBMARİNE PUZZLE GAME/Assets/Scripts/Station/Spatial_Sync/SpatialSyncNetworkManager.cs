using PurrNet;
using UnityEngine;
using System.Collections.Generic;

public class SpatialSyncNetworkManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private SpatialPatternDatabase patternDatabase;
    [SerializeField] private EngineerUIManager engineerUIManager;
    private SpatialSyncCore _coreLogic;

    // --- SENKRONİZE DEĞİŞKENLER (Ağ Optimizasyonu: Sadece Dünya Koordinatları gider) ---
    public readonly SyncVar<ushort> CurrentState = new(0);
    public readonly SyncVar<ushort> CurrentPatternID = new(0);
    public readonly SyncVar<ushort> OffsetX = new(0);
    public readonly SyncVar<ushort> OffsetY = new(0);

    // Artık indeks değil, direkt 8x8 üzerindeki gerçek konumlarını yolluyoruz
    public readonly SyncVar<ushort> RefWorldX = new(0);
    public readonly SyncVar<ushort> RefWorldY = new(0);
    public readonly SyncVar<ushort> TargetWorldX = new(0);
    public readonly SyncVar<ushort> TargetWorldY = new(0);
    public readonly SyncVar<ushort> CurrentStep = new(0);

    // Sunucuda ve istemcilerde (yerel) tutulan o anki aktif ağ haritası ve geçilen yerler
    private Dictionary<Vector2Int, List<Vector2Int>> _activeGraph;
    private Vector2Int _currentPos;
    private List<Vector2Int> _visitedNodes = new List<Vector2Int>();

    private void Awake()
    {
        _coreLogic = new SpatialSyncCore();
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
        ClearGridObserversRpc();
    }

    [ObserversRpc]
    private void ClearGridObserversRpc()
    {
        if (engineerUIManager != null)
        {
            engineerUIManager.ClearUI();
        }
    }

    // SADECE SUNUCU TARAFINDAN ÇALIŞTIRILIR
    private void GenerateNewPuzzle()
    {
        if (!isServer) return;

        bool isValid = false;
        int attempts = 0;

        while (!isValid && attempts < 50) // Sonsuz döngüyü önler
        {
            attempts++;
            ushort pId = (ushort)Random.Range(0, patternDatabase.PatternCount);

            if (patternDatabase.TryGetPattern(pId, out SpatialPattern pattern))
            {
                // Core algoritmamız devrede!
                isValid = _coreLogic.TryLoadManualPattern(pattern, out Vector2Int offset, out _activeGraph, out Vector2Int refP, out Vector2Int targetP);

                if (isValid)
                {
                    // Ağ üzerinden herkese bilgileri dağıt
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

                    // İstemcilere "Grafı kendi lokalinizde kurun" komutu gönder
                    BuildLocalGraphObserversRpc(pId, (ushort)offset.x, (ushort)offset.y, (ushort)refP.x, (ushort)refP.y, (ushort)targetP.x, (ushort)targetP.y);
                }
            }
        }

        if (!isValid) Debug.LogError("[SpatialSync] Veritabanındaki desenlerde çoklu çözüm bulunamadı!");
    }

    // TÜM OYUNCULARDA (Mühendis ve Teknisyen) ÇALIŞIR
    // TÜM OYUNCULARDA ÇALIŞIR (Mühendis ekranını çizer)
    [ObserversRpc]
    private void BuildLocalGraphObserversRpc(ushort pId, ushort ox, ushort oy, ushort rx, ushort ry, ushort tx, ushort ty)
    {
        if (patternDatabase.TryGetPattern(pId, out SpatialPattern pattern))
        {
            _activeGraph = new Dictionary<Vector2Int, List<Vector2Int>>();
            Vector2Int offset = new Vector2Int(ox, oy);

            // 1. Ağ verisini kullanarak kendi lokal Grafiğini (Sözlük) inşa et
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
            // 2. ÇİZİM İŞLEMİNİ TETİKLE (0 Referans Hatasının Çözümü)

            if (engineerUIManager != null)
            {
                // PurrNet üzerinden gelen senkronize değerleri alıyoruz
                Vector2Int refP = new Vector2Int(rx, ry);
                Vector2Int targetP = new Vector2Int(tx, ty);

                // Ve UI sınıfına "Bunu Ekrana Çiz" diyoruz!
                engineerUIManager.DrawCircuit(_activeGraph, refP);
            }
            else
            {
                Debug.LogWarning("[SpatialSync] Ekrana çizim yapılamadı çünkü sahnede EngineerUIManager bulunamadı!");
            }
        }
    }

    // TEKNİSYENİN TUŞLAMASI
    [ServerRpc(requireOwnership: false)]
    public void SubmitCoordinateServerRpc(ushort inputX, ushort inputY)
    {
        if (CurrentState.value != 1) return;

        Vector2Int inputPos = new Vector2Int(inputX, inputY);
        Vector2Int target = new Vector2Int(TargetWorldX.value, TargetWorldY.value);

        bool isCorrect = _coreLogic.ValidateCircuitInput(_activeGraph, _currentPos, inputPos, target, CurrentStep.value, _visitedNodes);

        if (isCorrect)
        {
            CurrentStep.value++;
            _currentPos = inputPos;
            _visitedNodes.Add(inputPos);

            if (CurrentStep.value >= SpatialSyncCore.REQUIRED_CLICKS)
            {
                ChangeStationStateServerRpc(2); // Çözüldü
            }
        }
        else
        {
            ChangeStationStateServerRpc(3); // Başarısız
        }
    }

    // Test
    [ContextMenu("Test Generate Puzzle")]
    private void TestGeneratePuzzle()
    {
        if (!isServer) return;
        ChangeStationStateServerRpc(1);
    }
}