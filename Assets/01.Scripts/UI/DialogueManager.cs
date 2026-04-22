// =============================================================================
// DialogueManager.cs
// =============================================================================
// 설명: 다이얼로그 시스템의 중앙 관리자
// 용도: 대화 진행, UI 제어, 이벤트 처리를 총괄
// =============================================================================

using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using GameDatabase.Dialogue;
using GameDatabase;

namespace GameDatabase.UI
{
    /// <summary>
    /// 다이얼로그 상태
    /// </summary>
    public enum DialogueState
    {
        Inactive,       // 대화 없음
        Playing,        // 대사 재생 중
        WaitingInput,   // 플레이어 입력 대기
        ShowingChoice,  // 선택지 표시 중
        Transitioning   // 전환 중
    }

    /// <summary>
    /// 다이얼로그 매니저
    /// 대화 진행과 UI를 총괄하는 싱글톤 클래스
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        // =============================================================================
        // 싱글톤
        // =============================================================================

        private static DialogueManager _instance;

        /// <summary>
        /// 싱글톤 인스턴스
        /// </summary>
        public static DialogueManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DialogueManager>();
                    if (_instance == null)
                    {
                        Debug.LogError("[DialogueManager] 씬에 DialogueManager가 없습니다.");
                    }
                }
                return _instance;
            }
        }

        // =============================================================================
        // UI 참조
        // =============================================================================

        [Header("UI 컴포넌트")]
        [Tooltip("다이얼로그 UI 컴포넌트")]
        [SerializeField] private DialogueUI _dialogueUI;

        [Tooltip("선택지 UI 컴포넌트 (미사용, 레거시)")]
        [SerializeField] private ChoiceUI _choiceUI;

        [Tooltip("월드 스페이스 선택지 UI (앨런 웨이크2 스타일)")]
        [SerializeField] private WorldSpaceChoiceUI _worldSpaceChoiceUI;

        // =============================================================================
        // 데이터베이스 참조
        // =============================================================================

        [Header("데이터베이스")]
        [Tooltip("다이얼로그 데이터베이스 (선택사항)")]
        [SerializeField] private DialogueDatabase _dialogueDatabase;

        // =============================================================================
        // 카메라 참조
        // =============================================================================

        [Header("카메라")]
        [Tooltip("다이얼로그 카메라 매니저 (선택사항)")]
        [SerializeField] private DialogueCameraManager _cameraManager;

        // =============================================================================
        // 입력 설정
        // =============================================================================

        [Header("입력 설정")]
        [Tooltip("다음 대사로 넘어가는 키")]
        [SerializeField] private KeyCode _nextKey = KeyCode.Space;

        [Tooltip("마우스 클릭으로도 진행 가능")]
        [SerializeField] private bool _allowMouseClick = true;

        [Tooltip("타이핑 스킵 키")]
        [SerializeField] private KeyCode _skipKey = KeyCode.Return;

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("이벤트")]
        [Tooltip("대화 시작 시 호출")]
        public UnityEvent OnDialogueStart;

        [Tooltip("대화 종료 시 호출")]
        public UnityEvent OnDialogueEnd;

        [Tooltip("대사 표시 시 호출")]
        public UnityEvent<DialogueLine> OnLineDisplayed;

        [Tooltip("선택지 표시 시 호출")]
        public UnityEvent<DialogueChoice> OnChoiceDisplayed;

        [Tooltip("선택 완료 시 호출")]
        public UnityEvent<int, ChoiceOption> OnChoiceMade;

        // =============================================================================
        // 내부 상태
        // =============================================================================

        // 현재 대화 데이터
        private DialogueData _currentDialogue;

        // 현재 노드 인덱스
        private int _currentNodeIndex;

        // 현재 상태
        private DialogueState _currentState = DialogueState.Inactive;

        // 자동 진행 코루틴
        private Coroutine _autoProceedCoroutine;

        // 대사 시작 직후 클릭 오입력 방지용 타이머
        private float _inputBlockUntil = 0f;
        private const float InputBlockDuration = 0.2f;

        // 현재 선택지를 제공하는 NPC Transform (WorldSpaceChoiceUI 배치용)
        private Transform _currentNpcTransform;

        // 추리형 선택지 추적: 정답/오답 판정을 받아 처리하는 WorldInteractable
        private GameDatabase.Player.WorldInteractable _choiceTracker;

        // 오답 후 다시 표시해야 할 선택지 (오답 다이얼로그 종료 후 재표시용)
        private DialogueChoice _pendingRetryChoice;

        // 오답 대사 재생 전 원본 다이얼로그 + 노드 인덱스 보존 (재진입 시 복원용)
        private DialogueData _pendingRetryDialogue;
        private int _pendingRetryNodeIndex;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 현재 대화 상태
        /// </summary>
        public DialogueState CurrentState => _currentState;

        /// <summary>
        /// 대화 중인지 확인
        /// </summary>
        public bool IsDialogueActive => _currentState != DialogueState.Inactive;

        /// <summary>
        /// 현재 대화 데이터
        /// </summary>
        public DialogueData CurrentDialogue => _currentDialogue;

        /// <summary>
        /// 현재 노드 인덱스
        /// </summary>
        public int CurrentNodeIndex => _currentNodeIndex;

        /// <summary>
        /// 다이얼로그 UI
        /// </summary>
        public DialogueUI DialogueUI => _dialogueUI;

        /// <summary>
        /// 선택지 UI
        /// </summary>
        public ChoiceUI ChoiceUI => _choiceUI;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // 싱글톤 설정
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // UI 이벤트 연결
            SetupUIEvents();
        }

        private void Update()
        {
            // 입력 처리
            HandleInput();
        }

        private void OnDestroy()
        {
            // 이벤트 해제
            CleanupUIEvents();
        }

        // =============================================================================
        // 공개 메서드 - 대화 시작
        // =============================================================================

        /// <summary>
        /// DialogueData로 대화 시작
        /// </summary>
        /// <param name="dialogue">시작할 대화 데이터</param>
        public void StartDialogue(DialogueData dialogue)
        {
            if (dialogue == null)
            {
                Debug.LogWarning("[DialogueManager] 대화 데이터가 null입니다.");
                return;
            }

            if (IsDialogueActive)
            {
                Debug.LogWarning("[DialogueManager] 이미 대화가 진행 중입니다.");
                return;
            }

            // 대화 데이터 설정
            _currentDialogue = dialogue;
            _currentNodeIndex = 0;

            // 상태 변경
            _currentState = DialogueState.Playing;

            // 대사 시작 직후 클릭 오입력 방지
            _inputBlockUntil = Time.unscaledTime + InputBlockDuration;

            // 추리형 추적 초기화
            _pendingRetryChoice = null;

            // 카메라 매니저에 대화 시작 알림
            if (_cameraManager != null)
            {
                _cameraManager.OnDialogueStarted();
            }

            // 이벤트 발생
            OnDialogueStart?.Invoke();

            // 첫 번째 노드 표시
            DisplayCurrentNode();
        }

        /// <summary>
        /// ID로 대화 시작 (데이터베이스 필요)
        /// </summary>
        /// <param name="dialogueId">대화 ID</param>
        public void StartDialogueById(string dialogueId)
        {
            if (_dialogueDatabase == null)
            {
                Debug.LogError("[DialogueManager] 다이얼로그 데이터베이스가 설정되지 않았습니다.");
                return;
            }

            DialogueData dialogue = _dialogueDatabase.GetDialogueById(dialogueId);
            if (dialogue != null)
            {
                StartDialogue(dialogue);
            }
        }

        /// <summary>
        /// 선택지를 제공하는 NPC Transform 설정
        /// WorldSpaceChoiceUI 배치에 사용 (StartDialogue 전에 호출)
        /// </summary>
        /// <param name="npcTransform">NPC의 Transform</param>
        /// <param name="anchorTransform">앵커 본 Transform (머리/어깨 등). null이면 루트 + heightOffset 사용</param>
        public void SetNpcTransform(Transform npcTransform, Transform anchorTransform = null)
        {
            _currentNpcTransform = npcTransform;
            if (_worldSpaceChoiceUI != null)
                _worldSpaceChoiceUI.SetNpcTransform(npcTransform, anchorTransform);
        }

        /// <summary>
        /// 추리형 선택지 정답 추적기 등록
        /// WorldInteractable.StartDialogue()에서 호출 (StartDialogue 전에 호출해야 함)
        /// </summary>
        public void RegisterChoiceTracker(GameDatabase.Player.WorldInteractable tracker)
        {
            _choiceTracker = tracker;
        }

        /// <summary>
        /// 제목으로 대화 시작 (데이터베이스 필요)
        /// </summary>
        /// <param name="title">대화 제목</param>
        public void StartDialogueByTitle(string title)
        {
            if (_dialogueDatabase == null)
            {
                Debug.LogError("[DialogueManager] 다이얼로그 데이터베이스가 설정되지 않았습니다.");
                return;
            }

            DialogueData dialogue = _dialogueDatabase.GetDialogueByTitle(title);
            if (dialogue != null)
            {
                StartDialogue(dialogue);
            }
        }

        // =============================================================================
        // 공개 메서드 - 대화 제어
        // =============================================================================

        /// <summary>
        /// 다음 대사로 진행
        /// </summary>
        public void ProceedToNext()
        {
            if (_currentState == DialogueState.Inactive) return;

            // 타이핑 중이면 스킵
            if (_dialogueUI != null && _dialogueUI.IsTyping)
            {
                _dialogueUI.SkipTyping();
                return;
            }

            // 선택지 표시 중이면 무시
            if (_currentState == DialogueState.ShowingChoice) return;

            // 다음 노드로 이동
            _currentNodeIndex++;

            // 대화 종료 확인
            if (_currentDialogue.IsLastNode(_currentNodeIndex - 1))
            {
                EndDialogue();
                return;
            }

            // 다음 노드 표시
            DisplayCurrentNode();
        }

        /// <summary>
        /// 대화 강제 종료
        /// </summary>
        public void EndDialogue()
        {
            if (_currentState == DialogueState.Inactive) return;

            // 코루틴 정지
            if (_autoProceedCoroutine != null)
            {
                StopCoroutine(_autoProceedCoroutine);
                _autoProceedCoroutine = null;
            }

            // 완료 플래그 설정
            if (_currentDialogue != null && !string.IsNullOrEmpty(_currentDialogue.CompletionFlagKey))
            {
                SetFlag(_currentDialogue.CompletionFlagKey, _currentDialogue.CompletionFlagValue);
            }

            // 카메라 매니저에 대화 종료 알림
            if (_cameraManager != null)
            {
                _cameraManager.OnDialogueEnded();
            }

            // UI 숨김
            if (_dialogueUI != null) _dialogueUI.Hide();
            if (_choiceUI != null) _choiceUI.Hide();
            if (_worldSpaceChoiceUI != null) _worldSpaceChoiceUI.Hide();

            // 상태 초기화
            _currentState = DialogueState.Inactive;
            _currentDialogue = null;
            _currentNodeIndex = 0;
            _currentNpcTransform = null;

            // 추리형 상태 초기화
            // _pendingRetryChoice가 있으면 오답 대사가 끝난 후 선택지 재표시
            if (_pendingRetryChoice != null)
            {
                var retryChoice = _pendingRetryChoice;
                var savedTracker = _choiceTracker;
                _pendingRetryChoice = null;
                // OnDialogueEnd를 발생시키지 않고 선택지 재표시
                // (_pendingRetryDialogue/_pendingRetryNodeIndex는 RetryChoiceNextFrame에서 복원)
                StartCoroutine(RetryChoiceNextFrame(retryChoice, savedTracker));
                return;
            }

            _choiceTracker = null;

            // 이벤트 발생
            OnDialogueEnd?.Invoke();
        }

        /// <summary>
        /// 특정 노드로 점프
        /// </summary>
        /// <param name="nodeIndex">이동할 노드 인덱스</param>
        public void JumpToNode(int nodeIndex)
        {
            if (_currentDialogue == null) return;

            if (nodeIndex < 0 || nodeIndex >= _currentDialogue.NodeCount)
            {
                Debug.LogWarning($"[DialogueManager] 잘못된 노드 인덱스: {nodeIndex}");
                return;
            }

            _currentNodeIndex = nodeIndex;
            DisplayCurrentNode();
        }

        /// <summary>
        /// 다른 대화로 전환
        /// </summary>
        /// <param name="newDialogue">전환할 대화 데이터</param>
        public void SwitchToDialogue(DialogueData newDialogue)
        {
            if (newDialogue == null)
            {
                EndDialogue();
                return;
            }

            _currentDialogue = newDialogue;
            _currentNodeIndex = 0;
            DisplayCurrentNode();
        }

        // =============================================================================
        // 공개 메서드 - 플래그 관리
        // =============================================================================

        /// <summary>
        /// 플래그 설정 (GameStateManager에 위임 → 씬 전환 후에도 유지)
        /// </summary>
        public void SetFlag(string key, bool value)
        {
            if (string.IsNullOrEmpty(key)) return;
            GameStateManager.Instance.SetFlag(key, value);
        }

        /// <summary>
        /// 플래그 가져오기 (GameStateManager에 위임)
        /// </summary>
        public bool GetFlag(string key, bool defaultValue = false)
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;
            return GameStateManager.Instance.GetFlag(key, defaultValue);
        }

        /// <summary>
        /// 플래그 확인 (GameStateManager에 위임)
        /// </summary>
        public bool HasFlag(string key)
        {
            return GameStateManager.Instance.HasFlag(key);
        }

        // =============================================================================
        // 내부 메서드 - 노드 표시
        // =============================================================================

        /// <summary>
        /// 현재 노드 표시
        /// </summary>
        private void DisplayCurrentNode()
        {
            if (_currentDialogue == null) return;

            DialogueNode node = _currentDialogue.GetNode(_currentNodeIndex);
            if (node == null)
            {
                EndDialogue();
                return;
            }

            // 노드 타입에 따라 처리
            switch (node.NodeType)
            {
                case DialogueNodeType.Line:
                    DisplayLine(node.Line);
                    break;

                case DialogueNodeType.Choice:
                    DisplayChoice(node.Choice);
                    break;

                case DialogueNodeType.End:
                    EndDialogue();
                    break;
            }
        }

        /// <summary>
        /// 대사 라인 표시
        /// </summary>
        private void DisplayLine(DialogueLine line)
        {
            if (line == null)
            {
                ProceedToNext();
                return;
            }

            _currentState = DialogueState.Playing;

            // 선택지 UI 숨김
            if (_choiceUI != null) _choiceUI.Hide();

            // 대사 UI 표시
            if (_dialogueUI != null)
            {
                _dialogueUI.DisplayLine(line);
            }

            // 카메라 전환
            if (_cameraManager != null)
            {
                _cameraManager.OnLineDisplayed(line);
            }

            // 이벤트 발생
            OnLineDisplayed?.Invoke(line);

            // 자동 진행 설정
            if (line.HasVoice)
            {
                // 음성 클립이 있으면 클립 길이에 맞춰 자동 진행
                _autoProceedCoroutine = StartCoroutine(AutoProceedAfterDelay(line.VoiceDuration));
            }
            else if (line.AutoProceed)
            {
                _autoProceedCoroutine = StartCoroutine(AutoProceedAfterDelay(line.AutoProceedDelay));
            }
            else
            {
                _currentState = DialogueState.WaitingInput;
            }
        }

        /// <summary>
        /// 선택지 표시
        /// </summary>
        private void DisplayChoice(DialogueChoice choice)
        {
            if (choice == null || !choice.HasValidOptions)
            {
                Debug.LogWarning("[DialogueManager] 유효한 선택지가 없습니다.");
                ProceedToNext();
                return;
            }

            _currentState = DialogueState.ShowingChoice;

            // 레거시 ChoiceUI 숨김 (WorldSpaceChoiceUI로 대체됨)
            if (_choiceUI != null) _choiceUI.Hide();

            // 선택지 전용 Over-the-shoulder 카메라 활성화
            if (_cameraManager != null && _currentNpcTransform != null)
                _cameraManager.OnChoiceDisplayed(_currentNpcTransform);

            // 월드 스페이스 선택지 UI 표시
            // NPC Transform/앵커는 SetNpcTransform()에서 이미 WorldSpaceChoiceUI로 전달됨
            if (_worldSpaceChoiceUI != null)
            {
                _worldSpaceChoiceUI.DisplayChoice(choice);
            }
            else if (_choiceUI != null)
            {
                // WorldSpaceChoiceUI가 없으면 레거시 ChoiceUI 사용
                _choiceUI.DisplayChoice(choice);
            }

            // 이벤트 발생
            OnChoiceDisplayed?.Invoke(choice);
        }

        // =============================================================================
        // 내부 메서드 - 입력 처리
        // =============================================================================

        /// <summary>
        /// 입력 처리
        /// </summary>
        private void HandleInput()
        {
            if (_currentState == DialogueState.Inactive) return;
            if (_currentState == DialogueState.ShowingChoice) return;

            // 스킵 키
            if (Input.GetKeyDown(_skipKey))
            {
                if (_dialogueUI != null && _dialogueUI.IsTyping)
                {
                    _dialogueUI.SkipTyping();
                }
            }

            // 다음 진행 키
            if (Input.GetKeyDown(_nextKey))
            {
                ProceedToNext();
            }

            // 마우스 클릭 (대사 시작 직후 오입력 방지)
            if (_allowMouseClick && Input.GetMouseButtonDown(0) && Time.unscaledTime >= _inputBlockUntil)
            {
                ProceedToNext();
            }
        }

        // =============================================================================
        // 내부 메서드 - 이벤트 처리
        // =============================================================================

        /// <summary>
        /// UI 이벤트 연결
        /// </summary>
        private void SetupUIEvents()
        {
            // 레거시 선택지 UI 이벤트 (사용하는 경우)
            if (_choiceUI != null)
            {
                _choiceUI.OnChoiceSelected += HandleChoiceSelected;
                _choiceUI.OnTimeExpired += HandleChoiceTimeExpired;
            }

            // 월드 스페이스 선택지 UI 이벤트
            if (_worldSpaceChoiceUI != null)
            {
                _worldSpaceChoiceUI.OnChoiceSelected += HandleChoiceSelected;
            }

            // 다이얼로그 UI 이벤트
            if (_dialogueUI != null)
            {
                _dialogueUI.OnLineDisplayComplete += HandleLineDisplayComplete;
                _dialogueUI.OnVoiceComplete += HandleVoiceComplete;
            }
        }

        /// <summary>
        /// UI 이벤트 해제
        /// </summary>
        private void CleanupUIEvents()
        {
            if (_choiceUI != null)
            {
                _choiceUI.OnChoiceSelected -= HandleChoiceSelected;
                _choiceUI.OnTimeExpired -= HandleChoiceTimeExpired;
            }

            if (_worldSpaceChoiceUI != null)
            {
                _worldSpaceChoiceUI.OnChoiceSelected -= HandleChoiceSelected;
            }

            if (_dialogueUI != null)
            {
                _dialogueUI.OnLineDisplayComplete -= HandleLineDisplayComplete;
                _dialogueUI.OnVoiceComplete -= HandleVoiceComplete;
            }
        }

        /// <summary>
        /// 선택지 선택 처리 (정답/오답 분기 포함)
        /// </summary>
        private void HandleChoiceSelected(int optionIndex, ChoiceOption option)
        {
            // 선택지 카메라 해제 (다음 대사 카메라로 자연스럽게 블렌딩)
            if (_cameraManager != null)
                _cameraManager.OnChoiceEnded();

            // 이벤트 발생
            OnChoiceMade?.Invoke(optionIndex, option);

            if (option == null)
            {
                _currentNodeIndex++;
                DisplayCurrentNode();
                return;
            }

            // ── 추리형 정답/오답 판정 ──
            // 현재 선택지 노드에 IsCorrectAnswer가 설정된 옵션이 하나라도 있으면 추리형 모드
            DialogueNode currentNode = _currentDialogue?.GetNode(_currentNodeIndex);
            bool isQuizMode = currentNode != null && currentNode.IsChoice &&
                              currentNode.Choice != null && HasAnyCorrectAnswer(currentNode.Choice);

            if (isQuizMode)
            {
                if (!option.IsCorrectAnswer)
                {
                    // 오답: WrongAnswerDialogue 재생 후 이 선택지 노드 재표시
                    HandleWrongAnswer(currentNode.Choice);
                    return;
                }
                else
                {
                    // 정답: WorldInteractable에 알림
                    _choiceTracker?.OnChoiceSolved();
                }
            }

            // 결과 플래그 설정
            if (option.HasResultFlag)
            {
                SetFlag(option.ResultFlagKey, option.ResultFlagValue);
            }

            // 다음 대화 또는 노드로 이동
            if (option.HasNextDialogue)
            {
                SwitchToDialogue(option.NextDialogue);
            }
            else if (option.HasJumpIndex)
            {
                JumpToNode(option.JumpToLineIndex);
            }
            else
            {
                _currentNodeIndex++;
                DisplayCurrentNode();
            }
        }

        /// <summary>
        /// 선택지에 IsCorrectAnswer가 true인 옵션이 하나라도 있는지 확인
        /// </summary>
        private bool HasAnyCorrectAnswer(DialogueChoice choice)
        {
            if (choice?.Options == null) return false;
            foreach (var opt in choice.Options)
            {
                if (opt != null && opt.IsCorrectAnswer) return true;
            }
            return false;
        }

        /// <summary>
        /// 오답 처리: WrongAnswerDialogue 재생 후 선택지 재표시
        /// </summary>
        private void HandleWrongAnswer(DialogueChoice choice)
        {
            if (choice.WrongAnswerDialogue != null)
            {
                // 오답 대사로 전환하기 전에 현재 다이얼로그/노드 인덱스 보존
                // SwitchToDialogue가 _currentDialogue와 _currentNodeIndex를 덮어쓰기 때문
                _pendingRetryChoice = choice;
                _pendingRetryDialogue = _currentDialogue;
                _pendingRetryNodeIndex = _currentNodeIndex;
                SwitchToDialogue(choice.WrongAnswerDialogue);
            }
            else
            {
                // 오답 대사 없이 바로 선택지 재표시
                DisplayChoice(choice);
            }
        }

        /// <summary>
        /// 오답 대사 종료 후 1프레임 뒤에 선택지 재표시
        /// OnDialogueEnd를 발생시키지 않으므로 PlayerController는 대화 종료 처리하지 않음
        /// </summary>
        private IEnumerator RetryChoiceNextFrame(DialogueChoice choice, GameDatabase.Player.WorldInteractable tracker)
        {
            yield return null;

            // 추적기 복원
            _choiceTracker = tracker;

            // 원본 다이얼로그와 노드 인덱스 복원
            // HandleChoiceSelected에서 _currentDialogue.GetNode(_currentNodeIndex)로 isQuizMode를 판정하므로 반드시 복원
            _currentDialogue = _pendingRetryDialogue;
            _currentNodeIndex = _pendingRetryNodeIndex;
            _pendingRetryDialogue = null;

            // 선택지 재표시
            DisplayChoice(choice);
        }

        /// <summary>
        /// 선택지 시간 초과 처리
        /// </summary>
        private void HandleChoiceTimeExpired()
        {
            Debug.Log("[DialogueManager] 선택지 시간 초과");
            // 기본 선택이 ChoiceUI에서 자동으로 처리됨
        }

        /// <summary>
        /// 대사 표시 완료 처리
        /// </summary>
        private void HandleLineDisplayComplete()
        {
            _currentState = DialogueState.WaitingInput;
        }

        /// <summary>
        /// 음성 재생 완료 처리 (타이밍은 AutoProceedAfterDelay 코루틴에서 관리)
        /// </summary>
        private void HandleVoiceComplete() { }

        // =============================================================================
        // 코루틴
        // =============================================================================

        /// <summary>
        /// 지정된 시간 후 자동 진행
        /// </summary>
        private IEnumerator AutoProceedAfterDelay(float delay)
        {
            // 타이핑 완료 대기
            while (_dialogueUI != null && _dialogueUI.IsTyping)
            {
                yield return null;
            }

            // 추가 대기
            yield return new WaitForSeconds(delay);

            _autoProceedCoroutine = null;

            // 다음으로 진행
            if (_currentState == DialogueState.WaitingInput || _currentState == DialogueState.Playing)
            {
                ProceedToNext();
            }
        }
    }
}
