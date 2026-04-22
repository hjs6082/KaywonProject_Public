// =============================================================================
// CaseNode3D.cs
// =============================================================================
// 설명: 3D 사건 보드 위 단일 노드의 시각적 표현
// 용도: Quad에 증거물 이미지를 표시하고, 제목을 World Space Canvas로 표시
// =============================================================================

using UnityEngine;
using TMPro;
using GameDatabase.Evidence;

namespace GameDatabase.CaseBoard
{
    /// <summary>
    /// 3D 사건 보드 노드
    /// </summary>
    public class CaseNode3D : MonoBehaviour
    {
        // =============================================================================
        // 참조
        // =============================================================================

        [Header("=== 참조 ===")]

        [Tooltip("증거물 이미지를 표시할 Renderer (Quad/Plane)")]
        [SerializeField] private Renderer _evidenceRenderer;

        [Tooltip("노드 제목 텍스트 (World Space Canvas)")]
        [SerializeField] private TextMeshProUGUI _titleText;

        [Tooltip("제목 Canvas Transform (X 반전용)")]
        [SerializeField] private Transform _titleCanvas;

        [Tooltip("증거물 이미지 오브젝트 (비활성화용)")]
        [SerializeField] private GameObject _evidenceObject;

        [Tooltip("핀 오브젝트 (장식용)")]
        [SerializeField] private GameObject _pinObject;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private CaseNodeData _nodeData;
        private Material _evidenceMaterial;
        private bool _isShowingEvidence = false;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 노드 데이터
        /// </summary>
        public CaseNodeData NodeData => _nodeData;

        /// <summary>
        /// 증거물이 표시 중인지 여부
        /// </summary>
        public bool IsShowingEvidence => _isShowingEvidence;

        // =============================================================================
        // 초기화
        // =============================================================================

        /// <summary>
        /// 노드 초기화
        /// </summary>
        public void Initialize(CaseNodeData nodeData)
        {
            _nodeData = nodeData;

            // 제목 설정
            if (_titleText != null)
            {
                _titleText.text = nodeData.NodeTitle;
            }

            // 증거물 이미지 기본 숨기기
            if (_evidenceObject != null)
            {
                _evidenceObject.SetActive(false);
            }

            // Material 복사 (인스턴스별 독립)
            if (_evidenceRenderer != null)
            {
                _evidenceMaterial = new Material(_evidenceRenderer.sharedMaterial);
                _evidenceRenderer.material = _evidenceMaterial;
            }
        }

        // =============================================================================
        // 증거물 표시
        // =============================================================================

        /// <summary>
        /// 증거물 이미지 표시
        /// </summary>
        public void ShowEvidence(EvidenceData evidence)
        {
            if (evidence == null || evidence.EvidenceImage == null) return;

            _isShowingEvidence = true;

            if (_evidenceObject != null)
            {
                _evidenceObject.SetActive(true);
            }

            // Sprite → Texture2D로 변환하여 Material에 적용
            if (_evidenceMaterial != null && evidence.EvidenceImage != null)
            {
                _evidenceMaterial.mainTexture = evidence.EvidenceImage.texture;
            }
        }

        /// <summary>
        /// 증거물 이미지 숨기기
        /// </summary>
        public void ClearEvidence()
        {
            _isShowingEvidence = false;

            if (_evidenceObject != null)
            {
                _evidenceObject.SetActive(false);
            }
        }

        // =============================================================================
        // 정리
        // =============================================================================

        private void OnDestroy()
        {
            // 인스턴스 Material 정리
            if (_evidenceMaterial != null)
            {
                Destroy(_evidenceMaterial);
            }
        }
    }
}
