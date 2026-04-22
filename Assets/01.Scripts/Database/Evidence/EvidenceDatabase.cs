// =============================================================================
// EvidenceDatabase.cs
// =============================================================================
// 설명: 증거물 데이터베이스 ScriptableObject
// 용도: 게임 내 모든 증거물을 중앙 관리
// 사용법:
//   1. Project 창에서 우클릭 → Create → GameDatabase → Evidence → Evidence Database
//   2. EvidenceData들을 리스트에 추가
//   3. 런타임에서 ID나 이름으로 증거물 검색
// =============================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameDatabase.Evidence
{
    /// <summary>
    /// 증거물 데이터베이스 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "EvidenceDatabase", menuName = "GameDatabase/Evidence/Evidence Database")]
    public class EvidenceDatabase : ScriptableObject
    {
        // =============================================================================
        // 데이터
        // =============================================================================

        [Header("=== 증거물 목록 ===")]

        [Tooltip("데이터베이스에 등록된 모든 증거물")]
        [SerializeField] private List<EvidenceData> _evidenceList = new List<EvidenceData>();

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 증거물 목록 (읽기 전용)
        /// </summary>
        public IReadOnlyList<EvidenceData> EvidenceList => _evidenceList;

        /// <summary>
        /// 증거물 개수
        /// </summary>
        public int Count => _evidenceList.Count;

        // =============================================================================
        // 검색
        // =============================================================================

        /// <summary>
        /// ID로 증거물 찾기
        /// </summary>
        /// <param name="evidenceID">증거물 ID</param>
        /// <returns>찾은 증거물 (없으면 null)</returns>
        public EvidenceData GetEvidenceByID(string evidenceID)
        {
            if (string.IsNullOrEmpty(evidenceID))
            {
                Debug.LogWarning("[EvidenceDatabase] 증거물 ID가 비어있습니다.");
                return null;
            }

            EvidenceData evidence = _evidenceList.FirstOrDefault(e => e.EvidenceID == evidenceID);

            if (evidence == null)
            {
                Debug.LogWarning($"[EvidenceDatabase] 증거물을 찾을 수 없습니다: {evidenceID}");
            }

            return evidence;
        }

        /// <summary>
        /// 이름으로 증거물 찾기
        /// </summary>
        /// <param name="evidenceName">증거물 이름</param>
        /// <returns>찾은 증거물 (없으면 null)</returns>
        public EvidenceData GetEvidenceByName(string evidenceName)
        {
            if (string.IsNullOrEmpty(evidenceName))
            {
                Debug.LogWarning("[EvidenceDatabase] 증거물 이름이 비어있습니다.");
                return null;
            }

            EvidenceData evidence = _evidenceList.FirstOrDefault(e => e.EvidenceName == evidenceName);

            if (evidence == null)
            {
                Debug.LogWarning($"[EvidenceDatabase] 증거물을 찾을 수 없습니다: {evidenceName}");
            }

            return evidence;
        }

        /// <summary>
        /// 인덱스로 증거물 가져오기
        /// </summary>
        /// <param name="index">인덱스</param>
        /// <returns>증거물 (범위 밖이면 null)</returns>
        public EvidenceData GetEvidenceByIndex(int index)
        {
            if (index < 0 || index >= _evidenceList.Count)
            {
                Debug.LogWarning($"[EvidenceDatabase] 인덱스 범위를 벗어났습니다: {index}");
                return null;
            }

            return _evidenceList[index];
        }

        /// <summary>
        /// 증거물이 데이터베이스에 있는지 확인
        /// </summary>
        /// <param name="evidenceID">증거물 ID</param>
        /// <returns>존재 여부</returns>
        public bool HasEvidence(string evidenceID)
        {
            return _evidenceList.Any(e => e.EvidenceID == evidenceID);
        }

        // =============================================================================
        // 에디터 전용 (유효성 검사)
        // =============================================================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 중복 ID 검사
            var duplicateIDs = _evidenceList
                .Where(e => e != null)
                .GroupBy(e => e.EvidenceID)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicateID in duplicateIDs)
            {
                Debug.LogWarning($"[EvidenceDatabase] 중복된 증거물 ID 발견: {duplicateID}");
            }

            // null 데이터 검사
            if (_evidenceList.Any(e => e == null))
            {
                Debug.LogWarning("[EvidenceDatabase] null 증거물 데이터가 포함되어 있습니다.");
            }
        }
#endif
    }
}
