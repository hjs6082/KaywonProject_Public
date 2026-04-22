// =============================================================================
// WorldInteractable.cs
// =============================================================================
// 설명: World Space UI 기반 상호작용 오브젝트
// 용도: 플레이어가 범위 내에 들어오면 UI가 표시되고, 키 입력으로 상호작용
// 작동 방식:
//   1. 플레이어가 감지 범위 내에 들어오면 UI 표시
//   2. UI는 항상 플레이어를 바라봄 (Billboard)
//   3. 상호작용 키 입력 시 설정된 동작 실행 (대화, 이벤트 등)
// =============================================================================

using UnityEngine;
using UnityEngine.Events;
using GameDatabase.Dialogue;
using GameDatabase.UI;
using GameDatabase;
using GameDatabase.Evidence;

namespace GameDatabase.Player
{
    /// <summary>
    /// 상호작용 타입
    /// </summary>
    public enum WorldInteractionType
    {
        /// <summary>
        /// 이벤트만 발생
        /// </summary>
        Event,

        /// <summary>
        /// 다이얼로그 재생
        /// </summary>
        Dialogue,

        /// <summary>
        /// 씬 전환
        /// </summary>
        SceneChange,

        /// <summary>
        /// 커스텀 (직접 구현)
        /// </summary>
        Custom,

        /// <summary>
        /// 조사 구역 진입 (추리 모드)
        /// </summary>
        Investigation
    }

    /// <summary>
    /// World Space UI 기반 상호작용 오브젝트
    /// 플레이어가 범위 내에 들어오면 UI가 표시되고 상호작용 가능
    /// </summary>
    public class WorldInteractable : MonoBehaviour
    {
        // =============================================================================
        // 기본 설정
        // =============================================================================

        [Header("=== 기본 설정 ===")]

        [Tooltip("상호작용 이름 (UI에 표시)")]
        [SerializeField] private string _interactionName = "상호작용";

        [Tooltip("상호작용 키")]
        [SerializeField] private KeyCode _interactKey = KeyCode.F;

        [Tooltip("상호작용 가능 여부")]
        [SerializeField] private bool _canInteract = true;

        [Tooltip("한 번만 상호작용 가능")]
        [SerializeField] private bool _oneTimeOnly = false;

        // =============================================================================
        // 감지 설정
        // =============================================================================

        [Header("=== 감지 설정 ===")]

        [Tooltip("플레이어 감지 범위")]
        [Range(1f, 20f)]
        [SerializeField] private float _detectionRange = 5f;

        [Tooltip("플레이어 태그")]
        [SerializeField] private string _playerTag = "Player";

        [Tooltip("감지 업데이트 간격 (초) - 성능 최적화")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _detectionInterval = 0.1f;

        // =============================================================================
        // 상호작용 타입 설정
        // =============================================================================

        [Header("=== 상호작용 타입 ===")]

        [Tooltip("상호작용 타입")]
        [SerializeField] private WorldInteractionType _interactionType = WorldInteractionType.Dialogue;

        [Header("다이얼로그 설정 (타입이 Dialogue일 때)")]
        [Tooltip("NPC 대화 데이터베이스. 설정 시 DB 기준으로 조건부 대화 재생.\n비워두면 아래 기본 _dialogue 사용 (하위 호환)")]
        [SerializeField] private NpcDialogueDatabase _npcDialogueDatabase;

        [Tooltip("기본 재생 다이얼로그 (_npcDialogueDatabase가 비어있을 때 사용)")]
        [SerializeField] private DialogueData _dialogue;

        [Tooltip("선택지 UI 기준 앵커 본 (머리, 어깨 등). 설정 시 해당 위치 기준으로 WorldSpaceChoiceUI 배치. 비워두면 NPC 루트 + Height Offset 사용")]
        [SerializeField] private Transform _npcAnchorTransform;

        [Tooltip("대화 시작 시 플레이어가 이동할 스탠드 포인트. 설정 시 대화 시작과 동시에 플레이어를 해당 위치로 이동시켜 구도를 고정함. 비워두면 이동 없음")]
        [SerializeField] private Transform _dialogueStandPoint;

        [Tooltip("단일 _dialogue 사용 시의 완료 대화 (NpcDialogueDatabase 사용 시에는 각 항목의 Completion Dialogue가 우선)")]
        [SerializeField] private DialogueData _completionDialogue;

        [Header("씬 전환 설정 (타입이 SceneChange일 때)")]
        [Tooltip("이동할 씬 이름")]
        [SerializeField] private string _targetSceneName;

        [Header("조사 구역 설정 (타입이 Investigation일 때)")]
        [Tooltip("진입할 조사 구역")]
        [SerializeField] private InvestigationZone _investigationZone;

        // =============================================================================
        // UI 설정
        // =============================================================================

        [Header("=== UI 설정 ===")]

        [Tooltip("World Space UI 오브젝트 (WorldInteractionUI 컴포넌트)")]
        [SerializeField] private WorldInteractionUI _worldUI;

        [Tooltip("UI가 없으면 자동 생성")]
        [SerializeField] private bool _autoCreateUI = true;

        [Tooltip("UI 오프셋 (오브젝트 기준)")]
        [SerializeField] private Vector3 _uiOffset = new Vector3(0, 2f, 0);

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("플레이어 감지 범위 진입 시")]
        public UnityEvent OnPlayerEnterRange;

