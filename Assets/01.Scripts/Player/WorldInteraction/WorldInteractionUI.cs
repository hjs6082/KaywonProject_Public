// =============================================================================
// WorldInteractionUI.cs
// =============================================================================
// 설명: World Space에 배치되는 상호작용 UI 컴포넌트
// 용도: 상호작용 가능할 때 표시되는 UI (키 안내, 이름 등)
// 작동 방식:
//   1. Canvas를 World Space로 설정
//   2. BillboardUI를 통해 플레이어를 바라봄
//   3. 상호작용 가능 시 표시, 불가능 시 숨김
// =============================================================================

using UnityEngine;
using UnityEngine.UI;

namespace GameDatabase.Player
{
    /// <summary>
    /// World Space 상호작용 UI
    /// 3D 공간에 배치되어 플레이어를 바라보는 UI
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class WorldInteractionUI : MonoBehaviour
    {
        // =============================================================================
        // UI 요소 참조
        // =============================================================================

        [Header("=== UI 요소 ===")]

        [Tooltip("상호작용 키 텍스트 (예: 'F')")]
        [SerializeField] private Text _keyText;

        [Tooltip("상호작용 이름 텍스트 (예: '대화하기')")]
        [SerializeField] private Text _nameText;

        [Tooltip("배경 이미지")]
        [SerializeField] private Image _backgroundImage;

        [Tooltip("키 아이콘 이미지 (선택사항)")]
        [SerializeField] private Image _keyIcon;

        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== 설정 ===")]

        [Tooltip("UI가 없을 때 자동 생성")]
        [SerializeField] private bool _autoCreateUI = true;

        [Tooltip("표시/숨김 애니메이션 사용")]
        [SerializeField] private bool _useAnimation = true;

        [Tooltip("애니메이션 속도")]
        [Range(1f, 20f)]
        [SerializeField] private float _animationSpeed = 10f;

        [Tooltip("플레이어를 바라보기 (Billboard)")]
        [SerializeField] private bool _lookAtPlayer = true;

        [Tooltip("Y축만 회전 (수평 빌보드)")]
        [SerializeField] private bool _horizontalOnly = true;

        // =============================================================================
        // 스타일 설정
        // =============================================================================

        [Header("=== 스타일 (자동 생성 시) ===")]

        [Tooltip("배경 색상")]
        [SerializeField] private Color _backgroundColor = new Color(0, 0, 0, 0.7f);

        [Tooltip("텍스트 색상")]
        [SerializeField] private Color _textColor = Color.white;

        [Tooltip("키 텍스트 색상")]
        [SerializeField] private Color _keyColor = Color.yellow;

        [Tooltip("폰트 크기")]
        [SerializeField] private int _fontSize = 24;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        // 연결된 상호작용 오브젝트
        private WorldInteractable _interactable;

        // Canvas
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;

        // 플레이어 Transform
        private Transform _playerTransform;

        // 상태
        private bool _isVisible = false;
        private float _targetAlpha = 0f;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// UI가 표시 중인지
        /// </summary>
        public bool IsVisible => _isVisible;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // Canvas 설정
            SetupCanvas();
        }

        private void Update()
        {
            // 플레이어 바라보기
            if (_lookAtPlayer && _isVisible)
            {
                LookAtPlayer();
            }

            // 애니메이션 업데이트
            if (_useAnimation && _canvasGroup != null)
            {
                UpdateAnimation();
            }
        }

        // =============================================================================
        // 초기화
        // =============================================================================

