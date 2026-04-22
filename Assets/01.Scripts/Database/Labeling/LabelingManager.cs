// =============================================================================
// LabelingManager.cs
// =============================================================================
// 설명: 라벨링 미니게임 전체 관리 싱글톤
// 용도: 라벨링 모드 진입/종료, 카메라 전환, 완료 시 다이얼로그 출력
// 사용법:
//   1. 씬에 빈 GameObject를 만들고 이 컴포넌트를 추가
//   2. _deskCamera에 책상 위 CinemachineVirtualCamera 할당
//   3. WorldInteractable의 OnInteracted 이벤트에서 EnterLabeling() 호출
// =============================================================================

using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Cinemachine;
using GameDatabase.UI;
using GameDatabase.Player;
using GameDatabase.Dialogue;

namespace GameDatabase.Labeling
{
    /// <summary>
    /// 라벨링 미니게임 매니저 (싱글톤)
    /// </summary>
    public class LabelingManager : MonoBehaviour
    {
        // =============================================================================
        // 싱글톤
        // =============================================================================

        private static LabelingManager _instance;
        public static LabelingManager Instance => _instance;

        // =============================================================================
        // 카메라 설정
        // =============================================================================

        [Header("=== 카메라 ===")]

        [Tooltip("책상 위 고정 카메라 (라벨링 모드 진입 시 활성화)")]
        [SerializeField] private CinemachineVirtualCamera _deskCamera;

        [Tooltip("라벨링 카메라 Priority (활성 시)")]
        [SerializeField] private int _activePriority = 20;

        [Tooltip("라벨링 카메라 Priority (비활성 시)")]
        [SerializeField] private int _inactivePriority = 0;

        // =============================================================================
        // UI 숨김 설정
        // =============================================================================

        [Header("=== 라벨링 중 숨길 UI ===")]

        [Tooltip("라벨링 모드 진입 시 숨길 UI 오브젝트 목록 (웨이포인트, Aim 등)")]
        [SerializeField] private GameObject[] _hideOnLabeling;

        // =============================================================================
        // 대사 설정
        // =============================================================================

        [Header("=== 대사 ===")]

        [Tooltip("오답 시 재생할 DialogueData")]
        [SerializeField] private DialogueData _failDialogue;

        [Tooltip("모든 라벨링 완료 시 재생할 DialogueData")]
        [SerializeField] private DialogueData _completionDialogue;

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("라벨링 모드 진입 시")]
        public UnityEvent OnLabelingEntered;

        [Tooltip("라벨링 모드 완료 시")]
        public UnityEvent OnLabelingCompleted;

        // =============================================================================
        // 상태
        // =============================================================================

        private bool _isLabeling = false;
        private List<MemoObject> _registeredMemos = new List<MemoObject>();

        public bool IsLabeling => _isLabeling;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // 카메라 초기 비활성화
            if (_deskCamera != null)
                _deskCamera.Priority = _inactivePriority;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        // =============================================================================
        // 메모 등록
        // =============================================================================

        /// <summary>
        /// MemoObject가 자신을 등록 (Start에서 호출)
        /// </summary>
        public void RegisterMemo(MemoObject memo)
        {
            if (!_registeredMemos.Contains(memo))
                _registeredMemos.Add(memo);
        }

        /// <summary>
        /// MemoObject 등록 해제
        /// </summary>
        public void UnregisterMemo(MemoObject memo)
        {
            _registeredMemos.Remove(memo);
        }

        // =============================================================================
        // 공개 API
        // =============================================================================

        /// <summary>
        /// 라벨링 모드 진입 (WorldInteractable의 OnInteracted에서 호출)
        /// </summary>
        public void EnterLabeling()
        {
            if (_isLabeling) return;

            _isLabeling = true;

            // 플레이어 입력 차단
            if (PlayerController.Instance != null)
                PlayerController.Instance.SetMovementEnabled(false);

            // Aim을 마우스 커서로 전환
            if (AimCursor.Instance != null)
                AimCursor.Instance.EnterLabelingMode();

            // 지정된 UI 숨김 (웨이포인트 등)
            foreach (var obj in _hideOnLabeling)
                if (obj != null) obj.SetActive(false);

            // 책상 카메라 활성화
            if (_deskCamera != null)
                _deskCamera.Priority = _activePriority;

            OnLabelingEntered?.Invoke();
            Debug.Log("[LabelingManager] 라벨링 모드 진입");
        }

        /// <summary>
        /// 메모 하나가 정답 처리됐을 때 MemoObject에서 호출
        /// 모두 완료됐는지 체크
        /// </summary>
        public void OnMemoSolved()
        {
            // 미완료 메모가 남아있으면 무시
            foreach (var memo in _registeredMemos)
            {
                if (!memo.IsSolved) return;
            }

            // 모두 완료
            CompleteLabeling();
        }

        /// <summary>
        /// 오답 시 MemoObject에서 호출 - 실패 대사 재생
        /// </summary>
        public void PlayFailDialogue()
        {
            if (_failDialogue != null)
                DialogueManager.Instance?.StartDialogue(_failDialogue);
        }

        // =============================================================================
        // 내부
        // =============================================================================

        /// <summary>
        /// 라벨링 모두 완료 처리
        /// </summary>
        private void CompleteLabeling()
        {
            _isLabeling = false;


            // 숨겼던 UI 복구
            foreach (var obj in _hideOnLabeling)
                if (obj != null) obj.SetActive(true);

            // 플레이어 입력 복구
            if (PlayerController.Instance != null)
                PlayerController.Instance.SetMovementEnabled(true);

            // Aim을 화면 중앙으로 복귀
            if (AimCursor.Instance != null)
                AimCursor.Instance.ExitLabelingMode();

            Debug.Log("[LabelingManager] 라벨링 완료!");

            // 완료 대사가 있으면 재생 후 OnLabelingCompleted 발생
            // 없으면 즉시 발생
            if (_completionDialogue != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueEnd.AddListener(OnCompletionDialogueEnd);
                DialogueManager.Instance.StartDialogue(_completionDialogue);
            }
            else
            {
                OnLabelingCompleted?.Invoke();
            }
        }

        private void OnCompletionDialogueEnd()
        {
            DialogueManager.Instance.OnDialogueEnd.RemoveListener(OnCompletionDialogueEnd);
            OnLabelingCompleted?.Invoke();
            // 책상 카메라 비활성화 → 원래 카메라로 복귀
            if (_deskCamera != null)
                _deskCamera.Priority = _inactivePriority;
        }
    }
}
