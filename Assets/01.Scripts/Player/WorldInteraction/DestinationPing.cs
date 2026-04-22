// =============================================================================
// DestinationPing.cs
// =============================================================================
// 설명: 목적지 오브젝트에 붙이는 핑 마커 컴포넌트
// 용도: 할당된 DestinationData SO와 DestinationManager의 현재 목적지가 일치할 때
//       DestinationPingUI에 등록되어 화면 가장자리 방향 아이콘을 표시한다.
// 사용법:
//   1. 목적지 GameObject에 이 컴포넌트를 붙인다.
//   2. Destination Data 필드에 해당 목적지의 SO를 할당한다.
//   3. DestinationManager가 이 SO를 현재 목적지로 설정하면 자동으로 핑이 활성화된다.
//   4. 플레이어가 도달하면 Complete()를 호출한다. (WorldInteractable 또는 외부에서 호출)
// =============================================================================

using UnityEngine;
using UnityEngine.Events;
using GameDatabase.UI;
using GameDatabase.Destination;
using Modules.Rendering.Outline;

namespace GameDatabase.Player
{
    /// <summary>
    /// 목적지 오브젝트에 붙이는 핑 마커 컴포넌트.
    /// DestinationManager의 현재 목적지 SO와 일치할 때 화면 가장자리 인디케이터를 표시한다.
    /// </summary>
    public class DestinationPing : MonoBehaviour
    {
        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== 목적지 데이터 ===")]

        [Tooltip("이 오브젝트가 나타내는 목적지 SO.\nDestinationManager의 현재 목적지와 일치하면 핑이 활성화된다.")]
        [SerializeField] private DestinationData _destinationData;

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        [Tooltip("이 목적지가 완료될 때 호출된다.")]
        public UnityEvent OnCompleted;

        // =============================================================================
        // 상태
        // =============================================================================

        private bool _isPingActive = false;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>할당된 목적지 SO</summary>
        public DestinationData DestinationData => _destinationData;

        /// <summary>현재 핑이 화면에 표시 중인지 여부</summary>
        public bool IsPingActive => _isPingActive;

        /// <summary>DestinationPingUI에 전달할 레이블 (SO의 Label 사용)</summary>
        public string Label => _destinationData != null ? _destinationData.Label : "M";

        /// <summary>DestinationPingUI에 전달할 아이콘 (SO의 Icon 사용)</summary>
        public Sprite Icon => _destinationData != null ? _destinationData.Icon : null;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void OnEnable()
        {
            // DestinationManager 이벤트 구독
            if (DestinationManager.Instance != null)
                DestinationManager.Instance.OnDestinationChanged.AddListener(OnDestinationChanged);

            // 현재 목적지와 일치하면 즉시 활성화
            CheckAndUpdatePingState(DestinationManager.Instance?.CurrentDestination);
        }

        private void OnDisable()
        {
            // 이벤트 구독 해제
            if (DestinationManager.Instance != null)
                DestinationManager.Instance.OnDestinationChanged.RemoveListener(OnDestinationChanged);

            DeactivatePing();
        }

        private void OnDestroy()
        {
            if (DestinationManager.Instance != null)
                DestinationManager.Instance.OnDestinationChanged.RemoveListener(OnDestinationChanged);

            DeactivatePing();
        }

        // =============================================================================
        // 목적지 변경 감지
        // =============================================================================

        /// <summary>
        /// DestinationManager.OnDestinationChanged 이벤트 핸들러.
        /// 새로운 목적지가 이 컴포넌트의 SO와 일치하면 핑을 활성화한다.
        /// </summary>
        private void OnDestinationChanged(DestinationData newDestination)
        {
            CheckAndUpdatePingState(newDestination);
        }

        /// <summary>
        /// 주어진 목적지 SO와 자신의 SO를 비교하여 핑 상태를 갱신한다.
        /// </summary>
        private void CheckAndUpdatePingState(DestinationData current)
        {
            if (_destinationData == null) return;

            bool shouldBeActive = (current == _destinationData);

            if (shouldBeActive && !_isPingActive)
                ActivatePing();
            else if (!shouldBeActive && _isPingActive)
                DeactivatePing();
        }

        // =============================================================================
        // 핑 활성/비활성
        // =============================================================================

        private void ActivatePing()
        {
            _isPingActive = true;
            Debug.Log($"[DestinationPing] '{gameObject.name}' ActivatePing → Register 시도");
            DestinationPingUI.Instance?.Register(this);
        }

        private void DeactivatePing()
        {
            _isPingActive = false;
            Debug.Log($"[DestinationPing] '{gameObject.name}' DeactivatePing → Unregister 시도");
            DestinationPingUI.Instance?.Unregister(this);

        }

        // =============================================================================
        // 공개 API
        // =============================================================================

        /// <summary>
        /// 이 목적지를 완료 처리한다.
        /// DestinationManager에 완료를 알려 다음 목적지 SO로 자동 전환한다.
        /// WorldInteractable의 상호작용 콜백이나 외부 코드에서 호출한다.
        /// </summary>
        public void Complete()
        {
            if (!_isPingActive)
            {
                Debug.LogWarning($"[DestinationPing] '{gameObject.name}': 활성 상태가 아닌 목적지를 완료 처리하려 했습니다.");
                return;
            }

            OnCompleted?.Invoke();
            DestinationManager.Instance?.CompleteCurrentDestination();
        }

        /// <summary>
        /// 런타임에 목적지 SO를 교체한다.
        /// DestinationManager의 현재 목적지와 비교해 핑 상태를 즉시 갱신한다.
        /// </summary>
        public void SetDestinationData(DestinationData data)
        {
            _destinationData = data;
            CheckAndUpdatePingState(DestinationManager.Instance?.CurrentDestination);
        }
    }
}
