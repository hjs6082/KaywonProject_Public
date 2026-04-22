// =============================================================================
// DialogueData.cs
// =============================================================================
// 설명: 하나의 대화 시퀀스를 저장하는 ScriptableObject
// 용도: 여러 대사 라인과 선택지를 포함한 대화 데이터
// 형식: 영화 자막 스타일 (캐릭터이름 : 대사)
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace GameDatabase.Dialogue
{
    /// <summary>
    /// 다이얼로그 노드 타입
    /// 대화 흐름에서 각 노드의 역할을 정의
    /// </summary>
    public enum DialogueNodeType
    {
        Line,       // 일반 대사 라인
        Choice,     // 선택지
        End         // 대화 종료
    }

    /// <summary>
    /// 다이얼로그 노드
    /// 대사 라인 또는 선택지를 담는 컨테이너
    /// </summary>
    [System.Serializable]
    public class DialogueNode
    {
        // 커스텀 에디터(DialogueDataEditor)에서 UI를 직접 그리므로 [Header] 제거
        public DialogueNodeType NodeType = DialogueNodeType.Line;
        public DialogueLine Line;
        public DialogueChoice Choice;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 대사 라인인지 확인
        /// </summary>
        public bool IsLine => NodeType == DialogueNodeType.Line;

        /// <summary>
        /// 선택지인지 확인
        /// </summary>
        public bool IsChoice => NodeType == DialogueNodeType.Choice;

        /// <summary>
        /// 종료 노드인지 확인
        /// </summary>
        public bool IsEnd => NodeType == DialogueNodeType.End;

        /// <summary>
        /// 유효한 노드인지 확인
        /// </summary>
        public bool IsValid
        {
            get
            {
                switch (NodeType)
                {
                    case DialogueNodeType.Line:
                        return Line != null && Line.IsValid;
                    case DialogueNodeType.Choice:
                        return Choice != null && Choice.HasValidOptions;
                    case DialogueNodeType.End:
                        return true;
                    default:
                        return false;
                }
            }
        }

        // =============================================================================
        // 생성자
        // =============================================================================

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public DialogueNode()
        {
            NodeType = DialogueNodeType.Line;
            Line = new DialogueLine();
        }

        /// <summary>
        /// 대사 라인으로 생성
        /// </summary>
        /// <param name="line">대사 라인</param>
        public DialogueNode(DialogueLine line)
        {
            NodeType = DialogueNodeType.Line;
            Line = line;
        }

        /// <summary>
        /// 선택지로 생성
        /// </summary>
        /// <param name="choice">선택지</param>
        public DialogueNode(DialogueChoice choice)
        {
            NodeType = DialogueNodeType.Choice;
            Choice = choice;
        }
    }

    /// <summary>
    /// 다이얼로그 데이터 - 하나의 완전한 대화 시퀀스
    /// ScriptableObject로 저장되어 에셋으로 관리
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(menuName = "Database/Dialogue/Dialogue Data", fileName = "NewDialogue")]
    public class DialogueData : ScriptableObject
    {
        // =============================================================================
        // 기본 정보
        // =============================================================================

        // 커스텀 에디터(DialogueDataEditor)에서 UI를 직접 그리므로 [Header] 제거
        [SerializeField] private string _dialogueId;
        [SerializeField] private string _dialogueTitle;
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        // =============================================================================
        // 대화 데이터
        // =============================================================================

        [SerializeField] private List<DialogueNode> _nodes = new List<DialogueNode>();

        // =============================================================================
        // 대화 완료 시 보상
        // =============================================================================

        [SerializeField] private string _completionFlagKey;
        [SerializeField] private bool _completionFlagValue = true;

        // =============================================================================
        // TODO: 아이템 보상 시스템
        // =============================================================================
        // 향후 아이템 시스템 연동 시 구현
        // [Header("아이템 보상")]
        // [SerializeField] private ItemData _rewardItem;
        // [SerializeField] private int _rewardItemCount = 1;

        // =============================================================================
        // 프로퍼티 (읽기 전용)
        // =============================================================================

        /// <summary>
        /// 다이얼로그 ID
        /// </summary>
        public string DialogueId => _dialogueId;

        /// <summary>
        /// 다이얼로그 제목
        /// </summary>
        public string DialogueTitle => _dialogueTitle;

        /// <summary>
        /// 다이얼로그 설명
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// 노드 리스트 (읽기 전용)
        /// </summary>
        public IReadOnlyList<DialogueNode> Nodes => _nodes;

        /// <summary>
        /// 노드 수
        /// </summary>
        public int NodeCount => _nodes.Count;

        /// <summary>
        /// 완료 플래그 키
        /// </summary>
        public string CompletionFlagKey => _completionFlagKey;

        /// <summary>
        /// 완료 플래그 값
        /// </summary>
        public bool CompletionFlagValue => _completionFlagValue;

        // =============================================================================
        // 노드 접근 메서드
        // =============================================================================

        /// <summary>
        /// 인덱스로 노드 가져오기
        /// </summary>
        /// <param name="index">노드 인덱스</param>
        /// <returns>해당 인덱스의 노드, 범위 밖이면 null</returns>
        public DialogueNode GetNode(int index)
        {
            // 인덱스 유효성 검사
            if (index < 0 || index >= _nodes.Count)
            {
                Debug.LogWarning($"[DialogueData] 인덱스 {index}가 범위를 벗어났습니다. (총 {_nodes.Count}개)");
                return null;
            }

            return _nodes[index];
        }

        /// <summary>
        /// 첫 번째 노드 가져오기
        /// </summary>
        /// <returns>첫 번째 노드, 없으면 null</returns>
        public DialogueNode GetFirstNode()
        {
            return _nodes.Count > 0 ? _nodes[0] : null;
        }

        /// <summary>
        /// 다음 노드 가져오기
        /// </summary>
        /// <param name="currentIndex">현재 인덱스</param>
        /// <returns>다음 노드, 없으면 null</returns>
        public DialogueNode GetNextNode(int currentIndex)
        {
            int nextIndex = currentIndex + 1;
            return GetNode(nextIndex);
        }

        /// <summary>
        /// 마지막 노드인지 확인
        /// </summary>
        /// <param name="index">확인할 인덱스</param>
        /// <returns>마지막이면 true</returns>
        public bool IsLastNode(int index)
        {
            return index >= _nodes.Count - 1;
        }

        // =============================================================================
        // 대사 라인 접근 메서드 (편의용)
        // =============================================================================

        /// <summary>
        /// 모든 대사 라인 가져오기 (선택지 제외)
        /// </summary>
        /// <returns>대사 라인 리스트</returns>
        public List<DialogueLine> GetAllLines()
        {
            var lines = new List<DialogueLine>();

            foreach (var node in _nodes)
            {
                if (node.IsLine && node.Line != null)
                {
                    lines.Add(node.Line);
                }
            }

            return lines;
        }

        /// <summary>
        /// 모든 선택지 가져오기
        /// </summary>
        /// <returns>선택지 리스트</returns>
        public List<DialogueChoice> GetAllChoices()
        {
            var choices = new List<DialogueChoice>();

            foreach (var node in _nodes)
            {
                if (node.IsChoice && node.Choice != null)
                {
                    choices.Add(node.Choice);
                }
            }

            return choices;
        }

        // =============================================================================
        // 유틸리티 메서드
        // =============================================================================

        /// <summary>
        /// 유효한 다이얼로그인지 확인
        /// </summary>
        /// <returns>하나 이상의 유효한 노드가 있으면 true</returns>
        public bool IsValid()
        {
            if (_nodes.Count == 0)
                return false;

            // 최소 하나의 유효한 노드가 있는지 확인
            foreach (var node in _nodes)
            {
                if (node != null && node.IsValid)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 대화 내용을 영화 자막 형식의 문자열로 반환
        /// 디버그나 미리보기 용도
        /// </summary>
        /// <returns>포맷된 대화 문자열</returns>
        public string ToSubtitleFormat()
        {
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];

                if (node.IsLine && node.Line != null)
                {
                    sb.AppendLine(node.Line.ToSubtitleFormat());
                }
                else if (node.IsChoice && node.Choice != null)
                {
                    sb.AppendLine("[선택지]");
                    for (int j = 0; j < node.Choice.OptionCount; j++)
                    {
                        var option = node.Choice.Options[j];
                        sb.AppendLine($"  {j + 1}. {option?.ChoiceText ?? "(빈 옵션)"}");
                    }
                }
                else if (node.IsEnd)
                {
                    sb.AppendLine("[대화 종료]");
                }
            }

            return sb.ToString();
        }

#if UNITY_EDITOR
        // =============================================================================
        // 에디터 전용 메서드
        // =============================================================================

        /// <summary>
        /// 새 ID 생성
        /// </summary>
        public void GenerateNewId()
        {
            _dialogueId = System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 제목 설정
        /// </summary>
        /// <param name="title">설정할 제목</param>
        public void SetTitle(string title)
        {
            _dialogueTitle = title;
        }

        /// <summary>
        /// 노드 추가
        /// </summary>
        /// <param name="node">추가할 노드</param>
        public void AddNode(DialogueNode node)
        {
            _nodes.Add(node);
        }

        /// <summary>
        /// 대사 라인 추가 (편의 메서드)
        /// </summary>
        /// <param name="speakerName">말하는 캐릭터 이름</param>
        /// <param name="text">대사 내용</param>
        public void AddLine(string speakerName, string text)
        {
            var line = new DialogueLine(speakerName, text);
            var node = new DialogueNode(line);
            _nodes.Add(node);
        }

        /// <summary>
        /// 선택지 추가 (편의 메서드)
        /// </summary>
        /// <param name="choice">선택지</param>
        public void AddChoice(DialogueChoice choice)
        {
            var node = new DialogueNode(choice);
            _nodes.Add(node);
        }

        /// <summary>
        /// 노드 제거
        /// </summary>
        /// <param name="index">제거할 인덱스</param>
        public void RemoveNode(int index)
        {
            if (index >= 0 && index < _nodes.Count)
            {
                _nodes.RemoveAt(index);
            }
        }

        /// <summary>
        /// 모든 노드 제거
        /// </summary>
        public void ClearNodes()
        {
            _nodes.Clear();
        }

        /// <summary>
        /// 노드 리스트 직접 접근 (에디터용)
        /// </summary>
        public List<DialogueNode> NodeList => _nodes;
#endif
    }
}
