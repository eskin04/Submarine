using UnityEngine;
using PurrNet;

public class ItemSway : NetworkBehaviour
{
    [Header("Rotation Sway")]
    [SerializeField] private float amount = 0.02f;
    [SerializeField] private float maxAmount = 0.06f;
    [SerializeField] private float smoothAmount = 6f;

    [SerializeField] private float tiltAmount = 3f;

    [Header("Position Sway")]
    [SerializeField] private float moveSwayAmount = 0.01f;
    [SerializeField] private float maxMoveSwayAmount = 0.02f;
    [SerializeField] private float smoothMoveAmount = 4f;


    private Vector3 originPosition;
    private Quaternion originRotation;

    private bool isEquipped = false;

    protected override void OnSpawned()
    {
        if (!isOwner)
        {
            enabled = false;
            return;
        }

        originPosition = transform.localPosition;
        originRotation = transform.localRotation;
    }

    public void SetActiveItem(bool isEquipped)
    {
        this.isEquipped = isEquipped;


    }

    private void Update()
    {
        if (!isOwner || !isEquipped) return;

        float mouseX = Input.GetAxis("Mouse X") * amount;
        float mouseY = Input.GetAxis("Mouse Y") * amount;

        mouseX = Mathf.Clamp(mouseX, -maxAmount, maxAmount);
        mouseY = Mathf.Clamp(mouseY, -maxAmount, maxAmount);

        float moveInputX = Input.GetAxis("Horizontal");

        Quaternion targetX = Quaternion.AngleAxis(-mouseY, Vector3.right);
        Quaternion targetY = Quaternion.AngleAxis(mouseX, Vector3.up);
        Quaternion targetZ = Quaternion.AngleAxis(-moveInputX * tiltAmount, Vector3.forward);

        Quaternion targetRotation = originRotation * targetX * targetY * targetZ;

        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smoothAmount);


        float moveX = -Input.GetAxis("Horizontal") * moveSwayAmount;
        float moveY = -Input.GetAxis("Vertical") * moveSwayAmount;

        moveX = Mathf.Clamp(moveX, -maxMoveSwayAmount, maxMoveSwayAmount);
        moveY = Mathf.Clamp(moveY, -maxMoveSwayAmount, maxMoveSwayAmount);

        Vector3 targetPosition = originPosition + new Vector3(moveX, moveY, 0);

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * smoothMoveAmount);
    }
}