// =============================================================================
// CharacterDatabase.cs
// =============================================================================
// 설명: 모든 캐릭터 데이터를 관리하는 데이터베이스 ScriptableObject
// 용도: 게임에서 사용되는 모든 캐릭터를 중앙에서 관리
// 작성: 모듈화 시스템용 독립 스크립트
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace GameDatabase.Character
{
    /// <summary>
    /// 캐릭터 데이터베이스 - 모든 캐릭터 데이터를 관리하는 중앙 저장소
    /// 게임에서 캐릭터를 검색하고 관리하는 기능 제공
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(menuName = "Database/Character/Character Database", fileName = "CharacterDatabase")]
    public class CharacterDatabase : ScriptableObject
    {
        // =============================================================================
        // 데이터 저장소
        // =============================================================================

        [Header("캐릭터 목록")]
        [Tooltip("데이터베이스에 등록된 모든 캐릭터")]
        [SerializeField] private List<CharacterData> _characters = new List<CharacterData>();

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 등록된 캐릭터 목록 (읽기 전용)
        /// </summary>
        public IReadOnlyList<CharacterData> Characters => _characters;

        /// <summary>
        /// 등록된 캐릭터 수
        /// </summary>
        public int Count => _characters.Count;

        // =============================================================================
        // 캐릭터 검색 메서드
        // =============================================================================

        /// <summary>
        /// 인덱스로 캐릭터 가져오기
        /// </summary>
        /// <param name="index">캐릭터 인덱스</param>
        /// <returns>해당 인덱스의 캐릭터, 범위 밖이면 null</returns>
        public CharacterData GetCharacterByIndex(int index)
        {
            // 인덱스 유효성 검사
            if (index < 0 || index >= _characters.Count)
            {
                Debug.LogWarning($"[CharacterDatabase] 인덱스 {index}가 범위를 벗어났습니다. (총 {_characters.Count}개)");
                return null;
            }

            return _characters[index];
        }

        /// <summary>
        /// 이름으로 캐릭터 검색
        /// </summary>
        /// <param name="name">검색할 캐릭터 이름</param>
        /// <returns>찾은 캐릭터, 없으면 null</returns>
        public CharacterData GetCharacterByName(string name)
        {
            // null 또는 빈 문자열 체크
            if (string.IsNullOrWhiteSpace(name))
            {
                Debug.LogWarning("[CharacterDatabase] 검색할 이름이 비어있습니다.");
                return null;
            }

            // 리스트를 순회하며 이름이 일치하는 캐릭터 찾기
            foreach (var character in _characters)
            {
                // null 체크 후 이름 비교
                if (character != null && character.CharacterName == name)
                {
                    return character;
                }
            }

            // 찾지 못한 경우 경고 로그
            Debug.LogWarning($"[CharacterDatabase] '{name}' 이름의 캐릭터를 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// ID로 캐릭터 검색
        /// </summary>
        /// <param name="id">검색할 캐릭터 ID</param>
        /// <returns>찾은 캐릭터, 없으면 null</returns>
        public CharacterData GetCharacterById(string id)
        {
            // null 또는 빈 문자열 체크
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("[CharacterDatabase] 검색할 ID가 비어있습니다.");
                return null;
            }

            // 리스트를 순회하며 ID가 일치하는 캐릭터 찾기
            foreach (var character in _characters)
            {
                // null 체크 후 ID 비교
                if (character != null && character.CharacterId == id)
                {
                    return character;
                }
            }

            // 찾지 못한 경우 경고 로그
            Debug.LogWarning($"[CharacterDatabase] ID '{id}'의 캐릭터를 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 이름에 특정 문자열이 포함된 캐릭터 검색 (부분 일치)
        /// </summary>
        /// <param name="partialName">검색할 문자열</param>
        /// <returns>찾은 캐릭터 리스트</returns>
        public List<CharacterData> FindCharactersByPartialName(string partialName)
        {
            // 결과를 저장할 리스트 생성
            List<CharacterData> results = new List<CharacterData>();

            // null 또는 빈 문자열 체크
            if (string.IsNullOrWhiteSpace(partialName))
            {
                Debug.LogWarning("[CharacterDatabase] 검색할 문자열이 비어있습니다.");
                return results;
            }

            // 리스트를 순회하며 이름에 검색 문자열이 포함된 캐릭터 찾기
            foreach (var character in _characters)
            {
                // null 체크 및 이름에 검색어 포함 여부 확인
                if (character != null &&
                    !string.IsNullOrEmpty(character.CharacterName) &&
                    character.CharacterName.Contains(partialName))
                {
                    results.Add(character);
                }
            }

            return results;
        }

        // =============================================================================
        // 유틸리티 메서드
        // =============================================================================

        /// <summary>
        /// 프리팹이 할당된 캐릭터만 가져오기
        /// </summary>
        /// <returns>프리팹이 있는 캐릭터 리스트</returns>
        public List<CharacterData> GetCharactersWithPrefab()
        {
            // 결과를 저장할 리스트 생성
            List<CharacterData> results = new List<CharacterData>();

            // 리스트를 순회하며 프리팹이 있는 캐릭터 찾기
            foreach (var character in _characters)
            {
                // null 체크 및 프리팹 존재 여부 확인
                if (character != null && character.HasPrefab())
                {
                    results.Add(character);
                }
            }

            return results;
        }

        /// <summary>
        /// 모든 캐릭터 이름 목록 가져오기
        /// </summary>
        /// <returns>캐릭터 이름 배열</returns>
        public string[] GetAllCharacterNames()
        {
            // 이름을 저장할 리스트 생성
            List<string> names = new List<string>();

            // 리스트를 순회하며 이름 수집
            foreach (var character in _characters)
            {
                // null 체크 후 이름 추가
                if (character != null)
                {
                    names.Add(character.CharacterName ?? "이름 없음");
                }
                else
                {
                    names.Add("(빈 슬롯)");
                }
            }

            return names.ToArray();
        }

        /// <summary>
        /// 캐릭터가 데이터베이스에 존재하는지 확인
        /// </summary>
        /// <param name="character">확인할 캐릭터</param>
        /// <returns>존재하면 true</returns>
        public bool Contains(CharacterData character)
        {
            return _characters.Contains(character);
        }

        /// <summary>
        /// 캐릭터의 인덱스 찾기
        /// </summary>
        /// <param name="character">찾을 캐릭터</param>
        /// <returns>인덱스, 없으면 -1</returns>
        public int IndexOf(CharacterData character)
        {
            return _characters.IndexOf(character);
        }

#if UNITY_EDITOR
        // =============================================================================
        // 에디터 전용 메서드
        // =============================================================================

        /// <summary>
        /// 에디터에서 캐릭터 추가
        /// </summary>
        /// <param name="character">추가할 캐릭터</param>
        public void AddCharacter(CharacterData character)
        {
            // null 체크
            if (character == null)
            {
                Debug.LogWarning("[CharacterDatabase] 추가할 캐릭터가 null입니다.");
                return;
            }

            // 중복 체크
            if (_characters.Contains(character))
            {
                Debug.LogWarning($"[CharacterDatabase] '{character.CharacterName}'은(는) 이미 데이터베이스에 존재합니다.");
                return;
            }

            // 캐릭터 추가
            _characters.Add(character);
        }

        /// <summary>
        /// 에디터에서 캐릭터 제거
        /// </summary>
        /// <param name="character">제거할 캐릭터</param>
        /// <returns>제거 성공 여부</returns>
        public bool RemoveCharacter(CharacterData character)
        {
            return _characters.Remove(character);
        }

        /// <summary>
        /// 에디터에서 특정 인덱스의 캐릭터 제거
        /// </summary>
        /// <param name="index">제거할 인덱스</param>
        public void RemoveCharacterAt(int index)
        {
            // 인덱스 유효성 검사
            if (index < 0 || index >= _characters.Count)
            {
                Debug.LogWarning($"[CharacterDatabase] 인덱스 {index}가 범위를 벗어났습니다.");
                return;
            }

            _characters.RemoveAt(index);
        }

        /// <summary>
        /// 에디터에서 모든 캐릭터 제거
        /// </summary>
        public void ClearAllCharacters()
        {
            _characters.Clear();
        }

        /// <summary>
        /// null 참조 정리 (삭제된 에셋 참조 제거)
        /// </summary>
        public void CleanupNullReferences()
        {
            // null인 항목 제거
            _characters.RemoveAll(c => c == null);
        }

        /// <summary>
        /// 캐릭터 리스트에 직접 접근 (에디터용)
        /// </summary>
        public List<CharacterData> CharacterList => _characters;
#endif
    }
}
