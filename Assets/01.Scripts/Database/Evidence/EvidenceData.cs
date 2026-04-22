// =============================================================================
// EvidenceData.cs
// =============================================================================
// 설명: 증거물 데이터 ScriptableObject
// 용도: 게임 내 증거물의 기본 정보를 저장
// 사용법:
//   1. Project 창에서 우클릭 → Create → GameDatabase → Evidence → Evidence Data
//   2. 증거물 이름, 설명, ID, 이미지를 설정
//   3. EvidenceDatabase에 등록하여 관리
// =============================================================================

using UnityEngine;

namespace GameDatabase.Evidence
{
    /// <summary>
    /// 증거물 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewEvidence", menuName = "GameDatabase/Evidence/Evidence Data")]
    public class EvidenceData : ScriptableObject
    {
        // =============================================================================
        // 기본 정보
        // =============================================================================

        [Header("=== 기본 정보 ===")]

        [Tooltip("증거물 고유 ID (예: EVIDENCE_001)")]
        [SerializeField] private string _evidenceID;

        [Tooltip("증거물 이름 (플레이어에게 보여질 이름)")]
        [SerializeField] private string _evidenceName;

        [Tooltip("증거물 설명 (상세 정보)")]
        [TextArea(3, 6)]
        [SerializeField] private string _evidenceDescription;

        [Tooltip("증거물 이미지/사진")]
        [SerializeField] private Sprite _evidenceImage;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 증거물 고유 ID
        /// </summary>
        public string EvidenceID => _evidenceID;

        /// <summary>
        /// 증거물 이름
        /// </summary>
        public string EvidenceName => _evidenceName;

        /// <summary>
        /// 증거물 설명
        /// </summary>
        public string EvidenceDescription => _evidenceDescription;

        /// <summary>
        /// 증거물 이미지
        /// </summary>
        public Sprite EvidenceImage => _evidenceImage;

        // =============================================================================
        // 유효성 검사
        // =============================================================================

        private void OnValidate()
        {
            // ID가 비어있으면 경고
            if (string.IsNullOrEmpty(_evidenceID))
            {
                Debug.LogWarning($"[EvidenceData] 증거물 ID가 비어있습니다: {name}");
            }

            // 이름이 비어있으면 경고
            if (string.IsNullOrEmpty(_evidenceName))
            {
                Debug.LogWarning($"[EvidenceData] 증거물 이름이 비어있습니다: {name}");
            }
        }
    }
}
