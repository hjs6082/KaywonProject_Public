// =============================================================================
// EvidenceDataEditor.cs
// =============================================================================
// 설명: EvidenceData 커스텀 인스펙터
// 용도: 증거물 데이터 편집을 더 편리하게
// =============================================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameDatabase.Evidence
{
    [CustomEditor(typeof(EvidenceData))]
    public class EvidenceDataEditor : UnityEditor.Editor
    {
        private SerializedProperty _evidenceID;
        private SerializedProperty _evidenceName;
        private SerializedProperty _evidenceDescription;
        private SerializedProperty _evidenceImage;

        private void OnEnable()
        {
            _evidenceID = serializedObject.FindProperty("_evidenceID");
            _evidenceName = serializedObject.FindProperty("_evidenceName");
            _evidenceDescription = serializedObject.FindProperty("_evidenceDescription");
            _evidenceImage = serializedObject.FindProperty("_evidenceImage");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 헤더
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("증거물 데이터", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 기본 정보 박스
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                // 증거물 ID 필드와 자동 생성 버튼
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(_evidenceID, new GUIContent("증거물 ID"));

                    if (GUILayout.Button("ID 자동 생성", GUILayout.Width(100)))
                    {
                        GenerateRandomID();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(_evidenceName, new GUIContent("증거물 이름"));
                EditorGUILayout.PropertyField(_evidenceDescription, new GUIContent("증거물 설명"));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 이미지 박스
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("이미지", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                EditorGUILayout.PropertyField(_evidenceImage, new GUIContent("증거물 이미지"));

                // 이미지 미리보기
                if (_evidenceImage.objectReferenceValue != null)
                {
                    Sprite sprite = _evidenceImage.objectReferenceValue as Sprite;
                    if (sprite != null)
                    {
                        EditorGUILayout.Space(5);
                        Rect previewRect = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(false));
                        GUI.DrawTexture(previewRect, sprite.texture, ScaleMode.ScaleToFit);
                    }
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 경고 표시
            if (string.IsNullOrEmpty(_evidenceID.stringValue))
            {
                EditorGUILayout.HelpBox("증거물 ID를 입력해주세요.", MessageType.Warning);
            }

            if (string.IsNullOrEmpty(_evidenceName.stringValue))
            {
                EditorGUILayout.HelpBox("증거물 이름을 입력해주세요.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 랜덤 ID 생성 (EVIDENCE_XXXX 형식)
        /// </summary>
        private void GenerateRandomID()
        {
            // EVIDENCE_XXXX 형식으로 랜덤 ID 생성 (XXXX는 0000~9999)
            int randomNumber = UnityEngine.Random.Range(0, 10000);
            string newID = $"EVIDENCE_{randomNumber:D4}";

            _evidenceID.stringValue = newID;
            serializedObject.ApplyModifiedProperties();

            Debug.Log($"[EvidenceDataEditor] 랜덤 ID 생성: {newID}");
        }
    }
}
#endif
