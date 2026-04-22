using System.Collections;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 관찰 모드(Observation Mode):
/// - 어디서든 토글 가능
/// - 시작 시 3인칭 컨트롤러를 끄고(있다면) 1인칭 컨트롤러를 켭니다.
/// - 관찰 중에는 이동이 불가능하도록 1인칭 이동 속도를 0으로 만들고, 입력 move를 계속 0으로 유지합니다.
/// </summary>
public class ObservationModeController : MonoBehaviour
{
    public static bool IsActive { get; private set; }

    [Header("디버그")]
    public bool enableDebugLog = false;

    [Header("모델 표시")]
    [Tooltip("관찰 모드 진입 시 숨길 3인칭 플레이어 모델 루트(예: PlayerArmature). 비워두면 이름으로 자동 탐색합니다.")]
    public GameObject thirdPersonModelRootToHide;

    [Header("입력/이동 제한")]
    [Tooltip("관찰 중 이동/달리기 속도를 0으로 만들어 이동을 막습니다.")]
    public bool freezeMovement = true;

    [Tooltip("관찰 중 look(시선)은 허용하고, move만 0으로 고정합니다.")]
    public bool zeroMoveInputWhileActive = true;

    [Header("대상 컨트롤러")]
    public StarterAssets.FirstPersonController firstPersonController;
    public StarterAssets.ThirdPersonController thirdPersonController;
    public StarterAssets.StarterAssetsInputs starterInputs;
    public ThirdFirstPersonCameraRig cameraRig;

    private bool _savedModelRootActive;
    private float _savedMoveSpeed;
    private float _savedSprintSpeed;
    private bool _savedFirstEnabled;
    private bool _savedThirdEnabled;

    private void Awake()
    {
        if (firstPersonController == null)
            firstPersonController = FindObjectOfType<StarterAssets.FirstPersonController>();
        if (thirdPersonController == null)
            thirdPersonController = FindObjectOfType<StarterAssets.ThirdPersonController>();
        if (starterInputs == null)
            starterInputs = FindObjectOfType<StarterAssets.StarterAssetsInputs>();
        if (cameraRig == null && thirdPersonController != null)
            cameraRig = thirdPersonController.GetComponent<ThirdFirstPersonCameraRig>();
        if (cameraRig == null)
            cameraRig = FindObjectOfType<ThirdFirstPersonCameraRig>();

        if (thirdPersonModelRootToHide == null && thirdPersonController != null)
            thirdPersonModelRootToHide = FindModelRootUnder(thirdPersonController.transform);
    }

    public void Toggle()
    {
        if (IsActive) End();
        else Begin();
    }

    public void Begin()
    {
        if (IsActive) return;

        if (firstPersonController == null)
            firstPersonController = FindObjectOfType<StarterAssets.FirstPersonController>();
        if (thirdPersonController == null)
            thirdPersonController = FindObjectOfType<StarterAssets.ThirdPersonController>();
        if (starterInputs == null)
            starterInputs = FindObjectOfType<StarterAssets.StarterAssetsInputs>();
        if (cameraRig == null && thirdPersonController != null)
            cameraRig = thirdPersonController.GetComponent<ThirdFirstPersonCameraRig>();
        if (cameraRig == null)
            cameraRig = FindObjectOfType<ThirdFirstPersonCameraRig>();

        if (thirdPersonModelRootToHide == null && thirdPersonController != null)
            thirdPersonModelRootToHide = FindModelRootUnder(thirdPersonController.transform);

        _savedFirstEnabled = firstPersonController != null && firstPersonController.enabled;
        _savedThirdEnabled = thirdPersonController != null && thirdPersonController.enabled;

        // 3인칭 모델 숨기기
        if (thirdPersonModelRootToHide != null)
        {
            _savedModelRootActive = thirdPersonModelRootToHide.activeSelf;
            thirdPersonModelRootToHide.SetActive(false);
        }

        // 카메라: 3인칭 -> 1인칭 (Rig가 있으면 카메라만 토글)
        if (cameraRig != null)
            cameraRig.SetFirstPersonActive(true);
        else if (enableDebugLog)
            Debug.LogWarning("[ObservationModeController] cameraRig를 찾지 못해 카메라 전환을 건너뜁니다.", this);

        // 컨트롤러: 프로젝트에 1인칭 컨트롤러가 따로 있으면 그쪽을 켜고, 3인칭은 끕니다.
        if (thirdPersonController != null)
            thirdPersonController.enabled = false;
        if (firstPersonController != null)
            firstPersonController.enabled = true;
        else if (enableDebugLog)
            Debug.LogWarning("[ObservationModeController] FirstPersonController가 씬에 없어 1인칭 컨트롤러 활성화는 건너뜁니다(카메라 전환만 수행).", this);

        if (freezeMovement && firstPersonController != null)
        {
            _savedMoveSpeed = firstPersonController.MoveSpeed;
            _savedSprintSpeed = firstPersonController.SprintSpeed;
            firstPersonController.MoveSpeed = 0f;
            firstPersonController.SprintSpeed = 0f;
        }

        if (starterInputs != null)
            starterInputs.move = Vector2.zero;

        IsActive = true;

        if (enableDebugLog)
            Debug.Log("[ObservationModeController] Observation Begin", this);
    }

