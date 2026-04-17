using UnityEngine;
using DG.Tweening; // Animasyon için DOTween'i ekliyoruz
using TMPro; // TextMeshPro için
public class SSTechnicianInputManager : MonoBehaviour
{
    private int? _pendingX = null;
    private int? _pendingY = null;

    public SSTechnicianUIManager _uiManager;
    public SpatialSyncNetworkManager _networkManager;

    public void OnButtonInput(ButtonAxis axis, int value)
    {
        if (axis == ButtonAxis.LetterX)
        {
            _pendingX = value;
        }
        else if (axis == ButtonAxis.NumberY)
        {
            _pendingY = value;
        }

        string displayStr = "";

        if (_pendingX.HasValue)
            displayStr += ((char)('A' + _pendingX.Value)).ToString();

        if (_pendingY.HasValue)
            displayStr += (_pendingY.Value + 1).ToString();

        if (_uiManager != null)
        {
            _uiManager.UpdateInputText(displayStr);
        }
        if (_pendingX.HasValue && _pendingY.HasValue)
        {
            Vector2Int newCoordinate = new Vector2Int(_pendingX.Value, _pendingY.Value);
            ProcessCompleteCoordinate(newCoordinate);

            _pendingX = null;
            _pendingY = null;
        }
    }

    private void ProcessCompleteCoordinate(Vector2Int newCoord)
    {
        Debug.Log($"KOORDİNAT OLUŞTURULDU: {(char)('A' + newCoord.x)}{newCoord.y + 1}");

        if (_networkManager != null)
        {
            _networkManager.SubmitCoordinateServerRpc((ushort)newCoord.x, (ushort)newCoord.y);
        }
    }


}