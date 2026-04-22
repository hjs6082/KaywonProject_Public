// =============================================================================
// PlayerInspection.cs
// =============================================================================
// 설명: 조사 모드 관리 컴포넌트
// 용도: 조사 모드 진입/종료, 마우스 레이캐스트, 오브젝트 회전 처리
// 작동 방식:
//   1. Tab 키로 조사 모드 진입 (커서 표시, 이동 비활성화)
//   2. 마우스 이동 시 IInspectable 오브젝트 감지
//   3. 좌클릭으로 조사, 드래그로 IRotatable 회전
//   4. Escape 또는 Tab으로 조사 모드 종료
// =============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace GameDatabase.Player
{
    /// <summary>
    /// 플레이어 조사 모드 관리 컴포넌트
    /// 마우스로 오브젝트를 조사하고 회전시킬 수 있습니다.
    /// </summary>
    public class PlayerInspection : MonoBehaviour
    {
        // =============================================================================
        // 참조
        // =============================================================================

        [Header("=== 참조 ===")]

        [Tooltip("PlayerController 참조 (자동 할당)")]
        [SerializeField] private PlayerController _controller;

        [Tooltip("메인 카메라 (자동 할당)")]
        [SerializeField] private Camera _mainCamera;

        // =============================================================================
        // 상태 (읽기 전용)
        // =============================================================================

        [Header("=== 현재 상태 (읽기 전용) ===")]

        [Tooltip("현재 호버된 오브젝트")]
        [SerializeField] private GameObject _hoveredObject;

        [Tooltip("현재 호버된 IInspectable 이름")]
        [SerializeField] private string _hoveredInspectableName;

        [Tooltip("현재 회전 중인 오브젝트")]
        [SerializeField] private GameObject _rotatingObject;

        [Tooltip("드래그 중 여부")]
        [SerializeField] private bool _isDragging;

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("조사 모드 진입 시 호출")]
        public UnityEvent OnInspectModeEnter;

        [Tooltip("조사 모드 종료 시 호출")]
        public UnityEvent OnInspectModeExit;

        [Tooltip("오브젝트 호버 시 호출 (제목, 설명)")]
        public UnityEvent<string, string> OnObjectHovered;

        [Tooltip("호버 해제 시 호출")]
        public UnityEvent OnHoverCleared;

        [Tooltip("오브젝트 조사 시 호출")]
        public UnityEvent<IInspectable> OnObjectInspected;

        [Tooltip("회전 시작 시 호출")]
        public UnityEvent<IRotatable> OnRotationStarted;

        [Tooltip("회전 종료 시 호출")]
        public UnityEvent OnRotationEnded;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        // 현재 호버/회전 중인 대상
        private IInspectable _currentHovered;
        private IRotatable _currentRotatable;

        // 레이캐스트
        private RaycastHit _hit;

        // 마우스 위치
        private Vector3 _lastMousePosition;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 조사 모드 활성화 여부 (Inspecting 또는 Investigating 상태)
        /// </summary>
        public bool IsInspectModeActive => _controller != null &&
            (_controller.IsInspecting || _controller.IsInvestigating);

        /// <summary>
        /// 현재 호버된 오브젝트
        /// </summary>
        public GameObject HoveredObject => _hoveredObject;

        /// <summary>
        /// 현재 호버된 IInspectable
        /// </summary>
        public IInspectable CurrentHovered => _currentHovered;

        /// <summary>
        /// 드래그 중 여부
        /// </summary>
        public bool IsDragging => _isDragging;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // 컴포넌트 자동 할당
            if (_controller == null)
            {
                _controller = GetComponentInParent<PlayerController>();
            }

            if (_mainCamera == null && _controller != null)
            {
                _mainCamera = _controller.MainCamera;
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            // 조사 모드 토글 입력
            HandleModeToggleInput();

            // 조사 모드가 아니면 종료
            if (!IsInspectModeActive) return;

            // 마우스 레이캐스트
            HandleMouseRaycast();

            // 클릭/드래그 처리
            HandleMouseInput();

            // 종료 입력 처리
            HandleExitInput();
        }

        // =============================================================================
        // 입력 처리
        // =============================================================================

        /// <summary>
        /// 조사 모드 토글 입력 처리
        /// </summary>
        private void HandleModeToggleInput()
        {
            if (_controller?.Data == null) return;

            // 대화 중이면 무시
            if (_controller.IsInDialogue) return;

            // 조사 구역 모드(Investigating)일 때는 Tab 토글 무시
            if (_controller.IsInvestigating) return;

            // 조사 모드 토글 키
            if (Input.GetKeyDown(_controller.Data.inspectModeKey))
            {
                _controller.ToggleInspectMode();

                if (_controller.IsInspecting)
                {
                    OnInspectModeEnter?.Invoke();
                }
                else
                {
                    ClearHover();
                    OnInspectModeExit?.Invoke();
                }
            }
        }

        /// <summary>
        /// 종료 입력 처리
        /// </summary>
        private void HandleExitInput()
        {
            if (_controller?.Data == null) return;

            // 조사 구역 모드(Investigating)일 때는 ESC 종료 불가
            if (_controller.IsInvestigating) return;

            // ESC 키로 종료
            if (Input.GetKeyDown(_controller.Data.inspectExitKey))
            {
                // 드래그 중이면 드래그 먼저 종료
                if (_isDragging)
                {
                    EndDrag();
                }
                else
                {
                    _controller.ExitInspectMode();
                    ClearHover();
                    OnInspectModeExit?.Invoke();
                }
            }
        }

        // =============================================================================
        // 마우스 레이캐스트
        // =============================================================================

        /// <summary>
        /// 마우스 레이캐스트 처리
        /// </summary>
        private void HandleMouseRaycast()
        {
            if (_mainCamera == null || _controller?.Data == null) return;

            // 드래그 중에는 레이캐스트 생략
            if (_isDragging) return;

            // 마우스 위치에서 레이캐스트
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            LayerMask layer = _controller.Data.inspectableLayer;

            if (Physics.Raycast(ray, out _hit, 100f, layer))
            {
                // IInspectable 찾기
                IInspectable inspectable = _hit.collider.GetComponent<IInspectable>();
                if (inspectable == null)
                {
                    inspectable = _hit.collider.GetComponentInParent<IInspectable>();
                }

                if (inspectable != null && inspectable.CanInspect)
                {
                    // 새로운 대상이면 호버 변경
                    if (inspectable != _currentHovered)
                    {
                        SetHover(inspectable, _hit.collider.gameObject);
                    }
                }
                else
                {
                    ClearHover();
                }
            }
            else
            {
                ClearHover();
            }
        }

        /// <summary>
        /// 호버 설정
        /// </summary>
        private void SetHover(IInspectable inspectable, GameObject gameObject)
        {
            // 이전 호버 해제
            ClearHover();

            // 새 호버 설정
            _currentHovered = inspectable;
            _hoveredObject = gameObject;
            _hoveredInspectableName = gameObject.name;

            // 호버 시작 호출
            _currentHovered.OnHoverEnter();

            // 커서 변경
            UpdateCursor();

            // 이벤트 발생
            OnObjectHovered?.Invoke(inspectable.InspectTitle, inspectable.InspectDescription);
        }

        /// <summary>
        /// 호버 해제
        /// </summary>
        private void ClearHover()
        {
            if (_currentHovered == null) return;

            // 호버 종료 호출
            _currentHovered.OnHoverExit();

            // 초기화
            _currentHovered = null;
            _hoveredObject = null;
            _hoveredInspectableName = "";

            // 커서 복원
            if (_controller?.Cursor != null)
            {
                _controller.Cursor.SetCursorState(CursorState.Normal);
            }

            // 이벤트 발생
            OnHoverCleared?.Invoke();
        }

        /// <summary>
        /// 커서 업데이트
        /// </summary>
        private void UpdateCursor()
        {
            if (_controller?.Cursor == null) return;

            // IRotatable이면 잡기 커서
            if (_currentHovered is IRotatable)
            {
                _controller.Cursor.SetCursorState(CursorState.Grab);
            }
            else
            {
                _controller.Cursor.SetCursorState(CursorState.Inspect);
            }
        }

        // =============================================================================
        // 마우스 클릭/드래그
        // =============================================================================

        /// <summary>
        /// 마우스 입력 처리
        /// </summary>
        private void HandleMouseInput()
        {
            // 좌클릭 시작
            if (Input.GetMouseButtonDown(0))
            {
                OnMouseDown();
            }

            // 드래그 중
            if (_isDragging && Input.GetMouseButton(0))
            {
                OnMouseDrag();
            }

            // 좌클릭 종료
            if (Input.GetMouseButtonUp(0))
            {
                OnMouseUp();
            }

            // 마우스 위치 저장
            _lastMousePosition = Input.mousePosition;
        }

        /// <summary>
        /// 마우스 버튼 다운
        /// </summary>
        private void OnMouseDown()
        {
            if (_currentHovered == null) return;

            _lastMousePosition = Input.mousePosition;

            // IRotatable이면 드래그 시작
            if (_currentHovered is IRotatable rotatable && rotatable.CanRotate)
            {
                StartDrag(rotatable);
            }
        }

        /// <summary>
        /// 마우스 드래그
        /// </summary>
        private void OnMouseDrag()
        {
            if (_currentRotatable == null) return;

            // 마우스 이동량 계산
            Vector3 currentMousePos = Input.mousePosition;
            Vector2 delta = new Vector2(
                currentMousePos.x - _lastMousePosition.x,
                currentMousePos.y - _lastMousePosition.y
            );

            // 감도 적용
            if (_controller?.Data != null)
            {
                delta *= _controller.Data.rotationSensitivity;
            }

            // 회전 처리
            _currentRotatable.OnRotate(delta);
        }

        /// <summary>
        /// 마우스 버튼 업
        /// </summary>
        private void OnMouseUp()
        {
            // 드래그 중이면 드래그 종료
            if (_isDragging)
            {
                EndDrag();
            }
            // 아니면 조사 실행
            else if (_currentHovered != null)
            {
                InspectCurrent();
            }
        }

        /// <summary>
        /// 드래그 시작
        /// </summary>
        private void StartDrag(IRotatable rotatable)
        {
            _isDragging = true;
            _currentRotatable = rotatable;
            _rotatingObject = _hoveredObject;

            // 회전 시작 호출
            _currentRotatable.OnRotateStart();

            // 상태 변경
            if (_controller != null)
            {
                _controller.SetState(PlayerState.RotatingObject);
            }

            // 커서 변경
            if (_controller?.Cursor != null)
            {
                _controller.Cursor.SetCursorState(CursorState.Grabbing);
            }

            // 이벤트 발생
            OnRotationStarted?.Invoke(rotatable);
        }

        /// <summary>
        /// 드래그 종료
        /// </summary>
        private void EndDrag()
        {
            if (!_isDragging) return;

            _isDragging = false;

            // 회전 종료 호출
            if (_currentRotatable != null)
            {
                _currentRotatable.OnRotateEnd();
            }

            _currentRotatable = null;
            _rotatingObject = null;

            // 상태 복원
            if (_controller != null)
            {
                _controller.SetState(PlayerState.Inspecting);
            }

            // 커서 복원
            UpdateCursor();

            // 이벤트 발생
            OnRotationEnded?.Invoke();
        }

        /// <summary>
        /// 현재 호버된 오브젝트 조사
        /// </summary>
        private void InspectCurrent()
        {
            if (_currentHovered == null) return;
            if (!_currentHovered.CanInspect) return;

            // 조사 실행
            _currentHovered.OnInspect(_controller);

            // 이벤트 발생
            OnObjectInspected?.Invoke(_currentHovered);

            Debug.Log($"[PlayerInspection] '{_hoveredInspectableName}' 조사 완료");
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 조사 모드 진입
        /// </summary>
        public void EnterInspectMode()
        {
            if (_controller != null)
            {
                _controller.EnterInspectMode();
                OnInspectModeEnter?.Invoke();
            }
        }

        /// <summary>
        /// 조사 모드 종료
        /// </summary>
        public void ExitInspectMode()
        {
            if (_isDragging)
            {
                EndDrag();
            }

            ClearHover();

            if (_controller != null)
            {
                _controller.ExitInspectMode();
                OnInspectModeExit?.Invoke();
            }
        }

        /// <summary>
        /// 특정 오브젝트 강제 조사
        /// </summary>
        public void ForceInspect(GameObject target)
        {
            if (target == null) return;

            IInspectable inspectable = target.GetComponent<IInspectable>();
            if (inspectable == null)
            {
                inspectable = target.GetComponentInParent<IInspectable>();
            }

            if (inspectable != null && inspectable.CanInspect)
            {
                inspectable.OnInspect(_controller);
                OnObjectInspected?.Invoke(inspectable);
            }
        }
    }
}
