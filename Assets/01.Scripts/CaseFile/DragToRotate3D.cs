using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GameDatabase.UI;
using GameDatabase.Player;

public class DragToRotate3D : MonoBehaviour, IDragHandler
{
    [Header("돌려볼 3D 오브젝트")]
    public Transform targetObject;

    [Header("회전 속도")]
    public float rotationSpeed = 0.5f;

    [Header("버튼 회전 속도 (초당 각도)")]
    public float buttonRotationSpeed = 90f;

    [Header("버튼")]
    public Button leftButton;
    public Button rightButton;

    private bool _aimMode = false;

    public void OnDrag(PointerEventData eventData)
    {
        if (targetObject != null)
        {
            float rotY = -eventData.delta.x * rotationSpeed;
            targetObject.Rotate(Vector3.up, rotY, Space.World);
        }
    }

    void Update()
    {
        if (targetObject != null && Input.GetMouseButton(0))
        {
            if (leftButton != null && IsPointerOver(leftButton.gameObject))
                targetObject.Rotate(Vector3.up, -buttonRotationSpeed * Time.deltaTime, Space.World);
            else if (rightButton != null && IsPointerOver(rightButton.gameObject))
                targetObject.Rotate(Vector3.up, buttonRotationSpeed * Time.deltaTime, Space.World);
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            _aimMode = !_aimMode;

            if (AimCursor.Instance != null)
            {
                if (_aimMode)
                    AimCursor.Instance.EnterLabelingMode();
                else
                    AimCursor.Instance.ExitLabelingMode();
            }

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetMovementEnabled(!_aimMode);
                PlayerController.Instance.SetCameraEnabled(!_aimMode);
            }
        }
    }

    private bool IsPointerOver(GameObject target)
    {
        var pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);
        foreach (var r in results)
            if (r.gameObject == target) return true;
        return false;
    }
}
