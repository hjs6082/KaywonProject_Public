// =============================================================================
// WorldInteractionDetector.cs
// =============================================================================
// 설명: 플레이어 측에서 주변의 WorldInteractable을 감지하고 관리
// 용도: 범위 내 가장 가까운 상호작용 오브젝트 추적, 입력 중앙 처리
// 작동 방식:
//   1. 주기적으로 주변 WorldInteractable 검색
//   2. 가장 가까운 상호작용 가능 오브젝트 추적
//   3. 상호작용 키 입력 시 해당 오브젝트와 상호작용
//   4. 화면 중앙 레이캐스트로 조준 중인 WorldInteractable의 OutlineComponent 활성화
// 사용법: PlayerController 또는 플레이어 오브젝트에 추가
// =============================================================================

using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Modules.Rendering.Outline;

namespace GameDatabase.Player
{
    /// <summary>
    /// 플레이어 측 상호작용 감지기
    /// 주변의 WorldInteractable을 감지하고 관리합니다.
    /// </summary>
    public class WorldInteractionDetector : MonoBehaviour
    {
        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== 감지 설정 ===")]

        [Tooltip("감지 범위 (이 범위 내의 WorldInteractable만 감지)")]
        [Range(1f, 30f)]
        [SerializeField] private float _detectionRange = 15f;

        [Tooltip("감지 업데이트 간격 (초)")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _detectionInterval = 0.2f;

        [Tooltip("시야 내에 있는 오브젝트만 상호작용 가능")]
        [SerializeField] private bool _requireLineOfSight = false;

        [Tooltip("시야 체크 레이어 마스크")]
        [SerializeField] private LayerMask _lineOfSightMask = ~0;

        // =============================================================================
        // 입력 설정
        // =============================================================================

        [Header("=== 입력 설정 ===")]

        [Tooltip("중앙 입력 처리 사용 (개별 오브젝트 입력 비활성화)")]
        [SerializeField] private bool _centralizedInput = false;

        [Tooltip("상호작용 키 (중앙 입력 사용 시)")]
        [SerializeField] private KeyCode _interactKey = KeyCode.F;

        // =============================================================================
        // UI 설정
        // =============================================================================

        [Header("=== UI 설정 ===")]

        [Tooltip("가장 가까운 오브젝트만 UI 표시")]
        [SerializeField] private bool _showOnlyClosest = true;

        // =============================================================================
        // 아웃라인 설정
        // =============================================================================

        [Header("=== 아웃라인 (화면 중앙 Aim) ===")]

        [Tooltip("화면 중앙 레이캐스트로 조준 중인 오브젝트에 아웃라인 표시 여부")]
        [SerializeField] private bool _enableAimOutline = true;

        [Tooltip("아웃라인 레이캐스트 최대 거리")]
        [Range(1f, 50f)]
        [SerializeField] private float _outlineRayDistance = 20f;

        [Tooltip("아웃라인 레이캐스트 레이어 마스크")]
        [SerializeField] private LayerMask _outlineRayMask = ~0;

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("가장 가까운 상호작용 오브젝트 변경 시")]
        public UnityEvent<WorldInteractable> OnClosestChanged;

        [Tooltip("상호작용 가능한 오브젝트가 없어질 때")]
        public UnityEvent OnNoInteractable;

        [Tooltip("상호작용 실행 시")]
        public UnityEvent<WorldInteractable> OnInteracted;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        // 감지된 상호작용 오브젝트 목록
        private List<WorldInteractable> _nearbyInteractables = new List<WorldInteractable>();

        // 가장 가까운 상호작용 오브젝트
        private WorldInteractable _closestInteractable;

        // 마지막 감지 시간
        private float _lastDetectionTime;

        // PlayerController 참조
        private PlayerController _playerController;

        // 현재 아웃라인이 생성된 오브젝트
        private GameObject _currentOutlineTarget;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 가장 가까운 상호작용 오브젝트
        /// </summary>
        public WorldInteractable ClosestInteractable => _closestInteractable;

        /// <summary>
        /// 상호작용 가능한 오브젝트가 있는지
        /// </summary>
        public bool HasInteractable => _closestInteractable != null && _closestInteractable.CanInteract;

