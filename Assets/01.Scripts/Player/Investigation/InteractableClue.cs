// =============================================================================
// InteractableClue.cs
// =============================================================================
// 설명: 조사 가능한 단서 오브젝트 (앨런 웨이크 2 스타일)
// 용도: 마우스 호버/클릭으로 조사할 수 있는 개별 단서
// 특징:
//   - 호버 시 Outline 효과 (DOTween)
//   - 조사 시 대화 실행 또는 커스텀 이벤트
//   - 1회 조사 후 재조사 방지 (선택 가능)
// =============================================================================

using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using GameDatabase.Dialogue;
using GameDatabase.UI;
using GameDatabase.Evidence;

namespace GameDatabase.Player
{
    /// <summary>
    /// 조사 가능한 단서 오브젝트
    /// 호버/조사 효과 및 대화 연동
    /// </summary>
    public class InteractableClue : MonoBehaviour
    {
        // =============================================================================
        // 기본 설정
        // =============================================================================

        [Header("=== 기본 설정 ===")]

        [Tooltip("단서 이름 (UI에 표시)")]
        [SerializeField] private string _clueName = "단서";

        [Tooltip("단서 설명 (디버그용)")]
        [TextArea(2, 4)]
        [SerializeField] private string _clueDescription;

        [Tooltip("1회만 조사 가능")]
        [SerializeField] private bool _onceOnly = true;

        [Tooltip("이미 조사했는지 여부")]
        [SerializeField] private bool _isInvestigated = false;

        // =============================================================================
        // 대화 설정
        // =============================================================================

        [Header("=== 대화 설정 ===")]

        [Tooltip("조사 시 실행할 대화 데이터")]
        [SerializeField] private DialogueData _dialogue;

        // =============================================================================
        // 증거물 설정
        // =============================================================================

        [Header("=== 증거물 설정 ===")]

        [Tooltip("조사 완료 시 획득할 증거물")]
        [SerializeField] private EvidenceData _linkedEvidence;

        [Tooltip("증거물을 획득했는지 여부")]
        [SerializeField] private bool _evidenceAcquired = false;

        // =============================================================================
        // Outline 효과
        // =============================================================================

        [Header("=== 호버 효과 ===")]

        [Tooltip("호버 시 스케일 효과 사용")]
        [SerializeField] private bool _useScaleEffect = true;

        [Tooltip("호버 시 스케일 배율")]
        [Range(1.0f, 1.5f)]
        [SerializeField] private float _hoverScale = 1.1f;

