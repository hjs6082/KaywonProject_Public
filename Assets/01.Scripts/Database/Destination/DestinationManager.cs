// =============================================================================
// DestinationManager.cs
// =============================================================================
// 설명: 목적지 체인을 관리하는 싱글톤 매니저
// 용도: 현재 활성 목적지를 추적하고, 목적지 완료 시 다음 SO로 자동 전환한다.
// 사용법:
//   1. 씬에 빈 GameObject를 만들고 이 컴포넌트를 붙인다.
//   2. Start Destination 필드에 첫 번째 DestinationData SO를 할당한다.
//   3. 씬에 배치된 각 DestinationPing이 자신의 SO와 일치하면 자동으로 활성화된다.
//   4. 목적지 도달 시 DestinationPing.Complete()를 호출하면 다음 목적지로 전환된다.
// =============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace GameDatabase.Destination
{
    /// <summary>
    /// 목적지 체인을 관리하는 싱글톤 매니저.
    /// 현재 활성 DestinationData를 보유하며, 완료 시 다음 SO로 자동 전환한다.
    /// </summary>
    public class DestinationManager : MonoBehaviour
    {
        // =============================================================================
        // 싱글톤
        // =============================================================================

        private static DestinationManager _instance;
        public static DestinationManager Instance => _instance;

        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== 목적지 체인 ===")]

        [Tooltip("게임 시작 시 첫 번째로 활성화할 목적지 SO.\n비워두면 목적지 없이 시작한다.")]
        [SerializeField] private DestinationData _startDestination;

        [Tooltip("씬 시작 시 자동으로 첫 번째 목적지를 활성화할지 여부")]
        [SerializeField] private bool _autoStartOnAwake = true;

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("목적지가 변경될 때 호출된다. (새 DestinationData 전달)")]
        public UnityEvent<DestinationData> OnDestinationChanged;

        [Tooltip("목적지 체인이 모두 완료되었을 때 호출된다.")]
        public UnityEvent OnAllDestinationsCompleted;

        // =============================================================================
        // 상태
        // =============================================================================

        private DestinationData _currentDestination;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>현재 활성 목적지 SO. null이면 활성 목적지 없음</summary>
        public DestinationData CurrentDestination => _currentDestination;

        /// <summary>현재 활성 목적지가 있는지 여부</summary>
        public bool HasActiveDestination => _currentDestination != null;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                Debug.Log("[DestinationManager] 싱글톤 초기화 완료");
            }
            else if (_instance != this)
            {
                Debug.LogWarning("[DestinationManager] 중복 인스턴스 감지, 제거됨");
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (_autoStartOnAwake && _startDestination != null)
            {
                Debug.Log($"[DestinationManager] 자동 시작 목적지: {_startDestination.DestinationName}");
                SetDestination(_startDestination);
            }
            else if (_autoStartOnAwake && _startDestination == null)
            {
                Debug.LogWarning("[DestinationManager] AutoStart가 활성화되어 있지만 Start Destination이 비어 있습니다.");
            }
        }

        private void Update()
        {
            // TODO: 테스트용 - 빌드 전 제거
            if (Input.GetKeyDown(KeyCode.K))
            {
                Debug.Log("[DestinationManager] [테스트] K키 → CompleteCurrentDestination()");
                CompleteCurrentDestination();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                Debug.Log("[DestinationManager] 인스턴스 해제");
            }
        }

        // =============================================================================
        // 공개 API
        // =============================================================================

        /// <summary>
        /// 목적지를 직접 지정한다.
        /// 씬 내 해당 SO를 가진 DestinationPing이 자동으로 활성화된다.
        /// </summary>
        /// <param name="destination">활성화할 목적지 SO. null 가능 (목적지 초기화)</param>
        public void SetDestination(DestinationData destination)
        {
            _currentDestination = destination;
            OnDestinationChanged?.Invoke(_currentDestination);

            if (destination != null)
                Debug.Log($"[DestinationManager] 목적지 변경: {destination.DestinationName} ({destination.DestinationID})");
            else
                Debug.Log("[DestinationManager] 활성 목적지 없음");
        }

        /// <summary>
        /// 현재 목적지를 완료 처리하고 다음 목적지 SO로 전환한다.
        /// Next Destination이 없으면 OnAllDestinationsCompleted 이벤트가 발생한다.
        /// </summary>
        public void CompleteCurrentDestination()
        {
            if (_currentDestination == null)
            {
                Debug.LogWarning("[DestinationManager] 완료 처리할 활성 목적지가 없습니다.");
                return;
            }

            DestinationData completed = _currentDestination;

            if (completed.HasNextDestination)
            {
                SetDestination(completed.NextDestination);
            }
            else
            {
                // 체인 종료
                _currentDestination = null;
                OnDestinationChanged?.Invoke(null);
                OnAllDestinationsCompleted?.Invoke();
                Debug.Log("[DestinationManager] 모든 목적지 완료");
            }
        }

        /// <summary>
        /// 목적지 체인을 처음부터 다시 시작한다.
        /// </summary>
        public void ResetToStart()
        {
            Debug.Log("[DestinationManager] 목적지 체인 처음으로 리셋");
            SetDestination(_startDestination);
        }

        // =============================================================================
        // 테스트 / 인스펙터 연동용
        // =============================================================================

        [Header("=== 인스펙터 연동 목적지 ===")]

        [Tooltip("GoToOverrideDestination() 호출 시 이동할 목적지 SO.\nUnityEvent에서 파라미터 없이 호출할 때 사용.")]
        [SerializeField] private DestinationData _overrideDestination;

        /// <summary>
        /// _overrideDestination으로 목적지를 변경한다.
        /// UnityEvent (파라미터 없음) 에서 직접 호출 가능.
        /// 예) DestinationPing.OnCompleted → GoToOverrideDestination()
        /// </summary>
        public void GoToOverrideDestination()
        {
            if (_overrideDestination == null)
            {
                Debug.LogWarning("[DestinationManager] Override Destination이 비어 있습니다. 인스펙터에서 할당해주세요.");
                return;
            }

            Debug.Log($"[DestinationManager] Override 목적지로 이동: {_overrideDestination.DestinationName}");
            SetDestination(_overrideDestination);
        }
    }
}
