// =============================================================================
// DialogueChoice.cs
// =============================================================================
// 설명: 다이얼로그 선택지 시스템
// 용도: 플레이어가 대화 중 선택할 수 있는 선택지와 결과 관리
// =============================================================================

using UnityEngine;

namespace GameDatabase.Dialogue
{
    /// <summary>
    /// 개별 선택지 항목
    /// 플레이어가 선택할 수 있는 하나의 옵션
    /// </summary>
    [System.Serializable]
    public class ChoiceOption
    {
        // 커스텀 에디터에서 UI를 직접 그리므로 [Header] 제거
        public string ChoiceText;
        public DialogueData NextDialogue;
        public int JumpToLineIndex = -1;
        public string ConditionKey;
        public bool ConditionValue = true;
        public string ResultFlagKey;
        public bool ResultFlagValue = true;

        /// <summary>
        /// 이 선택지가 정답인지 여부
        /// true → 선택 시 다음으로 진행 + 정답 카운트
        /// false → 선택 시 WrongAnswerDialogue 재생 후 선택지 재표시
        /// </summary>
        public bool IsCorrectAnswer = false;

        // =============================================================================
        // TODO: 아이템 보상 시스템
        // =============================================================================
        // 향후 아이템 시스템 연동 시 사용
        // [Header("아이템 보상")]
        // public ItemData RewardItem;
        // public int RewardItemCount = 1;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 유효한 선택지인지 확인
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(ChoiceText);

        /// <summary>
        /// 다른 다이얼로그로 이동하는지 확인
        /// </summary>
        public bool HasNextDialogue => NextDialogue != null;

        /// <summary>
        /// 특정 라인으로 점프하는지 확인
        /// </summary>
        public bool HasJumpIndex => JumpToLineIndex >= 0;

        /// <summary>
        /// 조건이 있는지 확인
        /// </summary>
        public bool HasCondition => !string.IsNullOrWhiteSpace(ConditionKey);

        /// <summary>
        /// 결과 플래그가 있는지 확인
        /// </summary>
        public bool HasResultFlag => !string.IsNullOrWhiteSpace(ResultFlagKey);

        // =============================================================================
        // 생성자
        // =============================================================================

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public ChoiceOption()
        {
            JumpToLineIndex = -1;
            ConditionValue = true;
            ResultFlagValue = true;
        }

        /// <summary>
        /// 텍스트로 생성
        /// </summary>
        /// <param name="text">선택지 텍스트</param>
        public ChoiceOption(string text)
        {
            ChoiceText = text;
            JumpToLineIndex = -1;
        }

        /// <summary>
        /// 텍스트와 다음 다이얼로그로 생성
        /// </summary>
        /// <param name="text">선택지 텍스트</param>
        /// <param name="nextDialogue">선택 시 이동할 다이얼로그</param>
        public ChoiceOption(string text, DialogueData nextDialogue)
        {
            ChoiceText = text;
            NextDialogue = nextDialogue;
            JumpToLineIndex = -1;
        }
    }

    /// <summary>
    /// 선택지 그룹
    /// 하나의 선택 시점에서 표시되는 모든 선택지들
    /// </summary>
    [System.Serializable]
    public class DialogueChoice
    {
        // 커스텀 에디터에서 UI를 직접 그리므로 [Header] 제거
        public string ChoiceId;
        public string PromptText;
        public ChoiceOption[] Options;
        [Range(0f, 60f)]
        public float TimeLimit = 0f;
        public int DefaultOptionIndex = -1;
        public bool ShuffleOptions = false;

        /// <summary>
        /// 오답 선택 시 재생할 다이얼로그
        /// 재생 후 이 선택지를 다시 표시함
        /// null이면 오답 피드백 없이 바로 선택지 재표시
        /// </summary>
        public DialogueData WrongAnswerDialogue;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 선택지 수
        /// </summary>
        public int OptionCount => Options?.Length ?? 0;

        /// <summary>
        /// 유효한 선택지가 있는지 확인
        /// </summary>
        public bool HasValidOptions
        {
            get
            {
                if (Options == null || Options.Length == 0)
                    return false;

                // 최소 하나의 유효한 옵션이 있는지 확인
                foreach (var option in Options)
                {
                    if (option != null && option.IsValid)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 시간 제한이 있는지 확인
        /// </summary>
        public bool HasTimeLimit => TimeLimit > 0f;

        /// <summary>
        /// 프롬프트 텍스트가 있는지 확인
        /// </summary>
        public bool HasPrompt => !string.IsNullOrWhiteSpace(PromptText);

        // =============================================================================
        // 생성자
        // =============================================================================

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public DialogueChoice()
        {
            Options = new ChoiceOption[0];
            DefaultOptionIndex = -1;
        }

        /// <summary>
        /// 옵션 배열로 생성
        /// </summary>
        /// <param name="options">선택지 옵션들</param>
        public DialogueChoice(params ChoiceOption[] options)
        {
            Options = options;
            DefaultOptionIndex = -1;
        }

        // =============================================================================
        // 메서드
        // =============================================================================

        /// <summary>
        /// 인덱스로 옵션 가져오기
        /// </summary>
        /// <param name="index">옵션 인덱스</param>
        /// <returns>해당 인덱스의 옵션, 범위 밖이면 null</returns>
        public ChoiceOption GetOption(int index)
        {
            // 인덱스 유효성 검사
            if (Options == null || index < 0 || index >= Options.Length)
            {
                return null;
            }

            return Options[index];
        }

        /// <summary>
        /// 기본 옵션 가져오기 (시간 초과 시 사용)
        /// </summary>
        /// <returns>기본 옵션</returns>
        public ChoiceOption GetDefaultOption()
        {
            // DefaultOptionIndex가 유효하면 해당 옵션 반환
            if (DefaultOptionIndex >= 0 && DefaultOptionIndex < OptionCount)
            {
                return Options[DefaultOptionIndex];
            }

            // 아니면 첫 번째 옵션 반환
            return OptionCount > 0 ? Options[0] : null;
        }

        /// <summary>
        /// 유효한 옵션들만 가져오기
        /// </summary>
        /// <returns>유효한 옵션 배열</returns>
        public ChoiceOption[] GetValidOptions()
        {
            if (Options == null)
                return new ChoiceOption[0];

            // 유효한 옵션만 필터링
            var validOptions = new System.Collections.Generic.List<ChoiceOption>();
            foreach (var option in Options)
            {
                if (option != null && option.IsValid)
                {
                    validOptions.Add(option);
                }
            }

            return validOptions.ToArray();
        }

        /// <summary>
        /// 모든 선택지 텍스트 가져오기
        /// </summary>
        /// <returns>선택지 텍스트 배열</returns>
        public string[] GetOptionTexts()
        {
            if (Options == null)
                return new string[0];

            var texts = new string[Options.Length];
            for (int i = 0; i < Options.Length; i++)
            {
                texts[i] = Options[i]?.ChoiceText ?? "";
            }
            return texts;
        }
    }
}
