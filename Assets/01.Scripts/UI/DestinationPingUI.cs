// =============================================================================
// DestinationPingUI.cs
// =============================================================================
// 설명: Screen Space Overlay 기반 목적지 방향 핑 UI 컨트롤러 (싱글톤)
// 용도: 목적지가 화면 안에 있으면 해당 위치에, 화면 밖이면 가장자리에 아이콘 표시.
//       레이블에는 남은 거리를 "XXM" 형식으로 표시.
//
// Unity 설정:
//   Hierarchy:
//     Canvas (DestinationPingUI)          ← Screen Space Overlay, Anchor: Stretch
//       └── Indicator                     ← _indicator (Anchor: Middle Center)
//             ├── Icon  (Image)           ← _icon
//             └── Label (TextMeshProUGUI) ← _label  ("M")
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameDatabase.Player;

namespace GameDatabase.UI
{
    [RequireComponent(typeof(Canvas))]
    public class DestinationPingUI : MonoBehaviour
    {
        // =============================================================================
        // 싱글톤
        // =============================================================================

        private static DestinationPingUI _instance;
        public static DestinationPingUI Instance => _instance;

        // =============================================================================
        // 인디케이터 참조
        // =============================================================================

        [Header("=== 인디케이터 ===")]

        [Tooltip("이동할 인디케이터 루트 RectTransform (Anchor: Middle Center)")]
        [SerializeField] private RectTransform _indicator;

        [Tooltip("아이콘 Image")]
        [SerializeField] private Image _icon;

        [Tooltip("아이콘 아래 레이블 (거리 표시, 예: '15M')")]
        [SerializeField] private TextMeshProUGUI _label;

        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== 가장자리 여백 ===")]

        [Tooltip("화면 가장자리에서 얼마나 안쪽에 클램프할지 (px, 캔버스 기준)")]
        [SerializeField] private float _edgePadding = 60f;

        [Header("=== 표시 설정 ===")]

        [Tooltip("핑이 없을 때 인디케이터를 숨김")]
        [SerializeField] private bool _hideWhenNoPing = true;

        [Tooltip("목적지와의 거리가 이 값 이하이면 인디케이터 숨김 (0이면 비활성)")]
        [SerializeField] private float _hideWithinDistance = 2f;

        [Tooltip("부드러운 이동 사용 여부")]
        [SerializeField] private bool _smoothMove = false;

        [Tooltip("부드러운 이동 속도")]
        [Range(1f, 30f)]
        [SerializeField] private float _moveSpeed = 20f;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private readonly List<DestinationPing> _registeredPings = new List<DestinationPing>();
        private Camera _mainCamera;
        private RectTransform _canvasRect;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else if (_instance != this) { Destroy(gameObject); return; }

            SetupCanvas();
            _canvasRect = GetComponent<RectTransform>();

            if (_hideWhenNoPing)
                _indicator?.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void LateUpdate()
        {
            UpdateCamera();
            UpdatePingDisplay();
        }

        // =============================================================================
        // 등록/해제 API
        // =============================================================================

        public void Register(DestinationPing ping)
        {
            if (ping == null || _registeredPings.Contains(ping)) return;
            _registeredPings.Add(ping);
            Debug.Log($"[DestinationPingUI] Register: '{ping.gameObject.name}' → 총 {_registeredPings.Count}개");
            _indicator?.gameObject.SetActive(true);
        }

        public void Unregister(DestinationPing ping)
        {
            _registeredPings.Remove(ping);
            Debug.Log($"[DestinationPingUI] Unregister: '{ping.gameObject.name}' → 남은 {_registeredPings.Count}개");
            if (_registeredPings.Count == 0 && _hideWhenNoPing)
            {
                Debug.Log("[DestinationPingUI] 등록된 핑 없음 → 인디케이터 OFF");
                _indicator?.gameObject.SetActive(false);
            }
        }

        // =============================================================================
        // 핵심 업데이트 로직
        // =============================================================================

