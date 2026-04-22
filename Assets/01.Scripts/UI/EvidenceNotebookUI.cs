// =============================================================================
// EvidenceNotebookUI.cs
// =============================================================================
// 설명: 증거물 노트북 UI (수사 노트 스타일)
// 용도: 획득한 증거물을 확인할 수 있는 노트북 UI
// 특징:
//   - Tab 키로 열고 닫기
//   - 좌측: 스크롤 가능한 증거물 목록
//   - 우측: 선택한 증거물 상세 정보
//   - DOTween 애니메이션 (페이지 넘기기 효과)
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using GameDatabase.Evidence;
using GameDatabase.Player;

namespace GameDatabase.UI
{
    /// <summary>
    /// 증거물 노트북 UI - 수사 노트 스타일
    /// </summary>
    public class EvidenceNotebookUI : MonoBehaviour
    {
        // =============================================================================
        // UI 요소 - 노트북
        // =============================================================================

        [Header("=== 노트북 UI ===")]

        [Tooltip("노트북 루트 (CanvasGroup 필요)")]
        [SerializeField] private CanvasGroup _notebookRoot;

        [Tooltip("노트북 RectTransform (애니메이션용)")]
        [SerializeField] private RectTransform _notebookRect;

        // =============================================================================
        // UI 요소 - 좌측 목록
        // =============================================================================

        [Header("=== 좌측 목록 ===")]

        [Tooltip("증거물 목록 스크롤뷰의 Content")]
        [SerializeField] private Transform _evidenceListContent;

        [Tooltip("증거물 슬롯 프리팹")]
        [SerializeField] private GameObject _evidenceSlotPrefab;

        // =============================================================================
        // UI 요소 - 우측 상세 정보
        // =============================================================================

        [Header("=== 우측 상세 정보 ===")]

        [Tooltip("상세 정보 루트 (초기 비활성화)")]
        [SerializeField] private GameObject _detailRoot;

        [Tooltip("증거물 이미지")]
        [SerializeField] private Image _detailImage;

        [Tooltip("증거물 이름 텍스트 (TMP)")]
        [SerializeField] private TextMeshProUGUI _detailNameText;

        [Tooltip("증거물 설명 텍스트 (TMP)")]
        [SerializeField] private TextMeshProUGUI _detailDescriptionText;

        // =============================================================================
        // 애니메이션 설정
        // =============================================================================

        [Header("=== 애니메이션 설정 ===")]

        [Tooltip("열기/닫기 애니메이션 시간 (초)")]
        [Range(0.3f, 2f)]
        [SerializeField] private float _animationDuration = 0.6f;

        [Tooltip("열릴 때 스케일 효과 사용")]
        [SerializeField] private bool _useScaleEffect = true;

        [Tooltip("시작 스케일 (닫힌 상태)")]
        [Range(0.5f, 1f)]
        [SerializeField] private float _startScale = 0.8f;

        // =============================================================================
        // 게임 일시정지 설정
        // =============================================================================

        [Header("=== 게임 제어 ===")]

        [Tooltip("노트북 열 때 게임 일시정지 (Time.timeScale = 0)")]
        [SerializeField] private bool _pauseGameOnOpen = true;

        [Tooltip("노트북 열 때 마우스 커서 표시")]
        [SerializeField] private bool _showCursorOnOpen = true;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private bool _isOpen = false;
        private bool _closedThisFrame = false; // 닫힌 직후 프레임 플래그
        private bool _openedThisFrame = false; // 열린 직후 프레임 플래그
        private List<EvidenceNotebookSlot> _slots = new List<EvidenceNotebookSlot>();
        private EvidenceNotebookSlot _currentSelectedSlot;
        private Sequence _animationSequence;

        // 이전 상태 저장 (복원용)
        private float _previousTimeScale;
        private CursorLockMode _previousCursorLockMode;
        private bool _previousCursorVisible;
        private PlayerState _previousPlayerState;

        // =============================================================================
        // 획득한 증거물 관리
        // =============================================================================

        private List<EvidenceData> _acquiredEvidences = new List<EvidenceData>();

