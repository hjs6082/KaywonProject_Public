// =============================================================================
// AimCursor.cs
// =============================================================================
// 설명: Aim UI 이미지를 마우스 커서 대신 사용
// 용도: 시스템 커서를 숨기고 UI 이미지가 마우스 위치를 따라다님
// 사용법:
//   1. Canvas (Screen Space Overlay) 하위 Image에 이 컴포넌트 추가
//   2. 기본적으로 시스템 커서 숨김 + Aim UI 표시
//   3. SetAimMode(false) 호출 시 시스템 커서 복구, Aim UI 숨김
// =============================================================================

using UnityEngine;

namespace GameDatabase.UI
{
    /// <summary>
    /// Aim UI를 마우스 커서 대신 사용하는 컴포넌트
    /// </summary>
    public class AimCursor : MonoBehaviour
    {
        // =============================================================================
        // 싱글톤
        // =============================================================================

        private static AimCursor _instance;
        public static AimCursor Instance => _instance;

        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== 설정 ===")]

        [Tooltip("라벨링 모드일 때 Aim이 마우스를 따라다님 (false면 화면 중앙 고정)")]
        [SerializeField] private bool _followMouseOnLabeling = true;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private RectTransform _rectTransform;

        // 현재 마우스 따라다니기 모드인지
        private bool _isFollowingMouse = false;

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

            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                // 커서 복구
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private void Update()
        {
            if (_isFollowingMouse)
                FollowMouse();
        }

        // =============================================================================
        // 공개 API
        // =============================================================================

        /// <summary>
        /// 라벨링 모드 진입 시 호출 - 마우스 따라다니기 모드로 전환
        /// </summary>
        public void EnterLabelingMode()
        {
            _isFollowingMouse = true;

            // 시스템 커서 숨김
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 라벨링 모드 종료 시 호출 - 화면 중앙 고정으로 복귀
        /// </summary>
        public void ExitLabelingMode()
        {
            _isFollowingMouse = false;

            // 시스템 커서 숨김 유지 (1인칭 모드)
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // Aim을 화면 중앙으로 복귀
            _rectTransform.anchoredPosition = Vector2.zero;
        }

        // =============================================================================
        // 내부
        // =============================================================================

        private void FollowMouse()
        {
            // 스크린 좌표 → 캔버스 로컬 좌표 변환
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                Input.mousePosition,
                canvas.worldCamera,
                out Vector2 localPoint
            );

            _rectTransform.anchoredPosition = localPoint;
        }
    }
}
