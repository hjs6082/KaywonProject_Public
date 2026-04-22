// =============================================================================
// PlayerController.cs
// =============================================================================
// 설명: 통합 플레이어 컨트롤러 (이동 + 카메라 + 조사 통합)
// 용도: Third Person Controller 기반 이동/카메라 + 조사/상호작용 시스템
// 참고: Unity ThirdPersonController + BasicRigidBodyPush + StarterAssetsInputs 통합
// =============================================================================

using UnityEngine;
using Cinemachine;
using GameDatabase.UI;

namespace GameDatabase.Player
{
    /// <summary>
    /// 통합 플레이어 컨트롤러
    /// 이동, 카메라, 조사 기능을 하나의 스크립트로 처리합니다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        // =============================================================================
        // 싱글톤
        // =============================================================================

        private static PlayerController _instance;
        public static PlayerController Instance => _instance;

        // =============================================================================
        // 플레이어 데이터
        // =============================================================================

        [Header("=== 플레이어 데이터 ===")]

        [Tooltip("플레이어 데이터 ScriptableObject (선택적)")]
        [SerializeField] private PlayerData _playerData;

        // =============================================================================
        // 이동 설정
        // =============================================================================

        [Header("=== 이동 설정 ===")]

        [Tooltip("걷기 속도 (m/s)")]
        public float MoveSpeed = 2.0f;

        [Tooltip("달리기 속도 (m/s)")]
        public float SprintSpeed = 5.335f;

        [Tooltip("회전 부드러움 (낮을수록 빠르게 회전)")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("가속/감속 속도")]
        public float SpeedChangeRate = 10.0f;

        // =============================================================================
        // 점프 및 중력
        // =============================================================================

        [Header("=== 점프 및 중력 ===")]

        [Tooltip("점프 높이")]
        public float JumpHeight = 1.2f;

        [Tooltip("중력 값")]
        public float Gravity = -15.0f;

        [Tooltip("점프 쿨다운")]
        public float JumpTimeout = 0.50f;

        [Tooltip("낙하 전환 시간")]
        public float FallTimeout = 0.15f;

        // =============================================================================
        // 지면 체크
        // =============================================================================

        [Header("=== 지면 체크 ===")]

        [Tooltip("지면에 있는지 여부")]
        public bool Grounded = true;

        [Tooltip("지면 체크 오프셋")]
        public float GroundedOffset = -0.14f;

        [Tooltip("지면 체크 반경")]
        public float GroundedRadius = 0.28f;

        [Tooltip("지면 레이어")]
        public LayerMask GroundLayers;

        // =============================================================================
        // 카메라 설정
        // =============================================================================

        [Header("=== 카메라 설정 ===")]

        [Tooltip("Cinemachine 카메라 타겟 (CameraRoot)")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("상하 최대 각도")]
        public float TopClamp = 70.0f;

        [Tooltip("상하 최소 각도")]
        public float BottomClamp = -30.0f;

        [Tooltip("카메라 각도 오프셋")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("카메라 위치 고정")]
        public bool LockCameraPosition = false;

        [Tooltip("마우스 감도")]
        [Range(0.1f, 10f)]
        public float MouseSensitivity = 2.0f;

        // =============================================================================
        // 조사 시스템
        // =============================================================================

        [Header("=== 조사 시스템 ===")]

        [Tooltip("조사 모드 토글 키")]
        public KeyCode InspectModeKey = KeyCode.Tab;

        [Tooltip("조사 모드 종료 키")]
        public KeyCode InspectExitKey = KeyCode.Escape;

        [Tooltip("조사 가능 레이어")]
        public LayerMask InspectableLayer;

        // =============================================================================
        // 상호작용 시스템
        // =============================================================================

        [Header("=== 상호작용 시스템 ===")]

        [Tooltip("상호작용 키")]
        public KeyCode InteractKey = KeyCode.E;

        [Tooltip("상호작용 거리")]
        public float InteractionRange = 3f;

        [Tooltip("상호작용 레이어")]
        public LayerMask InteractableLayer;

        // =============================================================================
        // 컴포넌트 참조
        // =============================================================================

        [Header("=== 컴포넌트 참조 ===")]

        [Tooltip("조사 컴포넌트 (선택)")]
        public PlayerInspection Inspection;

        [Tooltip("상호작용 컴포넌트 (선택)")]
        public PlayerInteraction Interaction;

        [Tooltip("커서 컴포넌트 (선택)")]
        public PlayerCursor Cursor;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        // 카메라
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // 이동
        private float _speed;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // 타임아웃
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // 컴포넌트
        private CharacterController _controller;
        private GameObject _mainCamera;
        private Animator _animator;
        private bool _hasAnimator;

        // 입력
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _jumpInput;
        private bool _sprintInput;

        // 상태
        private PlayerState _currentState = PlayerState.Idle;
        private bool _inputEnabled = true;

        private const float _inputThreshold = 0.01f;

