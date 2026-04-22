// =============================================================================
// EvidenceNotebookSlot.cs
// =============================================================================
// 설명: 증거물 노트북의 좌측 목록 슬롯 (프리팹용)
// 용도: 증거물 아이템을 표시하고 선택할 수 있는 UI 슬롯
// 특징:
//   - 썸네일, 이름 표시
//   - 버튼 클릭 시 우측 상세 정보 업데이트
//   - 선택 상태 하이라이트
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameDatabase.Evidence;

namespace GameDatabase.UI
{
    /// <summary>
    /// 증거물 노트북 슬롯 - 목록 아이템
    /// </summary>
    public class EvidenceNotebookSlot : MonoBehaviour
    {
        // =============================================================================
        // UI 요소
        // =============================================================================

        [Header("=== UI 요소 ===")]

        [Tooltip("썸네일 이미지")]
        [SerializeField] private Image _thumbnailImage;

        [Tooltip("증거물 이름 텍스트 (TMP)")]
        [SerializeField] private TextMeshProUGUI _evidenceNameText;

        [Tooltip("선택 하이라이트 배경 (선택 시 활성화)")]
        [SerializeField] private GameObject _highlightBackground;

        [Tooltip("버튼 컴포넌트")]
        [SerializeField] private Button _button;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private EvidenceData _evidenceData;
        private EvidenceNotebookUI _notebookUI;
        private bool _isSelected = false;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 연동된 증거물 데이터
        /// </summary>
        public EvidenceData EvidenceData => _evidenceData;

        /// <summary>
        /// 선택 여부
        /// </summary>
        public bool IsSelected => _isSelected;

        // =============================================================================
        // 초기화
        // =============================================================================

        private void Awake()
        {
            // 버튼 클릭 이벤트 등록
            if (_button != null)
            {
                _button.onClick.AddListener(OnClicked);
            }

            // 초기 선택 상태 비활성화
            SetSelected(false);
        }

        /// <summary>
        /// 슬롯 초기화
        /// </summary>
        /// <param name="evidence">증거물 데이터</param>
        /// <param name="notebookUI">부모 노트북 UI</param>
        public void Initialize(EvidenceData evidence, EvidenceNotebookUI notebookUI)
        {
            _evidenceData = evidence;
            _notebookUI = notebookUI;

            // UI 업데이트
            UpdateUI();
        }

        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateUI()
        {
            if (_evidenceData == null) return;

            // 이름 설정
            if (_evidenceNameText != null)
            {
                _evidenceNameText.text = _evidenceData.EvidenceName;
            }

            // 썸네일 설정
            if (_thumbnailImage != null)
            {
                if (_evidenceData.EvidenceImage != null)
                {
                    _thumbnailImage.sprite = _evidenceData.EvidenceImage;
                    _thumbnailImage.enabled = true;
                }
                else
                {
                    _thumbnailImage.enabled = false;
                }
            }
        }

        // =============================================================================
        // 버튼 이벤트
        // =============================================================================

        /// <summary>
        /// 버튼 클릭 이벤트
        /// </summary>
        private void OnClicked()
        {
            if (_notebookUI != null && _evidenceData != null)
            {
                _notebookUI.ShowEvidenceDetail(_evidenceData, this);
                Debug.Log($"[EvidenceNotebookSlot] 증거물 선택: {_evidenceData.EvidenceName}");
            }
        }

        // =============================================================================
        // 선택 상태
        // =============================================================================

        /// <summary>
        /// 선택 상태 설정
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            // 하이라이트 배경 표시/숨김
            if (_highlightBackground != null)
            {
                _highlightBackground.SetActive(selected);
            }
        }
    }
}
