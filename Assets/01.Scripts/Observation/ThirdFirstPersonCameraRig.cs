using UnityEngine;

/// <summary>
/// 3인칭 플레이어(ThirdPersonController) 아래에 1인칭 카메라를 생성/관리하고,
/// 3인칭 카메라/1인칭 카메라를 enable/disable로 토글합니다.
/// </summary>
public class ThirdFirstPersonCameraRig : MonoBehaviour
{
    [Header("카메라 참조")]
    [Tooltip("현재 사용 중인 3인칭 카메라(보통 MainCamera). 비어 있으면 Camera.main을 사용합니다.")]
    public Camera thirdPersonCamera;

    [Tooltip("3인칭 카메라가 들어있는 오브젝트 루트. 비어 있으면 thirdPersonCamera.gameObject를 사용합니다.")]
    public GameObject thirdPersonCameraRoot;

    [Tooltip("생성될 1인칭 카메라. 비어 있으면 런타임에 생성합니다.")]
    public Camera firstPersonCamera;

    [Header("1인칭 카메라 배치")]
    [Tooltip("플레이어 기준(로컬) 1인칭 카메라 위치 오프셋")]
    public Vector3 firstPersonLocalPosition = new Vector3(0f, 1.65f, 0.05f);

    [Tooltip("플레이어 기준(로컬) 1인칭 카메라 회전 오프셋")]
    public Vector3 firstPersonLocalEuler = Vector3.zero;

    [Header("1인칭 룩")]
    public float firstPersonLookSensitivity = 2f;
    public float firstPersonPitchMin = -80f;
    public float firstPersonPitchMax = 80f;

    [Header("1인칭 줌(FOV)")]
    [Tooltip("1인칭 전환 시 FOV 오프셋. 음수면 줌인(확대), 양수면 줌아웃. 예: -8")]
    public float firstPersonFovOffset = -8f;

    private float _savedThirdPersonFov;
    private float _savedFirstPersonFov;

    [Header("동작")]
    [Tooltip("1인칭 카메라 생성 시 3인칭 카메라 설정을 복사합니다.")]
    public bool copySettingsFromThirdPerson = true;

    private string _savedThirdRootTag;
    private string _savedFirstRootTag;

    private void Awake()
    {
        EnsureRig();
        SetFirstPersonActive(false);
    }

    public void EnsureRig()
    {
        if (thirdPersonCamera == null)
            thirdPersonCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();

        if (thirdPersonCameraRoot == null && thirdPersonCamera != null)
            thirdPersonCameraRoot = thirdPersonCamera.gameObject;

        if (firstPersonCamera == null)
            CreateFirstPersonCameraIfNeeded();
    }

