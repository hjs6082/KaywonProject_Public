// =============================================================================
// InteractableObject.cs
// =============================================================================
// 설명: IInteractable 인터페이스의 기본 구현
// 용도: 간단한 상호작용 오브젝트를 빠르게 만들 때 사용
// 사용법:
//   1. 상호작용할 오브젝트에 이 컴포넌트 추가
//   2. Inspector에서 프롬프트 텍스트 설정
//   3. OnInteracted 이벤트에 원하는 동작 연결
// =============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace GameDatabase.Player
{
    /// <summary>
    /// 기본 상호작용 오브젝트 컴포넌트
    /// IInteractable의 기본 구현을 제공합니다.
    /// </summary>
    public class InteractableObject : MonoBehaviour, IInteractable
    {
        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== 상호작용 설정 ===")]

        [Tooltip("상호작용 프롬프트 텍스트 (예: '문 열기', '아이템 줍기')")]
        [SerializeField] private string _interactionPrompt = "상호작용";

        [Tooltip("상호작용 가능 여부")]
        [SerializeField] private bool _canInteract = true;

        [Tooltip("한 번만 상호작용 가능")]
        [SerializeField] private bool _oneTimeOnly = false;

        [Tooltip("상호작용 후 오브젝트 비활성화")]
        [SerializeField] private bool _disableAfterInteract = false;

        // =============================================================================
        // 시각적 피드백 (선택사항)
        // =============================================================================

        [Header("=== 시각적 피드백 (선택사항) ===")]

        [Tooltip("포커스 시 하이라이트할 렌더러")]
        [SerializeField] private Renderer _highlightRenderer;

        [Tooltip("하이라이트 색상")]
        [SerializeField] private Color _highlightColor = new Color(1f, 1f, 0.5f, 1f);

        [Tooltip("포커스 시 활성화할 게임오브젝트 (예: UI 힌트)")]
        [SerializeField] private GameObject _focusIndicator;

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("상호작용 시 호출")]
        public UnityEvent OnInteracted;

        [Tooltip("상호작용 시 호출 (PlayerController 전달)")]
        public UnityEvent<PlayerController> OnInteractedWithPlayer;

        [Tooltip("포커스 진입 시 호출")]
        public UnityEvent OnFocusEntered;

        [Tooltip("포커스 종료 시 호출")]
        public UnityEvent OnFocusExited;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        // 하이라이트용 원래 색상
        private Color _originalColor;
        private bool _hasInteracted = false;
        private bool _isFocused = false;

        // =============================================================================
        // IInteractable 구현
        // =============================================================================

        /// <summary>
        /// 상호작용 프롬프트 텍스트
        /// </summary>
        public string InteractionPrompt => _interactionPrompt;

        /// <summary>
        /// 상호작용 가능 여부
        /// </summary>
        public bool CanInteract
        {
            get
            {
                // 한 번만 상호작용 가능한 경우 이미 상호작용했으면 불가
                if (_oneTimeOnly && _hasInteracted)
                {
                    return false;
                }
                return _canInteract;
            }
        }

        /// <summary>
        /// 상호작용 실행
        /// </summary>
        /// <param name="player">상호작용하는 플레이어</param>
        public void OnInteract(PlayerController player)
        {
            if (!CanInteract) return;

            // 상호작용 플래그 설정
            _hasInteracted = true;

            // 이벤트 발생
            OnInteracted?.Invoke();
            OnInteractedWithPlayer?.Invoke(player);

            Debug.Log($"[InteractableObject] '{gameObject.name}' 상호작용 실행");

            // 상호작용 후 비활성화
            if (_disableAfterInteract)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 포커스 시작
        /// </summary>
        public void OnFocusEnter()
        {
            if (_isFocused) return;
            _isFocused = true;

            // 하이라이트 적용
            ApplyHighlight(true);

            // 포커스 인디케이터 활성화
            if (_focusIndicator != null)
            {
                _focusIndicator.SetActive(true);
            }

            // 이벤트 발생
            OnFocusEntered?.Invoke();
        }

        /// <summary>
        /// 포커스 종료
        /// </summary>
        public void OnFocusExit()
        {
            if (!_isFocused) return;
            _isFocused = false;

            // 하이라이트 해제
            ApplyHighlight(false);

            // 포커스 인디케이터 비활성화
            if (_focusIndicator != null)
            {
                _focusIndicator.SetActive(false);
            }

            // 이벤트 발생
            OnFocusExited?.Invoke();
        }

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Start()
        {
            // 원래 색상 저장
            if (_highlightRenderer != null)
            {
                _originalColor = _highlightRenderer.material.color;
            }

            // 포커스 인디케이터 초기 비활성화
            if (_focusIndicator != null)
            {
                _focusIndicator.SetActive(false);
            }
        }

        private void OnDisable()
        {
            // 비활성화 시 포커스 해제
            if (_isFocused)
            {
                OnFocusExit();
            }
        }

        // =============================================================================
        // 하이라이트
        // =============================================================================

        /// <summary>
        /// 하이라이트 적용/해제
        /// </summary>
        /// <param name="highlight">적용 여부</param>
        private void ApplyHighlight(bool highlight)
        {
            if (_highlightRenderer == null) return;

            if (highlight)
            {
                _highlightRenderer.material.color = _highlightColor;
            }
            else
            {
                _highlightRenderer.material.color = _originalColor;
            }
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 상호작용 가능 여부 설정
        /// </summary>
        /// <param name="canInteract">가능 여부</param>
        public void SetCanInteract(bool canInteract)
        {
            _canInteract = canInteract;
        }

        /// <summary>
        /// 프롬프트 텍스트 설정
        /// </summary>
        /// <param name="prompt">새 프롬프트 텍스트</param>
        public void SetPrompt(string prompt)
        {
            _interactionPrompt = prompt;
        }

        /// <summary>
        /// 상호작용 상태 초기화 (다시 상호작용 가능하게)
        /// </summary>
        public void ResetInteraction()
        {
            _hasInteracted = false;
        }
    }
}