        /// <summary>
        /// 감지된 모든 상호작용 오브젝트
        /// </summary>
        public IReadOnlyList<WorldInteractable> NearbyInteractables => _nearbyInteractables;

        /// <summary>
        /// 감지 범위
        /// </summary>
        public float DetectionRange
        {
            get => _detectionRange;
            set => _detectionRange = value;
        }

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Start()
        {
            // PlayerController 참조
            _playerController = GetComponent<PlayerController>();
            if (_playerController == null)
            {
                _playerController = GetComponentInParent<PlayerController>();
            }
            if (_playerController == null)
            {
                _playerController = PlayerController.Instance;
            }
        }

        private void Update()
        {
            // 감지 업데이트
            if (Time.time - _lastDetectionTime >= _detectionInterval)
            {
                UpdateDetection();
                _lastDetectionTime = Time.time;
            }

            // 중앙 입력 처리
            if (_centralizedInput && HasInteractable)
            {
                HandleInput();
            }

            // 화면 중앙 Aim 아웃라인 업데이트
            if (_enableAimOutline)
            {
                UpdateAimOutline();
            }
        }

        // =============================================================================
        // 감지
        // =============================================================================

        /// <summary>
        /// 상호작용 오브젝트 감지 업데이트
        /// </summary>
        private void UpdateDetection()
        {
            // 이전 가장 가까운 오브젝트 저장
            WorldInteractable previousClosest = _closestInteractable;

            // 목록 초기화
            _nearbyInteractables.Clear();
            _closestInteractable = null;

            // 범위 내 모든 WorldInteractable 찾기
            Collider[] colliders = Physics.OverlapSphere(transform.position, _detectionRange);
            float closestDistance = float.MaxValue;

            foreach (Collider col in colliders)
            {
                // WorldInteractable 컴포넌트 찾기
                WorldInteractable interactable = col.GetComponent<WorldInteractable>();
                if (interactable == null)
                    interactable = col.GetComponentInParent<WorldInteractable>();
                if (interactable == null)
                    interactable = col.GetComponentInChildren<WorldInteractable>();

                if (interactable == null) continue;
                if (!interactable.CanInteract) continue;

                // 시야 체크
                if (_requireLineOfSight && !HasLineOfSight(interactable.transform.position))
                {
                    continue;
                }

                // 목록에 추가
                _nearbyInteractables.Add(interactable);

                // 거리 계산
                float distance = Vector3.Distance(transform.position, interactable.transform.position);

                // 가장 가까운 오브젝트 갱신
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    _closestInteractable = interactable;
                }
            }

            // 가장 가까운 오브젝트 변경 시 이벤트 발생
            if (_closestInteractable != previousClosest)
            {
                if (_closestInteractable != null)
                {
                    OnClosestChanged?.Invoke(_closestInteractable);
                }
                else
                {
                    OnNoInteractable?.Invoke();
                }

                // UI 표시 관리
                if (_showOnlyClosest)
                {
                    UpdateUIVisibility(previousClosest);
                }
            }
        }

