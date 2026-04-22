// =============================================================================
// EvidenceDatabaseEditor.cs
// =============================================================================
// 설명: EvidenceDatabase 커스텀 인스펙터
// 용도: 증거물 데이터베이스 편집을 더 편리하게
// =============================================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GameDatabase.Evidence
{
    [CustomEditor(typeof(EvidenceDatabase))]
    public class EvidenceDatabaseEditor : UnityEditor.Editor
    {
        private SerializedProperty _evidenceList;
        private ReorderableList _reorderableList;

        private void OnEnable()
        {
            _evidenceList = serializedObject.FindProperty("_evidenceList");

            // ReorderableList 생성
            _reorderableList = new ReorderableList(serializedObject, _evidenceList, true, true, true, true);

            // 헤더 그리기
            _reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, $"증거물 목록 ({_evidenceList.arraySize}개)");
            };

            // 요소 그리기
            _reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = _evidenceList.GetArrayElementAtIndex(index);
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;

                if (element.objectReferenceValue != null)
                {
                    EvidenceData evidence = element.objectReferenceValue as EvidenceData;
                    if (evidence != null)
                    {
                        // ID와 이름 표시
                        string label = $"[{evidence.EvidenceID}] {evidence.EvidenceName}";
                        EditorGUI.PropertyField(rect, element, new GUIContent(label));
                    }
                    else
                    {
                        EditorGUI.PropertyField(rect, element, new GUIContent($"Element {index}"));
                    }
                }
                else
                {
                    EditorGUI.PropertyField(rect, element, new GUIContent($"Element {index} (없음)"));
                }
            };

            // 높이 설정
            _reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 4;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 헤더
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("증거물 데이터베이스", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 통계 표시
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("통계", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"총 증거물 수: {_evidenceList.arraySize}개");
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // ReorderableList 그리기
            _reorderableList.DoLayoutList();

            EditorGUILayout.Space(10);

            // 중복 ID 검사 버튼
            if (GUILayout.Button("중복 ID 검사", GUILayout.Height(30)))
            {
                CheckDuplicateIDs();
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 중복 ID 검사
        /// </summary>
        private void CheckDuplicateIDs()
        {
            var idSet = new System.Collections.Generic.HashSet<string>();
            var duplicates = new System.Collections.Generic.List<string>();

            for (int i = 0; i < _evidenceList.arraySize; i++)
            {
                SerializedProperty element = _evidenceList.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != null)
                {
                    EvidenceData evidence = element.objectReferenceValue as EvidenceData;
                    if (evidence != null)
                    {
                        if (!string.IsNullOrEmpty(evidence.EvidenceID))
                        {
                            if (!idSet.Add(evidence.EvidenceID))
                            {
                                duplicates.Add(evidence.EvidenceID);
                            }
                        }
                    }
                }
            }

            if (duplicates.Count > 0)
            {
                EditorUtility.DisplayDialog("중복 ID 발견",
                    $"중복된 ID가 발견되었습니다:\n{string.Join(", ", duplicates)}",
                    "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("검사 완료",
                    "중복된 ID가 없습니다.",
                    "확인");
            }
        }
    }
}
#endif
