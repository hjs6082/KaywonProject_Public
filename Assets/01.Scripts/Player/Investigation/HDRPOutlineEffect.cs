// =============================================================================
// HDRPOutlineEffect.cs
// =============================================================================
// 설명: HDRP용 Outline 효과 (Emission + Rim Light 조합)
// 용도: QuickOutline이 작동하지 않는 HDRP 프로젝트에서 사용
// 효과:
//   - Emissive Glow (강한 빛)
//   - Rim Light (테두리 강조)
//   - Scale 애니메이션
// =============================================================================

using UnityEngine;
using DG.Tweening;

namespace GameDatabase.Player
{
    /// <summary>
    /// HDRP용 Outline 효과
    /// InteractableClue의 Outline 효과를 대체
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class HDRPOutlineEffect : MonoBehaviour
    {
        [Header("=== 효과 설정 ===")]

        [Tooltip("호버 시 스케일 배율")]
        [Range(1.0f, 1.2f)]
        [SerializeField] private float _hoverScale = 1.05f;

        [Tooltip("호버 애니메이션 시간")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _hoverDuration = 0.3f;

        [Tooltip("Emission 색상")]
        [SerializeField] private Color _emissionColor = Color.yellow;

        [Tooltip("Emission 강도 (HDRP는 높은 값 사용, Bloom 없으면 더 높게)")]
        [Range(0f, 100f)]
        [SerializeField] private float _emissionIntensity = 20f;

        [Tooltip("Rim Light 색상 (테두리 강조)")]
        [SerializeField] private Color _rimColor = Color.yellow;

        [Tooltip("Rim Light 강도")]
        [Range(0f, 10f)]
        [SerializeField] private float _rimIntensity = 2f;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private Renderer _renderer;
        private Material _material;
        private Vector3 _originalScale;
        private Color _originalEmission;
        private float _originalRim;
        private bool _hasEmission;
        private bool _hasRim;

        private Tween _scaleTween;
        private Tween _emissionTween;
        private Tween _rimTween;

        // HDRP Material Property 이름들
        private static readonly int EmissiveColorID = Shader.PropertyToID("_EmissiveColor");
        private static readonly int EmissiveColorLDRID = Shader.PropertyToID("_EmissiveColorLDR");
        private static readonly int EmissiveIntensityID = Shader.PropertyToID("_EmissiveIntensity");
        private static readonly int EmissiveExposureWeightID = Shader.PropertyToID("_EmissiveExposureWeight");
        private static readonly int EmissionID = Shader.PropertyToID("_EmissionColor"); // 일반적인 이름

        // 대체 프로퍼티 (Material에 따라 다를 수 있음)
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _originalScale = transform.localScale;

            // Material 체크
            if (_renderer != null && _renderer.material != null)
            {
                _material = _renderer.material;

                // HDRP Lit Material의 Emission 프로퍼티 확인
                _hasEmission = _material.HasProperty(EmissiveColorID) ||
                               _material.HasProperty(EmissiveColorLDRID) ||
                               _material.HasProperty(EmissionID);

                if (_hasEmission)
                {
                    // 원본 Emission 저장
                    if (_material.HasProperty(EmissiveColorID))
                    {
                        _originalEmission = _material.GetColor(EmissiveColorID);
                        Debug.Log($"[HDRPOutlineEffect] EmissiveColor 프로퍼티 찾음: {_originalEmission}");
                    }
                    else if (_material.HasProperty(EmissiveColorLDRID))
                    {
                        _originalEmission = _material.GetColor(EmissiveColorLDRID);
                        Debug.Log($"[HDRPOutlineEffect] EmissiveColorLDR 프로퍼티 찾음: {_originalEmission}");
                    }
                    else if (_material.HasProperty(EmissionID))
                    {
                        _originalEmission = _material.GetColor(EmissionID);
                        Debug.Log($"[HDRPOutlineEffect] EmissionColor 프로퍼티 찾음: {_originalEmission}");
                    }

                    // HDRP에서 Emission 활성화
                    _material.EnableKeyword("_EMISSION");
                    _material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

                    Debug.Log($"[HDRPOutlineEffect] Emission 초기화 완료: {gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"[HDRPOutlineEffect] Material이 Emission을 지원하지 않습니다: {gameObject.name}");
                    Debug.LogWarning($"[HDRPOutlineEffect] Material Shader: {_material.shader.name}");

                    // Material의 모든 프로퍼티 출력 (디버깅용)
                    int propertyCount = _material.shader.GetPropertyCount();
                    Debug.Log($"[HDRPOutlineEffect] Material 프로퍼티 개수: {propertyCount}");
                    for (int i = 0; i < propertyCount; i++)
                    {
                        string propName = _material.shader.GetPropertyName(i);
                        if (propName.ToLower().Contains("emis"))
                        {
                            Debug.Log($"[HDRPOutlineEffect] Emission 관련 프로퍼티: {propName}");
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            // DOTween 정리
            _scaleTween?.Kill();
            _emissionTween?.Kill();
            _rimTween?.Kill();

            // Material 복원
            if (_material != null && _hasEmission)
            {
                if (_material.HasProperty(EmissiveColorID))
                {
                    _material.SetColor(EmissiveColorID, _originalEmission);
                }
                else if (_material.HasProperty(EmissionID))
                {
                    _material.SetColor(EmissionID, _originalEmission);
                }
            }
        }

        // =============================================================================
        // 호버 효과
        // =============================================================================

        /// <summary>
        /// 호버 진입 효과
        /// </summary>
        public void OnHoverEnter()
        {
            // Scale 애니메이션
            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(_originalScale * _hoverScale, _hoverDuration)
                .SetEase(Ease.OutCubic);

            // Emission 효과 (HDRP)
            if (_material != null && _hasEmission)
            {
                _emissionTween?.Kill();

                // HDRP는 HDR 색상 사용 (Intensity 곱하기)
                Color targetEmission = _emissionColor * _emissionIntensity;

                // EmissiveColor 프로퍼티 사용
                if (_material.HasProperty(EmissiveColorID))
                {
                    _material.SetColor(EmissiveColorID, targetEmission);
                    Debug.Log($"[HDRPOutlineEffect] EmissiveColor 설정: {targetEmission}");
                }
                // EmissiveColorLDR 프로퍼티
                else if (_material.HasProperty(EmissiveColorLDRID))
                {
                    _material.SetColor(EmissiveColorLDRID, targetEmission);
                    Debug.Log($"[HDRPOutlineEffect] EmissiveColorLDR 설정: {targetEmission}");
                }
                // 대체: EmissionColor 프로퍼티
                else if (_material.HasProperty(EmissionID))
                {
                    _material.SetColor(EmissionID, targetEmission);
                    Debug.Log($"[HDRPOutlineEffect] EmissionColor 설정: {targetEmission}");
                }

                // Emission Intensity 별도 설정 (HDRP 전용)
                if (_material.HasProperty(EmissiveIntensityID))
                {
                    _material.SetFloat(EmissiveIntensityID, _emissionIntensity);
                    Debug.Log($"[HDRPOutlineEffect] EmissiveIntensity 설정: {_emissionIntensity}");
                }

                // HDRP Material Keyword 강제 활성화
                _material.EnableKeyword("_EMISSION");
                _material.EnableKeyword("_EMISSIVE_COLOR_MAP");

                Debug.Log($"[HDRPOutlineEffect] Emission 완전 활성화 - 색상: {targetEmission}, 강도: {_emissionIntensity}");
            }
        }

        /// <summary>
        /// 호버 종료 효과
        /// </summary>
        public void OnHoverExit()
        {
            // Scale 복원
            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(_originalScale, _hoverDuration)
                .SetEase(Ease.InCubic);

            // Emission 복원
            if (_material != null && _hasEmission)
            {
                _emissionTween?.Kill();

                if (_material.HasProperty(EmissiveColorID))
                {
                    _material.SetColor(EmissiveColorID, _originalEmission);
                }
                else if (_material.HasProperty(EmissiveColorLDRID))
                {
                    _material.SetColor(EmissiveColorLDRID, _originalEmission);
                }
                else if (_material.HasProperty(EmissionID))
                {
                    _material.SetColor(EmissionID, _originalEmission);
                }

                Debug.Log($"[HDRPOutlineEffect] Emission 복원: {_originalEmission}");
            }
        }
    }
}
