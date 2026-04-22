// =============================================================================
// InspectPoint.cs
// =============================================================================
// 설명: 씬에서 특정 위치를 조사할 수 있는 포인트
// 용도: 특정 지점 클릭 시 이벤트 발생, 대화 시작, 씬 전환 등
// 사용법:
//   1. 빈 게임오브젝트에 이 컴포넌트 추가
//   2. Collider 추가 (Trigger 권장)
//   3. Inspector에서 설정 후 이벤트 연결
// =============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace GameDatabase.Player
{
    /// <summary>
    /// 조사 포인트 컴포넌트
    /// 씬의 특정 위치를 클릭하여 상호작용할 수 있습니다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InspectPoint : MonoBehaviour, IInspectable
    {
        // =============================================================================
        // 조사 정보
        // =============================================================================

        [Header("=== 조사 정보 ===")]

        [Tooltip("조사 포인트 이름")]
        [SerializeField] private string _inspectTitle = "조사 포인트";

        [Tooltip("조사 설명")]
        [TextArea(3, 5)]
        [SerializeField] private string _inspectDescription = "클릭하여 조사합니다.";

        [Tooltip("조사 가능 여부")]
        [SerializeField] private bool _canInspect = true;

        [Tooltip("한 번만 조사 가능")]
        [SerializeField] private bool _oneTimeOnly = false;

        // =============================================================================
        // 동작 설정
        // =============================================================================

        [Header("=== 동작 설정 ===")]

        [Tooltip("조사 유형")]
        [SerializeField] private InspectPointType _inspectType = InspectPointType.Event;

        [Tooltip("조사 시 시작할 대화 (대화 유형일 때)")]
        [SerializeField] private GameDatabase.Dialogue.DialogueData _dialogue;

        [Tooltip("조사 시 이동할 씬 (씬 전환 유형일 때)")]
        [SerializeField] private string _targetSceneName;

        [Tooltip("조사 시 활성화할 오브젝트")]
        [SerializeField] private GameObject _activateObject;

        [Tooltip("조사 시 비활성화할 오브젝트")]
        [SerializeField] private GameObject _deactivateObject;

        // =============================================================================
        // 시각적 피드백
        // =============================================================================

        [Header("=== 시각적 피드백 ===")]

        [Tooltip("호버 시 표시할 인디케이터")]
        [SerializeField] private GameObject _hoverIndicator;

        [Tooltip("활성화 상태 표시 인디케이터")]
        [SerializeField] private GameObject _activeIndicator;

        [Tooltip("호버 시 커서 타입")]
        [SerializeField] private CursorState _hoverCursorState = CursorState.Inspect;

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("조사 시 호출")]
        public UnityEvent OnInspected;

        [Tooltip("조사 시 호출 (PlayerController 전달)")]
        public UnityEvent<PlayerController> OnInspectedWithPlayer;

        [Tooltip("호버 진입 시 호출")]
        public UnityEvent OnHoverEntered;

        [Tooltip("호버 종료 시 호출")]
        public UnityEvent OnHoverExited;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private bool _hasInspected = false;
        private bool _isHovered = false;
        private Collider _collider;

        // =============================================================================
        // 열거형
        // =============================================================================

        /// <summary>
        /// 조사 포인트 유형
        /// </summary>
        public enum InspectPointType
        {
            /// <summary>
            /// 이벤트만 발생
            /// </summary>
            Event,

            /// <summary>
            /// 대화 시작
            /// </summary>
            Dialogue,

            /// <summary>
            /// 씬 전환
            /// </summary>
            SceneChange,

            /// <summary>
            /// 오브젝트 토글
            /// </summary>
            ObjectToggle
        }

        // =============================================================================
        // IInspectable 구현
        // =============================================================================

        public string InspectTitle => _inspectTitle;
        public string InspectDescription => _inspectDescription;

        public bool CanInspect
        {
            get
            {
                if (_oneTimeOnly && _hasInspected)
                {
                    return false;
                }
                return _canInspect;
            }
        }

        public void OnInspect(PlayerController player)
        {
            if (!CanInspect) return;

            _hasInspected = true;

            // 유형에 따른 동작 실행
            ExecuteInspectAction(player);

            // 이벤트 발생
            OnInspected?.Invoke();
            OnInspectedWithPlayer?.Invoke(player);

            Debug.Log($"[InspectPoint] '{_inspectTitle}' 조사됨");
        }

        public void OnHoverEnter()
        {
            if (_isHovered) return;
            _isHovered = true;

            // 인디케이터 표시
            if (_hoverIndicator != null)
            {
                _hoverIndicator.SetActive(true);
            }

            // 커서 변경
            if (PlayerController.Instance?.Cursor != null)
            {
                PlayerController.Instance.Cursor.SetCursorState(_hoverCursorState);
            }

            OnHoverEntered?.Invoke();
        }

        public void OnHoverExit()
        {
            if (!_isHovered) return;
            _isHovered = false;

            // 인디케이터 숨김
            if (_hoverIndicator != null)
            {
                _hoverIndicator.SetActive(false);
            }

            // 커서 복원
            if (PlayerController.Instance?.Cursor != null)
            {
                PlayerController.Instance.Cursor.SetCursorState(CursorState.Normal);
            }

            OnHoverExited?.Invoke();
        }

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            _collider = GetComponent<Collider>();

            // Trigger로 설정 권장
            if (_collider != null && !_collider.isTrigger)
            {
                Debug.LogWarning($"[InspectPoint] '{gameObject.name}'의 Collider가 Trigger가 아닙니다. Trigger 사용을 권장합니다.");
            }
        }

        private void Start()
        {
            // 인디케이터 초기화
            if (_hoverIndicator != null)
            {
                _hoverIndicator.SetActive(false);
            }

            // 활성화 인디케이터 설정
            UpdateActiveIndicator();
        }

        private void OnDisable()
        {
            if (_isHovered)
            {
                OnHoverExit();
            }
        }

        // =============================================================================
        // 동작 실행
        // =============================================================================

        /// <summary>
        /// 조사 동작 실행
        /// </summary>
        /// <param name="player">플레이어 컨트롤러</param>
        private void ExecuteInspectAction(PlayerController player)
        {
            switch (_inspectType)
            {
                case InspectPointType.Event:
                    // 이벤트만 발생 (OnInspected에서 처리)
                    break;

                case InspectPointType.Dialogue:
                    StartDialogue();
                    break;

                case InspectPointType.SceneChange:
                    ChangeScene();
                    break;

                case InspectPointType.ObjectToggle:
                    ToggleObjects();
                    break;
            }
        }

        /// <summary>
        /// 대화 시작
        /// </summary>
        private void StartDialogue()
        {
            if (_dialogue == null)
            {
                Debug.LogWarning($"[InspectPoint] '{gameObject.name}'에 대화가 설정되지 않았습니다.");
                return;
            }

            if (UI.DialogueManager.Instance != null)
            {
                UI.DialogueManager.Instance.StartDialogue(_dialogue);
            }
            else
            {
                Debug.LogError("[InspectPoint] DialogueManager를 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// 씬 전환
        /// </summary>
        private void ChangeScene()
        {
            if (string.IsNullOrEmpty(_targetSceneName))
            {
                Debug.LogWarning($"[InspectPoint] '{gameObject.name}'에 대상 씬이 설정되지 않았습니다.");
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(_targetSceneName);
        }

        /// <summary>
        /// 오브젝트 토글
        /// </summary>
        private void ToggleObjects()
        {
            if (_activateObject != null)
            {
                _activateObject.SetActive(true);
            }

            if (_deactivateObject != null)
            {
                _deactivateObject.SetActive(false);
            }
        }

        /// <summary>
        /// 활성화 인디케이터 업데이트
        /// </summary>
        private void UpdateActiveIndicator()
        {
            if (_activeIndicator != null)
            {
                _activeIndicator.SetActive(CanInspect);
            }
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 조사 가능 여부 설정
        /// </summary>
        public void SetCanInspect(bool canInspect)
        {
            _canInspect = canInspect;
            UpdateActiveIndicator();
        }

        /// <summary>
        /// 조사 상태 초기화
        /// </summary>
        public void ResetInspection()
        {
            _hasInspected = false;
            UpdateActiveIndicator();
        }

        /// <summary>
        /// 대화 설정
        /// </summary>
        public void SetDialogue(GameDatabase.Dialogue.DialogueData dialogue)
        {
            _dialogue = dialogue;
            _inspectType = InspectPointType.Dialogue;
        }

        /// <summary>
        /// 대상 씬 설정
        /// </summary>
        public void SetTargetScene(string sceneName)
        {
            _targetSceneName = sceneName;
            _inspectType = InspectPointType.SceneChange;
        }

        // =============================================================================
        // Gizmo (에디터 표시)
        // =============================================================================

        private void OnDrawGizmos()
        {
            // 조사 포인트 위치 표시
            Gizmos.color = CanInspect ? Color.cyan : Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            // 아이콘 표시
            Gizmos.DrawIcon(transform.position, "d_ViewToolZoom", true);
        }

        private void OnDrawGizmosSelected()
        {
            // 선택 시 더 눈에 띄게
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
