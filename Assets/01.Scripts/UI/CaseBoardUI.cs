// =============================================================================
// CaseBoardUI.cs
// =============================================================================
// 설명: 사건 보드 2D Canvas UI 전체 제어
// 용도: 보드 열기/닫기, 노드 슬롯 생성, 줌/팬, Red String 표시
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using GameDatabase.CaseBoard;
using GameDatabase.Evidence;

namespace GameDatabase.UI
{
    /// <summary>
    /// 사건 보드 2D Canvas UI
    /// </summary>
    public class CaseBoardUI : MonoBehaviour
    {
        // =============================================================================
        // UI 요소 - 보드 루트
        // =============================================================================

        [Header("=== 보드 UI ===")]

        [Tooltip("보드 루트 (CanvasGroup)")]
        [SerializeField] private CanvasGroup _boardRoot;

        [Tooltip("보드 배경 이미지")]
        [SerializeField] private Image _boardBackgroundImage;

        // =============================================================================
        // UI 요소 - 보드 뷰포트 (줌/팬)
        // =============================================================================

        [Header("=== 보드 뷰포트 ===")]

        [Tooltip("보드 뷰포트 (RectMask2D 적용)")]
        [SerializeField] private RectTransform _boardViewport;

        [Tooltip("보드 콘텐츠 (줌/팬 대상)")]
        [SerializeField] private RectTransform _boardContent;

        // =============================================================================
        // UI 요소 - 헤더
        // =============================================================================

        [Header("=== 헤더 ===")]

        [Tooltip("사건 제목 텍스트")]
        [SerializeField] private TextMeshProUGUI _caseTitleText;

        [Tooltip("닫기 버튼")]
        [SerializeField] private Button _closeButton;

        // =============================================================================
        // UI 요소 - 인벤토리
        // =============================================================================

        [Header("=== 인벤토리 ===")]

        [Tooltip("인벤토리 UI 컴포넌트")]
        [SerializeField] private CaseBoardInventoryUI _inventoryUI;

        // =============================================================================
        // UI 요소 - 드래그
        // =============================================================================

        [Header("=== 드래그 ===")]

        [Tooltip("드래그 고스트 (CanvasGroup)")]
        [SerializeField] private CanvasGroup _dragGhost;

        [Tooltip("드래그 고스트 이미지")]
        [SerializeField] private Image _dragGhostImage;

        // =============================================================================
        // Red String
        // =============================================================================

        [Header("=== Red String ===")]

        [Tooltip("Red String 렌더러")]
        [SerializeField] private UILineRenderer _redStringRenderer;

        // =============================================================================
        // 프리팹
        // =============================================================================

        [Header("=== 프리팹 ===")]

        [Tooltip("노드 슬롯 프리팹")]
        [SerializeField] private GameObject _nodeSlotPrefab;

        // =============================================================================
        // 줌/팬 설정
        // =============================================================================

        [Header("=== 줌/팬 설정 ===")]

        [Tooltip("줌 속도")]
        [SerializeField] private float _zoomSpeed = 0.1f;

        [Tooltip("최소 줌")]
        [SerializeField] private float _minZoom = 0.5f;

        [Tooltip("최대 줌")]
        [SerializeField] private float _maxZoom = 2.0f;

        // =============================================================================
        // 애니메이션 설정
        // =============================================================================

        [Header("=== 애니메이션 설정 ===")]

        [Tooltip("열기/닫기 애니메이션 시간")]
        [Range(0.2f, 1.5f)]
        [SerializeField] private float _animationDuration = 0.5f;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private bool _isOpen = false;
        private CaseBoardData _currentBoardData;
        private CaseBoardRuntimeState _currentRuntimeState;

        // 줌/팬
        private float _currentZoom = 1.0f;
        private bool _isPanning = false;
        private Vector2 _lastMousePosition;

        // 노드 슬롯 관리
        private Dictionary<string, CaseNodeSlotUI> _nodeSlots = new Dictionary<string, CaseNodeSlotUI>();