        // =============================================================================
        // 프로퍼티 (외부 접근용)
        // =============================================================================

        /// <summary>
        /// 획득한 증거물 목록 (읽기 전용)
        /// </summary>
        public IReadOnlyList<EvidenceData> AcquiredEvidences => _acquiredEvidences;

        /// <summary>
        /// 특정 증거물을 획득했는지 확인
        /// </summary>
        public bool HasEvidence(string evidenceID)
        {
            return _acquiredEvidences.Exists(e => e.EvidenceID == evidenceID);
        }

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // 초기 상태 (숨김)
            HideImmediate();
        }

        private void LateUpdate()
        {
            // 프레임 플래그를 LateUpdate에서 해제 (같은 프레임 내 다른 Update에서 참조 가능)
            _closedThisFrame = false;
            _openedThisFrame = false;
        }

        private void Update()
        {
            // 노트북이 열려있을 때 커서 강제 표시
            if (_isOpen && _showCursorOnOpen)
            {
                // 매 프레임마다 커서 상태 강제 설정
                if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                    Debug.LogWarning("[EvidenceNotebookUI] 커서 강제 표시 (다른 컴포넌트가 커서를 숨겼습니다)");
                }
            }

            // Tab 키로 열고 닫기
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                // 대화 중이거나 조사 구역 모드, 사건 보드에서는 노트북 열지 않음
                bool isInDialogue = PlayerController.Instance != null && PlayerController.Instance.IsInDialogue;
                bool isInvestigating = InvestigationManager.Instance != null && InvestigationManager.Instance.IsInvestigating;
                bool isOnCaseBoard = PlayerController.Instance != null && PlayerController.Instance.IsOnCaseBoard;

