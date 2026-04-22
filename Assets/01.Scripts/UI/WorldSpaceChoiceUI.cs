// =============================================================================
// WorldSpaceChoiceUI.cs
// =============================================================================
// 설명: 앨런 웨이크2 스타일의 월드 스페이스 선택지 UI
// 용도: 다이얼로그 선택지 표시 시 NPC 주변 3D 공간에 세로 목록으로 배치
//       숫자 키(1, 2, 3...) 또는 마우스 호버로 선택
// 구조:
//   World Space Canvas (단일)
//     └── Panel (VerticalLayoutGroup) - 모든 선택지 담는 컨테이너
//           └── ChoiceItem (HorizontalLayoutGroup) x N
//                 ├── KeyIconRoot (Image 배경) ← 패드 아이콘으로 교체 가능
//                 │     └── KeyText (TMP) - 숫자
//                 └── ChoiceText (TMP) - 대사
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using GameDatabase.Dialogue;

namespace GameDatabase.UI
{
    /// <summary>
    /// 앨런 웨이크2 스타일 월드 스페이스 선택지 UI
    /// 단일 Canvas에 VerticalLayoutGroup으로 선택지를 정렬하고 숫자 키 / 마우스 호버로 선택
    /// </summary>
    public class WorldSpaceChoiceUI : MonoBehaviour
    {
        // =============================================================================
        // 배치 설정
        // =============================================================================

        [Header("배치 설정")]
        [Tooltip("NPC 기준 선택지 높이 오프셋 (월드 단위). _npcAnchorTransform이 있으면 해당 위치 기준, 없으면 NPC 루트 + 이 값")]
        [SerializeField] private float _heightOffset = 1.5f;

        [Tooltip("NPC 앞쪽(플레이어 방향) 추가 오프셋")]
        [SerializeField] private float _forwardOffset = 0.3f;

        [Tooltip("기준점에서 카메라 기준 Negative Space 방향으로 띄우는 거리")]
        [SerializeField] private float _negativeSpaceOffset = 1.5f;

        [Header("화면 이탈 방지")]
        [Tooltip("뷰포트 경계 여백 (0~0.5)")]
        [SerializeField] private float _viewportMargin = 0.1f;

        // =============================================================================
        // Canvas / 패널 크기 설정
        // =============================================================================

        [Header("Canvas 크기 설정")]
        [Tooltip("World Space Canvas 가로 (월드 단위)")]
        [SerializeField] private float _canvasWidth = 3.5f;

        [Tooltip("Canvas 전체 스케일 (값이 작을수록 화면에서 작게 보임)")]
        [SerializeField] private float _canvasScale = 0.005f;

        [Tooltip("선택지 1개당 높이 (월드 단위)")]
        [SerializeField] private float _itemHeight = 0.55f;

        [Tooltip("선택지 간 세로 간격 (월드 단위)")]
        [SerializeField] private float _itemSpacing = 0.08f;

        [Tooltip("패널 상하 패딩 (월드 단위)")]
        [SerializeField] private float _panelPaddingV = 0.12f;

        // =============================================================================
        // 아이콘 설정
        // =============================================================================

        [Header("키 아이콘 설정")]
        [Tooltip("아이콘 배경 크기 (픽셀 기준, Canvas px 단위)")]
        [SerializeField] private float _iconSize = 46f;

        [Tooltip("아이콘 배경 색상 (Normal)")]
        [SerializeField] private Color _iconBgNormal = new Color(0.18f, 0.18f, 0.18f, 0.9f);

        [Tooltip("아이콘 배경 색상 (Highlighted)")]
        [SerializeField] private Color _iconBgHighlight = new Color(1f, 0.85f, 0.2f, 1f);

        [Tooltip("아이콘 숫자 텍스트 색상 (Normal)")]
        [SerializeField] private Color _iconTextNormal = new Color(1f, 0.85f, 0.2f, 1f);

        [Tooltip("아이콘 숫자 텍스트 색상 (Highlighted)")]
        [SerializeField] private Color _iconTextHighlight = new Color(0.1f, 0.1f, 0.1f, 1f);

