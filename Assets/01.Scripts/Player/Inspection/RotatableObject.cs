// =============================================================================
// RotatableObject.cs
// =============================================================================
// 설명: 조사 모드에서 마우스 드래그로 회전시킬 수 있는 오브젝트
// 용도: 아이템 상세 조사, 3D 모델 회전 확인 등
// 작동 방식:
//   1. 조사 모드에서 오브젝트 클릭 & 드래그
//   2. 마우스 이동량에 따라 오브젝트 회전
//   3. 드래그 종료 시 현재 회전 유지 또는 초기화
// =============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace GameDatabase.Player
{
    /// <summary>
    /// 회전 가능한 오브젝트 컴포넌트
    /// 조사 모드에서 마우스 드래그로 회전시킬 수 있습니다.
    /// </summary>
    public class RotatableObject : MonoBehaviour, IRotatable
    {
        // =============================================================================
        // 조사 정보
        // =============================================================================

        [Header("=== 조사 정보 ===")]

        [Tooltip("조사 제목 (오브젝트 이름)")]
        [SerializeField] private string _inspectTitle = "회전 가능 오브젝트";

        [Tooltip("조사 설명 (자세한 내용)")]
        [TextArea(3, 5)]
        [SerializeField] private string _inspectDescription = "마우스 드래그로 회전시킬 수 있습니다.";

        [Tooltip("조사 가능 여부")]
        [SerializeField] private bool _canInspect = true;

        [Tooltip("회전 가능 여부")]
        [SerializeField] private bool _canRotate = true;

        // =============================================================================
        // 회전 설정
        // =============================================================================

        [Header("=== 회전 설정 ===")]

        [Tooltip("회전 감도")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _rotationSensitivity = 1f;

        [Tooltip("X축 회전 허용 (좌우 드래그)")]
        [SerializeField] private bool _allowRotationX = true;

        [Tooltip("Y축 회전 허용 (상하 드래그)")]
        [SerializeField] private bool _allowRotationY = true;

        [Tooltip("회전 축 반전")]
        [SerializeField] private bool _invertRotation = false;

        [Tooltip("회전 각도 제한 사용")]
        [SerializeField] private bool _useRotationLimits = false;

        [Tooltip("최소 회전 각도 (제한 사용 시)")]
        [SerializeField] private Vector3 _minRotation = new Vector3(-180f, -180f, -180f);

        [Tooltip("최대 회전 각도 (제한 사용 시)")]
        [SerializeField] private Vector3 _maxRotation = new Vector3(180f, 180f, 180f);

        [Tooltip("드래그 종료 시 원래 회전으로 복귀")]
        [SerializeField] private bool _resetOnRelease = false;

        [Tooltip("부드러운 회전 사용")]
        [SerializeField] private bool _smoothRotation = true;

        [Tooltip("부드러운 회전 속도")]
        [Range(1f, 30f)]
        [SerializeField] private float _smoothSpeed = 10f;

        // =============================================================================
        // 시각적 피드백
        // =============================================================================

        [Header("=== 시각적 피드백 ===")]

        [Tooltip("호버 시 하이라이트할 렌더러")]
        [SerializeField] private Renderer _highlightRenderer;

        [Tooltip("호버 하이라이트 색상")]
        [SerializeField] private Color _hoverColor = new Color(0.8f, 1f, 0.8f, 1f);

        [Tooltip("회전 중 색상")]
        [SerializeField] private Color _rotatingColor = new Color(0.5f, 1f, 0.5f, 1f);

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("조사 시 호출")]
        public UnityEvent OnInspected;

        [Tooltip("회전 시작 시 호출")]
        public UnityEvent OnRotationStarted;

        [Tooltip("회전 중 호출 (회전 각도)")]
        public UnityEvent<Vector3> OnRotating;

        [Tooltip("회전 종료 시 호출")]
        public UnityEvent OnRotationEnded;

        [Tooltip("회전 초기화 시 호출")]
        public UnityEvent OnRotationReset;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private Quaternion _initialRotation;
        private Vector3 _currentEulerRotation;
        private Vector3 _targetEulerRotation;
        private Color _originalColor;
        private bool _isHovered = false;
        private bool _isRotating = false;

        // =============================================================================
        // IInspectable 구현
        // =============================================================================

        public string InspectTitle => _inspectTitle;
        public string InspectDescription => _inspectDescription;
        public bool CanInspect => _canInspect;

        public void OnInspect(PlayerController player)
        {
            if (!CanInspect) return;

            OnInspected?.Invoke();
            Debug.Log($"[RotatableObject] '{_inspectTitle}' 조사됨");
        }

        public void OnHoverEnter()
        {
            if (_isHovered) return;
            _isHovered = true;

            if (_highlightRenderer != null)
            {
                _highlightRenderer.material.color = _hoverColor;
            }
        }

        public void OnHoverExit()
        {
            if (!_isHovered) return;
            _isHovered = false;

            if (_highlightRenderer != null && !_isRotating)
            {
                _highlightRenderer.material.color = _originalColor;
            }
        }

        // =============================================================================
        // IRotatable 구현
        // =============================================================================

        /// <summary>
        /// 현재 회전 각도
        /// </summary>
        public Vector3 CurrentRotation => _currentEulerRotation;

        /// <summary>
        /// 회전 가능 여부
        /// </summary>
        public bool CanRotate => _canRotate;

        /// <summary>
        /// 회전 시작
        /// </summary>
        public void OnRotateStart()
        {
            if (!CanRotate) return;

            _isRotating = true;

            // 회전 중 색상
            if (_highlightRenderer != null)
            {
                _highlightRenderer.material.color = _rotatingColor;
            }

            OnRotationStarted?.Invoke();
        }

        /// <summary>
        /// 회전 처리
        /// </summary>
        /// <param name="delta">마우스 이동량</param>
        public void OnRotate(Vector2 delta)
        {
            if (!_isRotating || !CanRotate) return;

            float sensitivity = _rotationSensitivity;
            float invertMultiplier = _invertRotation ? -1f : 1f;

            // 회전 계산
            float rotationX = _allowRotationY ? delta.y * sensitivity * invertMultiplier : 0f;
            float rotationY = _allowRotationX ? -delta.x * sensitivity * invertMultiplier : 0f;

            _targetEulerRotation.x += rotationX;
            _targetEulerRotation.y += rotationY;

            // 각도 제한 적용
            if (_useRotationLimits)
            {
                _targetEulerRotation.x = Mathf.Clamp(_targetEulerRotation.x, _minRotation.x, _maxRotation.x);
                _targetEulerRotation.y = Mathf.Clamp(_targetEulerRotation.y, _minRotation.y, _maxRotation.y);
                _targetEulerRotation.z = Mathf.Clamp(_targetEulerRotation.z, _minRotation.z, _maxRotation.z);
            }

            // 이벤트 발생
            OnRotating?.Invoke(_targetEulerRotation);
        }

        /// <summary>
        /// 회전 종료
        /// </summary>
        public void OnRotateEnd()
        {
            if (!_isRotating) return;

            _isRotating = false;

            // 색상 복원
            if (_highlightRenderer != null)
            {
                _highlightRenderer.material.color = _isHovered ? _hoverColor : _originalColor;
            }

            // 원래 회전으로 복귀
            if (_resetOnRelease)
            {
                ResetRotation();
            }

            OnRotationEnded?.Invoke();
        }

        /// <summary>
        /// 회전 초기화
        /// </summary>
        public void ResetRotation()
        {
            _targetEulerRotation = Vector3.zero;
            _currentEulerRotation = Vector3.zero;

            if (!_smoothRotation)
            {
                transform.rotation = _initialRotation;
            }

            OnRotationReset?.Invoke();
        }

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Start()
        {
            // 초기 상태 저장
            _initialRotation = transform.rotation;
            _currentEulerRotation = Vector3.zero;
            _targetEulerRotation = Vector3.zero;

            if (_highlightRenderer != null)
            {
                _originalColor = _highlightRenderer.material.color;
            }
        }

        private void Update()
        {
            // 부드러운 회전 적용
            if (_smoothRotation)
            {
                _currentEulerRotation = Vector3.Lerp(
                    _currentEulerRotation,
                    _targetEulerRotation,
                    _smoothSpeed * Time.deltaTime
                );
            }
            else
            {
                _currentEulerRotation = _targetEulerRotation;
            }

            // 회전 적용
            transform.rotation = _initialRotation * Quaternion.Euler(_currentEulerRotation);
        }

        private void OnDisable()
        {
            if (_isRotating)
            {
                OnRotateEnd();
            }
            if (_isHovered)
            {
                OnHoverExit();
            }
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// 특정 각도로 회전 설정
        /// </summary>
        /// <param name="eulerAngles">설정할 각도</param>
        public void SetRotation(Vector3 eulerAngles)
        {
            _targetEulerRotation = eulerAngles;

            if (!_smoothRotation)
            {
                _currentEulerRotation = eulerAngles;
                transform.rotation = _initialRotation * Quaternion.Euler(_currentEulerRotation);
            }
        }

        /// <summary>
        /// 회전 감도 설정
        /// </summary>
        public void SetSensitivity(float sensitivity)
        {
            _rotationSensitivity = sensitivity;
        }

        /// <summary>
        /// 회전 가능 여부 설정
        /// </summary>
        public void SetCanRotate(bool canRotate)
        {
            _canRotate = canRotate;
        }
    }
}