        // 애니메이션 ID
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        public PlayerData Data => _playerData;
        public PlayerState CurrentState => _currentState;
        public bool InputEnabled => _inputEnabled;
        public bool CanMove => _inputEnabled && (_currentState == PlayerState.Idle || _currentState == PlayerState.Walking || _currentState == PlayerState.Running);
        public bool CanLook => _inputEnabled && !IsInspecting;
        public bool IsInspecting => _currentState == PlayerState.Inspecting || _currentState == PlayerState.RotatingObject;
        public bool IsInDialogue => _currentState == PlayerState.InDialogue;
        public bool IsRunning => _currentState == PlayerState.Running;
        public bool IsInvestigating => _currentState == PlayerState.Investigating;
        public bool IsOnCaseBoard => _currentState == PlayerState.CaseBoard;
        public Camera MainCamera => _mainCamera != null ? _mainCamera.GetComponent<Camera>() : null;
        public CharacterController CharacterController => _controller;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // 싱글톤
            if (_instance == null) _instance = this;
            else if (_instance != this) Destroy(gameObject);

            // 메인 카메라
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            // 컴포넌트
            _controller = GetComponent<CharacterController>();
            _hasAnimator = TryGetComponent(out _animator);

            // 조사/상호작용 컴포넌트 찾기
            if (Inspection == null) Inspection = GetComponent<PlayerInspection>();
            if (Interaction == null) Interaction = GetComponent<PlayerInteraction>();
            if (Cursor == null) Cursor = GetComponent<PlayerCursor>();

