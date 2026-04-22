// =============================================================================
// UILineRenderer.cs
// =============================================================================
// 설명: 커스텀 MaskableGraphic으로 Canvas 위에 선(Red String)을 렌더링
// 용도: 사건 보드에서 노드 간 붉은 실 시각화
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameDatabase.UI
{
    /// <summary>
    /// Canvas 위에 선을 그리는 커스텀 MaskableGraphic 컴포넌트
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class UILineRenderer : MaskableGraphic
    {
        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== 선 설정 ===")]

        [Tooltip("선 두께")]
        [SerializeField] private float _lineWidth = 3f;

        [Tooltip("곡선 세그먼트 수 (0이면 직선)")]
        [Range(0, 20)]
        [SerializeField] private int _curveSegments = 0;

        [Tooltip("곡선 처짐 정도")]
        [Range(0f, 100f)]
        [SerializeField] private float _curveSag = 30f;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private List<(Vector2 start, Vector2 end)> _lines = new List<(Vector2, Vector2)>();

        // =============================================================================
        // 공개 메서드
        // =============================================================================

        /// <summary>
        /// 연결 목록 설정 (시작점, 끝점 쌍)
        /// </summary>
        public void SetConnections(List<(Vector2 start, Vector2 end)> lines)
        {
            _lines = lines ?? new List<(Vector2, Vector2)>();
            SetVerticesDirty();
        }

        /// <summary>
        /// 단일 연결 추가
        /// </summary>
        public void AddConnection(Vector2 start, Vector2 end)
        {
            _lines.Add((start, end));
            SetVerticesDirty();
        }

        /// <summary>
        /// 모든 연결 제거
        /// </summary>
        public void ClearConnections()
        {
            _lines.Clear();
            SetVerticesDirty();
        }

        // =============================================================================
        // 메시 생성
        // =============================================================================

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (_lines == null || _lines.Count == 0) return;

            foreach (var line in _lines)
            {
                if (_curveSegments > 0)
                {
                    DrawCurvedLine(vh, line.start, line.end);
                }
                else
                {
                    DrawStraightLine(vh, line.start, line.end);
                }
            }

        }

        // =============================================================================
        // 직선 그리기
        // =============================================================================

        private void DrawStraightLine(VertexHelper vh, Vector2 start, Vector2 end)
        {
            Vector2 dir = (end - start).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x) * _lineWidth * 0.5f;

            // Graphic.color를 사용하여 CanvasRenderer와 호환
            Color32 vertColor = color;

            AddQuad(vh, start + perp, start - perp, end - perp, end + perp, vertColor);
        }

        // =============================================================================
        // 곡선 그리기 (처짐 효과)
        // =============================================================================

        private void DrawCurvedLine(VertexHelper vh, Vector2 start, Vector2 end)
        {
            Vector2 midPoint = (start + end) * 0.5f + Vector2.down * _curveSag;

            Vector2 prevPoint = start;

            for (int i = 1; i <= _curveSegments; i++)
            {
                float t = (float)i / _curveSegments;
                Vector2 point = CalculateQuadraticBezier(start, midPoint, end, t);

                DrawStraightLine(vh, prevPoint, point);
                prevPoint = point;
            }
        }

        private Vector2 CalculateQuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        // =============================================================================
        // 쿼드 헬퍼
        // =============================================================================

        private void AddQuad(VertexHelper vh, Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3, Color32 vertColor)
        {
            int startIndex = vh.currentVertCount;

            vh.AddVert(CreateUIVertex(v0, vertColor));
            vh.AddVert(CreateUIVertex(v1, vertColor));
            vh.AddVert(CreateUIVertex(v2, vertColor));
            vh.AddVert(CreateUIVertex(v3, vertColor));

            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
        }

        private UIVertex CreateUIVertex(Vector2 pos, Color32 vertexColor)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = pos;
            vertex.color = vertexColor;
            return vertex;
        }
    }
}
