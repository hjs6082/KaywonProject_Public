// =============================================================================
// CharacterData.cs
// =============================================================================
// 설명: 개별 캐릭터의 데이터를 저장하는 ScriptableObject 클래스
// 용도: 캐릭터의 이름과 프리팹을 할당하여 게임에서 사용
// 작성: 모듈화 시스템용 독립 스크립트
// =============================================================================

using UnityEngine;

namespace GameDatabase.Character
{
    /// <summary>
    /// 개별 캐릭터 데이터를 저장하는 ScriptableObject
    /// Unity 에디터에서 에셋으로 생성하여 캐릭터 정보를 관리할 수 있음
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(menuName = "Database/Character/Character Data", fileName = "NewCharacter")]
    public class CharacterData : ScriptableObject
    {
        // =============================================================================
        // 기본 정보
        // =============================================================================

        [Header("기본 정보")]
        [Tooltip("캐릭터의 고유 ID (자동 생성 권장)")]
        [SerializeField] private string _characterId;

        [Tooltip("캐릭터의 표시 이름")]
        [SerializeField] private string _characterName;

        [Tooltip("캐릭터의 설명 (선택사항)")]
        [TextArea(2, 5)]
        [SerializeField] private string _description;

        // =============================================================================
        // 프리팹 참조
        // =============================================================================

        [Header("프리팹")]
        [Tooltip("캐릭터의 3D 또는 2D 프리팹")]
        [SerializeField] private GameObject _characterPrefab;

        [Tooltip("캐릭터의 UI용 프리팹 (선택사항)")]
        [SerializeField] private GameObject _uiPrefab;

        // =============================================================================
        // 프로퍼티 (읽기 전용 접근자)
        // =============================================================================

        /// <summary>
        /// 캐릭터의 고유 ID
        /// </summary>
        public string CharacterId => _characterId;

        /// <summary>
        /// 캐릭터의 표시 이름
        /// </summary>
        public string CharacterName => _characterName;

        /// <summary>
        /// 캐릭터의 설명
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// 캐릭터의 메인 프리팹
        /// </summary>
        public GameObject CharacterPrefab => _characterPrefab;

        /// <summary>
        /// 캐릭터의 UI용 프리팹
        /// </summary>
        public GameObject UIPrefab => _uiPrefab;

        // =============================================================================
        // 유틸리티 메서드
        // =============================================================================

        /// <summary>
        /// 캐릭터 ID가 유효한지 확인
        /// </summary>
        /// <returns>ID가 비어있지 않으면 true</returns>
        public bool HasValidId()
        {
            // ID가 null이 아니고 공백이 아닌지 확인
            return !string.IsNullOrWhiteSpace(_characterId);
        }

        /// <summary>
        /// 프리팹이 할당되어 있는지 확인
        /// </summary>
        /// <returns>프리팹이 존재하면 true</returns>
        public bool HasPrefab()
        {
            // 프리팹 참조가 null이 아닌지 확인
            return _characterPrefab != null;
        }

        /// <summary>
        /// 캐릭터 데이터가 유효한지 확인 (이름과 프리팹 모두 존재)
        /// </summary>
        /// <returns>이름과 프리팹이 모두 유효하면 true</returns>
        public bool IsValid()
        {
            // 이름이 존재하고 프리팹이 할당되어 있는지 확인
            return !string.IsNullOrWhiteSpace(_characterName) && _characterPrefab != null;
        }

        /// <summary>
        /// 캐릭터 프리팹을 인스턴스화 (생성)
        /// </summary>
        /// <param name="position">생성 위치</param>
        /// <param name="rotation">생성 회전값</param>
        /// <returns>생성된 GameObject, 프리팹이 없으면 null</returns>
        public GameObject InstantiateCharacter(Vector3 position, Quaternion rotation)
        {
            // 프리팹이 없으면 경고 로그 출력 후 null 반환
            if (_characterPrefab == null)
            {
                Debug.LogWarning($"[CharacterData] '{_characterName}'의 프리팹이 할당되지 않았습니다.");
                return null;
            }

            // 프리팹을 지정된 위치와 회전값으로 인스턴스화
            return Instantiate(_characterPrefab, position, rotation);
        }

        /// <summary>
        /// 캐릭터 프리팹을 부모 오브젝트 아래에 인스턴스화
        /// </summary>
        /// <param name="parent">부모 Transform</param>
        /// <returns>생성된 GameObject, 프리팹이 없으면 null</returns>
        public GameObject InstantiateCharacter(Transform parent)
        {
            // 프리팹이 없으면 경고 로그 출력 후 null 반환
            if (_characterPrefab == null)
            {
                Debug.LogWarning($"[CharacterData] '{_characterName}'의 프리팹이 할당되지 않았습니다.");
                return null;
            }

            // 프리팹을 부모 아래에 인스턴스화
            return Instantiate(_characterPrefab, parent);
        }

#if UNITY_EDITOR
        // =============================================================================
        // 에디터 전용 메서드
        // =============================================================================

        /// <summary>
        /// 에디터에서 새로운 고유 ID 생성
        /// </summary>
        public void GenerateNewId()
        {
            // GUID를 사용하여 고유 ID 생성
            _characterId = System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 에디터에서 캐릭터 이름 설정
        /// </summary>
        /// <param name="name">설정할 이름</param>
        public void SetName(string name)
        {
            _characterName = name;
        }

        /// <summary>
        /// 에디터에서 프리팹 설정
        /// </summary>
        /// <param name="prefab">설정할 프리팹</param>
        public void SetPrefab(GameObject prefab)
        {
            _characterPrefab = prefab;
        }
#endif
    }
}
