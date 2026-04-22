// =============================================================================
// InvestigationUI.cs
// =============================================================================
// 설명: 조사 모드 UI 관리 (DOTween 기반 연출)
// 용도: 좌측 상단 인디케이터, 상호작용 텍스트 표시
// UI 구성:
//   1. 좌측 상단: 돋보기 아이콘 + "조사 모드" 텍스트
//   2. 화면 하단: [E] 조사하기 텍스트 (단서 호버 시)
// 특징:
//   - DOTween으로 모든 페이드인/아웃 처리
//   - 돋보기 아이콘 빛나는 효과 (Glow Loop)
//   - TextMeshPro 사용
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace GameDatabase.Player
{
    /// <summary>
    /// 앨런 웨이크 2 스타일 조사 모드 UI
    /// DOTween 기반 시네마틱 연출
    /// </summary>
    public class InvestigationUI : MonoBehaviour
    {
        // =============================================================================
        // UI 요소 - 좌측 상단 인디케이터
        // =============================================================================

        [Header("=== 좌측 상단 인디케이터 ===")]

        [Tooltip("인디케이터 루트 (CanvasGroup 필요)")]
        [SerializeField] private CanvasGroup _indicatorRoot;

        [Tooltip("돋보기 아이콘 Image")]
        [SerializeField] private Image _magnifierIcon;

        [Tooltip("\"조사 모드\" 텍스트 (TMP)")]
        [SerializeField] private TextMeshProUGUI _modeText;

        // =============================================================================
        // UI 요소 - 상호작용 텍스트
        // =============================================================================

        [Header("=== 상호작용 텍스트 ===")]

        [Tooltip("상호작용 텍스트 루트 (CanvasGroup 필요)")]
        [SerializeField] private CanvasGroup _interactionRoot;

        [Tooltip("상호작용 텍스트 (TMP)")]
        [SerializeField] private TextMeshProUGUI _interactionText;

        [Tooltip("상호작용 텍스트 포맷 (예: \"[E] {0}\")")]
        [SerializeField] private string _interactionFormat = "[E] {0}";

        // =============================================================================
        // UI 요소 - 단서 정보
        // =============================================================================

        [Header("=== 단서 정보 ===")]

        [Tooltip("단서 정보 루트 (CanvasGroup 필요)")]
        [SerializeField] private CanvasGroup _clueInfoRoot;

        [Tooltip("단서 이름 텍스트 (TMP)")]
        [SerializeField] private TextMeshProUGUI _clueNameText;

        [Tooltip("단서 설명 텍스트 (TMP)")]
        [SerializeField] private TextMeshProUGUI _clueDescriptionText;

        [Tooltip("단서 정보 오프셋 (오브젝트 위 픽셀)")]
        [SerializeField] private Vector2 _clueInfoOffset = new Vector2(0, 100);

        [Tooltip("단서 정보가 오브젝트 위치를 따라다니기")]
        [SerializeField] private bool _followCluePosition = true;

        // =============================================================================
        // 애니메이션 설정
        // =============================================================================

        [Header("=== 애니메이션 설정 ===")]

        [Tooltip("페이드 인/아웃 시간 (초)")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _fadeDuration = 0.5f;

        [Tooltip("돋보기 아이콘 빛나는 효과 강도")]
        [Range(0f, 2f)]
        [SerializeField] private float _glowIntensity = 1.3f;

        [Tooltip("돋보기 아이콘 빛나는 속도 (초)")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _glowSpeed = 1.5f;

        [Tooltip("상호작용 텍스트 펄스 효과 사용")]
        [SerializeField] private bool _usePulseEffect = true;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private Sequence _indicatorSequence;
        private Sequence _interactionSequence;
        private Sequence _clueInfoSequence;
        private Tween _glowTween;

        private Transform _currentClueTransform;
        private Camera _mainCamera;
        private RectTransform _clueInfoRectTransform;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // 초기 상태 (숨김)
            HideImmediate();

            // 카메라 찾기
            _mainCamera = Camera.main;

            // RectTransform 캐싱
            if (_clueInfoRoot != null)
            {
                _clueInfoRectTransform = _clueInfoRoot.GetComponent<RectTransform>();
            }
        }

        private void Update()
        {
            // 단서 정보가 오브젝트를 따라다니도록
            if (_followCluePosition && _currentClueTransform != null && _clueInfoRectTransform != null && _mainCamera != null)
            {
                UpdateClueInfoPosition();
            }
        }

        private void OnDestroy()
        {
            // DOTween 정리
            _indicatorSequence?.Kill();
            _interactionSequence?.Kill();
            _clueInfoSequence?.Kill();
            _glowTween?.Kill();
        }

        // =============================================================================
        // 조사 모드 UI 표시/숨김
        // =============================================================================

        /// <summary>
        /// 조사 모드 UI 표시 (DOTween 페이드인)
        /// </summary>
        public void ShowInvestigationMode()
        {
            // 시퀀스 정리
            _indicatorSequence?.Kill();

            // 페이드인 시퀀스
            _indicatorSequence = DOTween.Sequence();
            _indicatorSequence.Append(
                _indicatorRoot.DOFade(1f, _fadeDuration).SetEase(Ease.OutCubic)
            );

            // 돋보기 아이콘 빛나는 효과 시작
            StartGlowEffect();

            Debug.Log("[InvestigationUI] 조사 모드 UI 표시");
        }

        /// <summary>
        /// 조사 모드 UI 숨김 (DOTween 페이드아웃)
        /// </summary>
        public void HideInvestigationMode()
        {
            // 빛나는 효과 중지
            StopGlowEffect();

            // 시퀀스 정리
            _indicatorSequence?.Kill();

            // 페이드아웃 시퀀스
            _indicatorSequence = DOTween.Sequence();
            _indicatorSequence.Append(
                _indicatorRoot.DOFade(0f, _fadeDuration).SetEase(Ease.InCubic)
            );

            Debug.Log("[InvestigationUI] 조사 모드 UI 숨김");
        }

        /// <summary>
        /// 즉시 숨김 (초기화용)
        /// </summary>
        public void HideImmediate()
        {
            if (_indicatorRoot != null)
            {
                _indicatorRoot.alpha = 0f;
            }
            if (_interactionRoot != null)
            {
                _interactionRoot.alpha = 0f;
            }
            if (_clueInfoRoot != null)
            {
                _clueInfoRoot.alpha = 0f;
            }

            StopGlowEffect();
        }

        // =============================================================================
        // 상호작용 프롬프트 표시/숨김
        // =============================================================================

        /// <summary>
        /// 상호작용 텍스트 표시 (DOTween 페이드인)
        /// </summary>
        /// <param name="clueName">단서 이름</param>
        public void ShowInteractionPrompt(string clueName)
        {
            if (_interactionText != null)
            {
                _interactionText.text = string.Format(_interactionFormat, clueName);
            }

            // 시퀀스 정리
            _interactionSequence?.Kill();

            // 페이드인 시퀀스
            _interactionSequence = DOTween.Sequence();
            _interactionSequence.Append(
                _interactionRoot.DOFade(1f, _fadeDuration * 0.5f).SetEase(Ease.OutCubic)
            );

            // 펄스 효과 (선택)
            if (_usePulseEffect && _interactionText != null)
            {
                _interactionSequence.Append(
                    _interactionText.transform.DOScale(1.05f, 0.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine)
                );
            }
        }

        /// <summary>
        /// 상호작용 텍스트 숨김 (DOTween 페이드아웃)
        /// </summary>
        public void HideInteractionPrompt()
        {
            // 시퀀스 정리
            _interactionSequence?.Kill();

            // 펄스 효과 중지
            if (_interactionText != null)
            {
                _interactionText.transform.DOKill();
                _interactionText.transform.localScale = Vector3.one;
            }

            // 페이드아웃 시퀀스
            _interactionSequence = DOTween.Sequence();
            _interactionSequence.Append(
                _interactionRoot.DOFade(0f, _fadeDuration * 0.3f).SetEase(Ease.InCubic)
            );
        }

        // =============================================================================
        // 돋보기 아이콘 빛나는 효과
        // =============================================================================

        /// <summary>
        /// 돋보기 아이콘 빛나는 효과 시작
        /// </summary>
        private void StartGlowEffect()
        {
            if (_magnifierIcon == null) return;

            // 기존 효과 중지
            _glowTween?.Kill();

            // 알파 값 루프 애니메이션 (0.6 ~ 1.0)
            _glowTween = _magnifierIcon.DOFade(_glowIntensity, _glowSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        /// <summary>
        /// 돋보기 아이콘 빛나는 효과 중지
        /// </summary>
        private void StopGlowEffect()
        {
            if (_magnifierIcon == null) return;

            // 효과 중지 및 원래 알파로 복원
            _glowTween?.Kill();
            _magnifierIcon.DOFade(1f, 0.2f);
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 인디케이터 텍스트 변경
        /// </summary>
        public void SetModeText(string text)
        {
            if (_modeText != null)
            {
                _modeText.text = text;
            }
        }

        /// <summary>
        /// 상호작용 텍스트 포맷 변경
        /// </summary>
        public void SetInteractionFormat(string format)
        {
            _interactionFormat = format;
        }

        // =============================================================================
        // 단서 정보 표시/숨김
        // =============================================================================

        /// <summary>
        /// 단서 정보 표시 (이름 + 설명)
        /// </summary>
        /// <param name="clueName">단서 이름</param>
        /// <param name="clueDescription">단서 설명</param>
        /// <param name="clueTransform">단서 Transform (위치 추적용)</param>
        public void ShowClueInfo(string clueName, string clueDescription, Transform clueTransform = null)
        {
            // 텍스트 설정
            if (_clueNameText != null)
            {
                _clueNameText.text = clueName;
            }

            if (_clueDescriptionText != null)
            {
                _clueDescriptionText.text = clueDescription;
            }

            // Transform 저장 (위치 추적용)
            _currentClueTransform = clueTransform;

            // 초기 위치 설정
            if (_followCluePosition && clueTransform != null)
            {
                UpdateClueInfoPosition();
            }

            // 시퀀스 정리
            _clueInfoSequence?.Kill();

            // 페이드인 시퀀스
            _clueInfoSequence = DOTween.Sequence();
            _clueInfoSequence.Append(
                _clueInfoRoot.DOFade(1f, _fadeDuration * 0.5f).SetEase(Ease.OutCubic)
            );

            Debug.Log($"[InvestigationUI] 단서 정보 표시: {clueName}");
        }

        /// <summary>
        /// 단서 정보 숨김
        /// </summary>
        public void HideClueInfo()
        {
            // Transform 해제
            _currentClueTransform = null;

            // 시퀀스 정리
            _clueInfoSequence?.Kill();

            // 페이드아웃 시퀀스
            _clueInfoSequence = DOTween.Sequence();
            _clueInfoSequence.Append(
                _clueInfoRoot.DOFade(0f, _fadeDuration * 0.3f).SetEase(Ease.InCubic)
            );

            Debug.Log("[InvestigationUI] 단서 정보 숨김");
        }

        /// <summary>
        /// 단서 정보 UI 위치 업데이트 (월드 좌표 → 스크린 좌표)
        /// </summary>
        private void UpdateClueInfoPosition()
        {
            if (_currentClueTransform == null || _clueInfoRectTransform == null || _mainCamera == null)
                return;

            // 월드 좌표를 스크린 좌표로 변환
            Vector3 worldPosition = _currentClueTransform.position;
            Vector3 screenPosition = _mainCamera.WorldToScreenPoint(worldPosition);

            // 오프셋 적용 (오브젝트 위)
            screenPosition.x += _clueInfoOffset.x;
            screenPosition.y += _clueInfoOffset.y;

            // RectTransform 위치 설정
            _clueInfoRectTransform.position = screenPosition;
        }
    }
}
