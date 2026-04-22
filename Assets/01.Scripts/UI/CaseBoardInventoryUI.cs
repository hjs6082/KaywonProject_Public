// =============================================================================
// CaseBoardInventoryUI.cs
// =============================================================================
// 설명: 사건 보드 하단 인벤토리 UI
// 용도: 획득한 증거물을 스크롤 가능한 리스트로 표시
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using GameDatabase.Evidence;

namespace GameDatabase.UI
{
    /// <summary>
    /// 사건 보드 하단 인벤토리 UI
    /// </summary>
    public class CaseBoardInventoryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // =============================================================================
        // UI 요소
        // =============================================================================

        [Header("=== UI 요소 ===")]

        [Tooltip("인벤토리 슬롯 부모 (HorizontalLayoutGroup)")]
        [SerializeField] private Transform _slotContainer;

        [Tooltip("인벤토리 슬롯 프리팹")]
        [SerializeField] private GameObject _slotPrefab;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private CaseBoardUI _boardUI;
        private List<CaseBoardInventorySlot> _slots = new List<CaseBoardInventorySlot>();
        private bool _isPointerOver = false;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 마우스가 인벤토리 영역 위에 있는지 여부
        /// </summary>
        public bool IsPointerOverInventory => _isPointerOver;

        // =============================================================================
        // 초기화
        // =============================================================================

        /// <summary>
        /// 인벤토리 초기화
        /// </summary>
        public void Initialize(CaseBoardUI boardUI)
        {
            _boardUI = boardUI;
        }

        // =============================================================================
        // 슬롯 관리
        // =============================================================================

        /// <summary>
        /// 증거물 목록으로 슬롯 새로고침
        /// </summary>
        public void RefreshSlots(List<EvidenceData> evidences)
        {
            ClearSlots();

            if (_slotContainer == null || _slotPrefab == null) return;

            foreach (EvidenceData evidence in evidences)
            {
                GameObject slotObj = Instantiate(_slotPrefab, _slotContainer);
                CaseBoardInventorySlot slot = slotObj.GetComponent<CaseBoardInventorySlot>();

                if (slot != null)
                {
                    slot.Initialize(evidence, _boardUI);
                    _slots.Add(slot);
                }
            }
        }

        private void ClearSlots()
        {
            foreach (var slot in _slots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            _slots.Clear();
        }

        // =============================================================================
        // 포인터 이벤트 (줌/팬 비활성화용)
        // =============================================================================

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerOver = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerOver = false;
        }
    }
}
