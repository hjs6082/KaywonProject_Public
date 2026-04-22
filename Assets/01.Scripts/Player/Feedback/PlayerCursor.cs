// =============================================================================
// PlayerCursor.cs
// =============================================================================
// 설명: 플레이어 커서 상태 관리 컴포넌트
// 용도: 상황에 따른 커서 표시/숨김, 커서 이미지 변경
// 작동 방식:
//   - 1인칭 모드: 커서 숨김 & 잠금
//   - 조사 모드: 커서 표시 & 잠금 해제
//   - 상호작용 가능: 상호작용 커서로 변경
//   - 회전 가능 오브젝트: 잡기 커서로 변경
// =============================================================================

using UnityEngine;

namespace GameDatabase.Player
{
    /// <summary>
    /// 플레이어 커서 관리 컴포넌트
    /// 상황에 맞게 커서 상태와 이미지를 관리합니다.
    /// </summary>
    public class PlayerCursor : MonoBehaviour
    {
        // =============================================================================
        // 참조
        // =============================================================================

        [Header("=== 참조 ===")]

        [Tooltip("PlayerController 참조 (자동 할당)")]
        [SerializeField] private PlayerController _controller;

        // =============================================================================
        // 커서 이미지 오버라이드
        // =============================================================================

        [Header("=== 커서 이미지 (비워두면 PlayerData 사용) ===")]

        [Tooltip("기본 커서")]
        [SerializeField] private Texture2D _normalCursor;

        [Tooltip("잡기 가능 커서")]
        [SerializeField] private Texture2D _grabCursor;

        [Tooltip("잡는 중 커서")]
        [SerializeField] private Texture2D _grabbingCursor;

        [Tooltip("조사 가능 커서")]
        [SerializeField] private Texture2D _inspectCursor;

        [Tooltip("상호작용 가능 커서")]
        [SerializeField] private Texture2D _interactCursor;

        // =============================================================================
        // 커서 핫스팟 설정
        // =============================================================================

        [Header("=== 커서 핫스팟 ===")]

        [Tooltip("기본 커서 핫스팟")]
        [SerializeField] private Vector2 _normalHotspot = Vector2.zero;

        [Tooltip("잡기 커서 핫스팟")]
        [SerializeField] private Vector2 _grabHotspot = new Vector2(8, 8);

        [Tooltip("조사/상호작용 커서 핫스팟")]
        [SerializeField] private Vector2 _centerHotspot = new Vector2(16, 16);

        // =============================================================================
        // 상태 (읽기 전용)
        // =============================================================================

        [Header("=== 현재 상태 (읽기 전용) ===")]

        [Tooltip("현재 커서 상태")]
        [SerializeField] private CursorState _currentState = CursorState.Hidden;

        [Tooltip("이전 커서 상태")]
        [SerializeField] private CursorState _previousState = CursorState.Hidden;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 현재 커서 상태
        /// </summary>
        public CursorState CurrentState => _currentState;

        /// <summary>
        /// 커서가 표시되어 있는지 여부
        /// </summary>
        public bool IsVisible => Cursor.visible;

        /// <summary>
        /// 커서가 잠겨 있는지 여부
        /// </summary>
        public bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

        // =============================================================================
        // 커서 이미지 가져오기
        // =============================================================================

        /// <summary>
        /// 기본 커서 이미지
        /// </summary>
        private Texture2D NormalCursor => _normalCursor ?? _controller?.Data?.normalCursor;

        /// <summary>
        /// 잡기 커서 이미지
        /// </summary>
        private Texture2D GrabCursor => _grabCursor ?? _controller?.Data?.grabCursor;

        /// <summary>
        /// 잡는 중 커서 이미지
        /// </summary>
        private Texture2D GrabbingCursor => _grabbingCursor ?? _controller?.Data?.grabbingCursor;

        /// <summary>
        /// 조사 커서 이미지
        /// </summary>
        private Texture2D InspectCursor => _inspectCursor ?? _controller?.Data?.inspectCursor;

        /// <summary>
        /// 상호작용 커서 이미지
        /// </summary>
        private Texture2D InteractCursor => _interactCursor ?? _controller?.Data?.interactCursor;

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
        }

        private void Start()
        {
            // 초기 커서 상태 설정 (숨김)
            SetCursorState(CursorState.Hidden);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // 포커스 복귀 시 커서 상태 복원
            if (hasFocus)
            {
                ApplyCursorState(_currentState);
            }
        }

