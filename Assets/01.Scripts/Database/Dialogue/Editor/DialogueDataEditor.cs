// =============================================================================
// DialogueDataEditor.cs
// =============================================================================
// 설명: DialogueData의 커스텀 에디터 인스펙터
// 용도: Unity 에디터에서 다이얼로그 데이터를 시각적으로 편집
// 형식: 영화 자막 스타일 미리보기 제공
// 주의: 이 스크립트는 반드시 Editor 폴더 안에 있어야 합니다!
// =============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using GameDatabase.Dialogue;

namespace GameDatabase.Dialogue.Editor
{
    /// <summary>
    /// DialogueData용 커스텀 에디터
    /// 영화 자막 스타일의 대화 편집 기능 제공
    /// </summary>
    [CustomEditor(typeof(DialogueData))]
    public class DialogueDataEditor : UnityEditor.Editor
    {
        // =============================================================================
        // 필드
        // =============================================================================

        // 대상 참조
        private DialogueData _dialogueData;

        // SerializedProperty
        private SerializedProperty _dialogueIdProp;
        private SerializedProperty _dialogueTitleProp;
        private SerializedProperty _descriptionProp;
        private SerializedProperty _nodesProp;
        private SerializedProperty _completionFlagKeyProp;
        private SerializedProperty _completionFlagValueProp;

        // ReorderableList
        private ReorderableList _nodesList;

        // 스크롤 위치
        private Vector2 _scrollPosition;
        private Vector2 _previewScrollPosition;

        // 폴드아웃 상태
        private bool _showBasicInfo = true;
        private bool _showNodes = true;
        private bool _showCompletion = false;
        private bool _showPreview = false;

        // 노드별 폴드아웃
        private bool[] _nodeFoldouts;

        // 스타일
        private GUIStyle _subtitleStyle;
        private GUIStyle _speakerStyle;
        private bool _stylesInitialized = false;

        // =============================================================================
        // 초기화
        // =============================================================================

        private void OnEnable()
        {
            // 대상 참조
            _dialogueData = target as DialogueData;

            // SerializedProperty 찾기
            _dialogueIdProp = serializedObject.FindProperty("_dialogueId");
            _dialogueTitleProp = serializedObject.FindProperty("_dialogueTitle");
            _descriptionProp = serializedObject.FindProperty("_description");
            _nodesProp = serializedObject.FindProperty("_nodes");
            _completionFlagKeyProp = serializedObject.FindProperty("_completionFlagKey");
            _completionFlagValueProp = serializedObject.FindProperty("_completionFlagValue");

            // 노드 폴드아웃 초기화
            InitializeNodeFoldouts();

            // ReorderableList 설정
            SetupNodesList();
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            // 자막 스타일 (대사 텍스트)
            _subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true,
                fontSize = 12
            };

            // 화자 스타일 (캐릭터 이름 - 녹색)
            _speakerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                fontSize = 12
            };