        // 애니메이션
        private Sequence _animationSequence;

        // Canvas 참조
        private Canvas _canvas;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 보드 UI가 열려있는지 여부
        /// </summary>
        public bool IsOpen => _isOpen;

        /// <summary>
        /// 드래그 고스트 CanvasGroup
        /// </summary>
        public CanvasGroup DragGhost => _dragGhost;

        /// <summary>
        /// 드래그 고스트 이미지
        /// </summary>
        public Image DragGhostImage => _dragGhostImage;

        /// <summary>
        /// 보드 콘텐츠 RectTransform (드롭 좌표 변환용)
        /// </summary>
        public RectTransform BoardContent => _boardContent;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            HideImmediate();

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        private void Update()
        {
            if (!_isOpen) return;

            HandleZoomAndPan();
            HandleInput();
        }

        private void OnDestroy()
        {
            _animationSequence?.Kill();
        }

        // =============================================================================
        // 열기/닫기
        // =============================================================================

        /// <summary>
        /// 보드 UI 열기
        /// </summary>
        public void Open(CaseBoardData boardData, CaseBoardRuntimeState runtimeState)
        {
            _currentBoardData = boardData;
            _currentRuntimeState = runtimeState;
            _isOpen = true;

            // Canvas를 Screen Space - Camera 모드로 전환
            if (_canvas != null)
            {
                _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                _canvas.worldCamera = Camera.main;
                _canvas.planeDistance = 1f;
            }

            // 제목 설정
            if (_caseTitleText != null)
            {
                _caseTitleText.text = boardData.CaseBoardTitle;
            }

            // 배경 이미지
            if (_boardBackgroundImage != null && boardData.BoardBackground != null)
            {
                _boardBackgroundImage.sprite = boardData.BoardBackground;
            }

            // 노드 슬롯 생성
            CreateNodeSlots();

            // Red String이 노드 슬롯 위에 그려지도록 순서 조정
            if (_redStringRenderer != null)
            {
                _redStringRenderer.transform.SetAsLastSibling();
                _redStringRenderer.raycastTarget = false;
            }

            // 인벤토리 초기화
            if (_inventoryUI != null)
            {
                _inventoryUI.Initialize(this);
                RefreshInventory();
            }

            // Red String 복원
            RefreshRedStrings(runtimeState);

            // 줌/팬 초기화
            _currentZoom = 1.0f;
            if (_boardContent != null)
            {
                _boardContent.localScale = Vector3.one;
                _boardContent.anchoredPosition = Vector2.zero;
            }

            // 열기 애니메이션
            PlayOpenAnimation();
        }

        /// <summary>
        /// 보드 UI 닫기
        /// </summary>
        public void Close()
        {
            PlayCloseAnimation(() =>
            {
                _isOpen = false;
                ClearNodeSlots();
                HideImmediate();
            });
        }

        private void HideImmediate()
        {
            if (_boardRoot != null)
            {
                _boardRoot.alpha = 0f;
                _boardRoot.blocksRaycasts = false;
                _boardRoot.interactable = false;
            }

            if (_dragGhost != null)
            {
                _dragGhost.alpha = 0f;
                _dragGhost.gameObject.SetActive(false);
            }

            // Canvas 모드를 Overlay로 복원
            if (_canvas != null)
            {
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.worldCamera = null;
            }
        }

        // =============================================================================
        // 노드 슬롯 관리
        // =============================================================================