        [Tooltip("플레이어 감지 범위 이탈 시")]
        public UnityEvent OnPlayerExitRange;

        [Tooltip("상호작용 시")]
        public UnityEvent OnInteracted;

        [Tooltip("상호작용 시 (PlayerController 전달)")]
        public UnityEvent<PlayerController> OnInteractedWithPlayer;

        [Tooltip("다이얼로그 완료 후 호출 (Dialogue 타입일 때)")]
        public UnityEvent OnDialogueCompleted;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        // 플레이어 참조
        private Transform _playerTransform;
        private PlayerController _playerController;

        // 상태
        private bool _isPlayerInRange = false;
        private bool _hasInteracted = false;
        private float _lastDetectionTime = 0f;

        // 추리형 선택지 진행 상태
        // _dialogue에 포함된 선택지 노드 중 정답이 설정된 것의 수
        private int _totalCorrectChoices = 0;
        // 현재까지 플레이어가 정답을 맞춘 선택지 수
        private int _solvedChoices = 0;
        // 마지막으로 Resolve된 NpcDialogueEntry (CompletionDialogue 참조용)
        private NpcDialogueEntry _currentResolvedEntry = null;

        /// <summary>
        /// 모든 정답 선택지를 다 맞췄는지 여부
        /// </summary>
        public bool IsAllChoicesSolved => _totalCorrectChoices > 0 && _solvedChoices >= _totalCorrectChoices;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 상호작용 이름
        /// </summary>
        public string InteractionName => _interactionName;

        /// <summary>
        /// 상호작용 키
        /// </summary>
        public KeyCode InteractKey => _interactKey;

        /// <summary>
        /// 상호작용 가능 여부
        /// </summary>
        public bool CanInteract
        {
            get
            {
                if (_oneTimeOnly && _hasInteracted) return false;
                return _canInteract;
            }
        }

        /// <summary>
        /// 플레이어가 범위 내에 있는지
        /// </summary>
        public bool IsPlayerInRange => _isPlayerInRange;

        /// <summary>
        /// 감지 범위
        /// </summary>
        public float DetectionRange => _detectionRange;

        /// <summary>
        /// 상호작용 타입
        /// </summary>
        public WorldInteractionType InteractionType => _interactionType;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Start()
        {
            // 플레이어 찾기
            FindPlayer();

            // UI 설정
            SetupUI();

            // 추리형 선택지: _dialogue에서 정답 있는 선택지 노드 수 카운트
            CountCorrectChoices();

        }

        private void Update()
        {
            // 감지 업데이트 (성능 최적화를 위해 간격 적용)
            if (Time.time - _lastDetectionTime >= _detectionInterval)
            {
                UpdateDetection();
                _lastDetectionTime = Time.time;
            }

            // 상호작용 입력 처리
            if (_isPlayerInRange && CanInteract)
            {
                HandleInput();
            }
        }