            _stylesInitialized = true;
        }

        private void InitializeNodeFoldouts()
        {
            if (_dialogueData != null)
            {
                _nodeFoldouts = new bool[_dialogueData.NodeCount];
                for (int i = 0; i < _nodeFoldouts.Length; i++)
                {
                    _nodeFoldouts[i] = true;
                }
            }
        }

        // =============================================================================
        // ReorderableList 설정
        // =============================================================================

        private void SetupNodesList()
        {
            _nodesList = new ReorderableList(serializedObject, _nodesProp, true, true, true, true);

            // 헤더 그리기
            _nodesList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, $"대화 노드 ({_nodesProp.arraySize}개)");
            };

            // 요소 높이
            _nodesList.elementHeightCallback = (int index) =>
            {
                if (index >= _nodeFoldouts.Length)
                    SyncNodeFoldouts();

                if (!_nodeFoldouts[index])
                    return EditorGUIUtility.singleLineHeight + 4;

                var element = _nodesProp.GetArrayElementAtIndex(index);
                var nodeType = element.FindPropertyRelative("NodeType");

                float height = EditorGUIUtility.singleLineHeight * 2 + 8; // 기본 (폴드아웃 + 타입)

                if ((DialogueNodeType)nodeType.enumValueIndex == DialogueNodeType.Line)
                {
                    // 캐릭터 이름 + 대사 + 텍스트속도/자동진행 + 음성클립
                    height += EditorGUIUtility.singleLineHeight * 6 + 22;

                    // 클립이 할당된 경우 길이 안내 박스 추가
                    var line = element.FindPropertyRelative("Line");
                    var voiceClip = line.FindPropertyRelative("VoiceClip");
                    if (voiceClip != null && voiceClip.objectReferenceValue != null)
                        height += EditorGUIUtility.singleLineHeight + 4;

                    // 카메라 설정 필드 높이 추가 (라벨 + CameraId + CameraMovement)
                    height += EditorGUIUtility.singleLineHeight * 3 + 12;

                    // CameraMovement가 None이 아닌 경우 Distance + Duration 추가
                    var cameraMovement = line.FindPropertyRelative("CameraMovement");
                    if (cameraMovement != null && cameraMovement.enumValueIndex != 0)
                        height += EditorGUIUtility.singleLineHeight + 4;
                }
                else if ((DialogueNodeType)nodeType.enumValueIndex == DialogueNodeType.Choice)
                {
                    var choice = element.FindPropertyRelative("Choice");
                    var options = choice.FindPropertyRelative("Options");
                    // 기본 선택지 필드 (프롬프트 + 시간제한 + 라벨 + WrongAnswerDialogue)
                    height += EditorGUIUtility.singleLineHeight * 5 + 14;
                    // 옵션당: ChoiceText + IsCorrectAnswer + NextDialogue
                    height += options.arraySize * (EditorGUIUtility.singleLineHeight * 3 + 12);
                }

                return height;
            };

            // 요소 그리기
            _nodesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index >= _nodeFoldouts.Length)
                    SyncNodeFoldouts();

                var element = _nodesProp.GetArrayElementAtIndex(index);
                DrawNodeElement(rect, element, index);
            };

            // 요소 추가
            _nodesList.onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("대사 라인"), false, () => AddNode(DialogueNodeType.Line));
                menu.AddItem(new GUIContent("선택지"), false, () => AddNode(DialogueNodeType.Choice));
                menu.AddItem(new GUIContent("대화 종료"), false, () => AddNode(DialogueNodeType.End));
                menu.ShowAsContext();
            };

            // 요소 제거
            _nodesList.onRemoveCallback = (ReorderableList list) =>
            {
                _nodesProp.DeleteArrayElementAtIndex(list.index);
                SyncNodeFoldouts();
            };
        }

        private void AddNode(DialogueNodeType type)
        {
            _nodesProp.InsertArrayElementAtIndex(_nodesProp.arraySize);
            var newElement = _nodesProp.GetArrayElementAtIndex(_nodesProp.arraySize - 1);
            newElement.FindPropertyRelative("NodeType").enumValueIndex = (int)type;

            if (type == DialogueNodeType.Line)
            {
                var line = newElement.FindPropertyRelative("Line");
                line.FindPropertyRelative("LineType").enumValueIndex = 0;
                line.FindPropertyRelative("DialogueText").stringValue = "";
                line.FindPropertyRelative("TextSpeed").floatValue = 1f;
            }
            else if (type == DialogueNodeType.Choice)
            {
                var choice = newElement.FindPropertyRelative("Choice");
                choice.FindPropertyRelative("Options").ClearArray();
            }

            SyncNodeFoldouts();
            serializedObject.ApplyModifiedProperties();
        }

        private void SyncNodeFoldouts()
        {
            if (_nodeFoldouts == null || _nodeFoldouts.Length != _nodesProp.arraySize)
            {
                var newFoldouts = new bool[_nodesProp.arraySize];
                if (_nodeFoldouts != null)
                {
                    int copyLen = Mathf.Min(_nodeFoldouts.Length, newFoldouts.Length);
                    for (int i = 0; i < copyLen; i++)
                        newFoldouts[i] = _nodeFoldouts[i];
                }
                for (int i = (_nodeFoldouts?.Length ?? 0); i < newFoldouts.Length; i++)
                    newFoldouts[i] = true;
                _nodeFoldouts = newFoldouts;
            }
        }

        // =============================================================================
        // 인스펙터 GUI
        // =============================================================================

        public override void OnInspectorGUI()
        {
            InitializeStyles();
            serializedObject.Update();

            // 헤더
            DrawHeader();

            EditorGUILayout.Space(10);

            // 기본 정보 섹션
            _showBasicInfo = EditorGUILayout.Foldout(_showBasicInfo, "기본 정보", true);
            if (_showBasicInfo)
            {
                DrawBasicInfoSection();
            }

            EditorGUILayout.Space(5);

            // 대화 노드 섹션
            _showNodes = EditorGUILayout.Foldout(_showNodes, "대화 내용", true);
            if (_showNodes)
            {
                DrawNodesSection();
            }

            EditorGUILayout.Space(5);

            // 완료 보상 섹션
            _showCompletion = EditorGUILayout.Foldout(_showCompletion, "대화 완료 설정", true);
            if (_showCompletion)
            {
                DrawCompletionSection();
            }

            EditorGUILayout.Space(5);

            // 미리보기 섹션
            _showPreview = EditorGUILayout.Foldout(_showPreview, "자막 미리보기", true);
            if (_showPreview)
            {
                DrawPreviewSection();
            }

            serializedObject.ApplyModifiedProperties();
        }

        // =============================================================================
        // 섹션 그리기
        // =============================================================================

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("다이얼로그 데이터", headerStyle);

            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private void DrawBasicInfoSection()
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                // ID
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(_dialogueIdProp, new GUIContent("다이얼로그 ID"));
                    if (GUILayout.Button("ID 생성", GUILayout.Width(80)))
                    {
                        _dialogueIdProp.stringValue = System.Guid.NewGuid().ToString();
                    }
                }

                EditorGUILayout.Space(3);

                // 제목
                EditorGUILayout.PropertyField(_dialogueTitleProp, new GUIContent("제목"));

                EditorGUILayout.Space(3);

                // 설명
                EditorGUILayout.PropertyField(_descriptionProp, new GUIContent("설명"));
            }
        }

        private void DrawNodesSection()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(500));

            _nodesList.DoLayoutList();

            EditorGUILayout.EndScrollView();
        }

        private void DrawNodeElement(Rect rect, SerializedProperty element, int index)
        {
            rect.y += 2;
            float lineHeight = EditorGUIUtility.singleLineHeight;

            var nodeType = element.FindPropertyRelative("NodeType");
            var nodeTypeEnum = (DialogueNodeType)nodeType.enumValueIndex;

            // 노드 타입에 따른 색상
            Color bgColor = nodeTypeEnum switch
            {
                DialogueNodeType.Line => new Color(0.2f, 0.3f, 0.4f, 0.3f),
                DialogueNodeType.Choice => new Color(0.4f, 0.3f, 0.2f, 0.3f),
                DialogueNodeType.End => new Color(0.3f, 0.2f, 0.3f, 0.3f),
                _ => Color.clear
            };
            EditorGUI.DrawRect(new Rect(rect.x - 5, rect.y - 2, rect.width + 10, _nodesList.elementHeightCallback(index)), bgColor);

            // 폴드아웃 헤더
            string nodeLabel = nodeTypeEnum switch
            {
                DialogueNodeType.Line => GetLinePreview(element),
                DialogueNodeType.Choice => "[선택지]",
                DialogueNodeType.End => "[대화 종료]",
                _ => "[알 수 없음]"
            };

            _nodeFoldouts[index] = EditorGUI.Foldout(
                new Rect(rect.x, rect.y, rect.width, lineHeight),
                _nodeFoldouts[index],
                $"#{index + 1} {nodeLabel}",
                true
            );

            if (!_nodeFoldouts[index])
                return;

            rect.y += lineHeight + 4;

            // 노드 타입 선택
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, lineHeight),
                nodeType,
                new GUIContent("노드 타입")
            );
            rect.y += lineHeight + 4;

            // 타입별 내용
            if (nodeTypeEnum == DialogueNodeType.Line)
            {
                DrawLineNode(rect, element.FindPropertyRelative("Line"));
            }
            else if (nodeTypeEnum == DialogueNodeType.Choice)
            {
                DrawChoiceNode(rect, element.FindPropertyRelative("Choice"));
            }
        }

        private string GetLinePreview(SerializedProperty element)
        {
            var line = element.FindPropertyRelative("Line");
            var speakerName = line.FindPropertyRelative("SpeakerName").stringValue;
            var text = line.FindPropertyRelative("DialogueText").stringValue;

            if (text.Length > 30)
                text = text.Substring(0, 30) + "...";

            if (string.IsNullOrEmpty(speakerName))
                return $"\"{text}\"";
            return $"{speakerName} : \"{text}\"";
        }

        private void DrawLineNode(Rect rect, SerializedProperty line)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;

            // 화자 이름
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, lineHeight),
                line.FindPropertyRelative("SpeakerName"),
                new GUIContent("캐릭터 이름")
            );
            rect.y += lineHeight + 2;

            // 대사 (여러 줄)
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, lineHeight * 3),
                line.FindPropertyRelative("DialogueText"),
                new GUIContent("대사")
            );
            rect.y += lineHeight * 3 + 4;

            // 텍스트 속도
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width / 2 - 5, lineHeight),
                line.FindPropertyRelative("TextSpeed"),
                new GUIContent("텍스트 속도")
            );

            // 자동 진행 (음성 없을 때만 의미 있음)
            EditorGUI.PropertyField(
                new Rect(rect.x + rect.width / 2 + 5, rect.y, rect.width / 2 - 5, lineHeight),
                line.FindPropertyRelative("AutoProceed"),
                new GUIContent("자동 진행")
            );
            rect.y += lineHeight + 4;

            // 음성 클립
            var voiceClipProp = line.FindPropertyRelative("VoiceClip");
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, lineHeight),
                voiceClipProp,
                new GUIContent("음성 (MP3)")
            );
            rect.y += lineHeight + 2;

            // 클립이 있으면 길이 표시 + 자동 진행 안내
            if (voiceClipProp.objectReferenceValue is AudioClip clip)
            {
                EditorGUI.HelpBox(
                    new Rect(rect.x, rect.y, rect.width, lineHeight),
                    $"클립 길이: {clip.length:F2}초  →  자동 진행",
                    MessageType.Info
                );
                rect.y += lineHeight + 2;
            }

            // =============================================================
            // 카메라 설정 (시네머신)
            // =============================================================
            rect.y += lineHeight + 6;

            // 구분 라벨
            EditorGUI.LabelField(
                new Rect(rect.x, rect.y, rect.width, lineHeight),
                "카메라 설정",
                EditorStyles.boldLabel
            );
            rect.y += lineHeight + 2;

            // 카메라 ID
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, lineHeight),
                line.FindPropertyRelative("CameraId"),
                new GUIContent("카메라 ID")
            );
            rect.y += lineHeight + 2;

            // 카메라 이동 방향
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, lineHeight),
                line.FindPropertyRelative("CameraMovement"),
                new GUIContent("카메라 이동 방향")
            );

            // CameraMovement가 None이 아닐 때만 Distance, Duration 표시
            var cameraMovementProp = line.FindPropertyRelative("CameraMovement");
            if (cameraMovementProp.enumValueIndex != 0)
            {
                rect.y += lineHeight + 2;

                // 이동 거리
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, rect.width / 2 - 5, lineHeight),
                    line.FindPropertyRelative("CameraMovementDistance"),
                    new GUIContent("이동 거리")
                );

                // 이동 시간
                EditorGUI.PropertyField(
                    new Rect(rect.x + rect.width / 2 + 5, rect.y, rect.width / 2 - 5, lineHeight),
                    line.FindPropertyRelative("CameraMovementDuration"),
                    new GUIContent("이동 시간")
                );
            }
        }

        private void DrawChoiceNode(Rect rect, SerializedProperty choice)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;

            // 프롬프트 텍스트
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, lineHeight),
                choice.FindPropertyRelative("PromptText"),
                new GUIContent("프롬프트")
            );
            rect.y += lineHeight + 2;

            // 시간 제한
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, lineHeight),
                choice.FindPropertyRelative("TimeLimit"),
                new GUIContent("시간 제한")
            );
            rect.y += lineHeight + 4;

            // 옵션들
            var options = choice.FindPropertyRelative("Options");
            EditorGUI.LabelField(new Rect(rect.x, rect.y, 100, lineHeight), "선택지 목록:", EditorStyles.boldLabel);

            // 선택지 추가 버튼
            if (GUI.Button(new Rect(rect.x + rect.width - 80, rect.y, 80, lineHeight), "+ 선택지"))
            {
                options.InsertArrayElementAtIndex(options.arraySize);
            }
            rect.y += lineHeight + 4;

            // 각 옵션
            for (int i = 0; i < options.arraySize; i++)
            {
                var option = options.GetArrayElementAtIndex(i);

                // 옵션 텍스트
                EditorGUI.PropertyField(
                    new Rect(rect.x + 20, rect.y, rect.width - 100, lineHeight),
                    option.FindPropertyRelative("ChoiceText"),
                    new GUIContent($"옵션 {i + 1}")
                );

                // 삭제 버튼
                if (GUI.Button(new Rect(rect.x + rect.width - 70, rect.y, 60, lineHeight), "삭제"))
                {
                    options.DeleteArrayElementAtIndex(i);
                    break;
                }
                rect.y += lineHeight + 2;

                // 정답 여부 (IsCorrectAnswer)
                var isCorrectProp = option.FindPropertyRelative("IsCorrectAnswer");
                // 배경 색상으로 정답/오답 시각적 구분
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = isCorrectProp.boolValue
                    ? new Color(0.2f, 0.8f, 0.2f, 0.5f)
                    : new Color(0.8f, 0.2f, 0.2f, 0.3f);
                EditorGUI.PropertyField(
                    new Rect(rect.x + 40, rect.y, rect.width - 50, lineHeight),
                    isCorrectProp,
                    new GUIContent("✓ 정답")
                );
                GUI.backgroundColor = prevBg;
                rect.y += lineHeight + 2;

                // 다음 다이얼로그
                EditorGUI.PropertyField(
                    new Rect(rect.x + 40, rect.y, rect.width - 50, lineHeight),
                    option.FindPropertyRelative("NextDialogue"),
                    new GUIContent("→ 다음 대화")
                );
                rect.y += lineHeight + 6;
            }

            // 오답 다이얼로그 (선택지 전체 공용)
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, lineHeight),
                choice.FindPropertyRelative("WrongAnswerDialogue"),
                new GUIContent("✗ 오답 대사")
            );
        }

        private void DrawCompletionSection()
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                EditorGUILayout.PropertyField(_completionFlagKeyProp, new GUIContent("완료 플래그 키"));
                EditorGUILayout.PropertyField(_completionFlagValueProp, new GUIContent("플래그 값"));

                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    "TODO: 아이템 보상 시스템\n" +
                    "향후 아이템 시스템 연동 시 대화 완료 보상 아이템을 설정할 수 있습니다.",
                    MessageType.Info
                );
            }
        }

        private void DrawPreviewSection()
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                EditorGUILayout.LabelField("영화 자막 스타일 미리보기", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                _previewScrollPosition = EditorGUILayout.BeginScrollView(
                    _previewScrollPosition,
                    GUILayout.Height(200)
                );

                // 배경
                Rect bgRect = EditorGUILayout.GetControlRect(GUILayout.Height(180));
                EditorGUI.DrawRect(bgRect, new Color(0.1f, 0.1f, 0.1f, 1f));

                // 자막 표시
                float y = bgRect.y + 10;
                for (int i = 0; i < _dialogueData.NodeCount; i++)
                {
                    var node = _dialogueData.GetNode(i);
                    if (node == null) continue;

                    if (node.IsLine && node.Line != null)
                    {
                        // 캐릭터 이름 (녹색)
                        string speaker = node.Line.DisplayName;
                        if (!string.IsNullOrEmpty(speaker))
                        {
                            GUI.Label(
                                new Rect(bgRect.x + 10, y, 150, 20),
                                speaker,
                                _speakerStyle
                            );
                        }

                        // 대사 (흰색)
                        string dialogue = node.Line.DialogueText ?? "";
                        GUI.Label(
                            new Rect(bgRect.x + 170, y, bgRect.width - 180, 40),
                            dialogue,
                            _subtitleStyle
                        );

                        y += 25;
                    }
                    else if (node.IsChoice && node.Choice != null)
                    {
                        GUI.Label(
                            new Rect(bgRect.x + 10, y, bgRect.width - 20, 20),
                            "[선택지]",
                            _subtitleStyle
                        );
                        y += 20;

                        foreach (var option in node.Choice.Options)
                        {
                            if (option != null)
                            {
                                GUI.Label(
                                    new Rect(bgRect.x + 30, y, bgRect.width - 40, 20),
                                    $"• {option.ChoiceText}",
                                    _subtitleStyle
                                );
                                y += 18;
                            }
                        }
                    }
                    else if (node.IsEnd)
                    {
                        GUI.Label(
                            new Rect(bgRect.x + 10, y, bgRect.width - 20, 20),
                            "[대화 종료]",
                            _subtitleStyle
                        );
                        y += 20;
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }
    }
}
#endif
