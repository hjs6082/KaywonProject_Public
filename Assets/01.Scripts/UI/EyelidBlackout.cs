// =============================================================================
// EyelidBlackout.cs
// =============================================================================
// 설명: 눈꺼풀 블랙아웃 연출 컨트롤러 (싱글톤)
// 용도:
//   - WakeUp()         : 기절에서 깨어나는 연출 (눈꺼풀이 무겁게 깜빡이다 열림)
//   - FadeToBlack()    : 화면을 서서히 검게 (씬 전환 전 블랙아웃)
//   - FadeFromBlack()  : 블랙아웃에서 서서히 복귀
//   - PrepareWakeUp()  : FadeToBlack 이후 WakeUp 직전 호출 (FullBlack → 눈꺼풀 교체)
//
// Unity 설정:
//   Hierarchy:
//     Canvas (EyelidBlackout, Screen Space Overlay, Sort Order: 200)
//       ├── FullBlack    (Image, Anchor Stretch, 검은색)
//       ├── TopEyelid    (Image, Anchor Top+Stretch, Pivot Y=1, 검은색)
//       └── BottomEyelid (Image, Anchor Bottom+Stretch, Pivot Y=0, 검은색)
//
// 주의사항:
//   - DOTween 패키지 필요
//   - _eyelidHalfHeight: 화면 세로 절반 (1080p → 540)
// =============================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace GameDatabase.UI
{
    public class EyelidBlackout : MonoBehaviour
    {
        // =============================================================================
        // 싱글톤
        // =============================================================================

        private static EyelidBlackout _instance;
        public static EyelidBlackout Instance => _instance;

        // =============================================================================
        // 참조
        // =============================================================================

        [Header("=== 참조 ===")]

        [Tooltip("위 눈꺼풀 RectTransform (Anchor: Top Stretch, Pivot Y=1)")]
        [SerializeField] private RectTransform _topEyelid;

        [Tooltip("아래 눈꺼풀 RectTransform (Anchor: Bottom Stretch, Pivot Y=0)")]
        [SerializeField] private RectTransform _bottomEyelid;

        [Tooltip("전체 블랙아웃용 Image (Anchor: Stretch)")]
        [SerializeField] private Image _fullBlack;

        // =============================================================================
        // WakeUp 설정
        // =============================================================================

        [Header("=== WakeUp 설정 ===")]

        [Tooltip("눈꺼풀 완전히 열릴 때 이동량 (px). 화면 절반보다 크게 해야 화면 밖으로 나감.")]
        [SerializeField] private float _eyelidHalfHeight = 600f;

        [Tooltip("기절 상태 초기 대기 시간 (초) - 눈 뜨기 전 암전 유지")]
        [SerializeField] private float _initialHoldDuration = 0.8f;

        // 깜빡임 데이터: 각 깜빡임의 열리는 양(px)과 속도를 직접 지정
        [System.Serializable]
        public struct BlinkStep
        {
            [Tooltip("이번 깜빡임에서 눈꺼풀이 열리는 양 (px). 240 = 화면의 44%")]
            public float openAmount;
            [Tooltip("열리는 시간 (초)")]
            public float openDuration;
            [Tooltip("열린 채 유지 시간 (초)")]
            public float holdDuration;
            [Tooltip("닫히는 시간 (초)")]
            public float closeDuration;
            [Tooltip("닫힌 후 다음 깜빡임까지 대기 (초)")]
            public float pauseDuration;
        }

        [Tooltip("깜빡임 단계. 위에서부터 순서대로 실행됨.")]
        [SerializeField] private BlinkStep[] _blinkSteps = new BlinkStep[]
        {
            // 1차: 아주 조금 열렸다 바로 닫힘 (눈이 정말 무거운 상태)
            new BlinkStep { openAmount = 80f,  openDuration = 0.5f, holdDuration = 0.05f, closeDuration = 0.2f, pauseDuration = 0.4f },
            // 2차: 좀 더 열림, 흐릿하게 보이다 다시 닫힘
            new BlinkStep { openAmount = 220f, openDuration = 0.5f, holdDuration = 0.15f, closeDuration = 0.3f, pauseDuration = 0.3f },
            // 3차: 절반 정도 열렸다 닫힘
            new BlinkStep { openAmount = 360f, openDuration = 0.45f, holdDuration = 0.2f, closeDuration = 0.25f, pauseDuration = 0.2f },
        };

        [Tooltip("마지막으로 완전히 눈 뜨는 시간 (초)")]
        [SerializeField] private float _finalOpenDuration = 0.7f;

        [Tooltip("완전히 열린 후 눈꺼풀 숨기기 전 대기 (초)")]
        [SerializeField] private float _finalHoldDuration = 0.15f;

        // =============================================================================
        // 페이드 설정
        // =============================================================================

        [Header("=== 페이드 설정 ===")]

        [Tooltip("FadeToBlack 기본 지속 시간 (초)")]
        [SerializeField] private float _fadeToBlackDuration = 0.6f;

        [Tooltip("FadeFromBlack 기본 지속 시간 (초)")]
        [SerializeField] private float _fadeFromBlackDuration = 0.8f;

        // =============================================================================
        // 내부 상태
        // =============================================================================

        private bool _isBusy = false;
        public bool IsBusy => _isBusy;

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

            // 시작 시 눈꺼풀 닫힌 상태 + FullBlack 투명
            SetEyelidsClosed();
            SetEyelidsVisible(false);
            SetFullBlackAlpha(0f);
            if (_fullBlack != null)
                _fullBlack.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        // =============================================================================
        // 공개 API
        // =============================================================================

        /// <summary>
        /// 기절에서 깨어나는 연출.
        /// 눈꺼풀이 점점 더 크게 깜빡이다가 완전히 열립니다.
        /// </summary>
        public void WakeUp(System.Action onComplete = null)
        {
            if (_isBusy)
            {
                Debug.LogWarning("[EyelidBlackout] 이미 연출 중입니다.");
                return;
            }
            StartCoroutine(WakeUpRoutine(onComplete));
        }

        /// <summary>
        /// 화면을 서서히 검게 만듭니다.
        /// </summary>
        public void FadeToBlack(float duration = 0f, System.Action onComplete = null)
        {
            if (duration <= 0f) duration = _fadeToBlackDuration;

            SetEyelidsVisible(false);
            SetFullBlackAlpha(0f);
            _fullBlack.gameObject.SetActive(true);

            _fullBlack.DOFade(1f, duration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// 블랙아웃에서 서서히 복귀합니다.
        /// </summary>
        public void FadeFromBlack(float duration = 0f, System.Action onComplete = null)
        {
            if (duration <= 0f) duration = _fadeFromBlackDuration;

            SetEyelidsVisible(false);
            SetFullBlackAlpha(1f);
            _fullBlack.gameObject.SetActive(true);

            _fullBlack.DOFade(0f, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    _fullBlack.gameObject.SetActive(false);
                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// FadeToBlack 이후 WakeUp 직전 호출.
        /// FullBlack을 끄고 눈꺼풀 닫힌 상태로 교체합니다.
        /// </summary>
        public void PrepareWakeUp()
        {
            if (_fullBlack != null)
                _fullBlack.gameObject.SetActive(false);

            SetEyelidsClosed();
            SetEyelidsVisible(true);
        }

        /// <summary>즉시 완전 블랙 (눈꺼풀 닫힘)</summary>
        public void SetInstantBlack()
        {
            DOTween.Kill(_topEyelid);
            DOTween.Kill(_bottomEyelid);
            SetEyelidsClosed();
            SetEyelidsVisible(true);
            SetFullBlackAlpha(0f);
            if (_fullBlack != null)
                _fullBlack.gameObject.SetActive(false);
        }

        /// <summary>즉시 화면 완전히 표시</summary>
        public void SetInstantClear()
        {
            DOTween.Kill(_topEyelid);
            DOTween.Kill(_bottomEyelid);
            SetEyelidsVisible(false);
            SetFullBlackAlpha(0f);
            if (_fullBlack != null)
                _fullBlack.gameObject.SetActive(false);
        }

        // =============================================================================
        // WakeUp 코루틴
        // =============================================================================

        private IEnumerator WakeUpRoutine(System.Action onComplete)
        {
            _isBusy = true;

            // FullBlack 끄고 눈꺼풀로 시작
            if (_fullBlack != null)
                _fullBlack.gameObject.SetActive(false);
            SetEyelidsClosed();
            SetEyelidsVisible(true);

            // 초기 암전 유지 (기절 상태)
            yield return new WaitForSeconds(_initialHoldDuration);

            // 단계별 깜빡임
            foreach (var step in _blinkSteps)
            {
                // 열기 (OutSine: 빠르게 열리다 끝에서 느려짐 → 힘겹게 버티는 느낌)
                yield return AnimateEyelids(step.openAmount, step.openDuration, Ease.OutSine);

                // 열린 채 유지
                if (step.holdDuration > 0f)
                    yield return new WaitForSeconds(step.holdDuration);

                // 닫기 (InQuad: 천천히 시작해서 빠르게 닫힘 → 무게에 끌려 닫히는 느낌)
                yield return AnimateEyelids(0f, step.closeDuration, Ease.InQuad);

                // 닫힌 채 대기
                if (step.pauseDuration > 0f)
                    yield return new WaitForSeconds(step.pauseDuration);
            }

            // 마지막: 완전히 열기 (OutExpo: 초반에 빠르게 확 열리고 부드럽게 마무리)
            yield return AnimateEyelids(_eyelidHalfHeight, _finalOpenDuration, Ease.OutExpo);

            yield return new WaitForSeconds(_finalHoldDuration);
            SetEyelidsVisible(false);

            _isBusy = false;
            onComplete?.Invoke();
        }

        // =============================================================================
        // 내부 헬퍼
        // =============================================================================

        /// <summary>
        /// 눈꺼풀을 목표 위치로 애니메이션.
        /// targetY = 0: 완전 닫힘 / targetY = _eyelidHalfHeight: 완전 열림
        /// </summary>
        private IEnumerator AnimateEyelids(float targetY, float duration, Ease ease)
        {
            bool done = false;

            DOTween.Kill(_topEyelid);
            DOTween.Kill(_bottomEyelid);

            _topEyelid.DOAnchorPosY(targetY, duration).SetEase(ease);
            _bottomEyelid.DOAnchorPosY(-targetY, duration).SetEase(ease)
                .OnComplete(() => done = true);

            yield return new WaitUntil(() => done);
        }

        private void SetEyelidsClosed()
        {
            if (_topEyelid != null)
                _topEyelid.anchoredPosition = new Vector2(_topEyelid.anchoredPosition.x, 0f);
            if (_bottomEyelid != null)
                _bottomEyelid.anchoredPosition = new Vector2(_bottomEyelid.anchoredPosition.x, 0f);
        }

        private void SetEyelidsVisible(bool visible)
        {
            if (_topEyelid != null)
                _topEyelid.gameObject.SetActive(visible);
            if (_bottomEyelid != null)
                _bottomEyelid.gameObject.SetActive(visible);
        }

        private void SetFullBlackAlpha(float alpha)
        {
            if (_fullBlack == null) return;
            Color c = _fullBlack.color;
            c.a = alpha;
            _fullBlack.color = c;
        }
    }
}
