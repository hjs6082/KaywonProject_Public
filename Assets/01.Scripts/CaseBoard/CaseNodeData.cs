// =============================================================================
// CaseNodeData.cs
// =============================================================================
// 설명: 사건 보드 위 단일 노드(질문/슬롯)의 데이터
// 용도: CaseBoardData 내에서 각 노드의 정보를 저장
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace GameDatabase.CaseBoard
{
    /// <summary>
    /// 사건 보드 위 단일 노드 데이터 (질문/슬롯)
    /// </summary>
    [System.Serializable]
    public class CaseNodeData
    {
        // =============================================================================
        // 기본 정보
        // =============================================================================

        [Tooltip("노드 고유 ID (예: NODE_001)")]
        [SerializeField] private string _nodeID;

        [Tooltip("노드 제목 (보드에 표시될 질문/개념)")]
        [SerializeField] private string _nodeTitle;

        [Tooltip("노드 설명 (선택)")]
        [TextArea(2, 4)]
        [SerializeField] private string _nodeDescription;

        // =============================================================================
        // 보드 배치
        // =============================================================================

        [Tooltip("보드 위 노드 위치 (0~1 정규화 좌표)")]
        [SerializeField] private Vector2 _boardPosition = new Vector2(0.5f, 0.5f);

        // =============================================================================
        // 연결 정보
        // =============================================================================

        [Tooltip("정답으로 인정되는 증거물 ID 목록")]
        [SerializeField] private List<string> _correctEvidenceIDs = new List<string>();

        [Tooltip("올바르게 배치 시 연결될 다른 노드 ID 목록 (Red String)")]
        [SerializeField] private List<string> _connectedNodeIDs = new List<string>();

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 노드 고유 ID
        /// </summary>
        public string NodeID => _nodeID;

        /// <summary>
        /// 노드 제목 (보드에 표시될 질문/개념)
        /// </summary>
        public string NodeTitle => _nodeTitle;

        /// <summary>
        /// 노드 설명
        /// </summary>
        public string NodeDescription => _nodeDescription;

        /// <summary>
        /// 보드 위 노드 위치 (0~1 정규화 좌표)
        /// </summary>
        public Vector2 BoardPosition => _boardPosition;

        /// <summary>
        /// 정답으로 인정되는 증거물 ID 목록
        /// </summary>
        public IReadOnlyList<string> CorrectEvidenceIDs => _correctEvidenceIDs;

        /// <summary>
        /// 연결될 다른 노드 ID 목록
        /// </summary>
        public IReadOnlyList<string> ConnectedNodeIDs => _connectedNodeIDs;
    }
}