    public void End()
    {
        if (!IsActive) return;

        Quaternion desiredViewRot = Quaternion.identity;
        bool hasDesiredViewRot = false;
        if (cameraRig != null && cameraRig.firstPersonCamera != null)
        {
            desiredViewRot = cameraRig.firstPersonCamera.transform.rotation;
            hasDesiredViewRot = true;
        }

        // 속도 복구
        if (freezeMovement && firstPersonController != null)
        {
            firstPersonController.MoveSpeed = _savedMoveSpeed;
            firstPersonController.SprintSpeed = _savedSprintSpeed;
        }

        // 카메라 복구
        if (cameraRig != null)
            cameraRig.SetFirstPersonActive(false);

        // 3인칭 모델 복구
        if (thirdPersonModelRootToHide != null)
        {
            // 원래 켜져 있던 경우에만 복구
            if (_savedModelRootActive)
                thirdPersonModelRootToHide.SetActive(true);
        }

        // 컨트롤러 활성 상태 복구
        if (firstPersonController != null)
            firstPersonController.enabled = _savedFirstEnabled;
        if (thirdPersonController != null)
        {
            thirdPersonController.enabled = _savedThirdEnabled;
        }

        IsActive = false;

        if (hasDesiredViewRot)
        {
            // 오브젝트를 다시 켜는 순간(특히 3인칭 + Cinemachine) 내부에 저장된 yaw/pitch가 카메라를 덮어쓸 수 있어
            // 다음 프레임까지 포함해 1~2회 더 강제 동기화합니다.
            StartCoroutine(ApplyThirdPersonViewRotationAfterReenable(desiredViewRot));
        }

        if (enableDebugLog)
            Debug.Log("[ObservationModeController] Observation End", this);
    }

    private IEnumerator ApplyThirdPersonViewRotationAfterReenable(Quaternion viewRotation)
    {
        // 이 프레임에서도 한번 적용 (가능한 즉시)
        ApplyThirdPersonViewRotation(viewRotation);

        // 다음 프레임: Cinemachine/컨트롤러가 한 번 업데이트한 뒤 다시 덮어쓰기
        yield return null;
        ApplyThirdPersonViewRotation(viewRotation);

        // 한 프레임 더: FreeLook/Brain 업데이트 타이밍 차이 보정
        yield return null;
        ApplyThirdPersonViewRotation(viewRotation);
    }

    /// <summary>
    /// 1인칭에서 바라보던 방향을 3인칭 시네머신 타깃/플레이어 회전에 적용해,
    /// 3인칭 복귀 시 시점이 튀지 않도록 합니다.
    /// </summary>
    private void ApplyThirdPersonViewRotation(Quaternion viewRotation)
    {
        if (thirdPersonController == null)
            return;

        // 플레이어 yaw는 수평만 반영 (3인칭 컨트롤러는 보통 캐릭터 yaw 기반)
        float yaw = viewRotation.eulerAngles.y;
        thirdPersonController.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // ThirdPersonController 내부에 저장된 yaw/pitch도 맞춰야,
        // 다음 업데이트에서 \"관찰 시작 전\" 값으로 되돌아가는 현상을 막을 수 있습니다.
        TrySetThirdPersonPrivateAngles(thirdPersonController, viewRotation);

        // 시네머신 타깃은 전체 회전 반영
        if (thirdPersonController.CinemachineCameraTarget != null)
            thirdPersonController.CinemachineCameraTarget.transform.rotation = viewRotation;

        // MainCamera(3인칭)가 바로 다음 프레임에 Cinemachine으로 덮일 수 있으므로,
        // 타깃을 맞춰두는 게 핵심입니다.
        if (cameraRig != null && cameraRig.thirdPersonCamera != null)
            cameraRig.thirdPersonCamera.transform.rotation = viewRotation;
    }

    private static void TrySetThirdPersonPrivateAngles(StarterAssets.ThirdPersonController tpc, Quaternion viewRotation)
    {
        if (tpc == null) return;

        // Starter Assets ThirdPersonController의 private 필드:
        // private float _cinemachineTargetYaw;
        // private float _cinemachineTargetPitch;
        // 버전 차이 대비: 리플렉션으로 존재할 때만 세팅
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo yawField = tpc.GetType().GetField("_cinemachineTargetYaw", flags);
        FieldInfo pitchField = tpc.GetType().GetField("_cinemachineTargetPitch", flags);
        if (yawField == null && pitchField == null) return;

        Vector3 e = viewRotation.eulerAngles;
        float yaw = e.y;
        float pitch = e.x;
        if (pitch > 180f) pitch -= 360f;

        if (yawField != null) yawField.SetValue(tpc, yaw);
        if (pitchField != null) pitchField.SetValue(tpc, pitch);
    }

    private void Update()
    {
        if (!IsActive) return;

        if (zeroMoveInputWhileActive && starterInputs != null)
            starterInputs.move = Vector2.zero;
    }

    private static GameObject FindModelRootUnder(Transform root)
    {
        if (root == null) return null;

        // 가장 흔한 이름 우선
        Transform t = root.Find("PlayerArmature");
        if (t != null) return t.gameObject;

        // 스킨드 메쉬가 있는 가장 위 루트를 찾아서 그 부모를 모델 루트로 간주
        var smr = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (smr != null)
        {
            // 가능하면 Armature/Model 같은 상위로 끌어올림
            Transform cur = smr.transform;
            for (int i = 0; i < 6 && cur != null && cur.parent != root; i++)
                cur = cur.parent;
            return cur != null ? cur.gameObject : smr.gameObject;
        }

        return null;
    }
}