        [Tooltip("아이콘 폰트 크기")]
        [SerializeField] private int _iconFontSize = 22;

        // =============================================================================
        // 텍스트 설정
        // =============================================================================

        [Header("텍스트 설정")]
        [Tooltip("선택지 텍스트 색상 (Normal)")]
        [SerializeField] private Color _textNormal = new Color(0.85f, 0.85f, 0.85f, 0.75f);

        [Tooltip("선택지 텍스트 색상 (Highlighted)")]
        [SerializeField] private Color _textHighlight = Color.white;

        [Tooltip("텍스트 폰트 크기 (Normal)")]
        [SerializeField] private int _fontSize = 24;

        [Tooltip("텍스트와 아이콘 사이 간격 (픽셀)")]
        [SerializeField] private float _iconTextGap = 12f;

        // =============================================================================
        // 애니메이션
        // =============================================================================

        [Header("애니메이션")]
        [Tooltip("페이드인 속도")]
        [SerializeField] private float _fadeInSpeed = 8f;

        [Tooltip("페이드아웃 속도")]
        [SerializeField] private float _fadeOutSpeed = 12f;

        [Tooltip("하이라이트 전환 속도")]
        [SerializeField] private float _highlightSpeed = 10f;

        // =============================================================================
        // 이벤트
        // =============================================================================

        /// <summary>
        /// 선택지 선택 시 호출되는 이벤트 (기존 ChoiceUI와 동일 시그니처)
        /// </summary>
        public event System.Action<int, ChoiceOption> OnChoiceSelected;

        // =============================================================================
        // 내부 상태
        // =============================================================================

        // ChoiceItem 프리팹 (Resources에서 로드, 캐시)
        private GameObject _choiceItemPrefab;
        private const string PREFAB_PATH = "Prefabs/Choice/ChoiceItem_1";

        // 현재 NPC Transform
        private Transform _npcTransform;

        // NPC 앵커 본 Transform (머리/어깨 등). WorldInteractable에서 NPC별로 런타임 전달됨
        private Transform _npcAnchorTransform;

        // 기준 월드 좌표 (화면 이탈 보정 전)
        private Vector3 _baseWorldPosition;

        // 현재 선택지 데이터
        private DialogueChoice _currentChoice;

        // 활성 여부
        private bool _isActive = false;

        // 선택 완료 여부
        private bool _hasSelected = false;

        // 생성된 루트 Canvas GameObject
        private GameObject _canvasRoot;

        // CanvasGroup (페이드)
        private CanvasGroup _canvasGroup;

        // 선택지 아이템 데이터 목록
        private List<ChoiceItemData> _items = new List<ChoiceItemData>();

        // 현재 호버/키보드 포커스 인덱스 (-1: 없음)
        private int _highlightedIndex = -1;

        // 페이드 코루틴
        private Coroutine _fadeCoroutine;

        // 하이라이트 코루틴들 (아이템별)
        private List<Coroutine> _highlightCoroutines = new List<Coroutine>();

        // =============================================================================
        // 내부 데이터 구조
        // =============================================================================

        /// <summary>
        /// 생성된 선택지 아이템 UI 참조 묶음
        /// </summary>
        private class ChoiceItemData
        {
            public GameObject Root;
            public Image IconBg;
            public Color IconBgOriginalColor; // 프리팹 원본 색상 보존
            public TextMeshProUGUI IconText;
            public TextMeshProUGUI ChoiceText;
            public int OriginalIndex;
            public ChoiceOption Option;
        }

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        public bool IsActive => _isActive;
        public bool HasSelected => _hasSelected;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            _choiceItemPrefab = Resources.Load<GameObject>(PREFAB_PATH);
            if (_choiceItemPrefab == null)
                Debug.LogError($"[WorldSpaceChoiceUI] 프리팹 없음: Resources/{PREFAB_PATH}");
        }

        private void Update()
        {
            if (!_isActive || _hasSelected) return;

            UpdateBillboard();
            HandleKeyInput();
        }

        private void OnDestroy()
        {
            ClearCanvas();
        }

