// =============================================================================
// NpcDialogueDatabase.cs
// =============================================================================
// 설명: NPC 한 명의 조건부 대화 목록을 묶어 관리하는 ScriptableObject
// 용도: WorldInteractable에 이 에셋 하나만 할당하면 NPC의 모든 대화 흐름 관리
// 사용법:
//   1. Project 창 우클릭 → Create → GameDatabase → Dialogue → NPC Dialogue Database
//   2. 대화 항목 추가, 각 항목에 조건 + DialogueData 설정
//   3. NPC의 WorldInteractable Inspector → Npc Dialogue Database 슬롯에 드래그
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using GameDatabase.Evidence;

namespace GameDatabase.Dialogue
{
    // =========================================================================
    // 조건 타입
    // =========================================================================

    /// <summary>
    /// 대화 재생 조건 타입
    /// </summary>
    public enum DialogueConditionType
    {
        /// <summary>
        /// 조건 없음 - 항상 재생 (기본 대화)
        /// </summary>
        None,

        /// <summary>
        /// 글로벌 플래그 값 체크
        /// </summary>
        Flag,

        /// <summary>
        /// 특정 증거물 소지 여부 체크
        /// </summary>
        HasEvidence,
    }

    // =========================================================================
    // 조건부 대화 항목
    // =========================================================================

    /// <summary>
    /// 조건 + 대화 데이터 한 쌍
    /// NpcDialogueDatabase의 배열 원소로 사용
    /// </summary>
    [System.Serializable]
    public class NpcDialogueEntry
    {
        [Tooltip("이 항목의 설명 (에디터 가독성용, 게임에 영향 없음)")]
        public string Label;

        [Tooltip("재생할 대화")]
        public DialogueData Dialogue;

        [Tooltip("이 대화의 선택지를 모두 클리어한 후 재상호작용 시 재생되는 완료 대화.\n비워두면 완료 후에도 동일 대화 재생.")]
        public DialogueData CompletionDialogue;

        [Tooltip("조건 타입")]
        public DialogueConditionType ConditionType = DialogueConditionType.None;

        // --- Flag 조건 ---
        [Tooltip("[Flag] 체크할 플래그 키")]
        public string FlagKey;

        [Tooltip("[Flag] 기대하는 플래그 값")]
        public bool FlagValue = true;

        // --- HasEvidence 조건 ---
        [Tooltip("[HasEvidence] 소지 여부를 체크할 증거물")]
        public EvidenceData RequiredEvidence;

        /// <summary>
        /// 현재 게임 상태에서 이 항목의 조건이 만족되는지 확인
        /// </summary>
        public bool IsSatisfied()
        {
            switch (ConditionType)
            {
                case DialogueConditionType.None:
                    return true;

                case DialogueConditionType.Flag:
                    if (string.IsNullOrEmpty(FlagKey)) return true;
                    return GameStateManager.Instance.GetFlag(FlagKey, false) == FlagValue;

                case DialogueConditionType.HasEvidence:
                    if (RequiredEvidence == null) return false;
                    var notebook = UI.EvidenceNotebookManager.Instance;
                    if (notebook == null) return false;
                    return notebook.HasEvidence(RequiredEvidence.EvidenceID);

                default:
                    return false;
            }
        }
    }

    // =========================================================================
    // NPC 대화 데이터베이스
    // =========================================================================

    /// <summary>
    /// NPC 한 명의 조건부 대화 목록 ScriptableObject
    /// 배열을 역순 검사해 조건 만족하는 첫 번째 항목 재생
    /// (= 뒤에 있을수록 높은 우선순위 → 기본 대화는 [0]에 배치)
    /// </summary>
    [CreateAssetMenu(menuName = "GameDatabase/Dialogue/NPC Dialogue Database", fileName = "NewNpcDialogueDB")]
    public class NpcDialogueDatabase : ScriptableObject
    {
        [Tooltip("이 NPC의 대화 목록.\n조건부 항목(Flag/HasEvidence)이 None(기본)보다 항상 우선.\n조건부 항목 간 우선순위: 뒤쪽 인덱스가 높음.\nNone(기본 대화)은 어디에 배치해도 폴백으로만 동작.")]
        [SerializeField] private List<NpcDialogueEntry> _entries = new List<NpcDialogueEntry>();

        /// <summary>
        /// 대화 항목 목록 (읽기 전용)
        /// </summary>
        public IReadOnlyList<NpcDialogueEntry> Entries => _entries;

        /// <summary>
        /// 현재 게임 상태에서 재생할 대화 항목 결정
        /// 1차: 조건부 항목(Flag/HasEvidence 등)을 배열 역순으로 검사 → 조건 만족하는 첫 번째 반환
        /// 2차: 조건부 항목이 하나도 만족되지 않으면 None(기본) 항목을 폴백으로 반환
        /// </summary>
        public NpcDialogueEntry ResolveEntry()
        {
            if (_entries == null || _entries.Count == 0) return null;

            NpcDialogueEntry fallback = null;

            // 역순 검사: 뒤쪽(높은 인덱스)이 높은 우선순위
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                if (entry == null || entry.Dialogue == null) continue;

                // None 항목은 폴백으로 저장해두고 건너뜀 (조건부 항목 우선)
                if (entry.ConditionType == DialogueConditionType.None)
                {
                    if (fallback == null) fallback = entry;
                    continue;
                }

                if (entry.IsSatisfied()) return entry;
            }

            // 조건 만족하는 항목이 없으면 None(기본) 폴백 반환
            return fallback;
        }

        /// <summary>
        /// 현재 게임 상태에서 재생할 DialogueData만 반환 (하위 호환용)
        /// </summary>
        public DialogueData Resolve()
        {
            return ResolveEntry()?.Dialogue;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 전용: 항목 리스트 직접 접근
        /// </summary>
        public List<NpcDialogueEntry> EntryList => _entries;
#endif
    }
}
