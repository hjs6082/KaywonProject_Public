// =============================================================================
// InspectableObject.cs
// =============================================================================
// 설명: IInspectable 인터페이스의 기본 구현
// 용도: 조사 모드에서 클릭하여 정보를 얻을 수 있는 오브젝트
// 사용법:
//   1. 조사할 오브젝트에 이 컴포넌트 추가
//   2. Inspector에서 제목과 설명 설정
//   3. OnInspected 이벤트에 원하는 동작 연결
// =============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace GameDatabase.Player
{
    /// <summary>
    /// 기본 조사 가능 오브젝트 컴포넌트
    /// IInspectable의 기본 구현을 제공합니다.
    /// </summary>
    public class InspectableObject : MonoBehaviour, IInspectable
    {
        // =============================================================================
        // 조사 정보
        // =============================================================================

        [Header("=== 조사 정보 ===")]

        [Tooltip("조사 제목 (오브젝트 이름)")]
        [SerializeField] private string _inspectTitle = "오브젝트";

        [Tooltip("조사 설명 (자세한 내용)")]
        [TextArea(3, 5)]
        [SerializeField] private string _inspectDescription = "이것은 조사할 수 있는 오브젝트입니다.";

        [Tooltip("조사 가능 여부")]
        [SerializeField] private bool _canInspect = true;

        [Tooltip("한 번만 조사 가능")]
        [SerializeField] private bool _oneTimeOnly = false;

        // =============================================================================
        // 시각적 피드백
        // =============================================================================

        [Header("=== 시각적 피드백 ===")]

        [Tooltip("호버 시 하이라이트할 렌더러")]
        [SerializeField] private Renderer _highlightRenderer;

        [Tooltip("호버 하이라이트 색상")]
        [SerializeField] private Color _hoverColor = new Color(1f, 1f, 0.7f, 1f);

        [Tooltip("호버 시 스케일 변화량")]
        [SerializeField] private float _hoverScaleMultiplier = 1.05f;

        [Tooltip("호버 시 활성화할 아웃라인 효과 오브젝트")]
        [SerializeField] private GameObject _outlineEffect;

        // =============================================================================
        // 대화 연결 (선택사항)
        // =============================================================================

        [Header("=== 대화 연결 (선택사항) ===")]

        [Tooltip("조사 시 시작할 대화 (DialogueData)")]
        [SerializeField] private GameDatabase.Dialogue.DialogueData _inspectDialogue;

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("조사 시 호출")]
        public UnityEvent OnInspected;

        [Tooltip("조사 시 호출 (PlayerController 전달)")]
        public UnityEvent<PlayerController> OnInspectedWithPlayer;

        [Tooltip("호버 진입 시 호출")]
        public UnityEvent OnHoverEntered;

        [Tooltip("호버 종료 시 호출")]
        public UnityEvent OnHoverExited;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private Color _originalColor;
        private Vector3 _originalScale;
        private bool _hasInspected = false;
        private bool _isHovered = false;

        // =============================================================================
        // IInspectable 구현
        // =============================================================================

        /// <summary>
        /// 조사 제목
        /// </summary>
        public string InspectTitle => _inspectTitle;

        /// <summary>
        /// 조사 설명
        /// </summary>
        public string InspectDescription => _inspectDescription;

        /// <summary>
        /// 조사 가능 여부
        /// </summary>
        public bool CanInspect
        {
            get
            {
                if (_oneTimeOnly && _hasInspected)
                {
                    return false;
                }
                return _canInspect;
            }
        }

        /// <summary>
        /// 조사 실행
        /// </summary>
        /// <param name="player">조사하는 플레이어</param>
        public void OnInspect(PlayerController player)
        {
            if (!CanInspect) return;

            _hasInspected = true;

            // 대화 시작 (설정된 경우)
            if (_inspectDialogue != null)
            {
                if (UI.DialogueManager.Instance != null)
                {
                    UI.DialogueManager.Instance.StartDialogue(_inspectDialogue);
                }
            }

            // 이벤트 발생
            OnInspected?.Invoke();
            OnInspectedWithPlayer?.Invoke(player);

            Debug.Log($"[InspectableObject] '{_inspectTitle}' 조사 완료");
        }

        /// <summary>
        /// 호버 시작
        /// </summary>
        public void OnHoverEnter()
        {
            if (_isHovered) return;
            _isHovered = true;

            // 하이라이트 적용
            ApplyHoverEffect(true);

            // 이벤트 발생
            OnHoverEntered?.Invoke();
        }

        /// <summary>
        /// 호버 종료
        /// </summary>
        public void OnHoverExit()
        {
            if (!_isHovered) return;
            _isHovered = false;

            // 하이라이트 해제
            ApplyHoverEffect(false);

            // 이벤트 발생
            OnHoverExited?.Invoke();
        }

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Start()
        {
            // 원래 상태 저장
            _originalScale = transform.localScale;

            if (_highlightRenderer != null)
            {
                _originalColor = _highlightRenderer.material.color;
            }

            // 아웃라인 효과 초기 비활성화
            if (_outlineEffect != null)
            {
                _outlineEffect.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (_isHovered)
            {
                OnHoverExit();
            }
        }

        // =============================================================================
        // 호버 효과
        // =============================================================================

        /// <summary>
        /// 호버 효과 적용/해제
        /// </summary>
        /// <param name="apply">적용 여부</param>
        private void ApplyHoverEffect(bool apply)
        {
            // 색상 변경
            if (_highlightRenderer != null)
            {
                _highlightRenderer.material.color = apply ? _hoverColor : _originalColor;
            }

            // 스케일 변경
            if (_hoverScaleMultiplier != 1f)
            {
                transform.localScale = apply ?
                    _originalScale * _hoverScaleMultiplier : _originalScale;
            }

            // 아웃라인 효과
            if (_outlineEffect != null)
            {
                _outlineEffect.SetActive(apply);
            }
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 조사 가능 여부 설정
        /// </summary>
        public void SetCanInspect(bool canInspect)
        {
            _canInspect = canInspect;
        }

        /// <summary>
        /// 조사 정보 설정
        /// </summary>
        public void SetInspectInfo(string title, string description)
        {
            _inspectTitle = title;
            _inspectDescription = description;
        }

        /// <summary>
        /// 조사 상태 초기화
        /// </summary>
        public void ResetInspection()
        {
            _hasInspected = false;
        }
    }
}
