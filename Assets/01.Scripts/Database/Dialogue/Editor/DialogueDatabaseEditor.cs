// =============================================================================
// DialogueDatabaseEditor.cs
// =============================================================================
// 설명: DialogueDatabase의 커스텀 에디터 인스펙터
// 용도: Unity 에디터에서 다이얼로그 데이터베이스를 시각적으로 관리
// 주의: 이 스크립트는 반드시 Editor 폴더 안에 있어야 합니다!
// =============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using GameDatabase.Dialogue;

namespace GameDatabase.Dialogue.Editor
{
    /// <summary>
    /// DialogueDatabase용 커스텀 에디터
    /// 다이얼로그 목록을 시각적으로 표시하고 관리
    /// </summary>
    [CustomEditor(typeof(DialogueDatabase))]
    public class DialogueDatabaseEditor : UnityEditor.Editor
    {
        // =============================================================================
        // 필드
        // =============================================================================

        // 대상 데이터베이스
        private DialogueDatabase _database;

        // 스크롤 위치
        private Vector2 _scrollPosition;

        // 폴드아웃 상태
        private bool[] _foldouts;

        // 검색
        private string _searchQuery = "";

        // =============================================================================
        // 초기화
        // =============================================================================

        private void OnEnable()
        {
            _database = target as DialogueDatabase;
            InitializeFoldouts();
        }

        private void InitializeFoldouts()
        {
            if (_database != null)
            {
                _foldouts = new bool[_database.Count];
            }
        }

        // =============================================================================
        // 인스펙터 GUI
        // =============================================================================

        public override void OnInspectorGUI()
        {
            if (_database == null)
            {
                EditorGUILayout.HelpBox("데이터베이스를 불러올 수 없습니다.", MessageType.Error);
                return;
            }

            SyncFoldoutsArray();

            // 헤더
            DrawHeader();

            EditorGUILayout.Space(10);

            // 툴바
            DrawToolbar();

            EditorGUILayout.Space(10);

            // 검색 바
            DrawSearchBar();

            EditorGUILayout.Space(5);

            // 다이얼로그 리스트
            DrawDialogueList();

            // 변경 저장
            if (GUI.changed)
            {
                EditorUtility.SetDirty(_database);
            }
        }

        // =============================================================================
        // 섹션 그리기
        // =============================================================================

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("다이얼로그 데이터베이스", headerStyle);

            GUIStyle countStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic
            };
            EditorGUILayout.LabelField($"등록된 다이얼로그: {_database.Count}개", countStyle);

            // 구분선
            EditorGUILayout.Space(5);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // 다이얼로그 추가 버튼
                if (GUILayout.Button("+ 다이얼로그 추가", GUILayout.Height(30)))
                {
                    AddDialogueSlot();
                }

