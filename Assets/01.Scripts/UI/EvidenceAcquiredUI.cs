// =============================================================================
// EvidenceAcquiredUI.cs
// =============================================================================
// 설명: 증거물 획득 UI (폴라로이드 스타일)
// 용도: 증거물을 획득했을 때 좌측 하단에서 팝업되는 UI
// 스타일:
//   - 폴라로이드 즉석 사진 느낌
//   - 빛바랜 아이보리 종이 질감
//   - 타자기/손글씨 폰트
//   - DOTween 애니메이션 (좌측 하단에서 슬라이드 인)
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using GameDatabase.Evidence;

namespace GameDatabase.UI
{
    /// <summary>
    /// 증거물 획득 UI - 폴라로이드 스타일
    /// </summary>
    public class EvidenceAcquiredUI : MonoBehaviour
    {
        // =============================================================================
        // UI 요소
        // =============================================================================

        [Header("=== UI 요소 ===")]

        [Tooltip("폴라로이드 루트 (CanvasGroup 필요)")]
        [SerializeField] private CanvasGroup _polaroidRoot;

        [Tooltip("폴라로이드 RectTransform (애니메이션용)")]
        [SerializeField] private RectTransform _polaroidRect;

        [Tooltip("\"증거 획득\" 헤더 텍스트 (TMP)")]
        [SerializeField] private TextMeshProUGUI _headerText;

        [Tooltip("증거물 이름 텍스트 (TMP)")]
        [SerializeField] private TextMeshProUGUI _evidenceNameText;

        [Tooltip("증거물 설명 텍스트 (TMP)")]
        [SerializeField] private TextMeshProUGUI _evidenceDescriptionText;

        [Tooltip("증거물 이미지 (Image)")]
        [SerializeField] private Image _evidenceImage;

        // =============================================================================
        // 애니메이션 설정
        // =============================================================================

        [Header("=== 애니메이션 설정 ===")]

        [Tooltip("등장 시간 (초)")]
        [Range(0.3f, 2f)]
        [SerializeField] private float _showDuration = 0.8f;

        [Tooltip("화면 표시 시간 (초)")]
        [Range(1f, 5f)]
        [SerializeField] private float _displayDuration = 3f;

        [Tooltip("퇴장 시간 (초)")]
        [Range(0.3f, 2f)]
        [SerializeField] private float _hideDuration = 0.6f;

        [Tooltip("등장 위치 오프셋 (좌측 하단 기준, X는 음수)")]
        [SerializeField] private Vector2 _startOffset = new Vector2(-400f, -200f);

        [Tooltip("최종 위치 (좌측 하단 기준)")]
        [SerializeField] private Vector2 _targetPosition = new Vector2(200f, 150f);

        [Tooltip("회전 효과 사용 (폴라로이드가 약간 기울어지며 등장)")]
        [SerializeField] private bool _useRotationEffect = true;

        [Tooltip("등장 시 회전 각도")]
        [Range(-30f, 30f)]
        [SerializeField] private float _startRotation = -15f;

        [Tooltip("펄스 효과 사용 (헤더 텍스트 빛나기)")]
        [SerializeField] private bool _useHeaderPulse = true;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private Sequence _animationSequence;
        private Tween _headerPulseTween;
        private Vector2 _originalAnchoredPosition;
        private Quaternion _originalRotation;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // 초기 상태 (숨김)
            if (_polaroidRoot != null)
            {
                _polaroidRoot.alpha = 0f;
            }

            if (_polaroidRect != null)
            {
                _originalAnchoredPosition = _polaroidRect.anchoredPosition;
                _originalRotation = _polaroidRect.localRotation;
            }

            // 초기 위치를 화면 밖으로 설정
            HideImmediate();
        }

        private void OnDestroy()
        {
            // DOTween 정리
            _animationSequence?.Kill();
            _headerPulseTween?.Kill();
        }

        // =============================================================================
        // 증거물 획득 UI 표시
        // =============================================================================

        /// <summary>
        /// 증거물 획득 UI 표시
        /// </summary>
        /// <param name="evidence">획득한 증거물 데이터</param>
        public void ShowEvidenceAcquired(EvidenceData evidence)
        {
            if (evidence == null)
            {
                Debug.LogWarning("[EvidenceAcquiredUI] 증거물 데이터가 null입니다.");
                return;
            }

            // UI 업데이트
            UpdateUI(evidence);

            // 애니메이션 시작
            PlayShowAnimation();

            Debug.Log($"[EvidenceAcquiredUI] 증거물 획득 UI 표시: {evidence.EvidenceName}");
        }