        private void OnDisable()
        {
            // 비활성화 시 UI 숨김
            if (_isPlayerInRange)
            {
                _isPlayerInRange = false;
                HideUI();
            }
        }

        // =============================================================================
        // 초기화
        // =============================================================================

        /// <summary>
        /// 플레이어 찾기
        /// </summary>
        private void FindPlayer()
        {
            // PlayerController 싱글톤 시도
            _playerController = PlayerController.Instance;
            if (_playerController != null)
            {
                _playerTransform = _playerController.transform;
                return;
            }

            // 태그로 찾기
            GameObject playerObj = GameObject.FindGameObjectWithTag(_playerTag);
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
                _playerController = playerObj.GetComponent<PlayerController>();
            }

            if (_playerTransform == null)
            {
                Debug.LogWarning($"[WorldInteractable] '{gameObject.name}': 플레이어를 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// UI 설정
        /// </summary>
        private void SetupUI()
        {
            // UI가 없고 자동 생성이 켜져 있으면 생성
            if (_worldUI == null && _autoCreateUI)
            {
                CreateDefaultUI();
            }

            // UI 초기화
            if (_worldUI != null)
            {
                _worldUI.Initialize(this);
                _worldUI.Hide();
            }
        }

        /// <summary>
        /// 기본 UI 생성
        /// </summary>
        private void CreateDefaultUI()
        {
            // WorldInteractionUI 프리팹이 있으면 인스턴스화
            // 없으면 간단한 UI 생성
            GameObject uiObj = new GameObject($"{gameObject.name}_InteractionUI");
            uiObj.transform.SetParent(transform);
            uiObj.transform.localPosition = _uiOffset;

            _worldUI = uiObj.AddComponent<WorldInteractionUI>();

            Debug.Log($"[WorldInteractable] '{gameObject.name}': 기본 UI 생성됨");
        }

        // =============================================================================
        // 감지
        // =============================================================================

        /// <summary>
        /// 플레이어 감지 업데이트
        /// </summary>
        private void UpdateDetection()
        {
            if (_playerTransform == null)
            {
                FindPlayer();
                if (_playerTransform == null) return;
            }

            // 거리 계산
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            bool inRange = distance <= _detectionRange;

            // 범위 진입
            if (inRange && !_isPlayerInRange)
            {
                OnEnterRange();
            }
            // 범위 이탈
            else if (!inRange && _isPlayerInRange)
            {
                OnExitRange();
            }
        }

        /// <summary>
        /// 범위 진입 처리
        /// </summary>
        private void OnEnterRange()
        {
            _isPlayerInRange = true;

            // UI 표시
            if (CanInteract)
            {
                ShowUI();
            }

            // 이벤트 발생
            OnPlayerEnterRange?.Invoke();

            Debug.Log($"[WorldInteractable] '{gameObject.name}': 플레이어 범위 진입");
        }

        /// <summary>
        /// 범위 이탈 처리
        /// </summary>
        private void OnExitRange()
        {
            _isPlayerInRange = false;

            // UI 숨김
            HideUI();

            // 이벤트 발생
            OnPlayerExitRange?.Invoke();

            Debug.Log($"[WorldInteractable] '{gameObject.name}': 플레이어 범위 이탈");
        }

        // =============================================================================
        // 입력 처리
        // =============================================================================

        /// <summary>
        /// 입력 처리
        /// </summary>
        private void HandleInput()
        {
            // 대화 중이거나 조사 중이면 무시
            if (_playerController != null && (_playerController.IsInDialogue || _playerController.IsInvestigating))
            {
                return;
            }

            // 상호작용 키 입력
            if (Input.GetKeyDown(_interactKey))
            {
                Interact();
            }
        }

        // =============================================================================
        // 상호작용
        // =============================================================================

        /// <summary>
        /// 상호작용 실행
        /// </summary>
        public void Interact()
        {
            if (!CanInteract) return;

            _hasInteracted = true;

            // UI 숨김
            HideUI();

            // 타입별 동작 실행
            ExecuteInteraction();

            // 이벤트 발생
            OnInteracted?.Invoke();
            OnInteractedWithPlayer?.Invoke(_playerController);

            Debug.Log($"[WorldInteractable] '{gameObject.name}': 상호작용 실행 (타입: {_interactionType})");
        }

        /// <summary>
        /// 타입별 상호작용 실행
        /// </summary>
        private void ExecuteInteraction()
        {
            switch (_interactionType)
            {
                case WorldInteractionType.Event:
                    // 이벤트만 발생 (OnInteracted에서 처리)
                    break;

                case WorldInteractionType.Dialogue:
                    StartDialogue();
                    break;

                case WorldInteractionType.SceneChange:
                    ChangeScene();
                    break;

                case WorldInteractionType.Custom:
                    // 커스텀 - 상속받아서 구현하거나 이벤트 사용
                    break;

                case WorldInteractionType.Investigation:
                    StartInvestigation();
                    break;
            }
        }

        /// <summary>
        /// 다이얼로그 시작
        /// </summary>
        private void StartDialogue()
        {
            // 재생할 대화 결정 (조건부 배열 우선, 없으면 기본 _dialogue 폴백)
            DialogueData dialogueToPlay = ResolveDialogue();

            if (dialogueToPlay == null)
            {
                Debug.LogWarning($"[WorldInteractable] '{gameObject.name}': 재생할 다이얼로그가 없습니다.");
                return;
            }

            // 스탠드 포인트가 설정된 경우 플레이어를 해당 위치로 이동 후 NPC 방향으로 고정
            // → 어떤 각도에서 말을 걸어도 대화 구도가 항상 동일하게 유지됨
            if (_dialogueStandPoint != null && _playerController != null)
            {
                _playerController.TeleportToDialoguePosition(_dialogueStandPoint, transform.position);
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogError($"[WorldInteractable] DialogueManager를 찾을 수 없습니다.");
                return;
            }

            // WorldSpaceChoiceUI가 선택지를 NPC 주변에 배치할 수 있도록 Transform 전달
            DialogueManager.Instance.SetNpcTransform(transform, _npcAnchorTransform);

            // DB 사용 시 매 상호작용마다 현재 조건으로 카운트 갱신
            if (_npcDialogueDatabase != null)
            {
                CountCorrectChoices();
            }

            // 추리형: 모든 정답을 이미 맞춘 상태이고 완료 다이얼로그가 있으면 완료 대사 재생
            DialogueData completionDialogue = ResolveCompletionDialogue();
            if (IsAllChoicesSolved && completionDialogue != null)
            {
                DialogueManager.Instance.StartDialogue(completionDialogue);
                return;
            }

            // 추리형: 정답 맞춘 선택지를 추적하기 위해 이 WorldInteractable을 등록
            if (_totalCorrectChoices > 0)
            {
                DialogueManager.Instance.RegisterChoiceTracker(this);
            }

            DialogueManager.Instance.OnDialogueEnd.AddListener(HandleDialogueEnd);
            DialogueManager.Instance.StartDialogue(dialogueToPlay);
        }

        private void HandleDialogueEnd()
        {
            DialogueManager.Instance.OnDialogueEnd.RemoveListener(HandleDialogueEnd);
            OnDialogueCompleted?.Invoke();
        }

        /// <summary>
        /// 현재 게임 상태에 따라 재생할 대화 결정
        /// NpcDialogueDatabase가 있으면 DB의 ResolveEntry() 사용, 없으면 기본 _dialogue 폴백
        /// </summary>
        private DialogueData ResolveDialogue()
        {
            if (_npcDialogueDatabase != null)
            {
                _currentResolvedEntry = _npcDialogueDatabase.ResolveEntry();
                return _currentResolvedEntry?.Dialogue;
            }

            _currentResolvedEntry = null;
            return _dialogue;
        }

        /// <summary>
        /// 현재 상태에서 완료 대화 결정
        /// DB 사용 시 현재 entry의 CompletionDialogue, 단일 모드 시 _completionDialogue
        /// </summary>
        private DialogueData ResolveCompletionDialogue()
        {
            if (_currentResolvedEntry != null)
                return _currentResolvedEntry.CompletionDialogue;

            return _completionDialogue;
        }

        /// <summary>
        /// 재생 대상 대화에서 IsCorrectAnswer가 true인 선택지 노드 수 카운트
        /// DB 사용 시에는 현재 조건으로 Resolve()해서 나온 대화를 대상으로 함
        /// </summary>
        private void CountCorrectChoices()
        {
            _totalCorrectChoices = 0;

            DialogueData target = ResolveDialogue();
            if (target == null) return;

            foreach (var node in target.Nodes)
            {
                if (!node.IsChoice || node.Choice?.Options == null) continue;
                foreach (var option in node.Choice.Options)
                {
                    if (option != null && option.IsCorrectAnswer)
                    {
                        _totalCorrectChoices++;
                        break; // 노드당 1개만 카운트
                    }
                }
            }
        }

        /// <summary>
        /// 특정 선택지 노드에서 정답이 맞춰졌을 때 DialogueManager에서 호출
        /// </summary>
        public void OnChoiceSolved()
        {
            _solvedChoices = Mathf.Min(_solvedChoices + 1, _totalCorrectChoices);
            Debug.Log($"[WorldInteractable] '{gameObject.name}': 정답 맞춤 ({_solvedChoices}/{_totalCorrectChoices})");
        }

        /// <summary>
        /// 씬 전환
        /// </summary>
        private void ChangeScene()
        {
            if (string.IsNullOrEmpty(_targetSceneName))
            {
                Debug.LogWarning($"[WorldInteractable] '{gameObject.name}': 대상 씬이 설정되지 않았습니다.");
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(_targetSceneName);
        }

        /// <summary>
        /// 조사 구역 진입
        /// </summary>
        private void StartInvestigation()
        {
            if (_investigationZone == null)
            {
                Debug.LogWarning($"[WorldInteractable] '{gameObject.name}': 조사 구역이 설정되지 않았습니다.");
                return;
            }

            if (InvestigationManager.Instance != null)
            {
                InvestigationManager.Instance.EnterInvestigation(_investigationZone);
            }
            else
            {
                Debug.LogError("[WorldInteractable] InvestigationManager를 찾을 수 없습니다.");
            }
        }

        // =============================================================================
        // UI 제어
        // =============================================================================

        /// <summary>
        /// UI 표시
        /// </summary>
        private void ShowUI()
        {
            if (_worldUI != null)
            {
                _worldUI.Show();
            }
        }

        /// <summary>
        /// UI 숨김
        /// </summary>
        private void HideUI()
        {
            if (_worldUI != null)
            {
                _worldUI.Hide();
            }
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 상호작용 가능 여부 설정
        /// </summary>
        public void SetCanInteract(bool canInteract)
        {
            _canInteract = canInteract;

            // 범위 내에 있으면 UI 상태 업데이트
            if (_isPlayerInRange)
            {
                if (canInteract)
                    ShowUI();
                else
                    HideUI();
            }
        }

        /// <summary>
        /// 상호작용 초기화 (다시 상호작용 가능하게)
        /// </summary>
        public void ResetInteraction()
        {
            _hasInteracted = false;
        }

        /// <summary>
        /// 다이얼로그 설정
        /// </summary>
        public void SetDialogue(DialogueData dialogue)
        {
            _dialogue = dialogue;
            _interactionType = WorldInteractionType.Dialogue;
        }

        /// <summary>
        /// 상호작용 이름 설정
        /// </summary>
        public void SetInteractionName(string name)
        {
            _interactionName = name;

            if (_worldUI != null)
            {
                _worldUI.UpdateText();
            }
        }

        /// <summary>
        /// 플레이어 Transform 가져오기
        /// </summary>
        public Transform GetPlayerTransform()
        {
            return _playerTransform;
        }

        // =============================================================================
        // Gizmo (에디터 표시)
        // =============================================================================

        private void OnDrawGizmosSelected()
        {
            // 감지 범위 표시
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            // UI 위치 표시
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + _uiOffset, new Vector3(0.5f, 0.3f, 0.1f));
        }
    }
}
