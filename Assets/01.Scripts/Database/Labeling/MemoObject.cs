// =============================================================================
// MemoObject.cs
// =============================================================================
// 설명: 라벨이 드롭되는 메모지 오브젝트
// 용도: 정답 라벨 ID를 가지고, 라벨 드롭 시 정답 판정
// 작동 방식:
//   1. LabelObject가 드롭되면 TryAttachLabel() 호출
//   2. 정답이면 라벨을 부착하고 LabelingManager에 완료 알림
//   3. 오답이면 라벨을 튕겨냄
// =============================================================================

using UnityEngine;
using UnityEngine.Events;
using GameDatabase.Dialogue;
using GameDatabase.UI;

namespace GameDatabase.Labeling
{
    /// <summary>
    /// 라벨이 드롭되는 메모지 오브젝트
    /// </summary>
    public class MemoObject : MonoBehaviour
    {
        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== 메모 설정 ===")]

        [Tooltip("이 메모지의 정답 라벨 ID (예: A, B, C)")]
        [SerializeField] private string _correctLabelId;

        [Tooltip("라벨이 부착될 위치 Transform (자식 오브젝트 LabelTransform 할당)")]
        [SerializeField] private Transform _labelTransform;

        [Tooltip("라벨 부착 시 LabelTransform 기준 로컬 오프셋")]
        [SerializeField] private Vector3 _labelLocalOffset = new Vector3(0f, 0f, 0.018f);

        [Header("=== 대사 ===")]

        [Tooltip("정답 시 재생할 DialogueData")]
        [SerializeField] private DialogueData _successDialogue;

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("정답 라벨 부착 시")]
        public UnityEvent OnSolved;

        [Tooltip("오답 라벨 드롭 시")]
        public UnityEvent OnWrongLabel;

        // =============================================================================
        // 상태
        // =============================================================================

        private bool _isSolved = false;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        public bool IsSolved => _isSolved;
        public Transform LabelTransform => _labelTransform != null ? _labelTransform : transform;
        public Vector3 LabelLocalOffset => _labelLocalOffset;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Start()
        {
            // LabelingManager에 등록
            LabelingManager.Instance?.RegisterMemo(this);
        }

        private void OnDestroy()
        {
            LabelingManager.Instance?.UnregisterMemo(this);
        }

        // =============================================================================
        // 공개 API
        // =============================================================================

        /// <summary>
        /// LabelObject에서 드롭 시 호출 - 정답 판정
        /// </summary>
        public void TryAttachLabel(LabelObject label)
        {
            if (_isSolved) return;

            if (label.LabelId == _correctLabelId)
            {
                // 정답
                _isSolved = true;
                label.AttachToMemo(this);
                OnSolved?.Invoke();
                Debug.Log($"[MemoObject] '{gameObject.name}' 정답! 라벨: {label.LabelId}");

                // 성공 대사 재생
                if (_successDialogue != null)
                    DialogueManager.Instance?.StartDialogue(_successDialogue);

                // 전체 완료 체크
                LabelingManager.Instance?.OnMemoSolved();
            }
            else
            {
                // 오답 - 라벨 튕겨냄
                label.Bounce();
                OnWrongLabel?.Invoke();
                Debug.Log($"[MemoObject] '{gameObject.name}' 오답. 정답: {_correctLabelId}, 입력: {label.LabelId}");

                // 실패 대사 재생
                LabelingManager.Instance?.PlayFailDialogue();
            }
        }

        // =============================================================================
        // Gizmo
        // =============================================================================

        private void OnDrawGizmosSelected()
        {
            // 라벨 부착 위치 표시
            Gizmos.color = Color.yellow;
            Vector3 pos = _labelTransform != null ? _labelTransform.position : transform.position;
            Gizmos.DrawWireSphere(pos, 0.05f);
        }
    }
}
