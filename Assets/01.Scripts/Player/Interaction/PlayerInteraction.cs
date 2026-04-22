// =============================================================================
// PlayerInteraction.cs
// =============================================================================
// 설명: 플레이어 상호작용 시스템
// 용도: E키로 오브젝트와 상호작용, 레이캐스트로 대상 감지
// 작동 방식:
//   1. 매 프레임 카메라 전방으로 레이캐스트 발사
//   2. IInteractable 구현 오브젝트 감지 시 UI 힌트 표시
//   3. 상호작용 키 입력 시 OnInteract() 호출
// =============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace GameDatabase.Player
{
    /// <summary>
    /// 플레이어 상호작용 컴포넌트
    /// 레이캐스트로 상호작용 대상을 감지하고 상호작용을 수행합니다.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        // =============================================================================
        // 참조
        // =============================================================================

        [Header("=== 참조 ===")]

        [Tooltip("PlayerController 참조 (자동 할당)")]
        [SerializeField] private PlayerController _controller;

        [Tooltip("레이캐스트 시작점 (카메라 Transform, 자동 할당)")]
        [SerializeField] private Transform _raycastOrigin;

        // =============================================================================
        // 상태 (읽기 전용)
        // =============================================================================

        [Header("=== 현재 상태 (읽기 전용) ===")]

        [Tooltip("현재 포커스된 오브젝트")]
        [SerializeField] private GameObject _focusedObject;

        [Tooltip("현재 포커스된 IInteractable")]
        [SerializeField] private string _focusedInteractableName; // Inspector 표시용

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("상호작용 가능 오브젝트 감지 시 호출 (프롬프트 텍스트)")]
        public UnityEvent<string> OnInteractableFound;

        [Tooltip("상호작용 가능 오브젝트에서 벗어날 때 호출")]
        public UnityEvent OnInteractableLost;

        [Tooltip("상호작용 실행 시 호출")]
        public UnityEvent<IInteractable> OnInteracted;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        // 현재 포커스된 상호작용 대상
        private IInteractable _currentInteractable;

        // 레이캐스트 결과
        private RaycastHit _hit;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 현재 포커스된 오브젝트
        /// </summary>
        public GameObject FocusedObject => _focusedObject;

        /// <summary>
        /// 현재 포커스된 IInteractable
        /// </summary>
        public IInteractable CurrentInteractable => _currentInteractable;

        /// <summary>
        /// 상호작용 가능한 대상이 있는지 여부
        /// </summary>
        public bool HasInteractable => _currentInteractable != null && _currentInteractable.CanInteract;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // 컴포넌트 자동 할당
            if (_controller == null)
            {
                _controller = GetComponentInParent<PlayerController>();
            }

            if (_raycastOrigin == null && _controller != null)
            {
                // 메인 카메라를 레이캐스트 시작점으로 사용
                if (_controller.MainCamera != null)
                {
                    _raycastOrigin = _controller.MainCamera.transform;
                }
            }
        }

        private void Update()
        {
            // 상호작용 가능한 상태인지 확인
            if (_controller == null || !_controller.CanMove)
            {
                // 조사 모드나 대화 중에는 1인칭 상호작용 비활성화
                ClearFocus();
                return;
            }

            // 레이캐스트로 대상 감지
            DetectInteractable();

            // 상호작용 입력 처리
            HandleInteractionInput();
        }

        // =============================================================================
        // 대상 감지
        // =============================================================================

        /// <summary>
        /// 레이캐스트로 상호작용 대상 감지
        /// </summary>
        private void DetectInteractable()
        {
            if (_raycastOrigin == null || _controller.Data == null) return;

            float range = _controller.Data.interactionRange;
            LayerMask layer = _controller.Data.interactableLayer;

            // 레이캐스트 발사
            Ray ray = new Ray(_raycastOrigin.position, _raycastOrigin.forward);
            bool hit = Physics.Raycast(ray, out _hit, range, layer);

            // 디버그용 레이 표시
            Debug.DrawRay(ray.origin, ray.direction * range, hit ? Color.green : Color.red);

            if (hit)
            {
                // IInteractable 컴포넌트 찾기
                IInteractable interactable = _hit.collider.GetComponent<IInteractable>();

                // 부모에서도 찾기
                if (interactable == null)
                {
                    interactable = _hit.collider.GetComponentInParent<IInteractable>();
                }

                if (interactable != null && interactable.CanInteract)
                {
                    // 새로운 대상이면 포커스 변경
                    if (interactable != _currentInteractable)
                    {
                        SetFocus(interactable, _hit.collider.gameObject);
                    }
                }
                else
                {
                    // 상호작용 불가능한 오브젝트
                    ClearFocus();
                }
            }
            else
            {
                // 아무것도 감지되지 않음
                ClearFocus();
            }
        }

        /// <summary>
        /// 포커스 설정
        /// </summary>
        /// <param name="interactable">포커스할 대상</param>
        /// <param name="gameObject">대상 게임오브젝트</param>
        private void SetFocus(IInteractable interactable, GameObject gameObject)
        {
            // 이전 포커스 해제
            if (_currentInteractable != null)
            {
                _currentInteractable.OnFocusExit();
            }

            // 새 포커스 설정
            _currentInteractable = interactable;
            _focusedObject = gameObject;
            _focusedInteractableName = gameObject.name;

            // 포커스 진입 호출
            _currentInteractable.OnFocusEnter();

            // 이벤트 발생
            OnInteractableFound?.Invoke(interactable.InteractionPrompt);

            // 커서 상태 변경
            if (_controller.Cursor != null)
            {
                _controller.Cursor.SetCursorState(CursorState.Interact);
            }
        }

        /// <summary>
        /// 포커스 해제
        /// </summary>
        private void ClearFocus()
        {
            if (_currentInteractable == null) return;

            // 포커스 종료 호출
            _currentInteractable.OnFocusExit();

            // 초기화
            _currentInteractable = null;
            _focusedObject = null;
            _focusedInteractableName = "";

            // 이벤트 발생
            OnInteractableLost?.Invoke();

            // 커서 상태 복원
            if (_controller.Cursor != null && !_controller.IsInspecting)
            {
                _controller.Cursor.SetCursorState(CursorState.Hidden);
            }
        }

        // =============================================================================
        // 입력 처리
        // =============================================================================

        /// <summary>
        /// 상호작용 입력 처리
        /// </summary>
        private void HandleInteractionInput()
        {
            if (_controller.Data == null) return;

            // 상호작용 키 입력
            if (Input.GetKeyDown(_controller.Data.interactKey))
            {
                TryInteract();
            }
        }

        /// <summary>
        /// 상호작용 시도
        /// </summary>
        public void TryInteract()
        {
            if (_currentInteractable == null) return;
            if (!_currentInteractable.CanInteract) return;

            // 상호작용 실행
            _currentInteractable.OnInteract(_controller);

            // 이벤트 발생
            OnInteracted?.Invoke(_currentInteractable);

            Debug.Log($"[PlayerInteraction] '{_focusedObject?.name}' 상호작용 완료");
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 특정 오브젝트와 강제 상호작용
        /// </summary>
        /// <param name="target">상호작용할 오브젝트</param>
        public void ForceInteract(GameObject target)
        {
            if (target == null) return;

            IInteractable interactable = target.GetComponent<IInteractable>();
            if (interactable == null)
            {
                interactable = target.GetComponentInParent<IInteractable>();
            }

            if (interactable != null && interactable.CanInteract)
            {
                interactable.OnInteract(_controller);
                OnInteracted?.Invoke(interactable);
            }
        }

        /// <summary>
        /// 현재 포커스 강제 해제
        /// </summary>
        public void ForceClearFocus()
        {
            ClearFocus();
        }
    }
}