        // =============================================================================
        // 커서 상태 설정
        // =============================================================================

        /// <summary>
        /// 커서 상태 설정
        /// </summary>
        /// <param name="state">새 커서 상태</param>
        public void SetCursorState(CursorState state)
        {
            if (_currentState == state) return;

            _previousState = _currentState;
            _currentState = state;

            ApplyCursorState(state);
        }

        /// <summary>
        /// 커서 상태 적용
        /// </summary>
        /// <param name="state">적용할 상태</param>
        private void ApplyCursorState(CursorState state)
        {
            switch (state)
            {
                case CursorState.Hidden:
                    HideCursor();
                    break;

                case CursorState.Normal:
                    ShowCursor(NormalCursor, _normalHotspot);
                    break;

                case CursorState.Grab:
                    ShowCursor(GrabCursor ?? NormalCursor, _grabHotspot);
                    break;

                case CursorState.Grabbing:
                    ShowCursor(GrabbingCursor ?? GrabCursor ?? NormalCursor, _grabHotspot);
                    break;

                case CursorState.Inspect:
                    ShowCursor(InspectCursor ?? NormalCursor, _centerHotspot);
                    break;

                case CursorState.Interact:
                    ShowCursor(InteractCursor ?? NormalCursor, _centerHotspot);
                    break;
            }
        }

        /// <summary>
        /// 커서 숨기기 (1인칭 모드)
        /// </summary>
        private void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        /// <summary>
        /// 커서 표시
        /// </summary>
        /// <param name="texture">커서 이미지 (null이면 시스템 기본)</param>
        /// <param name="hotspot">커서 핫스팟</param>
        private void ShowCursor(Texture2D texture, Vector2 hotspot)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (texture != null)
            {
                Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
            }
            else
            {
                // 시스템 기본 커서
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }

        /// <summary>
        /// 이전 상태로 복원
        /// </summary>
        public void RestorePreviousState()
        {
            SetCursorState(_previousState);
        }

        // =============================================================================
        // 간편 메서드
        // =============================================================================

        /// <summary>
        /// 커서 숨기기 (1인칭 모드용)
        /// </summary>
        public void Hide()
        {
            SetCursorState(CursorState.Hidden);
        }

        /// <summary>
        /// 커서 표시 (기본 커서)
        /// </summary>
        public void Show()
        {
            SetCursorState(CursorState.Normal);
        }

        /// <summary>
        /// 잡기 커서로 변경
        /// </summary>
        public void SetGrab()
        {
            SetCursorState(CursorState.Grab);
        }

        /// <summary>
        /// 잡는 중 커서로 변경
        /// </summary>
        public void SetGrabbing()
        {
            SetCursorState(CursorState.Grabbing);
        }

        /// <summary>
        /// 조사 커서로 변경
        /// </summary>
        public void SetInspect()
        {
            SetCursorState(CursorState.Inspect);
        }

        /// <summary>
        /// 상호작용 커서로 변경
        /// </summary>
        public void SetInteract()
        {
            SetCursorState(CursorState.Interact);
        }

        // =============================================================================
        // 커서 이미지 설정
        // =============================================================================

        /// <summary>
        /// 커서 이미지 설정
        /// </summary>
        /// <param name="state">커서 상태</param>
        /// <param name="texture">커서 이미지</param>
        public void SetCursorTexture(CursorState state, Texture2D texture)
        {
            switch (state)
            {
                case CursorState.Normal:
                    _normalCursor = texture;
                    break;
                case CursorState.Grab:
                    _grabCursor = texture;
                    break;
                case CursorState.Grabbing:
                    _grabbingCursor = texture;
                    break;
                case CursorState.Inspect:
                    _inspectCursor = texture;
                    break;
                case CursorState.Interact:
                    _interactCursor = texture;
                    break;
            }

            // 현재 상태와 같으면 즉시 적용
            if (_currentState == state)
            {
                ApplyCursorState(state);
            }
        }

        /// <summary>
        /// 모든 커서 이미지 초기화 (PlayerData 사용)
        /// </summary>
        public void ResetCursorTextures()
        {
            _normalCursor = null;
            _grabCursor = null;
            _grabbingCursor = null;
            _inspectCursor = null;
            _interactCursor = null;

            // 현재 상태 다시 적용
            ApplyCursorState(_currentState);
        }
    }
}
