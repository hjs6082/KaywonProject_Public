// =============================================================================
// NpcDialogueDatabaseEditor.cs
// =============================================================================
// 설명: NpcDialogueDatabase ScriptableObject 커스텀 인스펙터
// 특징:
//   - ReorderableList로 항목 순서 변경 가능
//   - ConditionType에 따라 관련 필드만 표시 (Flag / HasEvidence)
//   - Label을 헤더로 강조 표시
//   - 조건 없음(기본 대화) 항목은 초록 배경으로 강조
//   - Flag 항목은 파란 배경, HasEvidence 항목은 노란 배경
// =============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using GameDatabase.Dialogue;

namespace GameDatabase.Editor
{
    [CustomEditor(typeof(NpcDialogueDatabase))]
    public class NpcDialogueDatabaseEditor : UnityEditor.Editor
    {
        // =====================================================================
        // 상수
        // =====================================================================

        private const float LINE_HEIGHT   = 18f;
        private const float LINE_SPACING  = 2f;
        private const float PADDING       = 6f;
        private const float HEADER_HEIGHT = 20f;

        // =====================================================================
        // 내부 상태
        // =====================================================================

        private ReorderableList _list;
        private SerializedProperty _entriesProp;

        // =====================================================================
        // Unity 에디터 생명주기
        // =====================================================================

        private void OnEnable()
        {
            _entriesProp = serializedObject.FindProperty("_entries");
            BuildList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(4);

            // 상단 안내 박스
            EditorGUILayout.HelpBox(
                "조건부 항목(Flag/HasEvidence)은 None(기본)보다 항상 우선 재생됩니다.\n" +
                "조건부 항목 간 우선순위: 뒤쪽(높은 인덱스)이 먼저 검사됩니다.\n" +
                "None(기본 대화)은 어디에 배치해도 폴백으로만 동작합니다.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            _list.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        // =====================================================================
        // ReorderableList 구성
        // =====================================================================

        private void BuildList()
        {
            _list = new ReorderableList(
                serializedObject, _entriesProp,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true);

            _list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "대화 목록 (역순 우선순위)", EditorStyles.boldLabel);
            };

            _list.elementHeightCallback = index =>
            {
                var entry = _entriesProp.GetArrayElementAtIndex(index);
                return CalculateEntryHeight(entry);
            };

            _list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var entry = _entriesProp.GetArrayElementAtIndex(index);
                DrawEntry(rect, entry, index);
            };

            _list.onAddCallback = list =>
            {
                int idx = _entriesProp.arraySize;
                _entriesProp.InsertArrayElementAtIndex(idx);
                var newEntry = _entriesProp.GetArrayElementAtIndex(idx);
                // 기본값 초기화
                newEntry.FindPropertyRelative("Label").stringValue = $"대화 {idx + 1}";
                newEntry.FindPropertyRelative("Dialogue").objectReferenceValue = null;
                newEntry.FindPropertyRelative("CompletionDialogue").objectReferenceValue = null;
                newEntry.FindPropertyRelative("ConditionType").enumValueIndex = 0;
                newEntry.FindPropertyRelative("FlagKey").stringValue = "";
                newEntry.FindPropertyRelative("FlagValue").boolValue = true;
                newEntry.FindPropertyRelative("RequiredEvidence").objectReferenceValue = null;
            };
        }

        // =====================================================================
        // 높이 계산
        // =====================================================================

        private float CalculateEntryHeight(SerializedProperty entry)
        {
            // 헤더 + Label + Dialogue + CompletionDialogue + ConditionType = 5줄
            float height = PADDING * 2 + HEADER_HEIGHT + (LINE_HEIGHT + LINE_SPACING) * 4;

            var condType = (DialogueConditionType)entry.FindPropertyRelative("ConditionType").enumValueIndex;
            switch (condType)
            {
                case DialogueConditionType.Flag:
                    height += (LINE_HEIGHT + LINE_SPACING) * 2; // FlagKey + FlagValue
                    break;
                case DialogueConditionType.HasEvidence:
                    height += (LINE_HEIGHT + LINE_SPACING) * 1; // RequiredEvidence
                    break;
            }

            return height + PADDING;
        }

        // =====================================================================
        // 항목 그리기
        // =====================================================================

        private void DrawEntry(Rect rect, SerializedProperty entry, int index)
        {
            var condTypeProp = entry.FindPropertyRelative("ConditionType");
            var condType = (DialogueConditionType)condTypeProp.enumValueIndex;

            // 배경 색상
            Color bgColor = condType switch
            {
                DialogueConditionType.None        => new Color(0.2f, 0.7f, 0.2f, 0.15f),
                DialogueConditionType.Flag        => new Color(0.2f, 0.4f, 0.9f, 0.15f),
                DialogueConditionType.HasEvidence => new Color(0.9f, 0.8f, 0.1f, 0.15f),
                _                                 => Color.clear
            };

            // 배경 그리기
            var bgRect = new Rect(rect.x - 2, rect.y + 1, rect.width + 4, rect.height - 2);
            EditorGUI.DrawRect(bgRect, bgColor);

            float x = rect.x + 4;
            float y = rect.y + PADDING;
            float w = rect.width - 8;

            // ── 헤더 (인덱스 + Label) ──
            var labelProp = entry.FindPropertyRelative("Label");
            string headerText = $"[{index}] {(string.IsNullOrEmpty(labelProp.stringValue) ? "(이름 없음)" : labelProp.stringValue)}";
            var headerRect = new Rect(x, y, w, HEADER_HEIGHT);
            EditorGUI.LabelField(headerRect, headerText, EditorStyles.boldLabel);
            y += HEADER_HEIGHT + LINE_SPACING;

            // ── Label 필드 ──
            DrawField(ref y, x, w, labelProp, "메모");

            // ── Dialogue 필드 ──
            DrawField(ref y, x, w, entry.FindPropertyRelative("Dialogue"), "대화 (DialogueData)");

            // ── CompletionDialogue 필드 ──
            DrawField(ref y, x, w, entry.FindPropertyRelative("CompletionDialogue"), "완료 대화 (선택지 클리어 후)");

            // ── ConditionType ──
            DrawField(ref y, x, w, condTypeProp, "조건 타입");

            // ── 조건 타입별 추가 필드 ──
            switch (condType)
            {
                case DialogueConditionType.Flag:
                    DrawField(ref y, x, w, entry.FindPropertyRelative("FlagKey"), "플래그 키");
                    DrawField(ref y, x, w, entry.FindPropertyRelative("FlagValue"), "기대 값");
                    break;

                case DialogueConditionType.HasEvidence:
                    DrawField(ref y, x, w, entry.FindPropertyRelative("RequiredEvidence"), "필요 증거물");
                    break;
            }
        }

        // =====================================================================
        // 헬퍼
        // =====================================================================

        private void DrawField(ref float y, float x, float w, SerializedProperty prop, string label)
        {
            var fieldRect = new Rect(x, y, w, LINE_HEIGHT);
            EditorGUI.PropertyField(fieldRect, prop, new GUIContent(label));
            y += LINE_HEIGHT + LINE_SPACING;
        }
    }
}
#endif
