// =============================================================================
// BillboardUI.cs
// =============================================================================
// 설명: 항상 플레이어(카메라)를 바라보는 빌보드 컴포넌트
// 용도: World Space UI나 스프라이트가 항상 플레이어를 향하도록 회전
// 작동 방식:
//   - 매 프레임 타겟(플레이어/카메라) 방향으로 회전
//   - 옵션으로 Y축만 회전 가능 (수평 빌보드)
// =============================================================================

using UnityEngine;

namespace GameDatabase.Player
{
    /// <summary>
    /// 빌보드 모드
    /// </summary>
    public enum BillboardMode
    {
        /// <summary>
        /// 모든 축 회전 (완전히 카메라를 향함)
        /// </summary>
        Full,

        /// <summary>
        /// Y축만 회전 (수평 방향만 카메라를 향함)
        /// </summary>
        HorizontalOnly,

        /// <summary>
        /// 비활성화
        /// </summary>
        Disabled
    }

    /// <summary>
    /// 빌보드 타겟
    /// </summary>
    public enum BillboardTarget
    {
        /// <summary>
        /// 메인 카메라
        /// </summary>
        MainCamera,

        /// <summary>
        /// 플레이어 Transform
        /// </summary>
        Player,

        /// <summary>
        /// 커스텀 Transform
        /// </summary>
        Custom
    }

    /// <summary>
    /// 빌보드 컴포넌트
    /// 오브젝트가 항상 타겟을 바라보도록 회전시킵니다.
    /// </summary>
    public class BillboardUI : MonoBehaviour
    {
        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== 빌보드 설정 ===")]

        [Tooltip("빌보드 모드")]
        [SerializeField] private BillboardMode _mode = BillboardMode.HorizontalOnly;

        [Tooltip("바라볼 타겟")]
        [SerializeField] private BillboardTarget _target = BillboardTarget.MainCamera;

        [Tooltip("커스텀 타겟 (타겟이 Custom일 때)")]
        [SerializeField] private Transform _customTarget;

        [Tooltip("부드러운 회전 사용")]
        [SerializeField] private bool _smoothRotation = false;

        [Tooltip("부드러운 회전 속도")]
        [Range(1f, 30f)]
        [SerializeField] private float _smoothSpeed = 15f;

        [Tooltip("회전 반전 (등 뒤로 향하게)")]
        [SerializeField] private bool _invertRotation = false;

        // =============================================================================
        // 조건부 설정
        // =============================================================================

        [Header("=== 조건부 설정 ===")]

        [Tooltip("특정 거리 내에서만 빌보드 적용")]
        [SerializeField] private bool _distanceLimit = false;

        [Tooltip("빌보드 적용 최대 거리")]
        [SerializeField] private float _maxDistance = 20f;

        [Tooltip("특정 컴포넌트 활성화 시에만 작동")]
        [SerializeField] private Behaviour _requireEnabledComponent;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        // 타겟 Transform 캐시
        private Transform _targetTransform;

        // 초기 회전 저장
        private Quaternion _initialRotation;

        // 빌보드 활성화 여부
        private bool _isActive = true;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 빌보드 모드
        /// </summary>
        public BillboardMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        /// <summary>
        /// 빌보드 활성화 여부
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        /// <summary>
        /// 타겟 Transform
        /// </summary>
        public Transform TargetTransform => _targetTransform;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Start()
        {
            // 초기 회전 저장
            _initialRotation = transform.rotation;

            // 타겟 찾기
            FindTarget();
        }

        private void LateUpdate()
        {
            // 비활성화 상태면 무시
            if (_mode == BillboardMode.Disabled || !_isActive) return;

            // 조건 확인
            if (!CheckConditions()) return;

            // 빌보드 회전 적용
            ApplyBillboard();
        }

        // =============================================================================
        // 타겟 찾기
        // =============================================================================

