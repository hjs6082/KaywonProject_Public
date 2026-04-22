// =============================================================================
// SimpleHoverEffect.cs
// =============================================================================
// 설명: URP용 간단한 호버 효과 (Outline 대체)
// 용도: QuickOutline이 작동하지 않는 URP 프로젝트에서 사용
// 효과:
//   - Scale 애니메이션 (1.0 → 1.05)
//   - Emissive Glow 효과 (Material이 Emission을 지원하는 경우)
// =============================================================================

using UnityEngine;
using DG.Tweening;

namespace GameDatabase.Player
{
    /// <summary>
    /// URP용 간단한 호버 효과
    /// InteractableClue의 Outline 효과를 대체
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class SimpleHoverEffect : MonoBehaviour
    {
        [Header("=== 효과 설정 ===")]

        [Tooltip("호버 시 스케일 배율")]
        [Range(1.0f, 1.2f)]
        [SerializeField] private float _hoverScale = 1.05f;

        [Tooltip("호버 애니메이션 시간")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _hoverDuration = 0.3f;

        [Tooltip("Emission 색상 (Material이 Emission을 지원하는 경우)")]
        [SerializeField] private Color _emissionColor = Color.yellow;

        [Tooltip("Emission 강도")]
        [Range(0f, 5f)]
        [SerializeField] private float _emissionIntensity = 1.5f;

        [Tooltip("Emission 효과 사용 (Material에 _EmissionColor가 있어야 함)")]
        [SerializeField] private bool _useEmission = true;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private Renderer _renderer;
        private Material _material;
        private Vector3 _originalScale;
        private Color _originalEmission;
        private bool _hasEmission;

        private Tween _scaleTween;
        private Tween _emissionTween;

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

                // Emission 프로퍼티 확인
                _hasEmission = _material.HasProperty("_EmissionColor");

                if (_hasEmission && _useEmission)
                {
                    _originalEmission = _material.GetColor("_EmissionColor");
                }
            }
        }

        private void OnDestroy()
        {
            // DOTween 정리
            _scaleTween?.Kill();
            _emissionTween?.Kill();

            // Material 복원
            if (_material != null && _hasEmission && _useEmission)
            {
                _material.SetColor("_EmissionColor", _originalEmission);
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

            // Emission 효과
            if (_material != null && _hasEmission && _useEmission)
            {
                _emissionTween?.Kill();

                Color targetEmission = _emissionColor * _emissionIntensity;

                _emissionTween = _material.DOColor(targetEmission, "_EmissionColor", _hoverDuration)
                    .SetEase(Ease.OutCubic);
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
            if (_material != null && _hasEmission && _useEmission)
            {
                _emissionTween?.Kill();
                _emissionTween = _material.DOColor(_originalEmission, "_EmissionColor", _hoverDuration)
                    .SetEase(Ease.InCubic);
            }
        }
    }
}
