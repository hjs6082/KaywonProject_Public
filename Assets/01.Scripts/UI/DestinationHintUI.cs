// =============================================================================
// DestinationHintUI.cs
// =============================================================================
// 설명: 현재 목적지의 힌트 텍스트를 좌측 상단에 표시하는 UI
// 용도: DestinationManager의 OnDestinationChanged 이벤트를 구독하여
//       목적지 변경 시 힌트 텍스트를 자동으로 갱신
// 사용법:
//   1. Canvas 하위에 TextMeshProUGUI를 만들고 좌측 상단 앵커로 배치
//   2. 해당 오브젝트에 이 컴포넌트 추가
//   3. _hintText 필드에 TextMeshProUGUI 할당
// =============================================================================

using UnityEngine;
using TMPro;
using GameDatabase.Destination;

namespace GameDatabase.UI
{
    /// <summary>
    /// 현재 목적지 힌트 텍스트를 좌측 상단에 표시하는 UI
    /// </summary>
    public class DestinationHintUI : MonoBehaviour
    {
        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== UI 참조 ===")]

        [Tooltip("힌트 텍스트를 표시할 TextMeshProUGUI")]
        [SerializeField] private TextMeshProUGUI _hintText;

        [Tooltip("힌트가 없을 때 오브젝트 숨김 여부")]
        [SerializeField] private bool _hideWhenEmpty = true;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Start()
        {
            if (DestinationManager.Instance != null)
            {
                DestinationManager.Instance.OnDestinationChanged.AddListener(OnDestinationChanged);
                UpdateHint(DestinationManager.Instance.CurrentDestination);
            }
            else
            {
                Debug.LogWarning("[DestinationHintUI] DestinationManager를 찾을 수 없습니다.");
            }
        }

        private void OnDestroy()
        {
            if (DestinationManager.Instance != null)
                DestinationManager.Instance.OnDestinationChanged.RemoveListener(OnDestinationChanged);
        }

        // =============================================================================
        // 이벤트 핸들러
        // =============================================================================

        private void OnDestinationChanged(DestinationData destination)
        {
            UpdateHint(destination);
        }

        // =============================================================================
        // 내부
        // =============================================================================

        private void UpdateHint(DestinationData destination)
        {
            string hint = destination != null ? destination.HintText : string.Empty;

            if (_hintText != null)
                _hintText.text = hint;

            if (_hideWhenEmpty)
                gameObject.SetActive(!string.IsNullOrEmpty(hint));
        }
    }
}
