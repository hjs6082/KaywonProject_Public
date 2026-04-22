// =============================================================================
// DialogueLine.cs
// =============================================================================
// 설명: 개별 대사 라인을 저장하는 클래스
// 용도: 캐릭터가 말하는 한 줄의 대사 데이터
// 형식: 캐릭터이름 : 대사내용 (영화 자막 스타일)
// =============================================================================

using UnityEngine;

namespace GameDatabase.Dialogue
{
    /// <summary>
    /// 대사 라인 타입
    /// 일반 대사인지, 선택지인지, 시스템 메시지인지 구분
    /// </summary>
    public enum DialogueLineType
    {
        Normal,     // 일반 대사 (캐릭터가 말하는 대사)
        Choice,     // 선택지 (플레이어가 선택)
        Narration,  // 나레이션 (캐릭터 없이 표시되는 텍스트)
        System      // 시스템 메시지
    }

    /// <summary>
    /// 개별 대사 라인 데이터
    /// 한 캐릭터가 말하는 한 줄의 대사를 저장
    /// </summary>
    [System.Serializable]
    public class DialogueLine
    {
        // =============================================================================
        // 기본 데이터 (커스텀 에디터에서 UI를 직접 그리므로 [Header] 제거)
        // =============================================================================

        public DialogueLineType LineType = DialogueLineType.Normal;
        public string SpeakerName;
        [TextArea(2, 5)]
        public string DialogueText;

        // =============================================================================
        // 음성 데이터
        // =============================================================================

        public AudioClip VoiceClip;
        [Range(0f, 1f)]
        public float VoiceVolume = 1.0f;

        // =============================================================================
        // 표시 옵션
        // =============================================================================

        [Range(0.5f, 3.0f)]
        public float TextSpeed = 1.0f;
        public bool AutoProceed = false;
        [Range(0f, 10f)]
        public float AutoProceedDelay = 2.0f;

        // =============================================================================
        // 카메라 설정 (시네머신)
        // =============================================================================

        /// <summary>
        /// 이 대사에서 사용할 시네머신 가상 카메라의 식별자
        /// 빈 문자열이면 카메라 전환 없이 현재 카메라 유지
        /// DialogueCameraManager에 등록된 카메라 ID와 매칭
        /// </summary>
        public string CameraId;

        /// <summary>
        /// 카메라 이동 방향
        /// None이면 정적 카메라 (이동 없음)
        /// </summary>
        public CameraMovementType CameraMovement = CameraMovementType.None;

        /// <summary>
        /// 카메라 이동 거리 (월드 단위)
        /// CameraMovement가 None이 아닐 때만 사용
        /// </summary>
        [Range(0.1f, 5f)]
        public float CameraMovementDistance = 1.0f;

        /// <summary>
        /// 카메라 이동 지속시간 (초)
        /// CameraMovement가 None이 아닐 때만 사용
        /// </summary>
        [Range(0.5f, 10f)]
        public float CameraMovementDuration = 2.0f;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 표시할 캐릭터 이름 반환
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(SpeakerName))
                    return SpeakerName;

                return "";
            }
        }

        /// <summary>
        /// 유효한 대사인지 확인
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(DialogueText);

        /// <summary>
        /// 캐릭터가 지정되어 있는지 확인
        /// </summary>
        public bool HasSpeaker => !string.IsNullOrWhiteSpace(SpeakerName);

        /// <summary>
        /// 음성이 있는지 확인
        /// </summary>
        public bool HasVoice => VoiceClip != null;

        /// <summary>
        /// 카메라가 지정되어 있는지 확인
        /// </summary>
        public bool HasCamera => !string.IsNullOrEmpty(CameraId);

        /// <summary>
        /// 음성 길이 (초) - 음성이 없으면 0 반환
        /// </summary>
        public float VoiceDuration => VoiceClip != null ? VoiceClip.length : 0f;

        // =============================================================================
        // 생성자
        // =============================================================================

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public DialogueLine()
        {
            LineType = DialogueLineType.Normal;
            TextSpeed = 1.0f;
            AutoProceed = false;
            AutoProceedDelay = 2.0f;
        }

        /// <summary>
        /// 이름과 대사로 생성
        /// </summary>
        public DialogueLine(string speakerName, string text)
        {
            SpeakerName = speakerName;
            DialogueText = text;
            LineType = DialogueLineType.Normal;
            TextSpeed = 1.0f;
        }

        // =============================================================================
        // 유틸리티 메서드
        // =============================================================================

        /// <summary>
        /// 영화 자막 형식의 문자열로 반환
        /// 형식: "캐릭터이름 : 대사내용"
        /// </summary>
        public string ToSubtitleFormat()
        {
            if (LineType == DialogueLineType.Narration || string.IsNullOrEmpty(DisplayName))
                return DialogueText;

            return $"{DisplayName} : {DialogueText}";
        }

        /// <summary>
        /// 대사를 여러 줄로 분리
        /// </summary>
        public string[] GetTextLines()
        {
            if (string.IsNullOrEmpty(DialogueText))
                return new string[0];

            return DialogueText.Split(new[] { '\n' }, System.StringSplitOptions.None);
        }

        public override string ToString()
        {
            return $"[{LineType}] {DisplayName}: {DialogueText}";
        }
    }
}
