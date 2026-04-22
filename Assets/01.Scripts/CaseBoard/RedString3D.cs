// =============================================================================
// RedString3D.cs
// =============================================================================
// 설명: 3D 공간에서 두 노드 간 붉은 실을 렌더링
// 용도: LineRenderer를 사용하여 Red String 시각화
// =============================================================================

using UnityEngine;

namespace GameDatabase.CaseBoard
{
    /// <summary>
    /// 3D Red String (LineRenderer 기반)
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class RedString3D : MonoBehaviour
    {
        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== 설정 ===")]

        [Tooltip("선 색상")]
        [SerializeField] private Color _lineColor = new Color(0.8f, 0.1f, 0.1f, 1f);

        [Tooltip("선 시작 두께")]
        [SerializeField] private float _startWidth = 0.015f;

        [Tooltip("선 끝 두께")]
        [SerializeField] private float _endWidth = 0.015f;

        [Tooltip("처짐 효과 사용")]
        [SerializeField] private bool _useSag = true;

        [Tooltip("처짐 정도")]
        [SerializeField] private float _sagAmount = 0.05f;

        [Tooltip("처짐 세그먼트 수")]
        [Range(2, 20)]
        [SerializeField] private int _sagSegments = 10;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private LineRenderer _lineRenderer;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();

            // 기본 설정
            _lineRenderer.startColor = _lineColor;
            _lineRenderer.endColor = _lineColor;
            _lineRenderer.startWidth = _startWidth;
            _lineRenderer.endWidth = _endWidth;
            _lineRenderer.useWorldSpace = true;
        }

        // =============================================================================
        // 연결 설정
        // =============================================================================

        /// <summary>
        /// 두 점 사이의 Red String 설정
        /// </summary>
        public void SetConnection(Vector3 start, Vector3 end)
        {
            if (_lineRenderer == null) return;

            if (_useSag && _sagSegments > 2)
            {
                SetCurvedConnection(start, end);
            }
            else
            {
                _lineRenderer.positionCount = 2;
                _lineRenderer.SetPosition(0, start);
                _lineRenderer.SetPosition(1, end);
            }
        }

        private void SetCurvedConnection(Vector3 start, Vector3 end)
        {
            _lineRenderer.positionCount = _sagSegments;

            for (int i = 0; i < _sagSegments; i++)
            {
                float t = (float)i / (_sagSegments - 1);

                // 선형 보간
                Vector3 point = Vector3.Lerp(start, end, t);

                // 포물선 처짐 (중앙이 가장 많이 처짐)
                float sag = Mathf.Sin(t * Mathf.PI) * _sagAmount;
                point.y -= sag;

                _lineRenderer.SetPosition(i, point);
            }
        }
    }
}
