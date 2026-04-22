// =============================================================================
// DestinationData.cs
// =============================================================================
// 설명: 목적지 정보를 저장하는 ScriptableObject
// 용도: 목적지의 이름, 아이콘, 레이블, 다음 목적지 체인을 정의한다.
// 사용법:
//   1. Project 창에서 우클릭 → Create → GameDatabase → Destination → Destination Data
//   2. 목적지 이름, 아이콘 Sprite, 레이블("M")을 설정한다.
//   3. Next Destination 필드에 다음 목적지 SO를 연결하여 체인을 구성한다.
//   4. DestinationManager의 Start Destination 필드에 첫 번째 SO를 할당한다.
// =============================================================================

using UnityEngine;

namespace GameDatabase.Destination
{
    /// <summary>
    /// 목적지 데이터 ScriptableObject.
    /// Next Destination을 통해 체인 형태로 목적지 순서를 정의한다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDestination", menuName = "GameDatabase/Destination/Destination Data")]
    public class DestinationData : ScriptableObject
    {
        // =============================================================================
        // 기본 정보
        // =============================================================================

        [Header("=== 기본 정보 ===")]

        [Tooltip("목적지 고유 ID (예: DEST_001)")]
        [SerializeField] private string _destinationID;

        [Tooltip("목적지 이름 (플레이어에게 보여질 이름)")]
        [SerializeField] private string _destinationName;

        // =============================================================================
        // UI 설정
        // =============================================================================

        [Header("=== UI 설정 ===")]

        [Tooltip("화면 가장자리 인디케이터에 표시할 아이콘 스프라이트")]
        [SerializeField] private Sprite _icon;

        [Tooltip("아이콘 아래에 표시할 레이블 텍스트 (기본값: 'M')")]
        [SerializeField] private string _label = "M";

        [Tooltip("좌측 상단 UI에 표시할 힌트 텍스트 (예: '형사를 찾아가세요')")]
        [SerializeField] private string _hintText;

        // =============================================================================
        // 목적지 체인
        // =============================================================================

        [Header("=== 목적지 체인 ===")]

        [Tooltip("이 목적지를 완료한 후 이동할 다음 목적지 SO.\n비워두면 체인의 마지막 목적지로 처리된다.")]
        [SerializeField] private DestinationData _nextDestination;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>목적지 고유 ID</summary>
        public string DestinationID => _destinationID;

        /// <summary>목적지 이름</summary>
        public string DestinationName => _destinationName;

        /// <summary>화면 가장자리 아이콘 스프라이트</summary>
        public Sprite Icon => _icon;

        /// <summary>아이콘 아래 레이블 텍스트</summary>
        public string Label => _label;

        /// <summary>다음 목적지 SO. null이면 체인 종료</summary>
        public DestinationData NextDestination => _nextDestination;

        /// <summary>다음 목적지가 존재하는지 여부</summary>
        public bool HasNextDestination => _nextDestination != null;

        /// <summary>좌측 상단 힌트 텍스트</summary>
        public string HintText => _hintText;

        // =============================================================================
        // 유효성 검사
        // =============================================================================

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_destinationID))
                Debug.LogWarning($"[DestinationData] ID가 비어있습니다: {name}");

            if (string.IsNullOrEmpty(_destinationName))
                Debug.LogWarning($"[DestinationData] 이름이 비어있습니다: {name}");

            // 순환 참조 간단 체크 (직접 자기 자신을 가리키는 경우)
            if (_nextDestination == this)
            {
                Debug.LogError($"[DestinationData] '{name}'이 자기 자신을 Next Destination으로 참조하고 있습니다. 순환 참조!");
                _nextDestination = null;
            }
        }
    }
}
