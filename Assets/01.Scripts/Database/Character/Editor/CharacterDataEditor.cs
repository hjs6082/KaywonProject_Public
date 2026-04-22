// =============================================================================
// CharacterDataEditor.cs
// =============================================================================
// 설명: CharacterData의 커스텀 에디터 인스펙터
// 용도: Unity 에디터에서 개별 캐릭터 데이터를 시각적으로 편집
// 작성: 모듈화 시스템용 독립 스크립트
// 주의: 이 스크립트는 반드시 Editor 폴더 안에 있어야 합니다!
// =============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using GameDatabase.Character;

namespace GameDatabase.Character.Editor
{
    /// <summary>
    /// CharacterData용 커스텀 에디터
    /// 캐릭터 데이터를 시각적으로 편집하는 기능 제공
    /// </summary>
    [CustomEditor(typeof(CharacterData))]
    public class CharacterDataEditor : UnityEditor.Editor
    {
        // =============================================================================
        // SerializedProperty 필드
        // =============================================================================

        // 기본 정보
        private SerializedProperty _characterIdProp;
        private SerializedProperty _characterNameProp;
        private SerializedProperty _descriptionProp;

        // 프리팹
        private SerializedProperty _characterPrefabProp;
        private SerializedProperty _uiPrefabProp;

        // 대상 참조
        private CharacterData _characterData;

        // 프리팹 미리보기 에디터
        private UnityEditor.Editor _prefabPreviewEditor;

        // =============================================================================
        // 초기화
        // =============================================================================

        /// <summary>
        /// 에디터 활성화 시 호출
        /// </summary>
        private void OnEnable()
        {
            // 대상 오브젝트 참조
            _characterData = target as CharacterData;

            // SerializedProperty 찾기
            _characterIdProp = serializedObject.FindProperty("_characterId");
            _characterNameProp = serializedObject.FindProperty("_characterName");
            _descriptionProp = serializedObject.FindProperty("_description");
            _characterPrefabProp = serializedObject.FindProperty("_characterPrefab");
            _uiPrefabProp = serializedObject.FindProperty("_uiPrefab");
        }

        /// <summary>
        /// 에디터 비활성화 시 호출
        /// </summary>
        private void OnDisable()
        {
            // 미리보기 에디터 정리
            if (_prefabPreviewEditor != null)
            {
                DestroyImmediate(_prefabPreviewEditor);
            }
        }

        // =============================================================================
        // 인스펙터 GUI
        // =============================================================================

        /// <summary>
        /// 커스텀 인스펙터 GUI 그리기
        /// </summary>
        public override void OnInspectorGUI()
        {
            // serializedObject 업데이트
            serializedObject.Update();

            // 헤더
            DrawHeader();

            EditorGUILayout.Space(10);

            // 기본 정보 섹션
            DrawBasicInfoSection();

            EditorGUILayout.Space(10);

            // 프리팹 섹션
            DrawPrefabSection();

            EditorGUILayout.Space(10);

            // 유효성 검사 결과
            DrawValidationSection();

            // 변경 사항 적용
            serializedObject.ApplyModifiedProperties();
        }

        // =============================================================================
        // 섹션 그리기 메서드
        // =============================================================================

        /// <summary>
        /// 헤더 섹션 그리기
        /// </summary>
        private void DrawHeader()
        {
            // 헤더 스타일
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            // 헤더 표시
            EditorGUILayout.LabelField("캐릭터 데이터", headerStyle);

            // 구분선
            DrawHorizontalLine();
        }

        /// <summary>
        /// 기본 정보 섹션 그리기
        /// </summary>
        private void DrawBasicInfoSection()
        {
            // 섹션 헤더
            EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("Box"))
            {
                // ID 필드 (생성 버튼 포함)
                using (new EditorGUILayout.HorizontalScope())
                {
                    // ID 필드
                    EditorGUILayout.PropertyField(_characterIdProp, new GUIContent("캐릭터 ID"));

                    // ID 생성 버튼
                    if (GUILayout.Button("ID 생성", GUILayout.Width(80)))
                    {
                        // Undo 기록
                        Undo.RecordObject(_characterData, "Generate Character ID");

                        // 새 ID 생성
                        _characterData.GenerateNewId();

                        // 변경 표시
                        EditorUtility.SetDirty(_characterData);
                    }
                }

                EditorGUILayout.Space(5);

                // 이름 필드
                EditorGUILayout.PropertyField(_characterNameProp, new GUIContent("캐릭터 이름"));

                EditorGUILayout.Space(5);

                // 설명 필드
                EditorGUILayout.PropertyField(_descriptionProp, new GUIContent("설명"));
            }
        }

