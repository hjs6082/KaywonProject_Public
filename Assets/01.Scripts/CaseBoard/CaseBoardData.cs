// =============================================================================
// CaseBoardData.cs
// =============================================================================
// 설명: 사건 보드 전체 구조를 정의하는 ScriptableObject
// 용도: 하나의 사건(케이스)에 대한 보드 데이터를 에디터에서 편집/관리
// 사용법:
//   1. Project 창에서 우클릭 → Create → GameDatabase → CaseBoard → Case Board Data
//   2. 노드 목록과 사용 가능한 증거물을 설정
//   3. CaseBoardManager에서 OpenBoard()로 사용
// =============================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameDatabase.Evidence;

namespace GameDatabase.CaseBoard
{
    /// <summary>
    /// 사건 보드 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewCaseBoard", menuName = "GameDatabase/CaseBoard/Case Board Data")]
    public class CaseBoardData : ScriptableObject
    {
        // =============================================================================
        // 기본 정보
        // =============================================================================

        [Header("=== 기본 정보 ===")]

        [Tooltip("사건 보드 고유 ID (예: CASE_001)")]
        [SerializeField] private string _caseBoardID;

        [Tooltip("사건 보드 제목")]
        [SerializeField] private string _caseBoardTitle;

        [Tooltip("사건 보드 설명")]
        [TextArea(2, 4)]
        [SerializeField] private string _caseBoardDescription;

        // =============================================================================
        // 노드 목록
        // =============================================================================

        [Header("=== 노드 목록 ===")]

        [Tooltip("보드 위의 노드(질문/슬롯) 목록")]
        [SerializeField] private List<CaseNodeData> _nodes = new List<CaseNodeData>();

        // =============================================================================
        // 사용 가능한 증거물
        // =============================================================================

        [Header("=== 사용 가능한 증거물 ===")]

        [Tooltip("이 사건 보드에서 사용 가능한 증거물 데이터 목록")]
        [SerializeField] private List<EvidenceData> _availableEvidences = new List<EvidenceData>();

        // =============================================================================
        // 보드 설정
        // =============================================================================

        [Header("=== 보드 설정 ===")]

        [Tooltip("보드 배경 이미지 (선택)")]
        [SerializeField] private Sprite _boardBackground;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 사건 보드 고유 ID
        /// </summary>
        public string CaseBoardID => _caseBoardID;

        /// <summary>
        /// 사건 보드 제목
        /// </summary>
        public string CaseBoardTitle => _caseBoardTitle;

        /// <summary>
        /// 사건 보드 설명
        /// </summary>
        public string CaseBoardDescription => _caseBoardDescription;

        /// <summary>
        /// 보드 위의 노드 목록
        /// </summary>
        public IReadOnlyList<CaseNodeData> Nodes => _nodes;

        /// <summary>
        /// 사용 가능한 증거물 목록
        /// </summary>
        public IReadOnlyList<EvidenceData> AvailableEvidences => _availableEvidences;

        /// <summary>
        /// 보드 배경 이미지
        /// </summary>
        public Sprite BoardBackground => _boardBackground;

        // =============================================================================
        // 검색 메서드
        // =============================================================================

        /// <summary>
        /// ID로 노드 검색
        /// </summary>
        public CaseNodeData GetNodeByID(string nodeID)
        {
            if (string.IsNullOrEmpty(nodeID)) return null;
            return _nodes.FirstOrDefault(n => n.NodeID == nodeID);
        }

        /// <summary>
        /// 증거물 배치가 올바른지 검증
        /// </summary>
        /// <param name="nodeID">노드 ID</param>
        /// <param name="evidenceID">증거물 ID</param>
        /// <returns>올바른 배치이면 true</returns>
        public bool ValidateEvidencePlacement(string nodeID, string evidenceID)
        {
            CaseNodeData node = GetNodeByID(nodeID);
            if (node == null) return false;
            return node.CorrectEvidenceIDs.Contains(evidenceID);
        }

        /// <summary>
        /// ID로 증거물 검색
        /// </summary>
        public EvidenceData GetEvidenceByID(string evidenceID)
        {
            if (string.IsNullOrEmpty(evidenceID)) return null;
            return _availableEvidences.FirstOrDefault(e => e.EvidenceID == evidenceID);
        }

        // =============================================================================
        // 유효성 검사
        // =============================================================================

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_caseBoardID))
            {
                Debug.LogWarning($"[CaseBoardData] 보드 ID가 비어있습니다: {name}");
            }

            // 중복 노드 ID 검사
            var nodeIDs = _nodes
                .Where(n => !string.IsNullOrEmpty(n.NodeID))
                .Select(n => n.NodeID)
                .ToList();

            var duplicates = nodeIDs
                .GroupBy(id => id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (string duplicate in duplicates)
            {
                Debug.LogWarning($"[CaseBoardData] 중복 노드 ID 발견: {duplicate} (보드: {name})");
            }
        }
    }
}
