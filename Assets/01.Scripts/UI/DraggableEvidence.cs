// =============================================================================
// DraggableEvidence.cs
// =============================================================================
// 설명: 드래그 가능한 증거물 UI 컴포넌트
// 용도: 인벤토리 슬롯에서 드래그하여 보드 노드에 드롭
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GameDatabase.Evidence;

namespace GameDatabase.UI
{
    /// <summary>
    /// 드래그 가능한 증거물 (IBeginDragHandler, IDragHandler, IEndDragHandler)
    /// </summary>
    public class DraggableEvidence : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // =============================================================================
        // 내부 변수
        // =============================================================================

        private EvidenceData _evidenceData;
        private CaseBoardUI _boardUI;
        private CanvasGroup _slotCanvasGroup;
        private Canvas _rootCanvas;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 드래그 중인 증거물 데이터
        /// </summary>
        public EvidenceData EvidenceData => _evidenceData;

        // =============================================================================
        // 설정
        // =============================================================================

        /// <summary>
        /// 드래그 가능한 증거물 설정
        /// </summary>
        public void Setup(EvidenceData evidence, CaseBoardUI boardUI)
        {
            _evidenceData = evidence;
            _boardUI = boardUI;

            // CanvasGroup 확인/추가
            _slotCanvasGroup = GetComponent<CanvasGroup>();
            if (_slotCanvasGroup == null)
            {
                _slotCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Start()
        {
            // 루트 Canvas 찾기
            _rootCanvas = GetComponentInParent<Canvas>();
            while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
            {
                _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();
            }
        }

        // =============================================================================
        // 드래그 이벤트
        // =============================================================================

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_evidenceData == null || _boardUI == null) return;

            // 드래그 고스트 활성화
            CanvasGroup ghost = _boardUI.DragGhost;
            Image ghostImage = _boardUI.DragGhostImage;

            if (ghost != null)
            {
                ghost.gameObject.SetActive(true);
                ghost.alpha = 0.8f;
                ghost.blocksRaycasts = false;
            }

            if (ghostImage != null && _evidenceData.EvidenceImage != null)
            {
                ghostImage.sprite = _evidenceData.EvidenceImage;
            }

            // 원본 슬롯의 Raycast 차단 해제 (드롭 대상 감지용)
            if (_slotCanvasGroup != null)
            {
                _slotCanvasGroup.blocksRaycasts = false;
                _slotCanvasGroup.alpha = 0.5f;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_boardUI == null) return;

            // 드래그 고스트 위치 업데이트
            CanvasGroup ghost = _boardUI.DragGhost;
            if (ghost != null)
            {
                RectTransform ghostRect = ghost.GetComponent<RectTransform>();
                if (ghostRect != null && _rootCanvas != null)
                {
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _rootCanvas.GetComponent<RectTransform>(),
                        eventData.position,
                        eventData.pressEventCamera,
                        out localPoint);

                    ghostRect.anchoredPosition = localPoint;
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // 드래그 고스트 비활성화
            if (_boardUI != null)
            {
                CanvasGroup ghost = _boardUI.DragGhost;
                if (ghost != null)
                {
                    ghost.alpha = 0f;
                    ghost.gameObject.SetActive(false);
                }
            }

            // 원본 슬롯 복원
            if (_slotCanvasGroup != null)
            {
                _slotCanvasGroup.blocksRaycasts = true;
                _slotCanvasGroup.alpha = 1f;
            }
        }
    }
}
