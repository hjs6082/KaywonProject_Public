// =============================================================================
// RealOutlineEffect.cs
// =============================================================================
// 설명: 진짜 Outline 효과 (렌더러 복제 방식)
// 용도: HDRP/URP 모두에서 작동하는 Outline
// 방법:
//   1. 오브젝트를 한 단계 뒤에 복제
//   2. 복제본을 살짝 키워서 뒤에서 보이게
//   3. 복제본에 단색 Material 적용
// =============================================================================

using UnityEngine;
using DG.Tweening;

namespace GameDatabase.Player
{
    /// <summary>
    /// 진짜 Outline 효과 - 렌더러 복제 방식
    /// 모든 렌더 파이프라인에서 작동
    /// </summary>
    public class RealOutlineEffect : MonoBehaviour
    {
        [Header("=== Outline 설정 ===")]

        [Tooltip("Outline 색상")]
        [SerializeField] private Color _outlineColor = Color.yellow;

        [Tooltip("Outline 두께")]
        [Range(0f, 0.1f)]
        [SerializeField] private float _outlineWidth = 0.03f;

        [Tooltip("호버 애니메이션 시간")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _hoverDuration = 0.3f;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private GameObject _outlineObject;
        private Material _outlineMaterial;
        private Renderer _originalRenderer;
        private Renderer _outlineRenderer;
        private Vector3 _originalScale;

        private Tween _scaleTween;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            _originalRenderer = GetComponent<Renderer>();
            if (_originalRenderer == null)
            {
                Debug.LogError("[RealOutlineEffect] Renderer 컴포넌트가 없습니다!");
                enabled = false;
                return;
            }

            CreateOutline();
        }

        private void OnDestroy()
        {
            _scaleTween?.Kill();

            if (_outlineObject != null)
            {
                Destroy(_outlineObject);
            }

            if (_outlineMaterial != null)
            {
                Destroy(_outlineMaterial);
            }
        }

        // =============================================================================
        // Outline 생성
        // =============================================================================

        private void CreateOutline()
        {
            // Outline용 Material 생성
            _outlineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (_outlineMaterial.shader == null)
            {
                // HDRP인 경우
                _outlineMaterial = new Material(Shader.Find("HDRP/Unlit"));
            }
            if (_outlineMaterial.shader == null)
            {
                // Built-in인 경우
                _outlineMaterial = new Material(Shader.Find("Unlit/Color"));
            }

            _outlineMaterial.color = _outlineColor;

            // Outline 오브젝트 생성 (원본의 자식으로)
            _outlineObject = new GameObject($"{gameObject.name}_Outline");
            _outlineObject.transform.SetParent(transform, false);
            _outlineObject.transform.localPosition = Vector3.zero;
            _outlineObject.transform.localRotation = Quaternion.identity;
            _outlineObject.transform.localScale = Vector3.one;

            // MeshFilter 복사
            MeshFilter originalMeshFilter = GetComponent<MeshFilter>();
            if (originalMeshFilter != null)
            {
                MeshFilter outlineMeshFilter = _outlineObject.AddComponent<MeshFilter>();
                outlineMeshFilter.sharedMesh = originalMeshFilter.sharedMesh;
            }

            // MeshRenderer 추가 및 설정
            _outlineRenderer = _outlineObject.AddComponent<MeshRenderer>();
            _outlineRenderer.material = _outlineMaterial;

            // Outline은 원본보다 뒤에 렌더링
            _outlineRenderer.sortingOrder = -1;

            // 초기 스케일 저장 (부모 기준)
            _originalScale = _outlineObject.transform.localScale;

            // 초기에는 Outline 숨김
            _outlineObject.SetActive(false);

            Debug.Log($"[RealOutlineEffect] Outline 생성 완료: {gameObject.name}");
        }

        // =============================================================================
        // 호버 효과
        // =============================================================================

        /// <summary>
        /// 호버 진입 - Outline 표시
        /// </summary>
        public void OnHoverEnter()
        {
            if (_outlineObject == null) return;

            // Outline 오브젝트 활성화
            _outlineObject.SetActive(true);

            // Scale 애니메이션 (1.0 → 1.0 + outlineWidth)
            _scaleTween?.Kill();

            float targetScale = 1f + _outlineWidth;
            _scaleTween = _outlineObject.transform.DOScale(_originalScale * targetScale, _hoverDuration)
                .SetEase(Ease.OutCubic);

            Debug.Log($"[RealOutlineEffect] Outline 표시: {gameObject.name}");
        }

        /// <summary>
        /// 호버 종료 - Outline 숨김
        /// </summary>
        public void OnHoverExit()
        {
            if (_outlineObject == null) return;

            // Scale 복원 후 숨김
            _scaleTween?.Kill();

            _scaleTween = _outlineObject.transform.DOScale(_originalScale, _hoverDuration)
                .SetEase(Ease.InCubic)
                .OnComplete(() => _outlineObject.SetActive(false));

            Debug.Log($"[RealOutlineEffect] Outline 숨김: {gameObject.name}");
        }

        // =============================================================================
        // 외부 제어
        // =============================================================================

        /// <summary>
        /// Outline 색상 변경
        /// </summary>
        public void SetOutlineColor(Color color)
        {
            _outlineColor = color;
            if (_outlineMaterial != null)
            {
                _outlineMaterial.color = color;
            }
        }

        /// <summary>
        /// Outline 두께 변경
        /// </summary>
        public void SetOutlineWidth(float width)
        {
            _outlineWidth = width;
        }
    }
}