        [Tooltip("스케일 애니메이션 시간 (초)")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _scaleDuration = 0.3f;

        [Header("=== Outline 효과 (선택) ===")]

        [Tooltip("호버 시 Outline 효과 사용")]
        [SerializeField] private bool _useOutlineEffect = false;

        [Tooltip("Outline 컴포넌트 (선택, 없으면 자동 찾기)")]
        [SerializeField] private Component _outline;

        [Tooltip("Outline 색상")]
        [SerializeField] private Color _outlineColor = Color.yellow;

        [Tooltip("Outline 두께")]
        [Range(0f, 10f)]
        [SerializeField] private float _outlineThickness = 5f;

        [Tooltip("Outline 페이드 시간 (초)")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _outlineFadeDuration = 0.3f;

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("호버 진입 시 호출")]
        public UnityEvent OnHoverEnterEvent;

        [Tooltip("호버 종료 시 호출")]
        public UnityEvent OnHoverExitEvent;

        [Tooltip("조사 시 호출 (대화 실행 전)")]
        public UnityEvent OnInvestigateEvent;

        [Tooltip("조사 완료 시 호출 (대화 종료 후)")]
        public UnityEvent OnInvestigateCompletedEvent;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private bool _isHovered = false;
        private Vector3 _originalScale;
        private Tween _scaleTween;
        private Tween _outlineTween;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 단서 이름
        /// </summary>
        public string ClueName => _clueName;

        /// <summary>
        /// 단서 설명
        /// </summary>
        public string ClueDescription => _clueDescription;

        /// <summary>
        /// 상호작용 가능 여부
        /// </summary>
        public bool CanInteract
        {
            get
            {
                // 1회만 조사 가능하면, 이미 조사했으면 false
                if (_onceOnly && _isInvestigated) return false;
                return true;
            }
        }

        /// <summary>
        /// 이미 조사했는지 여부
        /// </summary>
        public bool IsInvestigated => _isInvestigated;

        /// <summary>
        /// 대화 데이터
        /// </summary>
        public DialogueData Dialogue => _dialogue;

        /// <summary>
        /// 연동된 증거물
        /// </summary>
        public EvidenceData LinkedEvidence => _linkedEvidence;

        /// <summary>
        /// 증거물 획득 여부
        /// </summary>
        public bool EvidenceAcquired => _evidenceAcquired;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // 원본 스케일 저장
            _originalScale = transform.localScale;

            // Outline 컴포넌트 찾기
            if (_useOutlineEffect && _outline == null)
            {
                // RealOutlineEffect 먼저 찾기 (모든 파이프라인 호환)
                var realEffect = GetComponent<RealOutlineEffect>();
                if (realEffect != null)
                {
                    _outline = realEffect;
                    Debug.Log($"[InteractableClue] RealOutlineEffect 찾음: {_clueName}");
                }
                // HDRPOutlineEffect 찾기 (HDRP용)
                else
                {
                    var hdprEffect = GetComponent<HDRPOutlineEffect>();
                    if (hdprEffect != null)
                    {
                        _outline = hdprEffect;
                        Debug.Log($"[InteractableClue] HDRPOutlineEffect 찾음: {_clueName}");
                    }
                    // SimpleHoverEffect 찾기 (URP용)
                    else
                    {
                        var simpleEffect = GetComponent<SimpleHoverEffect>();
                        if (simpleEffect != null)
                        {
                            _outline = simpleEffect;
                            Debug.Log($"[InteractableClue] SimpleHoverEffect 찾음: {_clueName}");
                        }
                        // QuickOutline 찾기 - 글로벌 네임스페이스의 Outline 클래스
                        else
                        {
                            // GetComponent<Outline>()는 UnityEngine.UI.Outline을 찾으므로 리플렉션 사용
                            var allComponents = GetComponents<Component>();
                            foreach (var comp in allComponents)
                            {
                                if (comp != null && comp.GetType().Name == "Outline" && comp.GetType().Namespace == null)
                                {
                                    _outline = comp;
                                    Debug.Log($"[InteractableClue] QuickOutline 찾음: {_clueName}");
                                    break;
                                }
                            }
                        }
                    }
                }

                // 없으면 RealOutlineEffect 자동 추가 (모든 파이프라인 호환)
                if (_outline == null)
                {
                    _outline = gameObject.AddComponent<RealOutlineEffect>();
                    Debug.Log($"[InteractableClue] RealOutlineEffect 자동 추가 (모든 파이프라인 호환): {_clueName}");
                }
            }

            // Outline 초기 설정 (QuickOutline만 해당)
            if (_outline != null)
            {
                // QuickOutline인 경우에만 프로퍼티 설정
                if (_outline.GetType().Name == "Outline")
                {
                    SetOutlineProperty("OutlineColor", _outlineColor);
                    SetOutlineProperty("OutlineWidth", 0f); // 초기 비활성화

                    if (_outline is Behaviour behaviour)
                    {
                        behaviour.enabled = true;
                    }

                    Debug.Log($"[InteractableClue] QuickOutline 초기화 완료: {_clueName}");
                }
                // HDRPOutlineEffect는 자체적으로 초기화됨
                else if (_outline is HDRPOutlineEffect)
                {
                    Debug.Log($"[InteractableClue] HDRPOutlineEffect 준비 완료: {_clueName}");
                }
                // SimpleHoverEffect는 자체적으로 초기화됨
                else if (_outline is SimpleHoverEffect)
                {
                    Debug.Log($"[InteractableClue] SimpleHoverEffect 준비 완료: {_clueName}");
                }
            }
        }

        /// <summary>
        /// Outline 프로퍼티 설정 (리플렉션)
        /// </summary>
        private void SetOutlineProperty(string propertyName, object value)
        {
            if (_outline == null) return;

            var property = _outline.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(_outline, value);
            }
        }

        /// <summary>
        /// Outline 프로퍼티 가져오기 (리플렉션)
        /// </summary>
        private object GetOutlineProperty(string propertyName)
        {
            if (_outline == null) return null;

            var property = _outline.GetType().GetProperty(propertyName);
            if (property != null && property.CanRead)
            {
                return property.GetValue(_outline);
            }
            return null;
        }

        private void OnDestroy()
        {
            // DOTween 정리
            _scaleTween?.Kill();
            _outlineTween?.Kill();
        }

        // =============================================================================
        // 호버 처리
        // =============================================================================

        /// <summary>
        /// 호버 진입
        /// </summary>
        public void OnHoverEnter()
        {
            if (_isHovered) return;
            _isHovered = true;

            // 스케일 효과
            if (_useScaleEffect)
            {
                _scaleTween?.Kill();
                _scaleTween = transform.DOScale(_originalScale * _hoverScale, _scaleDuration)
                    .SetEase(Ease.OutCubic);
            }

            // Outline 효과 페이드인 (QuickOutline)
            if (_useOutlineEffect && _outline != null)
            {
                // Outline 타입 확인
                if (_outline.GetType().Name == "Outline")
                {
                    _outlineTween?.Kill();
                    _outlineTween = DOTween.To(
                        () => (float)GetOutlineProperty("OutlineWidth"),
                        width => SetOutlineProperty("OutlineWidth", width),
                        _outlineThickness,
                        _outlineFadeDuration
                    ).SetEase(Ease.OutCubic);
                }
                // HDRPOutlineEffect
                else if (_outline is HDRPOutlineEffect hdrpEffect)
                {
                    hdrpEffect.OnHoverEnter();
                }
                // SimpleHoverEffect
                else if (_outline is SimpleHoverEffect simpleEffect)
                {
                    simpleEffect.OnHoverEnter();
                }
            }

            // 이벤트 발생
            OnHoverEnterEvent?.Invoke();

            Debug.Log($"[InteractableClue] 호버 진입: {_clueName}");
        }

