// =============================================================================
// CaseBoardDataEditor.cs
// =============================================================================
// 설명: CaseBoardData 커스텀 인스펙터
// 용도: 사건 보드 데이터를 편리하게 편집
// =============================================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using GameDatabase.Evidence;

namespace GameDatabase.CaseBoard
{
    [CustomEditor(typeof(CaseBoardData))]
    public class CaseBoardDataEditor : UnityEditor.Editor
    {
        // =============================================================================
        // SerializedProperty
        // =============================================================================

        private SerializedProperty _caseBoardID;
        private SerializedProperty _caseBoardTitle;
        private SerializedProperty _caseBoardDescription;
        private SerializedProperty _nodes;
        private SerializedProperty _availableEvidences;
        private SerializedProperty _boardBackground;

        private ReorderableList _nodesList;
        private ReorderableList _evidencesList;

        // =============================================================================
        // 초기화
        // =============================================================================

        private void OnEnable()
        {
            _caseBoardID = serializedObject.FindProperty("_caseBoardID");
            _caseBoardTitle = serializedObject.FindProperty("_caseBoardTitle");
            _caseBoardDescription = serializedObject.FindProperty("_caseBoardDescription");
            _nodes = serializedObject.FindProperty("_nodes");
            _availableEvidences = serializedObject.FindProperty("_availableEvidences");
            _boardBackground = serializedObject.FindProperty("_boardBackground");

            SetupNodesList();
            SetupEvidencesList();
        }

        // =============================================================================
        // 노드 리스트 설정
        // =============================================================================

        private void SetupNodesList()
        {
            _nodesList = new ReorderableList(serializedObject, _nodes, true, true, true, true);

            // 헤더
            _nodesList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, $"노드 목록 ({_nodes.arraySize}개)");
            };

            // 요소 그리기
            _nodesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = _nodes.GetArrayElementAtIndex(index);
                rect.y += 2;
                float lineHeight = EditorGUIUtility.singleLineHeight;
                float spacing = 2f;

                // 1줄: 노드 ID + 제목
                SerializedProperty nodeID = element.FindPropertyRelative("_nodeID");
                SerializedProperty nodeTitle = element.FindPropertyRelative("_nodeTitle");

                float halfWidth = rect.width * 0.35f;
                Rect idRect = new Rect(rect.x, rect.y, halfWidth - 5, lineHeight);
                Rect titleRect = new Rect(rect.x + halfWidth, rect.y, rect.width - halfWidth, lineHeight);

                EditorGUI.PropertyField(idRect, nodeID, new GUIContent("ID"));
                EditorGUI.PropertyField(titleRect, nodeTitle, new GUIContent("제목"));

                rect.y += lineHeight + spacing;

                // 2줄: 설명
                SerializedProperty nodeDesc = element.FindPropertyRelative("_nodeDescription");
                float descHeight = EditorGUI.GetPropertyHeight(nodeDesc);
                Rect descRect = new Rect(rect.x, rect.y, rect.width, descHeight);
                EditorGUI.PropertyField(descRect, nodeDesc, new GUIContent("설명"));

                rect.y += descHeight + spacing;

                // 3줄: 위치
                SerializedProperty pos = element.FindPropertyRelative("_boardPosition");
                Rect posRect = new Rect(rect.x, rect.y, rect.width, lineHeight);
                EditorGUI.PropertyField(posRect, pos, new GUIContent("보드 위치 (0~1)"));

                rect.y += lineHeight + spacing;

                // 4줄: 정답 증거물 ID 목록
                SerializedProperty correctIDs = element.FindPropertyRelative("_correctEvidenceIDs");
                float correctHeight = EditorGUI.GetPropertyHeight(correctIDs, true);
                Rect correctRect = new Rect(rect.x, rect.y, rect.width, correctHeight);
                EditorGUI.PropertyField(correctRect, correctIDs, new GUIContent("정답 증거물 IDs"), true);

                rect.y += correctHeight + spacing;

