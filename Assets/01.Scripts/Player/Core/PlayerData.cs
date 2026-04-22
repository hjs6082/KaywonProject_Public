// =============================================================================
// PlayerData.cs
// =============================================================================
// 설명: 플레이어 설정 데이터를 저장하는 ScriptableObject (선택적)
// 용도: 조사/상호작용 관련 추가 설정 (대부분 설정은 PlayerController에 직접 있음)
// 사용법: Project 창에서 우클릭 > Create > GameDatabase > Player > PlayerData
// =============================================================================

using UnityEngine;

namespace GameDatabase.Player
{
    /// <summary>
    /// 플레이어 설정 데이터 ScriptableObject (선택적)
    /// PlayerController의 public 필드로 대체 가능한 설정들이 많음
    /// </summary>
    [CreateAssetMenu(fileName = "NewPlayerData", menuName = "GameDatabase/Player/PlayerData")]
    public class PlayerData : ScriptableObject
    {
        // =============================================================================
        // 상호작용 설정
        // =============================================================================

        [Header("=== 상호작용 설정 ===")]

        [Tooltip("상호작용 가능 거리 (m)")]
        [Range(1f, 10f)]
        public float interactionRange = 3f;

        [Tooltip("상호작용 가능한 레이어")]
        public LayerMask interactableLayer;

        [Tooltip("상호작용 키")]
        public KeyCode interactKey = KeyCode.E;

        // =============================================================================
        // 조사 모드 설정
        // =============================================================================

        [Header("=== 조사 모드 설정 ===")]

        [Tooltip("조사 모드 전환 키")]
        public KeyCode inspectModeKey = KeyCode.Tab;

        [Tooltip("조사 모드 종료 키")]
        public KeyCode inspectExitKey = KeyCode.Escape;

        [Tooltip("조사 가능한 레이어")]
        public LayerMask inspectableLayer;

        [Tooltip("오브젝트 회전 감도")]
        [Range(0.1f, 10f)]
        public float rotationSensitivity = 5f;

        // =============================================================================
        // 커서 설정
        // =============================================================================

        [Header("=== 커서 설정 ===")]

        [Tooltip("기본 커서 이미지")]
        public Texture2D normalCursor;

        [Tooltip("잡기 가능 커서 이미지")]
        public Texture2D grabCursor;

        [Tooltip("잡는 중 커서 이미지")]
        public Texture2D grabbingCursor;

        [Tooltip("조사 가능 커서 이미지")]
        public Texture2D inspectCursor;

        [Tooltip("상호작용 가능 커서 이미지")]
        public Texture2D interactCursor;

        // =============================================================================
        // 유효성 검사
        // =============================================================================

        /// <summary>
        /// 설정 값의 유효성을 검사합니다.
        /// </summary>
        public bool Validate()
        {
            bool isValid = true;

            if (interactionRange <= 0)
            {
                Debug.LogWarning("[PlayerData] 상호작용 거리가 0 이하입니다.");
                isValid = false;
            }

            return isValid;
        }
    }
}
