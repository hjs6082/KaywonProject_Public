// =============================================================================
// DialogueDatabase.cs
// =============================================================================
// 설명: 모든 다이얼로그 데이터를 관리하는 데이터베이스
// 용도: 게임에서 사용되는 모든 대화를 중앙에서 관리하고 검색
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace GameDatabase.Dialogue
{
    /// <summary>
    /// 다이얼로그 데이터베이스 - 모든 대화 데이터를 관리하는 중앙 저장소
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(menuName = "Database/Dialogue/Dialogue Database", fileName = "DialogueDatabase")]
    public class DialogueDatabase : ScriptableObject
    {
        // =============================================================================
        // 데이터 저장소
        // =============================================================================

        [Header("다이얼로그 목록")]
        [Tooltip("데이터베이스에 등록된 모든 다이얼로그")]
        [SerializeField] private List<DialogueData> _dialogues = new List<DialogueData>();

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 등록된 다이얼로그 목록 (읽기 전용)
        /// </summary>
        public IReadOnlyList<DialogueData> Dialogues => _dialogues;

        /// <summary>
        /// 등록된 다이얼로그 수
        /// </summary>
        public int Count => _dialogues.Count;

        // =============================================================================
        // 검색 메서드
        // =============================================================================

        /// <summary>
        /// 인덱스로 다이얼로그 가져오기
        /// </summary>
        /// <param name="index">다이얼로그 인덱스</param>
        /// <returns>해당 인덱스의 다이얼로그, 범위 밖이면 null</returns>
        public DialogueData GetDialogueByIndex(int index)
        {
            // 인덱스 유효성 검사
            if (index < 0 || index >= _dialogues.Count)
            {
                Debug.LogWarning($"[DialogueDatabase] 인덱스 {index}가 범위를 벗어났습니다. (총 {_dialogues.Count}개)");
                return null;
            }

            return _dialogues[index];
        }

        /// <summary>
        /// ID로 다이얼로그 검색
        /// </summary>
        /// <param name="id">검색할 다이얼로그 ID</param>
        /// <returns>찾은 다이얼로그, 없으면 null</returns>
        public DialogueData GetDialogueById(string id)
        {
            // null 또는 빈 문자열 체크
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("[DialogueDatabase] 검색할 ID가 비어있습니다.");
                return null;
            }

            // 리스트를 순회하며 ID가 일치하는 다이얼로그 찾기
            foreach (var dialogue in _dialogues)
            {
                if (dialogue != null && dialogue.DialogueId == id)
                {
                    return dialogue;
                }
            }

            Debug.LogWarning($"[DialogueDatabase] ID '{id}'의 다이얼로그를 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 제목으로 다이얼로그 검색
        /// </summary>
        /// <param name="title">검색할 제목</param>
        /// <returns>찾은 다이얼로그, 없으면 null</returns>
        public DialogueData GetDialogueByTitle(string title)
        {
            // null 또는 빈 문자열 체크
            if (string.IsNullOrWhiteSpace(title))
            {
                Debug.LogWarning("[DialogueDatabase] 검색할 제목이 비어있습니다.");
                return null;
            }

            // 리스트를 순회하며 제목이 일치하는 다이얼로그 찾기
            foreach (var dialogue in _dialogues)
            {
                if (dialogue != null && dialogue.DialogueTitle == title)
                {
                    return dialogue;
                }
            }

            Debug.LogWarning($"[DialogueDatabase] 제목 '{title}'의 다이얼로그를 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 제목에 특정 문자열이 포함된 다이얼로그 검색 (부분 일치)
        /// </summary>
        /// <param name="partialTitle">검색할 문자열</param>
        /// <returns>찾은 다이얼로그 리스트</returns>
        public List<DialogueData> FindDialoguesByPartialTitle(string partialTitle)
        {
            var results = new List<DialogueData>();

            if (string.IsNullOrWhiteSpace(partialTitle))
            {
                Debug.LogWarning("[DialogueDatabase] 검색할 문자열이 비어있습니다.");
                return results;
            }

            // 리스트를 순회하며 제목에 검색 문자열이 포함된 다이얼로그 찾기
            foreach (var dialogue in _dialogues)
            {
                if (dialogue != null &&
                    !string.IsNullOrEmpty(dialogue.DialogueTitle) &&
                    dialogue.DialogueTitle.Contains(partialTitle))
                {
                    results.Add(dialogue);
                }
            }

            return results;
        }

        // =============================================================================
        // 유틸리티 메서드
        // =============================================================================

        /// <summary>
        /// 모든 다이얼로그 제목 가져오기
        /// </summary>
        /// <returns>다이얼로그 제목 배열</returns>
        public string[] GetAllDialogueTitles()
        {
            var titles = new List<string>();

            foreach (var dialogue in _dialogues)
            {
                if (dialogue != null)
                {
                    titles.Add(dialogue.DialogueTitle ?? "제목 없음");
                }
                else
                {
                    titles.Add("(빈 슬롯)");
                }
            }

            return titles.ToArray();
        }

        /// <summary>
        /// 유효한 다이얼로그만 가져오기
        /// </summary>
        /// <returns>유효한 다이얼로그 리스트</returns>
        public List<DialogueData> GetValidDialogues()
        {
            var results = new List<DialogueData>();

            foreach (var dialogue in _dialogues)
            {
                if (dialogue != null && dialogue.IsValid())
                {
                    results.Add(dialogue);
                }
            }

            return results;
        }

        /// <summary>
        /// 다이얼로그가 데이터베이스에 존재하는지 확인
        /// </summary>
        /// <param name="dialogue">확인할 다이얼로그</param>
        /// <returns>존재하면 true</returns>
        public bool Contains(DialogueData dialogue)
        {
            return _dialogues.Contains(dialogue);
        }

        /// <summary>
        /// 다이얼로그의 인덱스 찾기
        /// </summary>
        /// <param name="dialogue">찾을 다이얼로그</param>
        /// <returns>인덱스, 없으면 -1</returns>
        public int IndexOf(DialogueData dialogue)
        {
            return _dialogues.IndexOf(dialogue);
        }

#if UNITY_EDITOR
        // =============================================================================
        // 에디터 전용 메서드
        // =============================================================================

        /// <summary>
        /// 다이얼로그 추가
        /// </summary>
        /// <param name="dialogue">추가할 다이얼로그</param>
        public void AddDialogue(DialogueData dialogue)
        {
            if (dialogue == null)
            {
                Debug.LogWarning("[DialogueDatabase] 추가할 다이얼로그가 null입니다.");
                return;
            }

            if (_dialogues.Contains(dialogue))
            {
                Debug.LogWarning($"[DialogueDatabase] '{dialogue.DialogueTitle}'은(는) 이미 데이터베이스에 존재합니다.");
                return;
            }

            _dialogues.Add(dialogue);
        }

        /// <summary>
        /// 다이얼로그 제거
        /// </summary>
        /// <param name="dialogue">제거할 다이얼로그</param>
        /// <returns>제거 성공 여부</returns>
        public bool RemoveDialogue(DialogueData dialogue)
        {
            return _dialogues.Remove(dialogue);
        }

        /// <summary>
        /// 특정 인덱스의 다이얼로그 제거
        /// </summary>
        /// <param name="index">제거할 인덱스</param>
        public void RemoveDialogueAt(int index)
        {
            if (index >= 0 && index < _dialogues.Count)
            {
                _dialogues.RemoveAt(index);
            }
        }

        /// <summary>
        /// 모든 다이얼로그 제거
        /// </summary>
        public void ClearAllDialogues()
        {
            _dialogues.Clear();
        }

        /// <summary>
        /// null 참조 정리
        /// </summary>
        public void CleanupNullReferences()
        {
            _dialogues.RemoveAll(d => d == null);
        }

        /// <summary>
        /// 다이얼로그 리스트에 직접 접근 (에디터용)
        /// </summary>
        public List<DialogueData> DialogueList => _dialogues;
#endif
    }
}
