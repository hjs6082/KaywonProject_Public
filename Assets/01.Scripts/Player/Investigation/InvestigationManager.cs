// =============================================================================
// InvestigationManager.cs
// =============================================================================
// 설명: 조사 시스템 매니저 (앨런 웨이크 2 스타일 UI + 기존 Zone 시스템)
// 용도: 조사 모드 진입/종료, 카메라 전환, UI 연출, 단서 조사 관리
// 작동 방식:
//   1. WorldInteractable로 조사 구역(InvestigationZone) 진입
//   2. 플레이어를 지정된 위치로 이동
//   3. 조사 전용 카메라로 전환 (Cinemachine)
//   4. UI 페이드인 (좌측 상단 인디케이터)
//   5. 마우스 커서로 단서(InteractableClue) 조사
//   6. 모든 단서 조사 완료 시 탈출 가능
// =============================================================================

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using Cinemachine;
using DG.Tweening;
using GameDatabase.UI;

namespace GameDatabase.Player
{
    /// <summary>
    /// 조사 시스템 매니저
    /// 앨런 웨이크 2 스타일 UI + 기존 Zone 시스템 결합
    /// </summary>
    public class InvestigationManager : MonoBehaviour
    {
        // =============================================================================
        // 싱글톤
        // =============================================================================

        private static InvestigationManager _instance;