        private void UpdatePingDisplay()
        {
            DestinationPing activePing = FindActivePing();

            if (activePing == null)
            {
                if (_hideWhenNoPing) _indicator?.gameObject.SetActive(false);
                return;
            }

            if (_mainCamera == null || _indicator == null || _canvasRect == null) return;

            // 거리 체크 - 너무 가까우면 인디케이터 숨김
            if (_hideWithinDistance > 0f)
            {
                Vector3 playerPos = PlayerController.Instance != null
                    ? PlayerController.Instance.transform.position
                    : _mainCamera.transform.position;

                float dist = Vector3.Distance(playerPos, activePing.transform.position);
                if (dist <= _hideWithinDistance)
                {
                    _indicator.gameObject.SetActive(false);
                    return;
                }
            }

            _indicator.gameObject.SetActive(true);

            // ── 뷰포트 좌표 계산 ──────────────────────────────────────────────
            // x: 0 = 왼쪽, 1 = 오른쪽 / y: 0 = 아래, 1 = 위
            // z: 음수이면 카메라 뒤 → x, y 반전 필요
            Vector3 viewport = _mainCamera.WorldToViewportPoint(activePing.transform.position);

            bool behindCamera = viewport.z < 0f;
            if (behindCamera)
            {
                viewport.x = 1f - viewport.x;
                viewport.y = 1f - viewport.y;
            }

            // ── 캔버스 로컬 좌표로 변환 ───────────────────────────────────────
            // 캔버스 중앙이 (0,0)이므로 viewport 0.5 기준으로 이동
            float halfW = _canvasRect.rect.width  * 0.5f;
            float halfH = _canvasRect.rect.height * 0.5f;

            float canvasX = (viewport.x - 0.5f) * _canvasRect.rect.width;
            float canvasY = (viewport.y - 0.5f) * _canvasRect.rect.height;

            // ── 화면 안/밖 판별 및 위치 결정 ─────────────────────────────────
            bool isOnScreen = !behindCamera
                && viewport.x > 0f && viewport.x < 1f
                && viewport.y > 0f && viewport.y < 1f;

            Vector2 targetPos;
            if (isOnScreen)
            {
                // 화면 안: 목적지 실제 위치에 표시
                targetPos = new Vector2(canvasX, canvasY);
            }
            else
            {
                // 화면 밖: 화면 중심 → 목적지 방향으로 가장자리까지 클램프
                // 방향 벡터를 화면 가장자리에 맞춰 스케일
                float clampW = halfW - _edgePadding;
                float clampH = halfH - _edgePadding;

                if (Mathf.Approximately(canvasX, 0f) && Mathf.Approximately(canvasY, 0f))
                {
                    targetPos = new Vector2(clampW, 0f);
                }
                else
                {
                    float scaleX = Mathf.Abs(canvasX) > 0.001f ? clampW / Mathf.Abs(canvasX) : float.MaxValue;
                    float scaleY = Mathf.Abs(canvasY) > 0.001f ? clampH / Mathf.Abs(canvasY) : float.MaxValue;
                    float scale  = Mathf.Min(scaleX, scaleY);
                    targetPos = new Vector2(canvasX * scale, canvasY * scale);
                }
            }

            // ── 위치 적용 ─────────────────────────────────────────────────────
            if (_smoothMove)
                _indicator.anchoredPosition = Vector2.Lerp(_indicator.anchoredPosition, targetPos, _moveSpeed * Time.deltaTime);
            else
                _indicator.anchoredPosition = targetPos;

            // ── 레이블: 남은 거리 (XXM) ──────────────────────────────────────
            if (_label != null)
            {
                float distance = Vector3.Distance(
                    PlayerController.Instance != null
                        ? PlayerController.Instance.transform.position
                        : _mainCamera.transform.position,
                    activePing.transform.position);

                _label.text = $"{Mathf.RoundToInt(distance)}M";
            }

            // ── 아이콘 갱신 ───────────────────────────────────────────────────
            if (_icon != null && activePing.Icon != null)
                _icon.sprite = activePing.Icon;
        }

        // =============================================================================
        // 내부 헬퍼
        // =============================================================================

        private DestinationPing FindActivePing()
        {
            _registeredPings.RemoveAll(p => p == null);
            for (int i = 0; i < _registeredPings.Count; i++)
            {
                if (_registeredPings[i].IsPingActive)
                    return _registeredPings[i];
            }
            return null;
        }

        private void UpdateCamera()
        {
            if (PlayerController.Instance?.MainCamera != null)
                _mainCamera = PlayerController.Instance.MainCamera;
            else if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        private void SetupCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
        }
    }
}
