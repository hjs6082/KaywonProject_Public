// =============================================================================
// CaseBoardInventorySlot.cs
// =============================================================================
// 설명: 사건 보드 인벤토리 내 단서 슬롯
// 용도: 증거물을 표시하고 드래그를 시작하는 원본 슬롯
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameDatabase.Evidence;

namespace GameDatabase.UI
{
    /// <summary>
    /// 사건 보드 인벤토리 슬롯
    /// </summary>
    public class CaseBoardInventorySlot : MonoBehaviour
    {
        // =============================================================================
        // UI 요소
        // =============================================================================

        [Header("=== UI 요소 ===")]

        [Tooltip("증거물 아이콘 이미지")]
        [SerializeField] private Image _iconImage;

        [Tooltip("증거물 이름 텍스트")]
        [SerializeField] private TextMeshProUGUI _nameText;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private EvidenceData _evidenceData;
        private CaseBoardUI _boardUI;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 슬롯에 담긴 증거물 데이터
        /// </summary>
        public EvidenceData EvidenceData => _evidenceData;

        // =============================================================================
        // 초기화
        // =============================================================================

        /// <summary>
        /// 슬롯 초기화
        /// </summary>
        public void Initialize(EvidenceData evidence, CaseBoardUI boardUI)
        {
            _evidenceData = evidence;
            _boardUI = boardUI;

            // UI 업데이트
            if (_iconImage != null && evidence.EvidenceImage != null)
            {
                _iconImage.sprite = evidence.EvidenceImage;
            }

            if (_nameText != null)
            {
                _nameText.text = evidence.EvidenceName;
            }

            // DraggableEvidence 컴포넌트 설정
            DraggableEvidence draggable = GetComponent<DraggableEvidence>();
            if (draggable != null)
            {
                draggable.Setup(evidence, boardUI);
            }
        }
    }
}