        /// <summary>
        /// Canvas 설정
        /// </summary>
        private void SetupCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }

            // World Space로 설정
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.worldCamera = Camera.main;

            // Canvas 크기 설정
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(200, 60);
                rectTransform.localScale = new Vector3(-0.01f, 0.01f, 0.01f);
            }

            // CanvasGroup 추가 (페이드 효과용)
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            _canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// 상호작용 오브젝트와 연결
        /// </summary>
        /// <param name="interactable">연결할 상호작용 오브젝트</param>
        public void Initialize(WorldInteractable interactable)
        {
            _interactable = interactable;

            // 플레이어 Transform 가져오기
            _playerTransform = interactable.GetPlayerTransform();
            if (_playerTransform == null && PlayerController.Instance != null)
            {
                _playerTransform = PlayerController.Instance.transform;
            }

            // UI 요소 확인 및 생성
            if (_autoCreateUI)
            {
                EnsureUIElements();
            }

            // 텍스트 업데이트
            UpdateText();
        }

        /// <summary>
        /// UI 요소가 없으면 생성
        /// </summary>
        private void EnsureUIElements()
        {
            // 배경이 없으면 생성
            if (_backgroundImage == null)
            {
                CreateBackground();
            }

            // 키 텍스트가 없으면 생성
            if (_keyText == null)
            {
                CreateKeyText();
            }

            // 이름 텍스트가 없으면 생성
            if (_nameText == null)
            {
                CreateNameText();
            }
        }

        /// <summary>
        /// 배경 생성
        /// </summary>
        private void CreateBackground()
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(transform, false);

            _backgroundImage = bgObj.AddComponent<Image>();
            _backgroundImage.color = _backgroundColor;

            RectTransform rt = bgObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 키 텍스트 생성
        /// </summary>
        private void CreateKeyText()
        {
            GameObject textObj = new GameObject("KeyText");
            textObj.transform.SetParent(transform, false);

            _keyText = textObj.AddComponent<Text>();
            _keyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _keyText.fontSize = _fontSize + 4;
            _keyText.fontStyle = FontStyle.Bold;
            _keyText.color = _keyColor;
            _keyText.alignment = TextAnchor.MiddleCenter;

            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0.3f, 1);
            rt.offsetMin = new Vector2(5, 5);
            rt.offsetMax = new Vector2(-5, -5);
        }

        /// <summary>
        /// 이름 텍스트 생성
        /// </summary>
        private void CreateNameText()
        {
            GameObject textObj = new GameObject("NameText");
            textObj.transform.SetParent(transform, false);

            _nameText = textObj.AddComponent<Text>();
            _nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _nameText.fontSize = _fontSize;
            _nameText.color = _textColor;
            _nameText.alignment = TextAnchor.MiddleLeft;

            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.3f, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(5, 5);
            rt.offsetMax = new Vector2(-5, -5);
        }

        // =============================================================================
        // 표시/숨김
        // =============================================================================

        /// <summary>
        /// UI 표시
        /// </summary>
        public void Show()
        {
            _isVisible = true;
            _targetAlpha = 1f;

            // 플레이어 Transform 업데이트
            if (_playerTransform == null && _interactable != null)
            {
                _playerTransform = _interactable.GetPlayerTransform();
            }

            if (!_useAnimation && _canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// UI 숨김
        /// </summary>
        public void Hide()
        {
            _isVisible = false;
            _targetAlpha = 0f;

            if (!_useAnimation)
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0f;
                }
                gameObject.SetActive(false);
            }
        }

        // =============================================================================
        // 텍스트 업데이트
        // =============================================================================

        /// <summary>
        /// 텍스트 업데이트
        /// </summary>
        public void UpdateText()
        {
            if (_interactable == null) return;

            // 키 텍스트
            if (_keyText != null)
            {
                _keyText.text = GetKeyDisplayText(_interactable.InteractKey);
            }

            // 이름 텍스트
            if (_nameText != null)
            {
                _nameText.text = _interactable.InteractionName;
            }
        }

        /// <summary>
        /// KeyCode를 표시용 문자열로 변환
        /// </summary>
        private string GetKeyDisplayText(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Space:
                    return "Space";
                case KeyCode.Return:
                    return "Enter";
                case KeyCode.Escape:
                    return "ESC";
                case KeyCode.Tab:
                    return "Tab";
                default:
                    return key.ToString();
            }
        }

        // =============================================================================
        // 플레이어 바라보기
        // =============================================================================

        /// <summary>
        /// 플레이어를 바라보기 (Billboard)
        /// </summary>
        private void LookAtPlayer()
        {
            if (_playerTransform == null)
            {
                // 카메라를 바라보기
                Camera cam = Camera.main;
                if (cam != null)
                {
                    LookAtTarget(cam.transform.position);
                }
                return;
            }

            // 플레이어 머리 위치 추정 (카메라 또는 플레이어 + 오프셋)
            Vector3 targetPos = _playerTransform.position;

            // PlayerController가 있으면 카메라 위치 사용
            if (PlayerController.Instance?.MainCamera != null)
            {
                targetPos = PlayerController.Instance.MainCamera.transform.position;
            }
            else
            {
                targetPos.y += 4f; // 대략적인 눈 높이
            }

            LookAtTarget(targetPos);
        }

        /// <summary>
        /// 특정 위치를 바라보기
        /// </summary>
        private void LookAtTarget(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;

            if (direction == Vector3.zero) return;

            Quaternion lookRotation = Quaternion.LookRotation(direction);

            if (_horizontalOnly)
            {
                // Y축만 회전
                transform.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);
            }
            else
            {
                transform.rotation = lookRotation;
            }
        }

        // =============================================================================
        // 애니메이션
        // =============================================================================

        /// <summary>
        /// 애니메이션 업데이트
        /// </summary>
        private void UpdateAnimation()
        {
            if (_canvasGroup == null) return;

            // 부드러운 페이드
            _canvasGroup.alpha = Mathf.Lerp(
                _canvasGroup.alpha,
                _targetAlpha,
                _animationSpeed * Time.deltaTime
            );

            // 완전히 사라지면 비활성화
            if (!_isVisible && _canvasGroup.alpha < 0.01f)
            {
                _canvasGroup.alpha = 0f;
                gameObject.SetActive(false);
            }
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 색상 설정
        /// </summary>
        public void SetColors(Color background, Color text, Color key)
        {
            _backgroundColor = background;
            _textColor = text;
            _keyColor = key;

            if (_backgroundImage != null) _backgroundImage.color = background;
            if (_nameText != null) _nameText.color = text;
            if (_keyText != null) _keyText.color = key;
        }

        /// <summary>
        /// 플레이어 Transform 설정
        /// </summary>
        public void SetPlayerTransform(Transform playerTransform)
        {
            _playerTransform = playerTransform;
        }
    }
}
