// =============================================================================
// InvestigationPoint.cs
// =============================================================================
// 설명: 조사 구역 내 개별 조사 포인트
// 용도: 포인트앤클릭 모드에서 클릭하여 대사/아이템 이벤트를 발생시키는 포인트
// 사용법:
//   1. InvestigationZone의 자식 오브젝트에 이 컴포넌트 추가
//   2. Collider 추가 (Trigger 권장)
//   3. Inspector에서 대사, 아이템 등 설정
// =============================================================================

using UnityEngine;
using UnityEngine.Events;
using GameDatabase.Dialogue;
using GameDatabase.UI;

namespace GameDatabase.Player
{
    /// <summary>
    /// 조사 구역 내 개별 조사 포인트
    /// IInspectable을 구현하여 기존 PlayerInspection 레이캐스트 시스템과 호환
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InvestigationPoint : MonoBehaviour, IInspectable
    {
        // =============================================================================
        // 조사 정보
        // =============================================================================

        [Header("=== 조사 정보 ===")]

        [Tooltip("조사 포인트 이름")]
        [SerializeField] private string _pointTitle = "조사 포인트";

        [Tooltip("조사 포인트 설명 (호버 시 표시)")]
        [TextArea(2, 4)]
        [SerializeField] private string _pointDescription = "클릭하여 조사합니다.";

        // =============================================================================
        // 대사 설정
        // =============================================================================

        [Header("=== 대사 설정 ===")]

        [Tooltip("클릭 시 재생할 대사")]
        [SerializeField] private DialogueData _dialogue;

        [Tooltip("이미 조사한 후 재클릭 시 대사 (비어있으면 Zone의 기본 대사 사용)")]
        [SerializeField] private DialogueData _alreadyInspectedDialogue;

        // =============================================================================
        // 아이템 설정
        // =============================================================================

        [Header("=== 아이템 설정 ===")]

        [Tooltip("아이템 지급 여부")]
        [SerializeField] private bool _giveItem = false;

        [Tooltip("지급할 아이템 ID")]
        [SerializeField] private string _itemId;

        // =============================================================================
        // 시각적 피드백
        // =============================================================================

        [Header("=== 시각적 피드백 ===")]

        [Tooltip("클릭 가능 표시 오브젝트 (동그라미 등)")]
        [SerializeField] private GameObject _visualIndicator;

        [Tooltip("완료 시 표시할 오브젝트 (체크마크 등)")]
        [SerializeField] private GameObject _completedIndicator;

        [Tooltip("호버 시 커서 타입")]
        [SerializeField] private CursorState _hoverCursorState = CursorState.Inspect;

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("조사 완료 시 호출")]
        public UnityEvent OnPointInspected;

        [Tooltip("아이템 획득 시 호출 (아이템 ID 전달)")]
        public UnityEvent<string> OnItemObtained;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private bool _isCompleted = false;
        private bool _isHovered = false;
        private InvestigationZone _parentZone;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 조사 완료 여부
        /// </summary>
        public bool IsCompleted => _isCompleted;

        /// <summary>
        /// 소속된 조사 구역
        /// </summary>
        public InvestigationZone ParentZone
        {
            get => _parentZone;
            set => _parentZone = value;
        }

        // =============================================================================
        // IInspectable 구현
        // =============================================================================

        public string InspectTitle => _pointTitle;
        public string InspectDescription => _pointDescription;

        public bool CanInspect => true; // 항상 클릭 가능 (완료 여부와 무관)

        public void OnInspect(PlayerController player)
        {
            if (_isCompleted)
            {
                // 이미 조사한 포인트 → 재조사 대사 재생
                PlayAlreadyInspectedDialogue();
                return;
            }

            // 첫 조사
            _isCompleted = true;

            // 아이템 지급
            if (_giveItem && !string.IsNullOrEmpty(_itemId))
            {
                OnItemObtained?.Invoke(_itemId);
                Debug.Log($"[InvestigationPoint] 아이템 획득: {_itemId}");
            }

            // 시각적 피드백 업데이트
            UpdateVisuals();

            // 이벤트 발생
            OnPointInspected?.Invoke();

            // 대사 시작
            PlayDialogue();

            // 부모 존에 완료 알림
            if (_parentZone != null)
            {
                _parentZone.OnPointCompleted(this);
            }

            Debug.Log($"[InvestigationPoint] '{_pointTitle}' 조사 완료");
        }

        public void OnHoverEnter()
        {
            if (_isHovered) return;
            _isHovered = true;

            // 커서 변경
            if (PlayerController.Instance?.Cursor != null)
            {
                PlayerController.Instance.Cursor.SetCursorState(_hoverCursorState);
            }
        }

        public void OnHoverExit()
        {
            if (!_isHovered) return;
            _isHovered = false;

            // 커서 복원
            if (PlayerController.Instance?.Cursor != null)
            {
                PlayerController.Instance.Cursor.SetCursorState(CursorState.Normal);
            }
        }

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // Collider 확인
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning($"[InvestigationPoint] '{gameObject.name}'의 Collider가 Trigger가 아닙니다. Trigger 사용을 권장합니다.");
            }

            // 부모 존 자동 할당
            if (_parentZone == null)
            {
                _parentZone = GetComponentInParent<InvestigationZone>();
            }
        }

        private void Start()
        {
            UpdateVisuals();
        }

        private void OnDisable()
        {
            if (_isHovered)
            {
                OnHoverExit();
            }
        }

        // =============================================================================
        // 대사 처리
        // =============================================================================

        /// <summary>
        /// 첫 조사 대사 재생
        /// </summary>
        private void PlayDialogue()
        {
            if (_dialogue == null)
            {
                Debug.LogWarning($"[InvestigationPoint] '{gameObject.name}'에 대사가 설정되지 않았습니다.");
                return;
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(_dialogue);
            }
        }

        /// <summary>
        /// 이미 조사한 포인트 재클릭 대사 재생
        /// </summary>
        private void PlayAlreadyInspectedDialogue()
        {
            // 개별 포인트에 설정된 대사 우선, 없으면 존의 기본 대사 사용
            DialogueData dialogue = _alreadyInspectedDialogue;
            if (dialogue == null && _parentZone != null)
            {
                dialogue = _parentZone.AlreadyInspectedDialogue;
            }

            if (dialogue == null)
            {
                Debug.Log($"[InvestigationPoint] '{_pointTitle}' - 이미 조사 완료 (재조사 대사 없음)");
                return;
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(dialogue);
            }
        }

        // =============================================================================
        // 시각적 피드백
        // =============================================================================

        /// <summary>
        /// 시각적 표시 업데이트
        /// </summary>
        private void UpdateVisuals()
        {
            if (_visualIndicator != null)
            {
                _visualIndicator.SetActive(!_isCompleted);
            }

            if (_completedIndicator != null)
            {
                _completedIndicator.SetActive(_isCompleted);
            }
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 조사 상태 초기화
        /// </summary>
        public void ResetPoint()
        {
            _isCompleted = false;
            UpdateVisuals();
        }

        // =============================================================================
        // Gizmo
        // =============================================================================

        private void OnDrawGizmos()
        {
            Gizmos.color = _isCompleted ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Gizmos.DrawIcon(transform.position, "d_ViewToolZoom", true);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }
    }
}