                // null 참조 정리
                if (GUILayout.Button("빈 슬롯 정리", GUILayout.Height(30)))
                {
                    CleanupNullReferences();
                }
            }

            EditorGUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("모두 펼치기"))
                {
                    SetAllFoldouts(true);
                }

                if (GUILayout.Button("모두 접기"))
                {
                    SetAllFoldouts(false);
                }
            }
        }

        private void DrawSearchBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("검색:", GUILayout.Width(40));
                _searchQuery = EditorGUILayout.TextField(_searchQuery);

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    _searchQuery = "";
                    GUI.FocusControl(null);
                }
            }
        }

        private void DrawDialogueList()
        {
            if (_database.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "등록된 다이얼로그가 없습니다.\n" +
                    "위의 '다이얼로그 추가' 버튼을 클릭하여 다이얼로그를 추가하세요.",
                    MessageType.Info
                );
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < _database.DialogueList.Count; i++)
            {
                var dialogue = _database.DialogueList[i];

                // 검색 필터
                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    string title = dialogue?.DialogueTitle ?? "";
                    if (!title.ToLower().Contains(_searchQuery.ToLower()))
                        continue;
                }

                DrawDialogueItem(i, dialogue);
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDialogueItem(int index, DialogueData dialogue)
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 인덱스
                    EditorGUILayout.LabelField($"[{index}]", GUILayout.Width(40));

                    // 제목
                    string displayTitle = dialogue != null
                        ? (string.IsNullOrEmpty(dialogue.DialogueTitle) ? "(제목 없음)" : dialogue.DialogueTitle)
                        : "(빈 슬롯)";

                    // 폴드아웃
                    _foldouts[index] = EditorGUILayout.Foldout(_foldouts[index], displayTitle, true);

                    // 노드 수 표시
                    if (dialogue != null)
                    {
                        GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            normal = { textColor = Color.gray }
                        };
                        EditorGUILayout.LabelField($"({dialogue.NodeCount}개 노드)", countStyle, GUILayout.Width(80));
                    }

                    // 삭제 버튼
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        if (EditorUtility.DisplayDialog("다이얼로그 제거",
                            $"'{displayTitle}'을(를) 데이터베이스에서 제거하시겠습니까?",
                            "제거", "취소"))
                        {
                            RemoveDialogueAt(index);
                            return;
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }

                // 펼쳐진 경우 상세 정보
                if (_foldouts[index])
                {
                    EditorGUI.indentLevel++;
                    DrawDialogueDetails(index, dialogue);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawDialogueDetails(int index, DialogueData dialogue)
        {
            EditorGUILayout.Space(5);

            // 다이얼로그 데이터 필드
            EditorGUILayout.LabelField("다이얼로그 데이터:", EditorStyles.boldLabel);
            DialogueData newDialogue = (DialogueData)EditorGUILayout.ObjectField(
                dialogue,
                typeof(DialogueData),
                false
            );

            if (newDialogue != dialogue)
            {
                _database.DialogueList[index] = newDialogue;
            }

            if (dialogue != null)
            {
                EditorGUILayout.Space(5);

                using (new EditorGUILayout.VerticalScope("HelpBox"))
                {
                    // 정보 표시
                    EditorGUILayout.LabelField("ID:", dialogue.DialogueId ?? "(없음)");
                    EditorGUILayout.LabelField("제목:", dialogue.DialogueTitle ?? "(없음)");
                    EditorGUILayout.LabelField("노드 수:", dialogue.NodeCount.ToString());

                    // 유효성 상태
                    bool isValid = dialogue.IsValid();
                    EditorGUILayout.LabelField("상태:", isValid ? "유효함 ✓" : "유효하지 않음 ✗");
                }

                EditorGUILayout.Space(5);

                // 미리보기 (첫 3줄)
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    EditorGUILayout.LabelField("대화 미리보기:", EditorStyles.miniLabel);

                    var lines = dialogue.GetAllLines();
                    int previewCount = Mathf.Min(3, lines.Count);

                    GUIStyle previewStyle = new GUIStyle(EditorStyles.label)
                    {
                        wordWrap = true,
                        fontSize = 10
                    };

                    for (int i = 0; i < previewCount; i++)
                    {
                        string preview = lines[i].ToSubtitleFormat();
                        if (preview.Length > 50)
                            preview = preview.Substring(0, 50) + "...";
                        EditorGUILayout.LabelField(preview, previewStyle);
                    }

                    if (lines.Count > 3)
                    {
                        EditorGUILayout.LabelField($"... 외 {lines.Count - 3}줄", EditorStyles.miniLabel);
                    }
                }

                EditorGUILayout.Space(5);

                // 에셋 열기 버튼
                if (GUILayout.Button("다이얼로그 에셋 열기"))
                {
                    Selection.activeObject = dialogue;
                    EditorGUIUtility.PingObject(dialogue);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("다이얼로그 데이터를 할당해주세요.", MessageType.Warning);
            }
        }

        // =============================================================================
        // 액션 메서드
        // =============================================================================

        private void AddDialogueSlot()
        {
            Undo.RecordObject(_database, "Add Dialogue Slot");
            _database.DialogueList.Add(null);
            SyncFoldoutsArray();
            EditorUtility.SetDirty(_database);
        }

        private void RemoveDialogueAt(int index)
        {
            Undo.RecordObject(_database, "Remove Dialogue");
            _database.RemoveDialogueAt(index);
            SyncFoldoutsArray();
            EditorUtility.SetDirty(_database);
        }

        private void CleanupNullReferences()
        {
            Undo.RecordObject(_database, "Cleanup Null References");
            int removed = _database.DialogueList.RemoveAll(d => d == null);
            SyncFoldoutsArray();
            EditorUtility.SetDirty(_database);
            EditorUtility.DisplayDialog("정리 완료", $"{removed}개의 빈 슬롯이 제거되었습니다.", "확인");
        }

        private void SetAllFoldouts(bool expanded)
        {
            for (int i = 0; i < _foldouts.Length; i++)
            {
                _foldouts[i] = expanded;
            }
        }

        private void SyncFoldoutsArray()
        {
            if (_foldouts == null || _foldouts.Length != _database.Count)
            {
                bool[] newFoldouts = new bool[_database.Count];
                if (_foldouts != null)
                {
                    int copyLen = Mathf.Min(_foldouts.Length, newFoldouts.Length);
                    for (int i = 0; i < copyLen; i++)
                    {
                        newFoldouts[i] = _foldouts[i];
                    }
                }
                _foldouts = newFoldouts;
            }
        }
    }
}
#endif