        /// <summary>
        /// 증거물 획득 UI 표시 (개별 데이터)
        /// </summary>
        public void ShowEvidenceAcquired(string evidenceName, string evidenceDescription, Sprite evidenceImage)
        {
            // UI 업데이트
            if (_evidenceNameText != null)
            {
                _evidenceNameText.text = evidenceName;
            }

            if (_evidenceDescriptionText != null)
            {
                _evidenceDescriptionText.text = evidenceDescription;
            }

            if (_evidenceImage != null && evidenceImage != null)
            {
                _evidenceImage.sprite = evidenceImage;
                _evidenceImage.enabled = true;
            }
            else if (_evidenceImage != null)
            {
                _evidenceImage.enabled = false;
            }

            // 애니메이션 시작
            PlayShowAnimation();

            Debug.Log($"[EvidenceAcquiredUI] 증거물 획득 UI 표시: {evidenceName}");
        }

        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateUI(EvidenceData evidence)
        {
            // 이름 설정
            if (_evidenceNameText != null)
            {
                _evidenceNameText.text = evidence.EvidenceName;
            }

            // 설명 설정
            if (_evidenceDescriptionText != null)
            {
                _evidenceDescriptionText.text = evidence.EvidenceDescription;
            }

            // 이미지 설정
            if (_evidenceImage != null)
            {
                if (evidence.EvidenceImage != null)
                {
                    _evidenceImage.sprite = evidence.EvidenceImage;
                    _evidenceImage.enabled = true;
                }
                else
                {
                    _evidenceImage.enabled = false;
                }
            }
        }

        // =============================================================================
        // 애니메이션
        // =============================================================================

        /// <summary>
        /// 등장 애니메이션 재생
        /// </summary>
        private void PlayShowAnimation()
        {
            // 기존 애니메이션 정리
            _animationSequence?.Kill();
            _headerPulseTween?.Kill();

            // 시작 위치 설정
            if (_polaroidRect != null)
            {
                _polaroidRect.anchoredPosition = _targetPosition + _startOffset;

                if (_useRotationEffect)
                {
                    _polaroidRect.localRotation = Quaternion.Euler(0f, 0f, _startRotation);
                }
            }

            // 애니메이션 시퀀스 생성
            _animationSequence = DOTween.Sequence();

            // 1. 페이드 인 + 슬라이드 인 + 회전
            _animationSequence.Append(
                _polaroidRoot.DOFade(1f, _showDuration).SetEase(Ease.OutCubic)
            );

            if (_polaroidRect != null)
            {
                _animationSequence.Join(
                    _polaroidRect.DOAnchorPos(_targetPosition, _showDuration).SetEase(Ease.OutBack)
                );

                if (_useRotationEffect)
                {
                    _animationSequence.Join(
                        _polaroidRect.DOLocalRotate(Vector3.zero, _showDuration).SetEase(Ease.OutBack)
                    );
                }
            }

            // 2. 헤더 텍스트 펄스 효과 (등장 완료 후)
            if (_useHeaderPulse && _headerText != null)
            {
                _animationSequence.AppendCallback(() =>
                {
                    _headerPulseTween = _headerText.transform.DOScale(1.05f, 0.6f)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine);
                });
            }

            // 3. 대기 시간
            _animationSequence.AppendInterval(_displayDuration);

            // 4. 퇴장 애니메이션
            _animationSequence.AppendCallback(() =>
            {
                PlayHideAnimation();
            });
        }

        /// <summary>
        /// 퇴장 애니메이션 재생
        /// </summary>
        private void PlayHideAnimation()
        {
            // 헤더 펄스 중지
            _headerPulseTween?.Kill();
            if (_headerText != null)
            {
                _headerText.transform.DOKill();
                _headerText.transform.localScale = Vector3.one;
            }

            // 퇴장 시퀀스
            Sequence hideSequence = DOTween.Sequence();

            // 페이드 아웃 + 슬라이드 아웃 (좌측 하단으로)
            hideSequence.Append(
                _polaroidRoot.DOFade(0f, _hideDuration).SetEase(Ease.InCubic)
            );

            if (_polaroidRect != null)
            {
                Vector2 hidePosition = _targetPosition + _startOffset;
                hideSequence.Join(
                    _polaroidRect.DOAnchorPos(hidePosition, _hideDuration).SetEase(Ease.InBack)
                );

                if (_useRotationEffect)
                {
                    hideSequence.Join(
                        _polaroidRect.DOLocalRotate(new Vector3(0f, 0f, _startRotation), _hideDuration).SetEase(Ease.InBack)
                    );
                }
            }

            Debug.Log("[EvidenceAcquiredUI] 증거물 획득 UI 숨김");
        }

        /// <summary>
        /// 즉시 숨김 (초기화용)
        /// </summary>
        public void HideImmediate()
        {
            // 애니메이션 정리
            _animationSequence?.Kill();
            _headerPulseTween?.Kill();

            // 투명도 0
            if (_polaroidRoot != null)
            {
                _polaroidRoot.alpha = 0f;
            }

            // 위치 초기화 (화면 밖)
            if (_polaroidRect != null)
            {
                _polaroidRect.anchoredPosition = _targetPosition + _startOffset;
                _polaroidRect.localRotation = _originalRotation;
            }

            // 헤더 스케일 복원
            if (_headerText != null)
            {
                _headerText.transform.DOKill();
                _headerText.transform.localScale = Vector3.one;
            }
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 수동으로 UI 숨기기
        /// </summary>
        public void Hide()
        {
            PlayHideAnimation();
        }

        /// <summary>
        /// 헤더 텍스트 변경
        /// </summary>
        public void SetHeaderText(string text)
        {
            if (_headerText != null)
            {
                _headerText.text = text;
            }
        }

        /// <summary>
        /// 표시 시간 변경
        /// </summary>
        public void SetDisplayDuration(float duration)
        {
            _displayDuration = duration;
        }
    }
}
