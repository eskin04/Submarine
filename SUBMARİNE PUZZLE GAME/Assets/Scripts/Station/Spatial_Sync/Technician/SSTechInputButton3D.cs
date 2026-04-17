using UnityEngine;
using DG.Tweening;

public enum ButtonAxis { LetterX, NumberY }

public class SSTechInputButton3D : MonoBehaviour
{
    [Header("Buton Tipi")]
    public ButtonAxis axis;
    public int indexValue;

    [Header("Animasyon Ayarları")]
    public float pushDepth = 0.05f;
    public float pushDuration = 0.1f;

    public bool moveOnZAxis = true;

    private Vector3 _originalPos;
    private bool _isPressed = false;
    public SSTechnicianInputManager _inputManager;

    void Start()
    {
        _originalPos = transform.localPosition;
    }

    void OnMouseDown()
    {
        if (_isPressed) return;

        Interact();
    }

    public void Interact()
    {
        _isPressed = true;

        if (moveOnZAxis)
        {
            transform.DOLocalMoveZ(_originalPos.z + pushDepth, pushDuration)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .OnComplete(() => _isPressed = false);
        }


        if (_inputManager != null)
        {
            _inputManager.OnButtonInput(axis, indexValue);
        }
    }
}