        public static InvestigationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<InvestigationManager>();
                    if (_instance == null)
                    {
                        //Debug.LogError("[InvestigationManager] 씬에 InvestigationManager가 없습니다.");
                    }
                }
                return _instance;
            }
        }

        // =============================================================================
        // 입력 설정
        // =============================================================================

        [Header("=== 입력 설정 ===")]

        [Tooltip("조사 모드 종료 시도 키 (모든 단서 조사 완료 시에만 종료)")]
        [SerializeField] private KeyCode _exitKey = KeyCode.Escape;

        // =============================================================================
        // Raycast 설정
        // =============================================================================

        [Header("=== Raycast 설정 ===")]

        [Tooltip("단서 감지 레이어")]
        [SerializeField] private LayerMask _clueLayer;

        [Tooltip("Raycast 최대 거리")]
        [Range(1f, 50f)]
        [SerializeField] private float _raycastDistance = 20f;

        [Tooltip("화면 중앙 기준 Raycast 사용 (false: 마우스 포인터 기준)")]
        [SerializeField] private bool _useScreenCenter = false;

        // =============================================================================
        // 포스트 프로세싱
        // =============================================================================

        [Header("=== 포스트 프로세싱 (선택) ===")]

        [Tooltip("Global Volume (Depth of Field 제어용)")]
        [SerializeField] private Volume _globalVolume;

        [Tooltip("조사 모드에서 Depth of Field 활성화")]
        [SerializeField] private bool _enableDepthOfField = false;

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("조사 모드 진입 시 호출")]
        public UnityEvent OnInvestigationEnter;

        [Tooltip("조사 모드 종료 시 호출")]
        public UnityEvent OnInvestigationExit;

        [Tooltip("단서 호버 시 호출 (InteractableClue)")]
        public UnityEvent<InteractableClue> OnClueHovered;

        [Tooltip("단서 호버 해제 시 호출")]
        public UnityEvent OnClueHoverExit;

        [Tooltip("단서 조사 시 호출 (InteractableClue)")]
        public UnityEvent<InteractableClue> OnClueInvestigated;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        // 상태
        private bool _isInvestigating = false;
        private InvestigationZone _currentZone;
        private InteractableClue _currentHoveredClue;

        // 컴포넌트
        private PlayerController _playerController;
        private InvestigationUI _investigationUI;
        private Camera _mainCamera;

        // DOTween 시퀀스
        private Sequence _currentSequence;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 조사 모드 활성화 여부
        /// </summary>
        public bool IsInvestigating => _isInvestigating;

        /// <summary>
        /// 현재 조사 중인 구역
        /// </summary>
        public InvestigationZone CurrentZone => _currentZone;

        /// <summary>
        /// 현재 호버된 단서
        /// </summary>
        public InteractableClue CurrentHoveredClue => _currentHoveredClue;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // 싱글톤
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Debug.LogWarning("[InvestigationManager] 중복된 InvestigationManager가 있습니다. 삭제됩니다.");
                Destroy(gameObject);
                return;
            }

            // 컴포넌트 찾기
            _mainCamera = Camera.main;
            _investigationUI = FindObjectOfType<InvestigationUI>();
        }

        private void Start()
        {
            // PlayerController 찾기
            _playerController = PlayerController.Instance;

            // UI 초기 숨김
            if (_investigationUI != null)
            {
                _investigationUI.HideImmediate();
            }
            else
            {
                Debug.LogWarning("[InvestigationManager] InvestigationUI를 찾을 수 없습니다. UI가 표시되지 않을 수 있습니다.");
            }

            // DialogueManager 연결
            ConnectToDialogueManager();
        }

        private void Update()
        {
            // 입력 처리
            HandleInput();

            // 조사 모드일 때만 Raycast
            if (_isInvestigating)
            {
                HandleRaycast();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            // DOTween 정리
            _currentSequence?.Kill();

            // DialogueManager 연결 해제
            DisconnectFromDialogueManager();
        }

        // =============================================================================
        // DialogueManager 연동
        // =============================================================================

        private void ConnectToDialogueManager()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueEnd.AddListener(OnDialogueEnded);
            }
        }

        private void DisconnectFromDialogueManager()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueEnd.RemoveListener(OnDialogueEnded);
            }
        }

        /// <summary>
        /// 대화 종료 콜백
        /// 조사 모드 중 대사가 끝나면 조사 모드로 복귀하거나 조사 완료 처리
        /// </summary>
        private void OnDialogueEnded()
        {
            if (!_isInvestigating || _currentZone == null) return;

            // 모든 단서 완료 체크
            if (_currentZone.AllCompleted)
            {
                // 자동 종료하지 않고 사용자가 ESC 누르도록 유도
                Debug.Log("[InvestigationManager] 모든 단서 조사 완료! ESC 키로 조사 모드를 종료할 수 있습니다.");

                // UI 업데이트 (선택: 완료 메시지 표시)
                if (_investigationUI != null)
                {
                    _investigationUI.SetModeText("조사 완료 - ESC로 종료");
                }
            }

            // 조사 모드 복귀
            ReturnToInvestigationMode();
        }

        /// <summary>
        /// 대화 종료 후 조사 모드로 복귀
        /// </summary>
        private void ReturnToInvestigationMode()
        {
            PlayerController player = PlayerController.Instance;
            if (player != null)
            {
                // InDialogue → Investigating 상태 복귀
                player.SetState(PlayerState.Investigating);
                player.SetInputEnabled(false);
                player.SetMovementEnabled(false);
                player.LockCameraPosition = true;

                // 커서 다시 표시
                if (player.Cursor != null)
                {
                    player.Cursor.SetCursorState(CursorState.Normal);
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            Debug.Log("[InvestigationManager] 조사 모드 복귀");
        }

        // =============================================================================
        // 입력 처리
        // =============================================================================

        /// <summary>
        /// 입력 처리
        /// </summary>
        private void HandleInput()
        {
            // 대화 중이면 입력 무시
            if (_playerController != null && _playerController.IsInDialogue)
            {
                return;
            }

            // 조사 모드 종료 (모든 단서 조사 완료 시에만)
            if (_isInvestigating && Input.GetKeyDown(_exitKey))
            {
                TryExitInvestigation();
            }

            // 조사 모드 중 상호작용 (E 키 또는 마우스 클릭)
            if (_isInvestigating && (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)))
            {
                InvestigateCurrentClue();
            }
        }

        // =============================================================================
        // 조사 모드 진입/종료
        // =============================================================================

        /// <summary>
        /// 조사 모드 진입 (InvestigationZone에서 호출)
        /// </summary>
        public void EnterInvestigation(InvestigationZone zone)
        {
            if (zone == null)
            {
                Debug.LogWarning("[InvestigationManager] zone이 null입니다.");
                return;
            }

            if (_isInvestigating)
            {
                Debug.LogWarning("[InvestigationManager] 이미 조사 모드입니다.");
                return;
            }

            _currentZone = zone;
            _isInvestigating = true;

            Debug.Log($"[InvestigationManager] ===== 조사 모드 진입 =====");
            Debug.Log($"[InvestigationManager] Zone: {zone.name}");
            Debug.Log($"[InvestigationManager] Camera: {(zone.InvestigationCamera != null ? zone.InvestigationCamera.name : "없음")}");
            Debug.Log($"[InvestigationManager] Clue Layer: {_clueLayer.value}");

            // 1. 플레이어 상태 전환
            if (_playerController != null)
            {
                _playerController.SetState(PlayerState.Investigating);
                _playerController.SetMovementEnabled(false);
                _playerController.LockCameraPosition = true;

                // 커서 표시
                if (_playerController.Cursor != null)
                {
                    _playerController.Cursor.SetCursorState(CursorState.Normal);
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            // 2. 조사 카메라 활성화
            if (zone.InvestigationCamera != null)
            {
                zone.InvestigationCamera.Priority = 20;
                Debug.Log($"[InvestigationManager] 조사 카메라 활성화 - {zone.InvestigationCamera.name}");
            }
            else
            {
                Debug.LogWarning($"[InvestigationManager] InvestigationZone '{zone.name}'에 카메라가 할당되지 않았습니다.");
            }

            // 3. 조사 구역 시작
            zone.StartInvestigation();

            // 4. UI 페이드인 (DOTween)
            if (_investigationUI != null)
            {
                _investigationUI.ShowInvestigationMode();
            }

            // 5. 포스트 프로세싱 활성화 (선택)
            if (_enableDepthOfField && _globalVolume != null)
            {
                EnableDepthOfField();
            }

            // 이벤트 발생
            OnInvestigationEnter?.Invoke();

            Debug.Log($"[InvestigationManager] 조사 모드 진입 - {zone.gameObject.name}");
        }

        /// <summary>
        /// 조사 모드 종료 시도
        /// </summary>
        private void TryExitInvestigation()
        {
            if (_currentZone == null) return;

            // 모든 단서를 조사했는지 확인
            if (!_currentZone.AllCompleted)
            {
                Debug.Log("[InvestigationManager] 아직 조사하지 않은 단서가 있습니다.");

                // UI 피드백 (선택)
                if (_investigationUI != null)
                {
                    // TODO: 경고 메시지 표시 (예: "아직 조사하지 않은 단서가 있습니다")
                }

                return;
            }

            // 모든 조사 완료 - 종료 허용
            ExitInvestigation();
        }

        /// <summary>
        /// 조사 모드 종료 (모든 단서 완료 시)
        /// </summary>
        public void ExitInvestigation()
        {
            if (!_isInvestigating || _currentZone == null) return;

            // 1. 조사 카메라 비활성화
            if (_currentZone.InvestigationCamera != null)
            {
                _currentZone.InvestigationCamera.Priority = 0;
            }

            // 2. 조사 구역 종료
            _currentZone.EndInvestigation();

            // 조사 완료 이벤트 발생
            _currentZone.OnInvestigationComplete?.Invoke();

            // 3. 호버 해제
            ClearHoveredClue();

            // 4. UI 페이드아웃 (DOTween)
            if (_investigationUI != null)
            {
                _investigationUI.HideInvestigationMode();
                _investigationUI.SetModeText("조사 모드"); // 텍스트 복원
            }

            // 5. 포스트 프로세싱 비활성화
            if (_enableDepthOfField && _globalVolume != null)
            {
                DisableDepthOfField();
            }

            // 6. 플레이어 상태 복원
            if (_playerController != null)
            {
                _playerController.SetState(PlayerState.Idle);
                _playerController.SetMovementEnabled(true);
                _playerController.LockCameraPosition = false;
                _playerController.SetInputEnabled(true);

                // 커서 숨김
                if (_playerController.Cursor != null)
                {
                    _playerController.Cursor.SetCursorState(CursorState.Hidden);
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            _isInvestigating = false;
            _currentZone = null;

            // 이벤트 발생
            OnInvestigationExit?.Invoke();

            Debug.Log("[InvestigationManager] 조사 모드 종료 - 3인칭 복귀");
        }

        // =============================================================================
        // Raycast 처리
        // =============================================================================

        /// <summary>
        /// Raycast로 단서 감지
        /// </summary>
        private void HandleRaycast()
        {
            if (_mainCamera == null)
            {
                Debug.LogWarning("[InvestigationManager] MainCamera가 없습니다.");
                return;
            }

            Ray ray;

            // 화면 중앙 또는 마우스 포인터 기준
            if (_useScreenCenter)
            {
                ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            }
            else
            {
                ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            }

            // Raycast 발사
            if (Physics.Raycast(ray, out RaycastHit hit, _raycastDistance, _clueLayer))
            {
                Debug.Log($"[InvestigationManager] Raycast 히트: {hit.collider.gameObject.name}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

                // InteractableClue 찾기
                InteractableClue clue = hit.collider.GetComponent<InteractableClue>();
                if (clue == null)
                {
                    clue = hit.collider.GetComponentInParent<InteractableClue>();
                }

                if (clue != null && clue.CanInteract)
                {
                    // 새로운 단서면 호버 설정
                    if (clue != _currentHoveredClue)
                    {
                        Debug.Log($"[InvestigationManager] 단서 호버: {clue.ClueName}");
                        SetHoveredClue(clue);
                    }
                }
                else
                {
                    if (clue == null)
                    {
                        Debug.LogWarning($"[InvestigationManager] {hit.collider.gameObject.name}에 InteractableClue 컴포넌트가 없습니다.");
                    }
                    ClearHoveredClue();
                }
            }
            else
            {
                ClearHoveredClue();
            }
        }

        /// <summary>
        /// 단서 호버 설정
        /// </summary>
        private void SetHoveredClue(InteractableClue clue)
        {
            // 이전 호버 해제
            ClearHoveredClue();

            // 새 호버 설정
            _currentHoveredClue = clue;

            // 단서의 호버 진입 호출
            _currentHoveredClue.OnHoverEnter();

            // UI 업데이트
            if (_investigationUI != null)
            {
                _investigationUI.ShowInteractionPrompt(_currentHoveredClue.ClueName);
                _investigationUI.ShowClueInfo(_currentHoveredClue.ClueName, _currentHoveredClue.ClueDescription, _currentHoveredClue.transform);
            }

            // 커서 변경
            if (_playerController?.Cursor != null)
            {
                _playerController.Cursor.SetCursorState(CursorState.Interact);
            }

            // 이벤트 발생
            OnClueHovered?.Invoke(_currentHoveredClue);
        }

        /// <summary>
        /// 단서 호버 해제
        /// </summary>
        private void ClearHoveredClue()
        {
            if (_currentHoveredClue == null) return;

            // 단서의 호버 종료 호출
            _currentHoveredClue.OnHoverExit();

            _currentHoveredClue = null;

            // UI 숨김
            if (_investigationUI != null)
            {
                _investigationUI.HideInteractionPrompt();
                _investigationUI.HideClueInfo();
            }

            // 커서 복원
            if (_playerController?.Cursor != null)
            {
                _playerController.Cursor.SetCursorState(CursorState.Normal);
            }

            // 이벤트 발생
            OnClueHoverExit?.Invoke();
        }

        // =============================================================================
        // 단서 조사
        // =============================================================================

        /// <summary>
        /// 현재 호버된 단서 조사
        /// </summary>
        private void InvestigateCurrentClue()
        {
            Debug.Log($"[InvestigationManager] E 키 입력 감지");

            if (_currentHoveredClue == null)
            {
                Debug.LogWarning("[InvestigationManager] 현재 호버된 단서가 없습니다.");
                return;
            }

            if (!_currentHoveredClue.CanInteract)
            {
                Debug.LogWarning($"[InvestigationManager] 단서 조사 불가: {_currentHoveredClue.ClueName}");
                return;
            }

            Debug.Log($"[InvestigationManager] 단서 조사 실행: {_currentHoveredClue.ClueName}");

            // 단서 조사 실행
            _currentHoveredClue.Investigate();

            // Zone에 진행도 업데이트 알림
            if (_currentZone != null)
            {
                _currentZone.OnPointCompleted(null); // InteractableClue는 InvestigationPoint가 아니므로 null
            }

            // 이벤트 발생
            OnClueInvestigated?.Invoke(_currentHoveredClue);

            Debug.Log($"[InvestigationManager] 단서 조사 완료: {_currentHoveredClue.ClueName}");
        }

        // =============================================================================
        // 포스트 프로세싱 제어
        // =============================================================================

        /// <summary>
        /// Depth of Field 활성화
        /// </summary>
        private void EnableDepthOfField()
        {
            // TODO: Post-Processing Stack V2 또는 URP Volume 사용 시 구현
            Debug.Log("[InvestigationManager] Depth of Field 활성화 (구현 필요)");
        }

        /// <summary>
        /// Depth of Field 비활성화
        /// </summary>
        private void DisableDepthOfField()
        {
            // TODO: Post-Processing Stack V2 또는 URP Volume 사용 시 구현
            Debug.Log("[InvestigationManager] Depth of Field 비활성화 (구현 필요)");
        }

        // =============================================================================
        // Gizmo
        // =============================================================================

        private void OnDrawGizmos()
        {
            if (!_isInvestigating || _mainCamera == null) return;

            // Raycast 방향 표시
            Ray ray;
            if (_useScreenCenter)
            {
                ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            }
            else
            {
                ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            }

            Gizmos.color = _currentHoveredClue != null ? Color.green : Color.red;
            Gizmos.DrawRay(ray.origin, ray.direction * _raycastDistance);
        }
    }
}
