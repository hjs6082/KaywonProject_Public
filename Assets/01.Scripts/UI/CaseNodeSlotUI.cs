// =============================================================================
// CaseNodeSlotUI.cs
// =============================================================================
// 설명: 사건 보드 위 단일 노드 슬롯 UI (드롭 대상)
// 용도: 증거물을 드래그하여 놓을 수 있는 슬롯
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using GameDatabase.CaseBoard;
using GameDatabase.Evidence;

namespace GameDatabase.UI
{
    /// <summary>
    /// 사건 보드 노드 슬롯 UI (IDropHandler로 증거물 수신)
    /// </summary>
    public class CaseNodeSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        // =============================================================================
        // UI 요소
        // =============================================================================

        [Header("=== UI 요소 ===")]

        [Tooltip("노드 제목 텍스트")]
        [SerializeField] private TextMeshProUGUI _titleText;

        [Tooltip("배치된 증거물 이미지")]
        [SerializeField] private Image _placedEvidenceImage;

        [Tooltip("드롭 하이라이트 이미지")]
        [SerializeField] private Image _dropHighlight;

        [Tooltip("슬롯 프레임 이미지")]
        [SerializeField] private Image _slotFrame;

        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== 설정 ===")]

        [Tooltip("정답 시 프레임 색상")]
        [SerializeField] private Color _solvedColor = new Color(0.2f, 0.8f, 0.2f, 1f);

        [Tooltip("기본 프레임 색상")]
        [SerializeField] private Color _defaultColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        [Tooltip("하이라이트 색상")]
        [SerializeField] private Color _highlightColor = new Color(1f, 1f, 0.5f, 0.5f);

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private CaseNodeData _nodeData;
        private CaseBoardUI _boardUI;
        private bool _isSolved = false;
        private EvidenceData _placedEvidence;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 노드 데이터
        /// </summary>
        public CaseNodeData NodeData => _nodeData;

        /// <summary>
        /// 풀렸는지 여부
        /// </summary>
        public bool IsSolved => _isSolved;

        /// <summary>
        /// 배치된 증거물
        /// </summary>
        public EvidenceData PlacedEvidence => _placedEvidence;

        // =============================================================================
        // 초기화
        // =============================================================================

        /// <summary>
        /// 슬롯 초기화
        /// </summary>
        public void Initialize(CaseNodeData nodeData, CaseBoardUI boardUI)
        {
            _nodeData = nodeData;
            _boardUI = boardUI;
            _isSolved = false;
            _placedEvidence = null;

            // 제목 설정
            if (_titleText != null)
            {
                _titleText.text = nodeData.NodeTitle;
            }

            // 증거물 이미지 숨기기
            if (_placedEvidenceImage != null)
            {
                _placedEvidenceImage.gameObject.SetActive(false);
            }

            // 하이라이트 숨기기
            if (_dropHighlight != null)
            {
                _dropHighlight.gameObject.SetActive(false);
            }

            // 기본 색상
            if (_slotFrame != null)
            {
                _slotFrame.color = _defaultColor;
            }
        }

        // =============================================================================
        // 드롭 처리
        // =============================================================================

        public void OnDrop(PointerEventData eventData)
        {
            if (_isSolved) return;

            // 드래그된 증거물 가져오기
            DraggableEvidence draggable = eventData.pointerDrag?.GetComponent<DraggableEvidence>();
            if (draggable == null || draggable.EvidenceData == null) return;

            string evidenceID = draggable.EvidenceData.EvidenceID;
            string nodeID = _nodeData.NodeID;

            // 배치 검증
            bool isCorrect = CaseBoardManager.Instance.TryPlaceEvidence(nodeID, evidenceID);

            if (isCorrect)
            {
                // 정답 처리
                ShowPlacedEvidence(draggable.EvidenceData);
                PlayCorrectAnimation();

                // 인벤토리 새로고침
                if (_boardUI != null)
                {
                    _boardUI.RefreshInventory();
                }
            }
            else
            {
                // 오답 처리
                PlayWrongAnimation();
            }

            // 하이라이트 숨기기
            HideDropHighlight();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // 드래그 중일 때만 하이라이트 표시
            if (!_isSolved && eventData.dragging)
            {
                ShowDropHighlight();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideDropHighlight();
        }

        // =============================================================================
        // 증거물 표시
        // =============================================================================

        /// <summary>
        /// 배치된 증거물 표시
        /// </summary>
        public void ShowPlacedEvidence(EvidenceData evidence)
        {
            _placedEvidence = evidence;
            _isSolved = true;

            if (_placedEvidenceImage != null)
            {
                _placedEvidenceImage.sprite = evidence.EvidenceImage;
                _placedEvidenceImage.gameObject.SetActive(true);
            }

            if (_slotFrame != null)
            {
                _slotFrame.color = _solvedColor;
            }
        }

        // =============================================================================
        // 하이라이트
        // =============================================================================

        private void ShowDropHighlight()
        {
            if (_dropHighlight != null)
            {
                _dropHighlight.gameObject.SetActive(true);
                _dropHighlight.color = _highlightColor;
            }
        }

        private void HideDropHighlight()
        {
            if (_dropHighlight != null)
            {
                _dropHighlight.gameObject.SetActive(false);
            }
        }

        // =============================================================================
        // 애니메이션
        // =============================================================================

        private void PlayCorrectAnimation()
        {
            // 스케일 펀치 효과
            transform.DOScale(1.15f, 0.15f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
                });

            // 프레임 색상 변화
            if (_slotFrame != null)
            {
                _slotFrame.DOColor(_solvedColor, 0.3f).SetEase(Ease.OutCubic);
            }
        }

        private void PlayWrongAnimation()
        {
            // 흔들림 효과
            transform.DOShakePosition(0.4f, 10f, 20, 90f, false, true);

            // 붉은색 깜빡임
            if (_slotFrame != null)
            {
                Color originalColor = _slotFrame.color;
                _slotFrame.DOColor(Color.red, 0.15f)
                    .SetLoops(2, LoopType.Yoyo)
                    .OnComplete(() => _slotFrame.color = originalColor);
            }
        }
    }
}