        /// <summary>
        /// 호버 종료
        /// </summary>
        public void OnHoverExit()
        {
            if (!_isHovered) return;
            _isHovered = false;

            // 스케일 복원
            if (_useScaleEffect)
            {
                _scaleTween?.Kill();
                _scaleTween = transform.DOScale(_originalScale, _scaleDuration)
                    .SetEase(Ease.InCubic);
            }

            // Outline 효과 페이드아웃 (QuickOutline)
            if (_useOutlineEffect && _outline != null)
            {
                // Outline 타입 확인
                if (_outline.GetType().Name == "Outline")
                {
                    _outlineTween?.Kill();
                    _outlineTween = DOTween.To(
                        () => (float)GetOutlineProperty("OutlineWidth"),
                        width => SetOutlineProperty("OutlineWidth", width),
                        0f,
                        _outlineFadeDuration
                    ).SetEase(Ease.InCubic);
                }
                // HDRPOutlineEffect
                else if (_outline is HDRPOutlineEffect hdrpEffect)
                {
                    hdrpEffect.OnHoverExit();
                }
                // SimpleHoverEffect
                else if (_outline is SimpleHoverEffect simpleEffect)
                {
                    simpleEffect.OnHoverExit();
                }
            }

            // 이벤트 발생
            OnHoverExitEvent?.Invoke();

            Debug.Log($"[InteractableClue] 호버 종료: {_clueName}");
        }

        // =============================================================================
        // 조사 처리
        // =============================================================================

        /// <summary>
        /// 조사 실행
        /// </summary>
        public void Investigate()
        {
            if (!CanInteract)
            {
                Debug.LogWarning($"[InteractableClue] 이미 조사한 단서입니다: {_clueName}");
                return;
            }

            // 조사 완료 플래그
            _isInvestigated = true;

            // 이벤트 발생 (대화 실행 전)
            OnInvestigateEvent?.Invoke();

            // 대화 실행
            if (_dialogue != null)
            {
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartDialogue(_dialogue);

                    // 대화 종료 리스너 등록 (1회용)
                    DialogueManager.Instance.OnDialogueEnd.AddListener(OnDialogueEnded);
                }
                else
                {
                    Debug.LogError("[InteractableClue] DialogueManager가 없습니다.");
                }
            }
            else
            {
                // 대화가 없으면 즉시 완료
                OnInvestigateCompletedEvent?.Invoke();
            }

            Debug.Log($"[InteractableClue] 조사 완료: {_clueName}");
        }

        /// <summary>
        /// 대화 종료 콜백
        /// </summary>
        private void OnDialogueEnded()
        {
            // 리스너 제거
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueEnd.RemoveListener(OnDialogueEnded);
            }

            // 증거물 획득 처리
            AcquireEvidence();

            // 조사 완료 이벤트 발생
            OnInvestigateCompletedEvent?.Invoke();

            Debug.Log($"[InteractableClue] 대화 종료: {_clueName}");
        }

        /// <summary>
        /// 증거물 획득
        /// </summary>
        private void AcquireEvidence()
        {
            // 이미 획득했으면 무시
            if (_evidenceAcquired) return;

            // 연동된 증거물이 있으면
            if (_linkedEvidence != null)
            {
                // 증거물 획득 UI 표시
                if (EvidenceAcquiredManager.Instance != null)
                {
                    EvidenceAcquiredManager.Instance.ShowEvidence(_linkedEvidence);
                }
                else
                {
                    Debug.LogWarning("[InteractableClue] EvidenceAcquiredManager가 없습니다!");
                }

                // 증거물 노트북에 추가
                if (EvidenceNotebookManager.Instance != null)
                {
                    EvidenceNotebookManager.Instance.AddEvidence(_linkedEvidence);
                }
                else
                {
                    Debug.LogWarning("[InteractableClue] EvidenceNotebookManager가 없습니다!");
                }

                _evidenceAcquired = true;
                Debug.Log($"[InteractableClue] 증거물 획득: {_linkedEvidence.EvidenceName}");
            }
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 조사 상태 초기화
        /// </summary>
        public void ResetClue()
        {
            _isInvestigated = false;
            _evidenceAcquired = false;
            Debug.Log($"[InteractableClue] 초기화: {_clueName}");
        }

        /// <summary>
        /// 단서 이름 변경
        /// </summary>
        public void SetClueName(string name)
        {
            _clueName = name;
        }

        /// <summary>
        /// 대화 데이터 변경
        /// </summary>
        public void SetDialogue(DialogueData dialogue)
        {
            _dialogue = dialogue;
        }

        /// <summary>
        /// 증거물 연동
        /// </summary>
        public void SetEvidence(EvidenceData evidence)
        {
            _linkedEvidence = evidence;
        }

        // =============================================================================
        // Gizmo
        // =============================================================================

        private void OnDrawGizmos()
        {
            Gizmos.color = _isInvestigated ? Color.gray : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
    }
}
