// =============================================================================
// CharacterDatabaseEditor.cs
// =============================================================================
// 설명: CharacterDatabase의 커스텀 에디터 인스펙터
// 용도: Unity 에디터에서 캐릭터 데이터베이스를 시각적으로 관리
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
    /// CharacterDatabase용 커스텀 에디터
    /// 캐릭터 목록을 시각적으로 표시하고 관리하는 기능 제공
    /// </summary>
    [CustomEditor(typeof(CharacterDatabase))]
    public class CharacterDatabaseEditor : UnityEditor.Editor
    {
        // =============================================================================
        // 필드
        // =============================================================================

        // 대상 데이터베이스 참조
        private CharacterDatabase _database;

        // 스크롤 뷰 위치
        private Vector2 _scrollPosition;

        // 폴드아웃 상태 저장 (캐릭터별)
        private bool[] _foldouts;

        // =============================================================================
        // 초기화
        // =============================================================================

        /// <summary>
        /// 에디터 활성화 시 호출
        /// </summary>
        private void OnEnable()
        {
            // 대상 오브젝트를 CharacterDatabase로 캐스팅
            _database = target as CharacterDatabase;

            // 폴드아웃 배열 초기화
            InitializeFoldouts();
        }

        /// <summary>
        /// 폴드아웃 배열 초기화
        /// </summary>
        private void InitializeFoldouts()
        {
            // 데이터베이스가 null이 아니면 캐릭터 수만큼 배열 생성
            if (_database != null)
            {
                _foldouts = new bool[_database.Count];
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
            // null 체크
            if (_database == null)
            {
                EditorGUILayout.HelpBox("데이터베이스를 불러올 수 없습니다.", MessageType.Error);
                return;
            }

            // 폴드아웃 배열 크기 동기화
            SyncFoldoutsArray();

            // 헤더 그리기
            DrawHeader();

            // 구분선
            EditorGUILayout.Space(10);
            DrawHorizontalLine();
            EditorGUILayout.Space(10);

            // 툴바 버튼들 그리기
            DrawToolbar();

            EditorGUILayout.Space(10);

            // 캐릭터 리스트 그리기
            DrawCharacterList();

            // 변경 사항 저장
            if (GUI.changed)
            {
                EditorUtility.SetDirty(_database);
            }
        }

        // =============================================================================
        // 헤더
        // =============================================================================

        /// <summary>
        /// 헤더 섹션 그리기
        /// </summary>
        private void DrawHeader()
        {
            // 중앙 정렬 스타일
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };

            // 헤더 제목
            EditorGUILayout.LabelField("캐릭터 데이터베이스", headerStyle);

            // 캐릭터 수 표시
            GUIStyle countStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic
            };
            EditorGUILayout.LabelField($"등록된 캐릭터: {_database.Count}명", countStyle);
        }

        // =============================================================================
        // 툴바
        // =============================================================================

        /// <summary>
        /// 툴바 버튼 그리기
        /// </summary>
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // 캐릭터 추가 버튼
                if (GUILayout.Button("+ 캐릭터 추가", GUILayout.Height(30)))
                {
                    AddNewCharacterSlot();
                }

                // null 참조 정리 버튼
                if (GUILayout.Button("빈 슬롯 정리", GUILayout.Height(30)))
                {
                    CleanupNullReferences();
                }
            }

            EditorGUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                // 모두 펼치기 버튼
                if (GUILayout.Button("모두 펼치기"))
                {
                    SetAllFoldouts(true);
                }

                // 모두 접기 버튼
                if (GUILayout.Button("모두 접기"))
                {
                    SetAllFoldouts(false);
                }
            }
        }

        // =============================================================================
        // 캐릭터 리스트
        // =============================================================================

        /// <summary>
        /// 캐릭터 리스트 그리기
        /// </summary>
        private void DrawCharacterList()
        {
            // 캐릭터가 없으면 안내 메시지 표시
            if (_database.Count == 0)
            {
                EditorGUILayout.HelpBox("등록된 캐릭터가 없습니다.\n위의 '캐릭터 추가' 버튼을 클릭하여 캐릭터를 추가하세요.", MessageType.Info);
                return;
            }

            // 스크롤 뷰 시작
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // 각 캐릭터 항목 그리기
            for (int i = 0; i < _database.CharacterList.Count; i++)
            {
                DrawCharacterItem(i);
                EditorGUILayout.Space(5);
            }

            // 스크롤 뷰 종료
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 개별 캐릭터 항목 그리기
        /// </summary>
        /// <param name="index">캐릭터 인덱스</param>
        private void DrawCharacterItem(int index)
        {
            CharacterData character = _database.CharacterList[index];

            // 박스 시작
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 인덱스 표시
                    EditorGUILayout.LabelField($"[{index}]", GUILayout.Width(40));

                    // 캐릭터 이름 또는 상태 표시
                    string displayName = character != null
                        ? (string.IsNullOrEmpty(character.CharacterName) ? "(이름 없음)" : character.CharacterName)
                        : "(빈 슬롯)";

                    // 폴드아웃 토글
                    _foldouts[index] = EditorGUILayout.Foldout(_foldouts[index], displayName, true);

                    // 삭제 버튼
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        if (EditorUtility.DisplayDialog("캐릭터 제거",
                            $"'{displayName}'을(를) 데이터베이스에서 제거하시겠습니까?",
                            "제거", "취소"))
                        {
                            RemoveCharacterAt(index);
                            return;
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }

                // 폴드아웃이 펼쳐진 경우 상세 정보 표시
                if (_foldouts[index])
                {
                    EditorGUI.indentLevel++;
                    DrawCharacterDetails(index, character);
                    EditorGUI.indentLevel--;
                }
            }
        }

        /// <summary>
        /// 캐릭터 상세 정보 그리기
        /// </summary>
        /// <param name="index">캐릭터 인덱스</param>
        /// <param name="character">캐릭터 데이터</param>
        private void DrawCharacterDetails(int index, CharacterData character)
        {
            EditorGUILayout.Space(5);

            // 캐릭터 데이터 오브젝트 필드
            EditorGUILayout.LabelField("캐릭터 데이터:", EditorStyles.boldLabel);
            CharacterData newCharacter = (CharacterData)EditorGUILayout.ObjectField(
                character,
                typeof(CharacterData),
                false
            );

            // 변경되었으면 업데이트
            if (newCharacter != character)
            {
                _database.CharacterList[index] = newCharacter;
            }

            // 캐릭터가 null이 아니면 추가 정보 표시
            if (character != null)
            {
                EditorGUILayout.Space(5);

                using (new EditorGUILayout.VerticalScope("HelpBox"))
                {
                    // 캐릭터 정보 표시 (읽기 전용)
                    EditorGUILayout.LabelField("이름:", character.CharacterName ?? "(없음)");
                    EditorGUILayout.LabelField("ID:", character.CharacterId ?? "(없음)");

                    // 프리팹 상태 표시
                    string prefabStatus = character.HasPrefab() ? "할당됨 ✓" : "미할당 ✗";
                    EditorGUILayout.LabelField("프리팹:", prefabStatus);

                    // 프리팹 미리보기
                    if (character.CharacterPrefab != null)
                    {
                        // 프리팹 오브젝트 표시 (수정 불가)
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField("프리팹 참조:", character.CharacterPrefab, typeof(GameObject), false);
                        EditorGUI.EndDisabledGroup();
                    }
                }

                EditorGUILayout.Space(5);

                // 캐릭터 에셋 열기 버튼
                if (GUILayout.Button("캐릭터 에셋 열기"))
                {
                    Selection.activeObject = character;
                    EditorGUIUtility.PingObject(character);
                }
            }
            else
            {
                // null인 경우 안내 메시지
                EditorGUILayout.HelpBox("캐릭터 데이터를 할당해주세요.", MessageType.Warning);
            }
        }

        // =============================================================================
        // 액션 메서드
        // =============================================================================

        /// <summary>
        /// 새 캐릭터 슬롯 추가
        /// </summary>
        private void AddNewCharacterSlot()
        {
            // Undo 기록
            Undo.RecordObject(_database, "Add Character Slot");

            // null 슬롯 추가 (나중에 사용자가 할당)
            _database.CharacterList.Add(null);

            // 폴드아웃 배열 동기화
            SyncFoldoutsArray();

            // 변경 표시
            EditorUtility.SetDirty(_database);
        }

        /// <summary>
        /// 특정 인덱스의 캐릭터 제거
        /// </summary>
        /// <param name="index">제거할 인덱스</param>
        private void RemoveCharacterAt(int index)
        {
            // Undo 기록
            Undo.RecordObject(_database, "Remove Character");

            // 캐릭터 제거
            _database.RemoveCharacterAt(index);

            // 폴드아웃 배열 동기화
            SyncFoldoutsArray();

            // 변경 표시
            EditorUtility.SetDirty(_database);
        }

        /// <summary>
        /// null 참조 정리
        /// </summary>
        private void CleanupNullReferences()
        {
            // Undo 기록
            Undo.RecordObject(_database, "Cleanup Null References");

            // null 참조 제거
            int removedCount = _database.CharacterList.RemoveAll(c => c == null);

            // 폴드아웃 배열 동기화
            SyncFoldoutsArray();

            // 변경 표시
            EditorUtility.SetDirty(_database);

            // 결과 알림
            EditorUtility.DisplayDialog("정리 완료",
                $"{removedCount}개의 빈 슬롯이 제거되었습니다.",
                "확인");
        }

        /// <summary>
        /// 모든 폴드아웃 상태 설정
        /// </summary>
        /// <param name="expanded">펼침 여부</param>
        private void SetAllFoldouts(bool expanded)
        {
            for (int i = 0; i < _foldouts.Length; i++)
            {
                _foldouts[i] = expanded;
            }
        }

        /// <summary>
        /// 폴드아웃 배열 크기를 데이터베이스와 동기화
        /// </summary>
        private void SyncFoldoutsArray()
        {
            // 배열이 null이거나 크기가 다르면 재생성
            if (_foldouts == null || _foldouts.Length != _database.Count)
            {
                bool[] newFoldouts = new bool[_database.Count];

                // 기존 값 복사
                if (_foldouts != null)
                {
                    int copyLength = Mathf.Min(_foldouts.Length, newFoldouts.Length);
                    for (int i = 0; i < copyLength; i++)
                    {
                        newFoldouts[i] = _foldouts[i];
                    }
                }

                _foldouts = newFoldouts;
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
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }
    }
}
#endif