                // 5줄: 연결 노드 ID 목록
                SerializedProperty connectedIDs = element.FindPropertyRelative("_connectedNodeIDs");
                float connectedHeight = EditorGUI.GetPropertyHeight(connectedIDs, true);
                Rect connectedRect = new Rect(rect.x, rect.y, rect.width, connectedHeight);
                EditorGUI.PropertyField(connectedRect, connectedIDs, new GUIContent("연결 노드 IDs"), true);
            };

            // 요소 높이 콜백 (필드 추가 시 반드시 수정!)
            _nodesList.elementHeightCallback = (int index) =>
            {
                SerializedProperty element = _nodes.GetArrayElementAtIndex(index);
                float lineHeight = EditorGUIUtility.singleLineHeight;
                float spacing = 2f;
                float height = 2f; // 상단 여백

                // ID + 제목
                height += lineHeight + spacing;

                // 설명
                SerializedProperty nodeDesc = element.FindPropertyRelative("_nodeDescription");
                height += EditorGUI.GetPropertyHeight(nodeDesc) + spacing;

                // 위치
                height += lineHeight + spacing;

                // 정답 증거물 IDs
                SerializedProperty correctIDs = element.FindPropertyRelative("_correctEvidenceIDs");
                height += EditorGUI.GetPropertyHeight(correctIDs, true) + spacing;

                // 연결 노드 IDs
                SerializedProperty connectedIDs = element.FindPropertyRelative("_connectedNodeIDs");
                height += EditorGUI.GetPropertyHeight(connectedIDs, true) + spacing;

                return height + 8f; // 하단 여백
            };
        }

        // =============================================================================
        // 증거물 리스트 설정
        // =============================================================================

        private void SetupEvidencesList()
        {
            _evidencesList = new ReorderableList(serializedObject, _availableEvidences, true, true, true, true);

            _evidencesList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, $"사용 가능한 증거물 ({_availableEvidences.arraySize}개)");
            };

            _evidencesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = _availableEvidences.GetArrayElementAtIndex(index);
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;

                if (element.objectReferenceValue != null)
                {
                    EvidenceData evidence = element.objectReferenceValue as EvidenceData;
                    if (evidence != null)
                    {
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

            _evidencesList.elementHeight = EditorGUIUtility.singleLineHeight + 4;
        }

        // =============================================================================
        // Inspector GUI
        // =============================================================================

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("사건 보드 데이터", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 기본 정보
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(_caseBoardID, new GUIContent("보드 ID"));
                if (GUILayout.Button("ID 자동 생성", GUILayout.Width(100)))
                {
                    _caseBoardID.stringValue = $"CASE_{Random.Range(0, 10000):D4}";
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(_caseBoardTitle, new GUIContent("보드 제목"));
                EditorGUILayout.PropertyField(_caseBoardDescription, new GUIContent("보드 설명"));
                EditorGUILayout.PropertyField(_boardBackground, new GUIContent("배경 이미지"));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 노드 목록
            _nodesList.DoLayoutList();

            EditorGUILayout.Space(10);

            // 사용 가능 증거물
            _evidencesList.DoLayoutList();

            EditorGUILayout.Space(10);

            // 유틸리티 버튼
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("데이터 유효성 검사", GUILayout.Height(30)))
                {
                    ValidateData();
                }

                if (GUILayout.Button("모든 노드 ID 자동 생성", GUILayout.Height(30)))
                {
                    GenerateAllNodeIDs();
                }
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        // =============================================================================
        // 유틸리티
        // =============================================================================

        private void ValidateData()
        {
            var issues = new System.Collections.Generic.List<string>();

            // 보드 ID 체크
            if (string.IsNullOrEmpty(_caseBoardID.stringValue))
            {
                issues.Add("보드 ID가 비어있습니다.");
            }

            // 중복 노드 ID 검사
            var nodeIDs = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < _nodes.arraySize; i++)
            {
                SerializedProperty element = _nodes.GetArrayElementAtIndex(i);
                string id = element.FindPropertyRelative("_nodeID").stringValue;

                if (string.IsNullOrEmpty(id))
                {
                    issues.Add($"노드 [{i}]의 ID가 비어있습니다.");
                }
                else if (!nodeIDs.Add(id))
                {
                    issues.Add($"중복 노드 ID: {id}");
                }
            }

            // 연결 노드 ID가 실제 노드에 존재하는지 검사
            for (int i = 0; i < _nodes.arraySize; i++)
            {
                SerializedProperty element = _nodes.GetArrayElementAtIndex(i);
                string nodeID = element.FindPropertyRelative("_nodeID").stringValue;
                SerializedProperty connectedIDs = element.FindPropertyRelative("_connectedNodeIDs");

                for (int j = 0; j < connectedIDs.arraySize; j++)
                {
                    string connectedID = connectedIDs.GetArrayElementAtIndex(j).stringValue;
                    if (!nodeIDs.Contains(connectedID))
                    {
                        issues.Add($"노드 [{nodeID}]의 연결 대상 [{connectedID}]이 존재하지 않습니다.");
                    }
                }
            }

            // 결과 표시
            if (issues.Count > 0)
            {
                EditorUtility.DisplayDialog("유효성 검사 결과",
                    $"발견된 문제 ({issues.Count}개):\n\n{string.Join("\n", issues)}",
                    "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("유효성 검사 완료",
                    "모든 검사를 통과했습니다.",
                    "확인");
            }
        }

        private void GenerateAllNodeIDs()
        {
            for (int i = 0; i < _nodes.arraySize; i++)
            {
                SerializedProperty element = _nodes.GetArrayElementAtIndex(i);
                SerializedProperty nodeID = element.FindPropertyRelative("_nodeID");

                if (string.IsNullOrEmpty(nodeID.stringValue))
                {
                    nodeID.stringValue = $"NODE_{Random.Range(0, 10000):D4}";
                }
            }

            Debug.Log("[CaseBoardDataEditor] 비어있는 노드 ID를 자동 생성했습니다.");
        }
    }
}
#endif