        private void CreateNodeSlots()
        {
            ClearNodeSlots();

            if (_currentBoardData == null || _boardContent == null || _nodeSlotPrefab == null) return;

            foreach (CaseNodeData nodeData in _currentBoardData.Nodes)
            {
                GameObject slotObj = Instantiate(_nodeSlotPrefab, _boardContent);
                CaseNodeSlotUI slotUI = slotObj.GetComponent<CaseNodeSlotUI>();

                if (slotUI != null)
                {
                    slotUI.Initialize(nodeData, this);

                    // 보드 위치 설정 (정규화 좌표 → 앵커 포지션)
                    RectTransform slotRect = slotObj.GetComponent<RectTransform>();
                    if (slotRect != null)
                    {
                        slotRect.anchorMin = nodeData.BoardPosition;
                        slotRect.anchorMax = nodeData.BoardPosition;
                        slotRect.anchoredPosition = Vector2.zero;
                    }

                    // 기존 배치 상태 복원
                    string placedEvidenceID = _currentRuntimeState?.GetPlacedEvidenceID(nodeData.NodeID);
                    if (!string.IsNullOrEmpty(placedEvidenceID))
                    {
                        EvidenceData evidence = _currentBoardData.GetEvidenceByID(placedEvidenceID);
                        if (evidence != null)
                        {
                            slotUI.ShowPlacedEvidence(evidence);
                        }
                    }

                    _nodeSlots[nodeData.NodeID] = slotUI;
                }
            }
        }

        private void ClearNodeSlots()
        {
            foreach (var kvp in _nodeSlots)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            _nodeSlots.Clear();
        }

        /// <summary>
        /// 노드 슬롯 UI 반환
        /// </summary>
        public CaseNodeSlotUI GetNodeSlot(string nodeID)
        {
            _nodeSlots.TryGetValue(nodeID, out CaseNodeSlotUI slot);
            return slot;
        }

        // =============================================================================
        // 인벤토리
        // =============================================================================

        /// <summary>
        /// 인벤토리 새로고침
        /// </summary>
        public void RefreshInventory()
        {
            if (_inventoryUI == null) return;

            List<EvidenceData> available = CaseBoardManager.Instance.GetAvailableEvidences();
            _inventoryUI.RefreshSlots(available);
        }

        // =============================================================================
        // Red String
        // =============================================================================

        /// <summary>
        /// 두 노드 간 Red String 그리기
        /// </summary>
        public void DrawRedString(string fromNodeID, string toNodeID)
        {
            if (_currentRuntimeState == null) return;
            RefreshRedStrings(_currentRuntimeState);
        }

        /// <summary>
        /// 모든 Red String 새로고침
        /// </summary>
        public void RefreshRedStrings(CaseBoardRuntimeState state)
        {
            if (_redStringRenderer == null)
            {
                Debug.LogWarning("[CaseBoardUI] _redStringRenderer가 null!");
                return;
            }

            Debug.Log($"[CaseBoardUI] RefreshRedStrings - connections: {state.ActiveConnections.Count}, 노드슬롯 수: {_nodeSlots.Count}");

            var lines = new List<(Vector2 start, Vector2 end)>();

            foreach (RedStringConnection conn in state.ActiveConnections)
            {
                CaseNodeSlotUI fromSlot = GetNodeSlot(conn.FromNodeID);
                CaseNodeSlotUI toSlot = GetNodeSlot(conn.ToNodeID);

                Debug.Log($"[CaseBoardUI] 연결: {conn.FromNodeID} → {conn.ToNodeID}, fromSlot null: {fromSlot == null}, toSlot null: {toSlot == null}");

                if (fromSlot != null && toSlot != null)
                {
                    RectTransform fromRect = fromSlot.GetComponent<RectTransform>();
                    RectTransform toRect = toSlot.GetComponent<RectTransform>();

                    if (fromRect != null && toRect != null)
                    {
                        Vector2 fromPos = GetLocalPositionInRenderer(fromRect);
                        Vector2 toPos = GetLocalPositionInRenderer(toRect);
                        Debug.Log($"[CaseBoardUI] Red String 좌표: ({fromPos}) → ({toPos})");
                        lines.Add((fromPos, toPos));
                    }
                }
            }

            Debug.Log($"[CaseBoardUI] SetConnections 호출: {lines.Count}개 라인");
            _redStringRenderer.SetConnections(lines);
        }