        /// <summary>
        /// 프리팹 섹션 그리기
        /// </summary>
        private void DrawPrefabSection()
        {
            // 섹션 헤더
            EditorGUILayout.LabelField("프리팹", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("Box"))
            {
                // 메인 프리팹 필드
                EditorGUILayout.PropertyField(_characterPrefabProp, new GUIContent("캐릭터 프리팹"));

                // UI 프리팹 필드
                EditorGUILayout.PropertyField(_uiPrefabProp, new GUIContent("UI 프리팹 (선택)"));

                // 프리팹 미리보기
                if (_characterPrefabProp.objectReferenceValue != null)
                {
                    EditorGUILayout.Space(10);
                    DrawPrefabPreview();
                }
            }
        }

        /// <summary>
        /// 프리팹 미리보기 그리기
        /// </summary>
        private void DrawPrefabPreview()
        {
            // 미리보기 헤더
            EditorGUILayout.LabelField("프리팹 미리보기:", EditorStyles.miniLabel);

            // 미리보기 영역
            Rect previewRect = GUILayoutUtility.GetRect(150, 150);

            // 프리팹 에디터 생성 또는 재사용
            GameObject prefab = _characterPrefabProp.objectReferenceValue as GameObject;
            if (prefab != null)
            {
                // 미리보기 에디터가 없거나 대상이 변경되었으면 새로 생성
                if (_prefabPreviewEditor == null ||
                    _prefabPreviewEditor.target != prefab)
                {
                    // 기존 에디터 정리
                    if (_prefabPreviewEditor != null)
                    {
                        DestroyImmediate(_prefabPreviewEditor);
                    }

                    // 새 에디터 생성
                    _prefabPreviewEditor = UnityEditor.Editor.CreateEditor(prefab);
                }

                // 미리보기 그리기
                if (_prefabPreviewEditor != null)
                {
                    _prefabPreviewEditor.OnInteractivePreviewGUI(previewRect, EditorStyles.helpBox);
                }
            }
        }

        /// <summary>
        /// 유효성 검사 섹션 그리기
        /// </summary>
        private void DrawValidationSection()
        {
            // 섹션 헤더
            EditorGUILayout.LabelField("상태", EditorStyles.boldLabel);

            // 유효성 검사
            bool hasValidId = _characterData.HasValidId();
            bool hasName = !string.IsNullOrWhiteSpace(_characterNameProp.stringValue);
            bool hasPrefab = _characterPrefabProp.objectReferenceValue != null;
            bool isValid = _characterData.IsValid();

            using (new EditorGUILayout.VerticalScope("Box"))
            {
                // 각 항목 상태 표시
                DrawStatusItem("ID", hasValidId);
                DrawStatusItem("이름", hasName);
                DrawStatusItem("프리팹", hasPrefab);

                EditorGUILayout.Space(5);
                DrawHorizontalLine();
                EditorGUILayout.Space(5);

                // 전체 상태
                if (isValid)
                {
                    EditorGUILayout.HelpBox("✓ 캐릭터 데이터가 유효합니다.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("✗ 이름과 프리팹을 모두 설정해주세요.", MessageType.Warning);
                }
            }
        }

        /// <summary>
        /// 상태 항목 그리기
        /// </summary>
        /// <param name="label">항목 라벨</param>
        /// <param name="isValid">유효 여부</param>
        private void DrawStatusItem(string label, bool isValid)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // 라벨
                EditorGUILayout.LabelField(label + ":", GUILayout.Width(80));

                // 상태 아이콘
                GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
                if (isValid)
                {
                    statusStyle.normal.textColor = new Color(0.2f, 0.8f, 0.2f);
                    EditorGUILayout.LabelField("✓ 설정됨", statusStyle);
                }
                else
                {
                    statusStyle.normal.textColor = new Color(0.8f, 0.4f, 0.2f);
                    EditorGUILayout.LabelField("✗ 미설정", statusStyle);
                }
            }
        }

        // =============================================================================
        // 유틸리티
        // =============================================================================

        /// <summary>
        /// 수평선 그리기
        /// </summary>
        private void DrawHorizontalLine()
        {
            EditorGUILayout.Space(5);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUILayout.Space(5);
        }
    }
}
#endif
