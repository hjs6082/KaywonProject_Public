// =============================================================================
// InvestigationZone.cs
// =============================================================================
// 설명: 조사 구역 컴포넌트
// 용도: 포인트앤클릭 조사 모드의 구역을 정의하고 카메라/포인트를 관리
// 사용법:
//   1. 빈 게임오브젝트에 이 컴포넌트 추가
//   2. Cinemachine VirtualCamera를 할당 (조사 시점 카메라)
//   3. 자식에 InvestigationPoint들을 배치
//   4. WorldInteractable(Investigation 타입)에서 이 Zone을 참조
// =============================================================================

using UnityEngine;
using UnityEngine.Events;
using Cinemachine;
using GameDatabase.Dialogue;

namespace GameDatabase.Player
{
    /// <summary>
    /// 조사 구역 - 포인트앤클릭 조사 모드의 구역 단위
    /// </summary>
    public class InvestigationZone : MonoBehaviour
    {
        // =============================================================================
        // 카메라 설정
        // =============================================================================

        [Header("=== 카메라 설정 ===")]

        [Tooltip("포인트앤클릭 시점 카메라 (Cinemachine VirtualCamera)")]
        [SerializeField] private CinemachineVirtualCamera _investigationCamera;

        // =============================================================================
        // 대사 설정
        // =============================================================================

        [Header("=== 대사 설정 ===")]

        [Tooltip("이미 조사한 포인트 재클릭 시 기본 대사 (개별 포인트에 없을 때 사용)")]
        [SerializeField] private DialogueData _alreadyInspectedDialogue;

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("조사 시작 시 호출")]
        public UnityEvent OnInvestigationStart;

        [Tooltip("조사 완료 시 호출 (모든 포인트 완료)")]
        public UnityEvent OnInvestigationComplete;

        [Tooltip("개별 포인트 완료 시 호출 (완료 수, 전체 수)")]
        public UnityEvent<int, int> OnProgressUpdated;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private InvestigationPoint[] _points;
        private InteractableClue[] _clues;
        private int _completedCount = 0;
        private bool _isActive = false;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 조사 카메라
        /// </summary>
        public CinemachineVirtualCamera InvestigationCamera => _investigationCamera;

        /// <summary>
        /// 이미 조사한 포인트 재클릭 시 기본 대사
        /// </summary>
        public DialogueData AlreadyInspectedDialogue => _alreadyInspectedDialogue;

        /// <summary>
        /// 조사 포인트 배열
        /// </summary>
        public InvestigationPoint[] Points => _points;

        /// <summary>
        /// 전체 포인트 수 (InvestigationPoint + InteractableClue)
        /// </summary>
        public int TotalPoints
        {
            get
            {
                int total = 0;
                if (_points != null) total += _points.Length;
                if (_clues != null) total += _clues.Length;
                return total;
            }
        }

        /// <summary>
        /// 완료된 포인트 수
        /// </summary>
        public int CompletedCount => _completedCount;

        /// <summary>
        /// 모든 포인트 완료 여부 (InvestigationPoint + InteractableClue)
        /// </summary>
        public bool AllCompleted => _completedCount >= TotalPoints;

        /// <summary>
        /// 현재 활성화 여부
        /// </summary>
        public bool IsActive => _isActive;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // 자식에서 InvestigationPoint들을 수집
            _points = GetComponentsInChildren<InvestigationPoint>();

            // 각 포인트에 부모 존 할당
            foreach (var point in _points)
            {
                point.ParentZone = this;
            }

            // 자식에서 InteractableClue들을 수집
            _clues = GetComponentsInChildren<InteractableClue>();

            // 카메라 초기 비활성화 (우선순위 낮춤)
            if (_investigationCamera != null)
            {
                _investigationCamera.Priority = 0;
            }

            Debug.Log($"[InvestigationZone] 초기화 - InvestigationPoint: {_points.Length}, InteractableClue: {_clues.Length}, 총합: {TotalPoints}");
        }

        // =============================================================================
        // 조사 제어
        // =============================================================================

        /// <summary>
        /// 조사 시작
        /// </summary>
        public void StartInvestigation()
        {
            _isActive = true;

            // 완료 수 갱신 (InvestigationPoint + InteractableClue)
            _completedCount = 0;
            foreach (var point in _points)
            {
                if (point.IsCompleted) _completedCount++;
            }
            foreach (var clue in _clues)
            {
                if (clue.IsInvestigated) _completedCount++;
            }

            // 이벤트 발생
            OnInvestigationStart?.Invoke();
            OnProgressUpdated?.Invoke(_completedCount, TotalPoints);

            Debug.Log($"[InvestigationZone] 조사 시작 - 완료 {_completedCount}/{TotalPoints}");
        }

        /// <summary>
        /// 조사 종료
        /// </summary>
        public void EndInvestigation()
        {
            _isActive = false;

            Debug.Log($"[InvestigationZone] 조사 종료");
        }

        /// <summary>
        /// 포인트 완료 콜백 (InvestigationPoint 또는 InteractableClue에서 호출)
        /// </summary>
        public void OnPointCompleted(InvestigationPoint point)
        {
            // 완료 수 재계산 (InvestigationPoint + InteractableClue)
            _completedCount = 0;
            foreach (var p in _points)
            {
                if (p.IsCompleted) _completedCount++;
            }
            foreach (var clue in _clues)
            {
                if (clue.IsInvestigated) _completedCount++;
            }

            // 진행도 이벤트
            OnProgressUpdated?.Invoke(_completedCount, TotalPoints);

            Debug.Log($"[InvestigationZone] 포인트 완료 - {_completedCount}/{TotalPoints}");

            // 모든 포인트 완료 체크는 InvestigationManager가 다이얼로그 종료 후 처리
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 모든 포인트 초기화 (InvestigationPoint + InteractableClue)
        /// </summary>
        public void ResetAllPoints()
        {
            foreach (var point in _points)
            {
                point.ResetPoint();
            }
            foreach (var clue in _clues)
            {
                clue.ResetClue();
            }
            _completedCount = 0;
        }

        // =============================================================================
        // Gizmo
        // =============================================================================

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
        }

        private void OnDrawGizmosSelected()
        {
            // 포인트 연결선 표시
            InvestigationPoint[] points = GetComponentsInChildren<InvestigationPoint>();
            if (points == null) return;

            Gizmos.color = Color.cyan;
            foreach (var point in points)
            {
                Gizmos.DrawLine(transform.position, point.transform.position);
            }
        }
    }
}