        // =============================================================================
        // 공개 메서드
        // =============================================================================

        /// <summary>
        /// NPC Transform 설정 (앵커 없이)
        /// </summary>
        public void SetNpcTransform(Transform npcTransform)
        {
            _npcTransform = npcTransform;
            _npcAnchorTransform = null;
        }

        /// <summary>
        /// NPC Transform + 앵커 본 동시 설정
        /// WorldInteractable에서 NPC별로 호출
        /// </summary>
        public void SetNpcTransform(Transform npcTransform, Transform anchorTransform)
        {
            _npcTransform = npcTransform;
            _npcAnchorTransform = anchorTransform;
        }

        /// <summary>
        /// 선택지 표시
        /// </summary>
        public void DisplayChoice(DialogueChoice choice)
        {
            if (choice == null || !choice.HasValidOptions)
            {
                Debug.LogWarning("[WorldSpaceChoiceUI] 표시할 선택지가 없습니다.");
                return;
            }

            ClearCanvas();

            _currentChoice = choice;
            _hasSelected = false;
            _isActive = true;
            _highlightedIndex = -1;

            ChoiceOption[] validOptions = choice.GetValidOptions();

            // 배치 기준점 계산
            _baseWorldPosition = CalculateBasePosition();

            // Canvas + 패널 생성
            BuildCanvas(validOptions);

            // 1프레임 대기 후 페이드인 (LayoutGroup 레이아웃 계산 완료 보장)
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(WaitOneFrameThenFadeIn());
        }

