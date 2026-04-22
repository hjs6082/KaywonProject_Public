// =============================================================================
// LabelObject.cs
// =============================================================================
// 설명: 드래그 가능한 라벨 오브젝트 (A / B / C)
// 용도: 마우스로 드래그해서 MemoObject 위에 드롭
// 작동 방식:
//   1. 마우스 클릭 → 드래그 시작, 카메라 평면 기준으로 3D 이동
//   2. 마우스 릴리즈 → MemoObject 위이면 드롭 판정, 아니면 원위치 복귀
//   3. 정답이면 MemoObject에 부착, 오답이면 DOTween으로 원위치 복귀
// =============================================================================

using UnityEngine;
using DG.Tweening;
using GameDatabase.Player;

namespace GameDatabase.Labeling
{
    /// <summary>
    /// 드래그 가능한 라벨 오브젝트
    /// </summary>
    public class LabelObject : MonoBehaviour
    {
        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== 라벨 설정 ===")]

        [Tooltip("라벨 ID (예: A, B, C) - MemoObject의 CorrectLabelId와 매칭")]
        [SerializeField] private string _labelId;

        [Tooltip("드래그 중 Y 오프셋 (살짝 들어올리는 효과)")]
        [SerializeField] private float _dragYOffset = 0.3f;

        [Tooltip("원위치 복귀 애니메이션 시간")]
        [SerializeField] private float _returnDuration = 0.3f;

        [Tooltip("오답 시 튕겨남 거리")]
        [SerializeField] private float _bounceDist = 0.5f;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        // 원래 위치/회전
        private Vector3 _originPosition;
        private Quaternion _originRotation;

        // 드래그 상태
        private bool _isDragging = false;
        private bool _isAttached = false;

        // 드래그 시 카메라 평면 거리 (카메라 → 오브젝트)
        private float _dragPlaneDistance;

        // 현재 호버 중인 메모
        private MemoObject _hoveredMemo;

        // Collider (드롭 감지용)
        private Collider _collider;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        public string LabelId => _labelId;
        public bool IsAttached => _isAttached;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            _originPosition = transform.position;
            _originRotation = transform.rotation;
            _collider = GetComponent<Collider>();
        }

        private void Update()
        {
            if (!_isDragging) return;
            FollowMouse();
        }

        private void OnMouseDown()
        {
            Debug.Log($"[LabelObject] OnMouseDown: {_labelId}, IsLabeling: {LabelingManager.Instance?.IsLabeling}");
            // 라벨링 모드가 아니거나 이미 부착됐으면 무시
            if (LabelingManager.Instance == null || !LabelingManager.Instance.IsLabeling) return;
            if (_isAttached) return;

            StartDrag();
        }

        private void OnMouseUp()
        {
            Debug.Log($"[LabelObject] OnMouseUp: {_labelId}, isDragging: {_isDragging}, hoveredMemo: {_hoveredMemo?.gameObject.name}");
            if (!_isDragging) return;
            EndDrag();
        }

        // =============================================================================
        // 드래그
        // =============================================================================

        private void StartDrag()
        {
            _isDragging = true;

            // 드래그 평면 높이 = 오브젝트의 Y + 오프셋
            _dragPlaneDistance = transform.position.y + _dragYOffset;

            Debug.Log($"[LabelObject] 드래그 시작: {_labelId}");
        }

        private void FollowMouse()
        {
            Camera cam = GetLabelingCamera();
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            // Y 높이 고정 평면과 레이 교차점 계산
            Plane dragPlane = new Plane(Vector3.up, new Vector3(0f, _dragPlaneDistance, 0f));
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 targetPos = ray.GetPoint(enter);
                transform.position = targetPos;
            }

            // 호버 중인 메모 감지
            DetectHoveredMemo(cam);
        }

        private void EndDrag()
        {
            _isDragging = false;

            if (_hoveredMemo != null)
            {
                // 메모에 드롭 시도
                _hoveredMemo.TryAttachLabel(this);
            }
            else
            {
                // 메모 위가 아니면 원위치 복귀
                ReturnToOrigin();
            }

            _hoveredMemo = null;
        }

        // =============================================================================
        // 호버 감지
        // =============================================================================

        /// <summary>
        /// 마우스 아래 MemoObject 감지 (드래그 중인 라벨 자신은 무시)
        /// </summary>
        private void DetectHoveredMemo(Camera cam)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            // 드래그 중인 라벨 Collider를 임시로 끄고 레이캐스트
            if (_collider != null) _collider.enabled = false;

            _hoveredMemo = null;
            RaycastHit[] hits = Physics.RaycastAll(ray, 50f);
            foreach (var hit in hits)
            {
                MemoObject memo = hit.collider.GetComponent<MemoObject>();
                if (memo == null)
                    memo = hit.collider.GetComponentInParent<MemoObject>();

                if (memo != null)
                {
                    _hoveredMemo = memo;
                    break;
                }
            }

            if (_collider != null) _collider.enabled = true;
        }

        // =============================================================================
        // 공개 API
        // =============================================================================

        /// <summary>
        /// 정답 - 메모지에 부착
        /// </summary>
        public void AttachToMemo(MemoObject memo)
        {
            _isAttached = true;
            _isDragging = false;

            // LabelTransform 위치/회전으로 이동
            Transform target = memo.LabelTransform;
            transform.SetParent(target);
            transform.DOLocalMove(memo.LabelLocalOffset, _returnDuration).SetEase(Ease.OutBack);
            transform.DOLocalRotateQuaternion(Quaternion.identity, _returnDuration);

            Debug.Log($"[LabelObject] 라벨 '{_labelId}' 부착 완료");
        }

        /// <summary>
        /// 오답 - 튕겨내고 원위치 복귀
        /// </summary>
        public void Bounce()
        {
            _isDragging = false;

            // 살짝 튕겨나는 방향 (현재 위치 → 원위치 반대)
            Vector3 bounceDir = (transform.position - _originPosition).normalized;
            if (bounceDir == Vector3.zero) bounceDir = Vector3.up;

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOMove(transform.position + bounceDir * _bounceDist, 0.1f).SetEase(Ease.OutQuad));
            seq.Append(transform.DOMove(_originPosition, _returnDuration).SetEase(Ease.OutBounce));
            seq.Join(transform.DORotateQuaternion(_originRotation, _returnDuration));

            Debug.Log($"[LabelObject] 라벨 '{_labelId}' 오답 → 튕겨남");
        }

        /// <summary>
        /// 원위치 복귀 (애니메이션)
        /// </summary>
        public void ReturnToOrigin()
        {
            transform.DOMove(_originPosition, _returnDuration).SetEase(Ease.OutQuad);
            transform.DORotateQuaternion(_originRotation, _returnDuration);
        }

        // =============================================================================
        // 헬퍼
        // =============================================================================

        private Camera GetLabelingCamera()
        {
            // PlayerController의 메인 카메라 사용
            if (PlayerController.Instance?.MainCamera != null)
                return PlayerController.Instance.MainCamera;
            return Camera.main;
        }
    }
}