            // CameraRoot 자동 생성
            if (CinemachineCameraTarget == null)
            {
                Transform existingRoot = transform.Find("CameraRoot");
                if (existingRoot != null)
                {
                    CinemachineCameraTarget = existingRoot.gameObject;
                }
                else
                {
                    GameObject root = new GameObject("CameraRoot");
                    root.transform.SetParent(transform);
                    root.transform.localPosition = new Vector3(0, 1.5f, 0);
                    CinemachineCameraTarget = root;
                }
            }
        }

        private void Start()
        {
            // 초기 카메라 회전
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            // 애니메이션 ID
            if (_hasAnimator)
            {
                _animIDSpeed = Animator.StringToHash("Speed");
                _animIDGrounded = Animator.StringToHash("Grounded");
                _animIDJump = Animator.StringToHash("Jump");
                _animIDFreeFall = Animator.StringToHash("FreeFall");
                _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            }

            // 타임아웃 초기화
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            // DialogueManager 연결
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueStart.AddListener(OnDialogueStarted);
                DialogueManager.Instance.OnDialogueEnd.AddListener(OnDialogueEnded);
            }
        }

        private void Update()
        {
            // 입력 수집
            GatherInput();

            // 지면 체크
            GroundedCheck();

            // 점프 및 중력
            JumpAndGravity();

            // 이동
            Move();

            // 조사 모드 토글
            HandleInspectMode();
        }

        private void LateUpdate()
        {
            // 카메라 회전
            CameraRotation();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;

            // DialogueManager 연결 해제
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueStart.RemoveListener(OnDialogueStarted);
                DialogueManager.Instance.OnDialogueEnd.RemoveListener(OnDialogueEnded);
            }
        }

        // =============================================================================
        // 입력 처리
        // =============================================================================

        private void GatherInput()
        {
            if (!_inputEnabled)
            {
                _moveInput = Vector2.zero;
                _lookInput = Vector2.zero;
                _jumpInput = false;
                _sprintInput = false;
                return;
            }

            // 이동 입력
            if (CanMove)
            {
                _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                _sprintInput = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                _jumpInput = Input.GetKeyDown(KeyCode.Space);
            }
            else
            {
                _moveInput = Vector2.zero;
                _sprintInput = false;
                _jumpInput = false;
            }

            // 카메라 입력
            if (CanLook)
            {
                _lookInput = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y")) * MouseSensitivity;
            }
            else
            {
                _lookInput = Vector2.zero;
            }
        }

        // =============================================================================
        // 이동
        // =============================================================================

        private void Move()
        {
            if (!CanMove)
            {
                _speed = 0f;
                return;
            }

            // 목표 속도
            float targetSpeed = _sprintInput ? SprintSpeed : MoveSpeed;
            if (_moveInput == Vector2.zero) targetSpeed = 0.0f;

            // 현재 수평 속도
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _moveInput.magnitude;

            // 가속/감속
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // 이동 방향
            Vector3 inputDirection = new Vector3(_moveInput.x, 0.0f, _moveInput.y).normalized;

            // 회전 (입력이 있을 때만)
            if (_moveInput != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // 이동 수행
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // 상태 업데이트
            UpdateMovementState();

            // 애니메이션
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _speed);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void UpdateMovementState()
        {
            if (!CanMove) return;

            if (_moveInput != Vector2.zero)
            {
                if (_sprintInput) SetState(PlayerState.Running);
                else SetState(PlayerState.Walking);
            }
            else
            {
                SetState(PlayerState.Idle);
            }
        }

        // =============================================================================
        // 점프 및 중력
        // =============================================================================

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // 점프
                if (_jumpInput && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                _jumpInput = false;
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        // =============================================================================
        // 카메라
        // =============================================================================

        private void CameraRotation()
        {
            if (!CanLook || LockCameraPosition) return;

            if (_lookInput.sqrMagnitude >= _inputThreshold)
            {
                _cinemachineTargetYaw += _lookInput.x;
                _cinemachineTargetPitch += _lookInput.y;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        // =============================================================================
        // 조사 모드
        // =============================================================================

        private void HandleInspectMode()
        {
            // 노트북이 열려있으면 조사 모드 입력 무시 (Tab 키 충돌 방지)
            if (EvidenceNotebookManager.Instance != null && EvidenceNotebookManager.Instance.IsNotebookOpen)
                return;

            // 사건 보드가 열려있으면 조사 모드 입력 무시
            if (_currentState == PlayerState.CaseBoard)
                return;

            if (Input.GetKeyDown(InspectModeKey))
            {
                if (!IsInspecting) EnterInspectMode();
            }

            if (Input.GetKeyDown(InspectExitKey))
            {
                if (IsInspecting) ExitInspectMode();
            }
        }

        public void EnterInspectMode()
        {
            if (_currentState == PlayerState.InDialogue || _currentState == PlayerState.Paused) return;

            SetState(PlayerState.Inspecting);

            if (Cursor != null)
            {
                Cursor.SetCursorState(CursorState.Normal);
            }
        }

        public void ExitInspectMode()
        {
            if (!IsInspecting) return;

            SetState(PlayerState.Idle);

            if (Cursor != null)
            {
                Cursor.SetCursorState(CursorState.Hidden);
            }
        }

        public void ToggleInspectMode()
        {
            if (IsInspecting)
            {
                ExitInspectMode();
            }
            else
            {
                EnterInspectMode();
            }
        }

        // =============================================================================
        // 이동/카메라 제어
        // =============================================================================

        public void SetMovementEnabled(bool enabled)
        {
            // 통합 컨트롤러에서는 LockCameraPosition으로 제어
            // 조사 구역 모드에서 이동을 막을 때 사용
            if (!enabled)
            {
                SetState(PlayerState.Investigating);
            }
            else
            {
                SetState(PlayerState.Idle);
            }
        }

        /// <summary>
        /// 대화 스탠드 포인트로 즉시 텔레포트하고 NPC 방향을 바라봄
        /// CharacterController는 이동 전 비활성화 후 재활성화 필요
        /// </summary>
        /// <param name="standPoint">이동할 위치와 방향 Transform</param>
        /// <param name="lookTarget">바라볼 대상 위치 (보통 NPC Transform.position)</param>
        public void TeleportToDialoguePosition(Transform standPoint, Vector3 lookTarget)
        {
            if (standPoint == null) return;

            // CharacterController가 활성화된 상태에서 position을 직접 바꾸면 무시되므로 일시 비활성화
            _controller.enabled = false;
            transform.position = standPoint.position;
            _controller.enabled = true;

            // NPC 방향으로 회전 (Y축만)
            Vector3 dir = lookTarget - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
                // Cinemachine 카메라 Yaw도 동기화
                _cinemachineTargetYaw = transform.eulerAngles.y;
            }
        }

        public void SetCameraEnabled(bool enabled)
        {
            LockCameraPosition = !enabled;
        }

        // =============================================================================
        // 상태 관리
        // =============================================================================

        public void SetState(PlayerState newState)
        {
            if (_currentState == newState) return;
            _currentState = newState;
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
        }

        // =============================================================================
        // 대화 시스템 연동
        // =============================================================================

        private void OnDialogueStarted()
        {
            // 조사 모드 중이면 커서를 숨기지 않음 (조사 모드 복귀 시 커서 필요)
            bool wasInvestigating = _currentState == PlayerState.Investigating;

            SetState(PlayerState.InDialogue);
            SetInputEnabled(false);

            if (Cursor != null && !wasInvestigating)
            {
                Cursor.SetCursorState(CursorState.Hidden);
            }
        }

        private void OnDialogueEnded()
        {
            // InvestigationManager가 조사 모드 복귀를 처리하므로, 조사 중이면 여기서 상태를 변경하지 않음
            if (InvestigationManager.Instance != null && InvestigationManager.Instance.IsInvestigating)
            {
                // 조사 모드 복귀는 InvestigationManager.OnDialogueEnded()에서 처리
                return;
            }

            SetState(PlayerState.Idle);
            SetInputEnabled(true);

            if (Cursor != null)
            {
                Cursor.SetCursorState(CursorState.Hidden);
            }
        }

        // =============================================================================
        // Gizmo
        // =============================================================================

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded ? transparentGreen : transparentRed;

            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }
    }
}