                if (_isOpen)
                {
                    Close();
                }
                else if (!isInDialogue && !isInvestigating && !isOnCaseBoard)
                {
                    Open();
                }
            }

            // ESC 키로 닫기
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            // DOTween 정리
            _animationSequence?.Kill();
        }

        // =============================================================================
        // 노트북 열기/닫기
        // =============================================================================

        /// <summary>
        /// 노트북 열기
        /// </summary>
        public void Open()
        {
            if (_isOpen) return;

            _isOpen = true;
            _openedThisFrame = true;

            // 마우스 커서 먼저 표시 (가장 먼저 실행)
            if (_showCursorOnOpen)
            {
                _previousCursorLockMode = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;

                // 강제로 커서 표시
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

                Debug.Log($"[EvidenceNotebookUI] 커서 표시 - 이전 상태: {_previousCursorLockMode}, {_previousCursorVisible}");
                Debug.Log($"[EvidenceNotebookUI] 커서 현재 상태: {Cursor.lockState}, {Cursor.visible}");
            }

            // PlayerController 상태 저장 및 입력 비활성화
            if (PlayerController.Instance != null)
            {
                _previousPlayerState = PlayerController.Instance.CurrentState;

                // Tab 키 충돌로 같은 프레임에서 Inspecting으로 진입했을 수 있으므로 Idle로 보정
                if (_previousPlayerState == PlayerState.Inspecting)
                {
                    _previousPlayerState = PlayerState.Idle;
                }

                PlayerController.Instance.SetState(PlayerState.Paused);
                PlayerController.Instance.SetInputEnabled(false);

                // PlayerCursor 비활성화 (커서 제어를 노트북 UI가 담당)
                if (PlayerController.Instance.Cursor != null)
                {
                    PlayerController.Instance.Cursor.enabled = false;
                }
            }

            // 게임 상태 저장 및 일시정지
            if (_pauseGameOnOpen)
            {
                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            // 커서 다시 한 번 강제 표시 (PlayerController가 숨겼을 수 있음)
            if (_showCursorOnOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            // 목록 갱신
            RefreshEvidenceList();

            // 애니메이션 시작
            PlayOpenAnimation();

            Debug.Log("[EvidenceNotebookUI] 노트북 열기");
        }

        /// <summary>
        /// 노트북 닫기
        /// </summary>
        public void Close()
        {
            if (!_isOpen) return;

            _isOpen = false;
            _closedThisFrame = true;

            // PlayerController 상태 및 입력 복원
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetState(_previousPlayerState);
                PlayerController.Instance.SetInputEnabled(true);

                // PlayerCursor 다시 활성화 후 Hidden 상태 적용
                if (PlayerController.Instance.Cursor != null)
                {
                    PlayerController.Instance.Cursor.enabled = true;
                    PlayerController.Instance.Cursor.SetCursorState(CursorState.Hidden);
                }
            }

            // 게임 상태 복원
            if (_pauseGameOnOpen)
            {
                Time.timeScale = _previousTimeScale;
            }

            // 애니메이션 시작
            PlayCloseAnimation();

            Debug.Log("[EvidenceNotebookUI] 노트북 닫기");
        }

        /// <summary>
        /// 즉시 숨김 (초기화용)
        /// </summary>
        private void HideImmediate()
        {
            _animationSequence?.Kill();

            if (_notebookRoot != null)
            {
                _notebookRoot.alpha = 0f;
                _notebookRoot.blocksRaycasts = false;
            }

            if (_notebookRect != null)
            {
                _notebookRect.localScale = Vector3.one * _startScale;
            }

            // 상세 정보 숨김
            if (_detailRoot != null)
            {
                _detailRoot.SetActive(false);
            }
        }

        // =============================================================================
        // 애니메이션
        // =============================================================================

        /// <summary>
        /// 열기 애니메이션
        /// </summary>
        private void PlayOpenAnimation()
        {
            _animationSequence?.Kill();

            // Raycast 활성화
            if (_notebookRoot != null)
            {
                _notebookRoot.blocksRaycasts = true;
            }

            // 시퀀스 생성 (SetUpdate(true)로 Time.timeScale 무시)
            _animationSequence = DOTween.Sequence();
            _animationSequence.SetUpdate(true); // Time.timeScale 영향 받지 않음

            // 페이드 인
            _animationSequence.Append(
                _notebookRoot.DOFade(1f, _animationDuration).SetEase(Ease.OutCubic)
            );

            // 스케일 효과
            if (_useScaleEffect && _notebookRect != null)
            {
                _notebookRect.localScale = Vector3.one * _startScale;
                _animationSequence.Join(
                    _notebookRect.DOScale(1f, _animationDuration).SetEase(Ease.OutBack)
                );
            }
        }

        /// <summary>
        /// 닫기 애니메이션
        /// </summary>
        private void PlayCloseAnimation()
        {
            _animationSequence?.Kill();

            // 시퀀스 생성 (SetUpdate(true)로 Time.timeScale 무시)
            _animationSequence = DOTween.Sequence();
            _animationSequence.SetUpdate(true); // Time.timeScale 영향 받지 않음

            // 페이드 아웃
            _animationSequence.Append(
                _notebookRoot.DOFade(0f, _animationDuration * 0.7f).SetEase(Ease.InCubic)
            );

            // 스케일 효과
            if (_useScaleEffect && _notebookRect != null)
            {
                _animationSequence.Join(
                    _notebookRect.DOScale(_startScale, _animationDuration * 0.7f).SetEase(Ease.InBack)
                );
            }

            // 완료 후 Raycast 비활성화
            _animationSequence.OnComplete(() =>
            {
                if (_notebookRoot != null)
                {
                    _notebookRoot.blocksRaycasts = false;
                }
            });
        }

        // =============================================================================
        // 증거물 목록 관리
        // =============================================================================

        /// <summary>
        /// 증거물 추가
        /// </summary>
        public void AddEvidence(EvidenceData evidence)
        {
            if (evidence == null)
            {
                Debug.LogWarning("[EvidenceNotebookUI] 증거물 데이터가 null입니다.");
                return;
            }

            // 이미 있으면 무시
            if (_acquiredEvidences.Contains(evidence))
            {
                Debug.LogWarning($"[EvidenceNotebookUI] 이미 획득한 증거물입니다: {evidence.EvidenceName}");
                return;
            }

            _acquiredEvidences.Add(evidence);
            Debug.Log($"[EvidenceNotebookUI] 증거물 추가: {evidence.EvidenceName}");
        }

        /// <summary>
        /// 증거물 목록 갱신 (프리팹 생성)
        /// </summary>
        private void RefreshEvidenceList()
        {
            // 기존 슬롯 제거
            ClearSlots();

            // 획득한 증거물마다 슬롯 생성
            foreach (var evidence in _acquiredEvidences)
            {
                CreateSlot(evidence);
            }

            Debug.Log($"[EvidenceNotebookUI] 목록 갱신 - 증거물 {_acquiredEvidences.Count}개");
        }

        /// <summary>
        /// 슬롯 생성
        /// </summary>
        private void CreateSlot(EvidenceData evidence)
        {
            if (_evidenceSlotPrefab == null || _evidenceListContent == null)
            {
                Debug.LogError("[EvidenceNotebookUI] 슬롯 프리팹 또는 Content가 없습니다!");
                return;
            }

            // 프리팹 생성
            GameObject slotObj = Instantiate(_evidenceSlotPrefab, _evidenceListContent);

            // 슬롯 활성화 (프리팹이 비활성화 상태일 수 있음)
            slotObj.SetActive(true);

            EvidenceNotebookSlot slot = slotObj.GetComponent<EvidenceNotebookSlot>();

            if (slot != null)
            {
                slot.Initialize(evidence, this);
                _slots.Add(slot);
                Debug.Log($"[EvidenceNotebookUI] 슬롯 생성 완료: {evidence.EvidenceName}");
            }
            else
            {
                Debug.LogError("[EvidenceNotebookUI] 슬롯 프리팹에 EvidenceNotebookSlot 컴포넌트가 없습니다!");
            }
        }

        /// <summary>
        /// 모든 슬롯 제거
        /// </summary>
        private void ClearSlots()
        {
            foreach (var slot in _slots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            _slots.Clear();
        }

        // =============================================================================
        // 상세 정보 표시
        // =============================================================================

        /// <summary>
        /// 증거물 상세 정보 표시
        /// </summary>
        /// <param name="evidence">증거물 데이터</param>
        /// <param name="slot">선택된 슬롯</param>
        public void ShowEvidenceDetail(EvidenceData evidence, EvidenceNotebookSlot slot)
        {
            if (evidence == null) return;

            // 이전 선택 해제
            if (_currentSelectedSlot != null)
            {
                _currentSelectedSlot.SetSelected(false);
            }

            // 새로운 슬롯 선택
            _currentSelectedSlot = slot;
            if (_currentSelectedSlot != null)
            {
                _currentSelectedSlot.SetSelected(true);
            }

            // 상세 정보 루트 활성화
            if (_detailRoot != null)
            {
                _detailRoot.SetActive(true);
            }

            // 이미지 설정
            if (_detailImage != null)
            {
                if (evidence.EvidenceImage != null)
                {
                    _detailImage.sprite = evidence.EvidenceImage;
                    _detailImage.enabled = true;
                }
                else
                {
                    _detailImage.enabled = false;
                }
            }

            // 이름 설정
            if (_detailNameText != null)
            {
                _detailNameText.text = evidence.EvidenceName;
            }

            // 설명 설정
            if (_detailDescriptionText != null)
            {
                _detailDescriptionText.text = evidence.EvidenceDescription;
            }

            Debug.Log($"[EvidenceNotebookUI] 상세 정보 표시: {evidence.EvidenceName}");
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 모든 증거물 초기화
        /// </summary>
        public void ClearAllEvidences()
        {
            _acquiredEvidences.Clear();
            ClearSlots();

            if (_detailRoot != null)
            {
                _detailRoot.SetActive(false);
            }

            Debug.Log("[EvidenceNotebookUI] 모든 증거물 초기화");
        }

        /// <summary>
        /// 열림 여부 (열린/닫힌 직후 프레임도 true 반환하여 Tab 키 충돌 방지)
        /// </summary>
        public bool IsOpen => _isOpen || _closedThisFrame || _openedThisFrame;
    }
}