        private Vector2 GetLocalPositionInRenderer(RectTransform target)
        {
            if (_redStringRenderer == null) return Vector2.zero;

            RectTransform rendererRect = _redStringRenderer.GetComponent<RectTransform>();
            if (rendererRect == null) return Vector2.zero;

            // target의 월드 좌표를 rendererRect의 로컬 좌표로 직접 변환
            Vector3 localPos = rendererRect.InverseTransformPoint(target.position);
            return new Vector2(localPos.x, localPos.y);
        }

        // =============================================================================
        // 줌 / 팬
        // =============================================================================

        private void HandleZoomAndPan()
        {
            if (_boardContent == null || _boardViewport == null) return;

            // 인벤토리 영역 위에서는 줌/팬 비활성화
            if (_inventoryUI != null && _inventoryUI.IsPointerOverInventory)
                return;

            // 줌: 마우스 스크롤 휠
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _currentZoom = Mathf.Clamp(_currentZoom + scroll * _zoomSpeed, _minZoom, _maxZoom);
                _boardContent.localScale = Vector3.one * _currentZoom;

                // Red String 갱신 (줌 시 위치 변경)
                if (_currentRuntimeState != null)
                {
                    RefreshRedStrings(_currentRuntimeState);
                }
            }

            // 팬: 마우스 우클릭 또는 중간 버튼 드래그
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                _isPanning = true;
                _lastMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
            {
                _isPanning = false;
            }

            if (_isPanning)
            {
                Vector2 delta = (Vector2)Input.mousePosition - _lastMousePosition;
                _boardContent.anchoredPosition += delta;
                _lastMousePosition = Input.mousePosition;

                // Red String 갱신 (팬 시 위치 변경)
                if (_currentRuntimeState != null)
                {
                    RefreshRedStrings(_currentRuntimeState);
                }
            }
        }

        // =============================================================================
        // 입력 처리
        // =============================================================================

        private void HandleInput()
        {
            // ESC 키로 보드 닫기
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CaseBoardManager.Instance.CloseBoard();
            }
        }

        private void OnCloseButtonClicked()
        {
            CaseBoardManager.Instance.CloseBoard();
        }

        // =============================================================================
        // 애니메이션
        // =============================================================================

        private void PlayOpenAnimation()
        {
            _animationSequence?.Kill();
            _animationSequence = DOTween.Sequence();
            _animationSequence.SetUpdate(true);

            if (_boardRoot != null)
            {
                _boardRoot.alpha = 0f;
                _boardRoot.blocksRaycasts = true;
                _boardRoot.interactable = true;

                _animationSequence.Append(
                    DOTween.To(() => _boardRoot.alpha, x => _boardRoot.alpha = x, 1f, _animationDuration)
                        .SetEase(Ease.OutCubic)
                );

                // 스케일 효과
                RectTransform rootRect = _boardRoot.GetComponent<RectTransform>();
                if (rootRect != null)
                {
                    rootRect.localScale = Vector3.one * 0.9f;
                    _animationSequence.Join(
                        rootRect.DOScale(1f, _animationDuration).SetEase(Ease.OutBack)
                    );
                }
            }
        }

        private void PlayCloseAnimation(System.Action onComplete)
        {
            _animationSequence?.Kill();
            _animationSequence = DOTween.Sequence();
            _animationSequence.SetUpdate(true);

            if (_boardRoot != null)
            {
                _animationSequence.Append(
                    DOTween.To(() => _boardRoot.alpha, x => _boardRoot.alpha = x, 0f, _animationDuration * 0.7f)
                        .SetEase(Ease.InCubic)
                );

                RectTransform rootRect = _boardRoot.GetComponent<RectTransform>();
                if (rootRect != null)
                {
                    _animationSequence.Join(
                        rootRect.DOScale(0.9f, _animationDuration * 0.7f).SetEase(Ease.InBack)
                    );
                }
            }

            _animationSequence.AppendCallback(() =>
            {
                onComplete?.Invoke();
            });
        }
    }
}
