using UnityEngine;

/// <summary>
/// 관찰 모드용 1인칭 카메라 룩 드라이버.
/// - 활성화 시 마우스로 yaw/pitch 회전
/// - 시작 시 외부에서 전달한 회전(3인칭 카메라 회전)을 기준으로 초기화 가능
/// </summary>
public class ObservationFirstPersonLook : MonoBehaviour
{
    [Header("감도/제한")]
    public float sensitivity = 2f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("입력")]
    [Tooltip("true면 Input.GetAxis(\"Mouse X/Y\") 사용, false면 외부에서 ApplyLookDelta로 넣어야 합니다.")]
    public bool readMouseAxis = true;

    private float _yaw;
    private float _pitch;

    private void OnEnable()
    {
        // enable 시점에 현재 transform 회전을 기준으로 초기화
        SyncFromRotation(transform.rotation);
    }

    private void Update()
    {
        if (!readMouseAxis) return;

        float mx = Input.GetAxis("Mouse X") * sensitivity;
        float my = Input.GetAxis("Mouse Y") * sensitivity;
        ApplyLookDelta(mx, my);
    }

    public void SyncFromRotation(Quaternion worldRotation)
    {
        Vector3 e = worldRotation.eulerAngles;
        _yaw = e.y;
        _pitch = e.x;
        if (_pitch > 180f) _pitch -= 360f;
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    /// <summary>
    /// mx/my는 이미 sensitivity가 적용된 값(도 단위)로 가정합니다.
    /// </summary>
    public void ApplyLookDelta(float mx, float my)
    {
        _yaw += mx;
        _pitch -= my;
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
}