        /// <summary>
        /// 시야 체크
        /// </summary>
        private bool HasLineOfSight(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            float distance = direction.magnitude;

            if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit, distance, _lineOfSightMask))
            {
                // 히트한 오브젝트가 타겟인지 확인
                return hit.point == targetPosition ||
                       Vector3.Distance(hit.point, targetPosition) < 0.5f;
            }

            return true;
        }

        /// <summary>
        /// UI 표시 상태 업데이트
        /// </summary>
        private void UpdateUIVisibility(WorldInteractable previousClosest)
        {
            // 이전 가장 가까운 오브젝트 UI 숨김
            // (WorldInteractable이 자체적으로 관리하므로 필요시에만)

            // 현재 가장 가까운 오브젝트 외에는 UI 숨김
            foreach (var interactable in _nearbyInteractables)
            {
                if (interactable != _closestInteractable)
                {
                    // WorldInteractable이 자체적으로 범위 체크하므로
                    // 여기서는 추가 처리 불필요
                }
            }
        }

        // =============================================================================
        // 입력 처리
        // =============================================================================

        /// <summary>
        /// 입력 처리
        /// </summary>
        private void HandleInput()
        {
            // 대화 중이면 무시
            if (_playerController != null && _playerController.IsInDialogue)
            {
                return;
            }

            // 상호작용 키 입력
            if (Input.GetKeyDown(_interactKey))
            {
                InteractWithClosest();
            }
        }

        /// <summary>
        /// 가장 가까운 오브젝트와 상호작용
        /// </summary>
        public void InteractWithClosest()
        {
            if (_closestInteractable == null) return;
            if (!_closestInteractable.CanInteract) return;

            _closestInteractable.Interact();
            OnInteracted?.Invoke(_closestInteractable);
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 특정 오브젝트와 강제 상호작용
        /// </summary>
        public void InteractWith(WorldInteractable interactable)
        {
            if (interactable == null) return;
            if (!interactable.CanInteract) return;

            interactable.Interact();
            OnInteracted?.Invoke(interactable);
        }

        /// <summary>
        /// 감지 강제 갱신
        /// </summary>
        public void ForceUpdateDetection()
        {
            UpdateDetection();
            _lastDetectionTime = Time.time;
        }

        /// <summary>
        /// 특정 오브젝트가 범위 내에 있는지 확인
        /// </summary>
        public bool IsInRange(WorldInteractable interactable)
        {
            if (interactable == null) return false;
            return _nearbyInteractables.Contains(interactable);
        }

        /// <summary>
        /// 특정 위치까지의 거리
        /// </summary>
        public float GetDistanceTo(WorldInteractable interactable)
        {
            if (interactable == null) return float.MaxValue;
            return Vector3.Distance(transform.position, interactable.transform.position);
        }

        // =============================================================================
        // 아웃라인 제어
        // =============================================================================

        /// <summary>
        /// 화면 중앙에서 레이캐스트를 쏴서 맞은 오브젝트가
        /// 범위 내 WorldInteractable이면 아웃라인을 켜고, 아니면 끈다.
        /// </summary>
        private void UpdateAimOutline()
        {
            Camera cam = PlayerController.Instance?.MainCamera ?? Camera.main;
            if (cam == null) return;

            // 화면 정중앙에서 레이 생성
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            GameObject hitTarget = null;

            if (Physics.Raycast(ray, out RaycastHit hit, _outlineRayDistance, _outlineRayMask))
            {
                // 맞은 오브젝트가 범위 내 WorldInteractable인지 확인
                WorldInteractable interactable = hit.collider.GetComponent<WorldInteractable>();
                if (interactable == null)
                    interactable = hit.collider.GetComponentInParent<WorldInteractable>();
                if (interactable == null)
                    interactable = hit.collider.GetComponentInChildren<WorldInteractable>();

                if (interactable != null && _nearbyInteractables.Contains(interactable))
                {
                    // 현재 활성 목적지 오브젝트인지 확인
                    DestinationPing ping = interactable.GetComponent<DestinationPing>();
                    if (ping == null) ping = interactable.GetComponentInChildren<DestinationPing>();

                    // DestinationPing이 없거나 활성 상태인 경우에만 아웃라인 허용
                    if (ping == null || ping.IsPingActive)
                        hitTarget = hit.collider.gameObject;
                }
            }

            // 이전 타겟과 같으면 무시
            if (hitTarget == _currentOutlineTarget) return;

            // 이전 아웃라인 제거
            if (_currentOutlineTarget != null)
            {
                OutlineComponent old = _currentOutlineTarget.GetComponent<OutlineComponent>();
                if (old == null) old = _currentOutlineTarget.GetComponentInParent<OutlineComponent>();
                if (old != null) Destroy(old);
                _currentOutlineTarget = null;
            }

            // 새 아웃라인 생성
            if (hitTarget != null)
            {
                hitTarget.AddComponent<OutlineComponent>();
                _currentOutlineTarget = hitTarget;
            }
        }

        // =============================================================================
        // Gizmo (에디터 표시)
        // =============================================================================

        private void OnDrawGizmosSelected()
        {
            // 감지 범위 표시
            Gizmos.color = new Color(0, 0.5f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            // 가장 가까운 오브젝트 연결선
            if (_closestInteractable != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _closestInteractable.transform.position);
            }
        }
    }
}