        /// <summary>
        /// 타겟 찾기
        /// </summary>
        private void FindTarget()
        {
            switch (_target)
            {
                case BillboardTarget.MainCamera:
                    if (Camera.main != null)
                    {
                        _targetTransform = Camera.main.transform;
                    }
                    break;

                case BillboardTarget.Player:
                    // PlayerController 싱글톤 시도
                    if (PlayerController.Instance != null)
                    {
                        // 카메라가 있으면 카메라를, 없으면 플레이어 자체를
                        if (PlayerController.Instance.MainCamera != null)
                        {
                            _targetTransform = PlayerController.Instance.MainCamera.transform;
                        }
                        else
                        {
                            _targetTransform = PlayerController.Instance.transform;
                        }
                    }
                    else
                    {
                        // 태그로 찾기
                        GameObject player = GameObject.FindGameObjectWithTag("Player");
                        if (player != null)
                        {
                            _targetTransform = player.transform;
                        }
                    }
                    break;

                case BillboardTarget.Custom:
                    _targetTransform = _customTarget;
                    break;
            }

            if (_targetTransform == null)
            {
                Debug.LogWarning($"[BillboardUI] '{gameObject.name}': 타겟을 찾을 수 없습니다.");
            }
        }

        // =============================================================================
        // 조건 확인
        // =============================================================================

        /// <summary>
        /// 빌보드 조건 확인
        /// </summary>
        private bool CheckConditions()
        {
            // 타겟이 없으면 다시 찾기 시도
            if (_targetTransform == null)
            {
                FindTarget();
                if (_targetTransform == null) return false;
            }

            // 거리 제한 확인
            if (_distanceLimit)
            {
                float distance = Vector3.Distance(transform.position, _targetTransform.position);
                if (distance > _maxDistance)
                {
                    return false;
                }
            }

            // 컴포넌트 활성화 확인
            if (_requireEnabledComponent != null && !_requireEnabledComponent.enabled)
            {
                return false;
            }

            return true;
        }

        // =============================================================================
        // 빌보드 적용
        // =============================================================================

        /// <summary>
        /// 빌보드 회전 적용
        /// </summary>
        private void ApplyBillboard()
        {
            // 타겟 방향 계산
            Vector3 direction = _targetTransform.position - transform.position;

            if (direction == Vector3.zero) return;

            // 반전 옵션
            if (_invertRotation)
            {
                direction = -direction;
            }

            // 목표 회전 계산
            Quaternion targetRotation;

            switch (_mode)
            {
                case BillboardMode.Full:
                    targetRotation = Quaternion.LookRotation(direction);
                    break;

                case BillboardMode.HorizontalOnly:
                    direction.y = 0;
                    if (direction == Vector3.zero) return;
                    targetRotation = Quaternion.LookRotation(direction);
                    break;

                default:
                    return;
            }

            // 회전 적용
            if (_smoothRotation)
            {
                transform.rotation = Quaternion.Lerp(
                    transform.rotation,
                    targetRotation,
                    _smoothSpeed * Time.deltaTime
                );
            }
            else
            {
                transform.rotation = targetRotation;
            }
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 타겟 설정
        /// </summary>
        /// <param name="target">새 타겟 Transform</param>
        public void SetTarget(Transform target)
        {
            _target = BillboardTarget.Custom;
            _customTarget = target;
            _targetTransform = target;
        }

        /// <summary>
        /// 모드 설정
        /// </summary>
        /// <param name="mode">새 빌보드 모드</param>
        public void SetMode(BillboardMode mode)
        {
            _mode = mode;
        }

        /// <summary>
        /// 빌보드 일시 비활성화
        /// </summary>
        public void Disable()
        {
            _isActive = false;
        }

        /// <summary>
        /// 빌보드 활성화
        /// </summary>
        public void Enable()
        {
            _isActive = true;
        }

        /// <summary>
        /// 초기 회전으로 복원
        /// </summary>
        public void ResetRotation()
        {
            transform.rotation = _initialRotation;
        }

        /// <summary>
        /// 즉시 타겟을 바라보기
        /// </summary>
        public void LookAtTargetImmediate()
        {
            bool wasSmoothEnabled = _smoothRotation;
            _smoothRotation = false;
            ApplyBillboard();
            _smoothRotation = wasSmoothEnabled;
        }
    }
}
