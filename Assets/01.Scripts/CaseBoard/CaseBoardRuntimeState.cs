// =============================================================================
// CaseBoardRuntimeState.cs
// =============================================================================
// 설명: 사건 보드의 런타임 상태를 추적하는 직렬화 가능 클래스
// 용도: 플레이어가 보드에 배치한 증거물과 Red String 연결 정보를 저장
// =============================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameDatabase.CaseBoard
{
    /// <summary>
    /// 노드에 배치된 증거물 정보
    /// </summary>
    [System.Serializable]
    public class NodePlacement
    {
        public string NodeID;
        public string PlacedEvidenceID;
    }

    /// <summary>
    /// 두 노드 간 Red String 연결 정보
    /// </summary>
    [System.Serializable]
    public class RedStringConnection
    {
        public string FromNodeID;
        public string ToNodeID;
    }

    /// <summary>
    /// 사건 보드의 런타임 상태
    /// </summary>
    [System.Serializable]
    public class CaseBoardRuntimeState
    {
        // =============================================================================
        // 직렬화 데이터
        // =============================================================================

        [SerializeField] private List<NodePlacement> _placements = new List<NodePlacement>();
        [SerializeField] private List<RedStringConnection> _activeConnections = new List<RedStringConnection>();
        [SerializeField] private bool _isCompleted = false;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 보드 완성 여부
        /// </summary>
        public bool IsCompleted
        {
            get => _isCompleted;
            set => _isCompleted = value;
        }

        /// <summary>
        /// 활성화된 Red String 연결 목록
        /// </summary>
        public IReadOnlyList<RedStringConnection> ActiveConnections => _activeConnections;

        /// <summary>
        /// 현재 배치 목록
        /// </summary>
        public IReadOnlyList<NodePlacement> Placements => _placements;

        // =============================================================================
        // 배치 관리
        // =============================================================================

        /// <summary>
        /// 노드에 증거물 배치
        /// </summary>
        public void PlaceEvidence(string nodeID, string evidenceID)
        {
            // 기존 배치가 있으면 업데이트
            NodePlacement existing = _placements.FirstOrDefault(p => p.NodeID == nodeID);
            if (existing != null)
            {
                existing.PlacedEvidenceID = evidenceID;
            }
            else
            {
                _placements.Add(new NodePlacement
                {
                    NodeID = nodeID,
                    PlacedEvidenceID = evidenceID
                });
            }
        }

        /// <summary>
        /// 노드에서 증거물 제거
        /// </summary>
        public void RemoveEvidence(string nodeID)
        {
            _placements.RemoveAll(p => p.NodeID == nodeID);

            // 관련 Red String 연결도 제거
            _activeConnections.RemoveAll(c => c.FromNodeID == nodeID || c.ToNodeID == nodeID);
        }

        /// <summary>
        /// 노드에 배치된 증거물 ID 반환 (없으면 null)
        /// </summary>
        public string GetPlacedEvidenceID(string nodeID)
        {
            NodePlacement placement = _placements.FirstOrDefault(p => p.NodeID == nodeID);
            return placement?.PlacedEvidenceID;
        }

        /// <summary>
        /// 노드가 올바르게 풀렸는지 확인
        /// </summary>
        public bool IsNodeSolved(string nodeID)
        {
            return !string.IsNullOrEmpty(GetPlacedEvidenceID(nodeID));
        }

        /// <summary>
        /// 증거물이 이미 보드 어딘가에 배치되었는지 확인
        /// </summary>
        public bool IsEvidencePlaced(string evidenceID)
        {
            return _placements.Any(p => p.PlacedEvidenceID == evidenceID);
        }

        // =============================================================================
        // Red String 연결 관리
        // =============================================================================

        /// <summary>
        /// Red String 연결 추가
        /// </summary>
        public void AddConnection(string fromNodeID, string toNodeID)
        {
            // 중복 방지 (양방향 체크)
            bool exists = _activeConnections.Any(c =>
                (c.FromNodeID == fromNodeID && c.ToNodeID == toNodeID) ||
                (c.FromNodeID == toNodeID && c.ToNodeID == fromNodeID));

            if (!exists)
            {
                _activeConnections.Add(new RedStringConnection
                {
                    FromNodeID = fromNodeID,
                    ToNodeID = toNodeID
                });
            }
        }

        /// <summary>
        /// 모든 상태 초기화
        /// </summary>
        public void Reset()
        {
            _placements.Clear();
            _activeConnections.Clear();
            _isCompleted = false;
        }
    }

    // =============================================================================
    // 저장/로드용 래퍼 클래스
    // =============================================================================

    /// <summary>
    /// 다중 케이스 상태 저장용 엔트리
    /// </summary>
    [System.Serializable]
    public class CaseBoardStateEntry
    {
        public string CaseBoardID;
        public CaseBoardRuntimeState State;
    }

    /// <summary>
    /// 전체 사건 보드 저장 데이터
    /// </summary>
    [System.Serializable]
    public class CaseBoardSaveData
    {
        public List<CaseBoardStateEntry> Entries = new List<CaseBoardStateEntry>();
    }
}