        /// <summary>
        /// 선택지 숨기기
        /// </summary>
        public void Hide()
        {
            if (!_isActive) return;
            _isActive = false;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOutAndClear());
        }

        /// <summary>
        /// 강제 선택 (외부 호출용)
        /// </summary>
        public void ForceSelect(int optionIndex)
        {
            if (_hasSelected) return;
            SelectOption(optionIndex);
        }

        // =============================================================================
        // Canvas 빌드
        // =============================================================================

        /// <summary>
        /// World Space Canvas + VerticalLayoutGroup 패널 + 선택지 아이템 생성
        /// </summary>
        private void BuildCanvas(ChoiceOption[] validOptions)
        {
            int count = validOptions.Length;

            // 픽셀 기준 Canvas 크기 계산 (1 world unit = 100 px)
            float canvasPxW = _canvasWidth * 100f;
            float itemPxH   = _itemHeight  * 100f;
            float spacingPx = _itemSpacing * 100f;
            float paddingPx = _panelPaddingV * 100f;
            float totalPxH  = itemPxH * count + spacingPx * (count - 1) + paddingPx * 2f;

            // ── Canvas 루트 ──
            _canvasRoot = new GameObject("WorldSpaceChoiceCanvas");
            _canvasRoot.transform.position = ClampToViewport(_baseWorldPosition);

            Canvas canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            RectTransform canvasRect = _canvasRoot.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(canvasPxW, totalPxH);
            canvasRect.localScale = Vector3.one * _canvasScale;

            _canvasGroup = _canvasRoot.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;

            // EventSystem이 없으면 마우스 호버 작동 안 하므로 GraphicRaycaster 추가
            _canvasRoot.AddComponent<GraphicRaycaster>();

            // ── 패널 배경 ──
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(_canvasRoot.transform, false);

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = Color.clear;

            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // VerticalLayoutGroup
            VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment       = TextAnchor.UpperLeft;
            vlg.spacing              = spacingPx;
            vlg.padding              = new RectOffset(
                (int)(_iconSize * 0.3f), (int)(_iconSize * 0.3f),
                (int)paddingPx, (int)paddingPx);
            vlg.childControlWidth    = true;
            vlg.childControlHeight   = false;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            // ── 선택지 아이템 생성 ──
            _items.Clear();
            _highlightCoroutines.Clear();

            for (int i = 0; i < count; i++)
            {
                int originalIndex = GetOriginalIndex(_currentChoice, validOptions[i]);
                ChoiceItemData item = BuildChoiceItem(panelObj.transform, i, originalIndex, validOptions[i], itemPxH);
                _items.Add(item);
                _highlightCoroutines.Add(null);
            }

            // LayoutGroup이 즉시 레이아웃을 계산하도록 강제 리빌드
            // (Instantiate 직후 레이아웃이 적용되지 않아 아이템이 어긋나는 문제 해결)
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        }

        /// <summary>
        /// 선택지 아이템 1개 빌드 - ChoiceItem_1 프리팹 Instantiate
        /// </summary>
        private ChoiceItemData BuildChoiceItem(
            Transform parent, int displayIndex, int originalIndex,
            ChoiceOption option, float itemPxH)
        {
            ChoiceItemData data = new ChoiceItemData
            {
                OriginalIndex = originalIndex,
                Option = option
            };

            // 프리팹 Instantiate
            GameObject itemRoot = _choiceItemPrefab != null
                ? Instantiate(_choiceItemPrefab, parent)
                : new GameObject($"ChoiceItem_{displayIndex + 1}");

            if (_choiceItemPrefab == null)
                itemRoot.transform.SetParent(parent, false);

            itemRoot.SetActive(true);
            data.Root = itemRoot;

            // 높이 설정
            RectTransform itemRect = itemRoot.GetComponent<RectTransform>();
            if (itemRect != null) itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, itemPxH);

            // 컴포넌트 탐색 (NumberBG → Image, Number_Text → TMP[0], ChoiceText → TMP[1])
            var tmps = itemRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmps.Length >= 2) { data.IconText = tmps[0]; data.ChoiceText = tmps[1]; }
            else if (tmps.Length == 1) { data.ChoiceText = tmps[0]; }

            // NumberBG Image (첫 번째 Image, 프리팹 원본 색상 저장)
            var images = itemRoot.GetComponentsInChildren<Image>(true);
            if (images.Length > 0)
            {
                data.IconBg = images[0];
                data.IconBgOriginalColor = images[0].color;
            }

            // 텍스트 채우기
            if (data.IconText   != null) data.IconText.text   = (displayIndex + 1).ToString();
            if (data.ChoiceText != null) data.ChoiceText.text = option.ChoiceText;

            // 텍스트 색상 초기화 (IconBg 색상은 프리팹 원본 유지)
            if (data.IconText   != null) data.IconText.color   = _iconTextNormal;
            if (data.ChoiceText != null) data.ChoiceText.color = _textNormal;

            // 마우스 호버 이벤트
            int capturedIndex = displayIndex;
            PointerEventForwarder hoverHandler = itemRoot.GetComponent<PointerEventForwarder>();
            if (hoverHandler == null) hoverHandler = itemRoot.AddComponent<PointerEventForwarder>();
            hoverHandler.OnPointerEnterCallback = () => SetHighlight(capturedIndex, true);
            hoverHandler.OnPointerExitCallback  = () => SetHighlight(capturedIndex, false);
            hoverHandler.OnPointerClickCallback = () =>
            {
                if (!_hasSelected) SelectOption(_items[capturedIndex].OriginalIndex);
            };

            return data;
        }

        // =============================================================================
        // 하이라이트 (Visual State)
        // =============================================================================

        /// <summary>
        /// 특정 아이템 하이라이트 on/off
        /// </summary>
        private void SetHighlight(int index, bool on)
        {
            if (index < 0 || index >= _items.Count) return;

            if (on)
            {
                // 이전 하이라이트 해제
                if (_highlightedIndex >= 0 && _highlightedIndex != index)
                    AnimateItemHighlight(_highlightedIndex, false);

                _highlightedIndex = index;
            }
            else
            {
                if (_highlightedIndex == index) _highlightedIndex = -1;
            }

            AnimateItemHighlight(index, on);
        }

        /// <summary>
        /// 아이템 하이라이트 애니메이션 (코루틴)
        /// </summary>
        private void AnimateItemHighlight(int index, bool highlighted)
        {
            if (index < 0 || index >= _items.Count) return;

            if (_highlightCoroutines[index] != null)
                StopCoroutine(_highlightCoroutines[index]);

            _highlightCoroutines[index] = StartCoroutine(
                LerpItemColors(index, highlighted));
        }

        private IEnumerator LerpItemColors(int index, bool highlighted)
        {
            ChoiceItemData item = _items[index];

            Color targetIconBg   = highlighted ? _iconBgHighlight : item.IconBgOriginalColor;
            Color targetIconText = highlighted ? _iconTextHighlight : _iconTextNormal;
            Color targetText     = highlighted ? _textHighlight     : _textNormal;

            Color startIconBg   = item.IconBg.color;
            Color startIconText = item.IconText.color;
            Color startText     = item.ChoiceText.color;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * _highlightSpeed;
                float s = Mathf.Clamp01(t);

                item.IconBg.color     = Color.Lerp(startIconBg,   targetIconBg,   s);
                item.IconText.color   = Color.Lerp(startIconText, targetIconText, s);
                item.ChoiceText.color = Color.Lerp(startText,     targetText,     s);

                yield return null;
            }

            item.IconBg.color     = targetIconBg;
            item.IconText.color   = targetIconText;
            item.ChoiceText.color = targetText;

            _highlightCoroutines[index] = null;
        }

        // =============================================================================
        // 입력 처리
        // =============================================================================

        private void HandleKeyInput()
        {
            if (_currentChoice == null) return;

            int count = Mathf.Min(_items.Count, 9);
            for (int i = 0; i < count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    // 키보드 입력 시 하이라이트 → 선택
                    SetHighlight(i, true);
                    SelectOption(_items[i].OriginalIndex);
                    return;
                }
            }
        }

        // =============================================================================
        // 선택 처리
        // =============================================================================

        private void SelectOption(int originalIndex)
        {
            if (_hasSelected) return;
            _hasSelected = true;
            _isActive    = false;

            ChoiceOption selectedOption = _currentChoice?.GetOption(originalIndex);
            OnChoiceSelected?.Invoke(originalIndex, selectedOption);

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOutAndClear());
        }

        // =============================================================================
        // Billboard + 위치 갱신
        // =============================================================================

        private void UpdateBillboard()
        {
            if (_canvasRoot == null || Camera.main == null) return;

            // NPC가 움직이면 기준 위치 재계산
            if (_npcTransform != null)
                _baseWorldPosition = CalculateBasePosition();

            // 화면 이탈 보정 후 위치 갱신
            _canvasRoot.transform.position = ClampToViewport(_baseWorldPosition);

            // 카메라 방향으로 회전 (Billboard)
            Vector3 dir = Camera.main.transform.position - _canvasRoot.transform.position;
            if (dir != Vector3.zero)
                // Canvas 앞면이 카메라를 바라보도록 방향 반전
                _canvasRoot.transform.rotation = Quaternion.LookRotation(-dir);
        }

        // =============================================================================
        // 배치 계산
        // =============================================================================

        /// <summary>
        /// 앵커 본 또는 NPC 루트 + heightOffset 위치 반환
        /// </summary>
        private Vector3 GetAnchorWorldPosition()
        {
            if (_npcAnchorTransform != null)
                return _npcAnchorTransform.position;

            Vector3 origin = _npcTransform != null ? _npcTransform.position : transform.position;
            return origin + Vector3.up * _heightOffset;
        }

        /// <summary>
        /// NPC 뷰포트 위치 기준 Negative Space 방향 반환
        /// </summary>
        private Vector3 GetNegativeSpaceDirection(Vector3 anchorWorldPos)
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector3.right;

            Vector3 viewportPos = cam.WorldToViewportPoint(anchorWorldPos);
            Vector3 camRight = cam.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            float sign = viewportPos.x < 0.5f ? 1f : -1f;
            return camRight * sign;
        }

        /// <summary>
        /// 패널 기준 중심 월드 좌표 계산 (단일 Canvas이므로 중심 1개만)
        /// </summary>
        private Vector3 CalculateBasePosition()
        {
            Vector3 anchor = GetAnchorWorldPosition();

            Vector3 toPlayer = Vector3.zero;
            if (Camera.main != null)
            {
                toPlayer = Camera.main.transform.position - anchor;
                toPlayer.y = 0f;
                toPlayer.Normalize();
            }

            Vector3 negSpaceDir = GetNegativeSpaceDirection(anchor);

            return anchor
                + toPlayer * _forwardOffset
                + negSpaceDir * _negativeSpaceOffset;
        }

        /// <summary>
        /// 월드 좌표를 뷰포트 경계 안으로 클램핑
        /// </summary>
        private Vector3 ClampToViewport(Vector3 worldPos)
        {
            Camera cam = Camera.main;
            if (cam == null) return worldPos;

            Vector3 vp = cam.WorldToViewportPoint(worldPos);
            if (vp.z <= 0f) return worldPos;

            float min = _viewportMargin;
            float max = 1f - _viewportMargin;
            bool clamped = false;

            if (vp.x < min) { vp.x = min; clamped = true; }
            if (vp.x > max) { vp.x = max; clamped = true; }
            if (vp.y < min) { vp.y = min; clamped = true; }
            if (vp.y > max) { vp.y = max; clamped = true; }

            return clamped ? cam.ViewportToWorldPoint(vp) : worldPos;
        }

        // =============================================================================
        // 정리
        // =============================================================================

        private void ClearCanvas()
        {
            StopAllHighlightCoroutines();
            _items.Clear();
            _highlightCoroutines.Clear();

            if (_canvasRoot != null)
            {
                Destroy(_canvasRoot);
                _canvasRoot  = null;
                _canvasGroup = null;
            }
        }

        private void StopAllHighlightCoroutines()
        {
            for (int i = 0; i < _highlightCoroutines.Count; i++)
            {
                if (_highlightCoroutines[i] != null)
                {
                    StopCoroutine(_highlightCoroutines[i]);
                    _highlightCoroutines[i] = null;
                }
            }
        }

        private int GetOriginalIndex(DialogueChoice choice, ChoiceOption option)
        {
            if (choice?.Options == null) return 0;
            for (int i = 0; i < choice.Options.Length; i++)
            {
                if (choice.Options[i] == option) return i;
            }
            return 0;
        }

        // =============================================================================
        // 페이드 코루틴
        // =============================================================================

        /// <summary>
        /// 1프레임 대기 후 페이드인 시작 (LayoutGroup 레이아웃 안정화 후 표시)
        /// </summary>
        private IEnumerator WaitOneFrameThenFadeIn()
        {
            yield return null; // 1프레임 대기 → LayoutGroup 계산 완료
            _fadeCoroutine = StartCoroutine(FadeCanvas(0f, 1f, _fadeInSpeed));
        }

        private IEnumerator FadeCanvas(float from, float to, float speed)
        {
            if (_canvasGroup == null) yield break;

            _canvasGroup.alpha = from;
            float alpha = from;

            while (!Mathf.Approximately(alpha, to))
            {
                alpha = Mathf.MoveTowards(alpha, to, speed * Time.deltaTime);
                if (_canvasGroup != null) _canvasGroup.alpha = alpha;
                yield return null;
            }

            if (_canvasGroup != null) _canvasGroup.alpha = to;
            _fadeCoroutine = null;
        }

        private IEnumerator FadeOutAndClear()
        {
            float startAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;
            yield return FadeCanvas(startAlpha, 0f, _fadeOutSpeed);
            ClearCanvas();
        }
    }

    // =============================================================================
    // PointerEventForwarder
    // =============================================================================

    /// <summary>
    /// 마우스 Enter/Exit/Click 이벤트를 WorldSpaceChoiceUI 아이템에 전달하는 헬퍼
    /// </summary>
    public class PointerEventForwarder : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public System.Action OnPointerEnterCallback;
        public System.Action OnPointerExitCallback;
        public System.Action OnPointerClickCallback;

        public void OnPointerEnter(PointerEventData eventData) => OnPointerEnterCallback?.Invoke();
        public void OnPointerExit(PointerEventData eventData)  => OnPointerExitCallback?.Invoke();
        public void OnPointerClick(PointerEventData eventData) => OnPointerClickCallback?.Invoke();
    }
}