    private void CreateFirstPersonCameraIfNeeded()
    {
        if (firstPersonCamera == null)
        {
            var camGo = new GameObject("FirstPersonCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = firstPersonLocalPosition;
            camGo.transform.localRotation = Quaternion.Euler(firstPersonLocalEuler);

            firstPersonCamera = camGo.AddComponent<Camera>();
            // 기본은 비활성(3인칭이 기본)
            camGo.SetActive(false);

            var look = camGo.AddComponent<ObservationFirstPersonLook>();
            look.sensitivity = firstPersonLookSensitivity;
            look.pitchMin = firstPersonPitchMin;
            look.pitchMax = firstPersonPitchMax;
            look.readMouseAxis = true;
        }

        // AudioListener 중복 방지
        var listener = firstPersonCamera.GetComponent<AudioListener>();
        if (listener != null)
            Destroy(listener);

        if (copySettingsFromThirdPerson && thirdPersonCamera != null && firstPersonCamera != null)
        {
            firstPersonCamera.fieldOfView = thirdPersonCamera.fieldOfView;
            firstPersonCamera.nearClipPlane = thirdPersonCamera.nearClipPlane;
            firstPersonCamera.farClipPlane = thirdPersonCamera.farClipPlane;
            firstPersonCamera.clearFlags = thirdPersonCamera.clearFlags;
            firstPersonCamera.backgroundColor = thirdPersonCamera.backgroundColor;
            firstPersonCamera.cullingMask = thirdPersonCamera.cullingMask;
            firstPersonCamera.depth = thirdPersonCamera.depth + 1f;
            firstPersonCamera.renderingPath = thirdPersonCamera.renderingPath;
            firstPersonCamera.allowHDR = thirdPersonCamera.allowHDR;
            firstPersonCamera.allowMSAA = thirdPersonCamera.allowMSAA;
            firstPersonCamera.orthographic = thirdPersonCamera.orthographic;
            firstPersonCamera.orthographicSize = thirdPersonCamera.orthographicSize;
        }
    }

    public void SetFirstPersonActive(bool active)
    {
        EnsureRig();

        if (thirdPersonCameraRoot == null && thirdPersonCamera != null)
            thirdPersonCameraRoot = thirdPersonCamera.gameObject;

        // 실수로 플레이어 루트(=이 리그가 붙은 오브젝트)나 상위 루트를 지정하면,
        // 1인칭 카메라도 같이 꺼져 전환이 실패할 수 있어 차단합니다.
        if (thirdPersonCameraRoot != null)
        {
            if (thirdPersonCameraRoot == gameObject || thirdPersonCameraRoot.transform.IsChildOf(transform))
            {
                // thirdPersonCameraRoot가 이 리그의 상위/동일/자식이면 SetActive로 전환 시 연쇄 비활성화 위험
                if (thirdPersonCamera != null)
                    thirdPersonCameraRoot = thirdPersonCamera.gameObject;
            }
        }

        if (thirdPersonCameraRoot != null)
        {
            if (string.IsNullOrEmpty(_savedThirdRootTag))
                _savedThirdRootTag = thirdPersonCameraRoot.tag;
        }
        if (firstPersonCamera != null)
        {
            if (string.IsNullOrEmpty(_savedFirstRootTag))
                _savedFirstRootTag = firstPersonCamera.gameObject.tag;
        }

        // 전환 방향 동기화:
        // - 3인칭 -> 1인칭: 3인칭 카메라가 보던 방향으로 1인칭 시작
        // - 1인칭 -> 3인칭: 1인칭 카메라가 보던 방향으로 3인칭 복귀
        if (active)
        {
            if (firstPersonCamera != null && thirdPersonCamera != null)
            {
                _savedThirdPersonFov = thirdPersonCamera.fieldOfView;
                _savedFirstPersonFov = firstPersonCamera.fieldOfView;

                // 살짝 줌인
                firstPersonCamera.fieldOfView = Mathf.Clamp(_savedThirdPersonFov + firstPersonFovOffset, 10f, 120f);

                firstPersonCamera.transform.position = thirdPersonCamera.transform.position;
                firstPersonCamera.transform.rotation = thirdPersonCamera.transform.rotation;

                var look = firstPersonCamera.GetComponent<ObservationFirstPersonLook>();
                if (look != null)
                    look.SyncFromRotation(thirdPersonCamera.transform.rotation);
            }
        }
        else
        {
            if (firstPersonCamera != null && thirdPersonCamera != null)
            {
                thirdPersonCamera.transform.position = firstPersonCamera.transform.position;
                thirdPersonCamera.transform.rotation = firstPersonCamera.transform.rotation;

                // FOV 복구
                if (_savedThirdPersonFov > 0f)
                    thirdPersonCamera.fieldOfView = _savedThirdPersonFov;
                if (_savedFirstPersonFov > 0f)
                    firstPersonCamera.fieldOfView = _savedFirstPersonFov;
            }
        }

        // Camera.main 기반 로직을 위해 MainCamera 태그는 한 쪽만 갖도록 전환
        if (active)
        {
            if (thirdPersonCameraRoot != null && thirdPersonCameraRoot.CompareTag("MainCamera"))
                thirdPersonCameraRoot.tag = "Untagged";
            if (firstPersonCamera != null)
                firstPersonCamera.gameObject.tag = "MainCamera";
        }
        else
        {
            if (firstPersonCamera != null && firstPersonCamera.gameObject.CompareTag("MainCamera"))
                firstPersonCamera.gameObject.tag = "Untagged";
            if (thirdPersonCameraRoot != null)
                thirdPersonCameraRoot.tag = "MainCamera";
        }

        // 핵심: 카메라가 들어있는 오브젝트 자체를 껐다/켰다
        if (thirdPersonCameraRoot != null)
            thirdPersonCameraRoot.SetActive(!active);
        if (firstPersonCamera != null)
            firstPersonCamera.gameObject.SetActive(active);
    }
}

