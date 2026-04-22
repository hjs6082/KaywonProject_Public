// =============================================================================
// DialogueUI.cs
// =============================================================================
// 설명: 영화 자막 스타일의 다이얼로그 UI 컴포넌트
// 용도: 화면에 캐릭터 이름(녹색)과 대사를 표시
// 형식: 캐릭터이름 : 대사내용
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using GameDatabase.Dialogue;

namespace GameDatabase.UI
{
    /// <summary>
    /// 다이얼로그 UI 컴포넌트
    /// 영화 자막 스타일로 대사를 화면에 표시
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        // =============================================================================
        // UI 참조
        // =============================================================================

        [Header("UI 요소")]
        [Tooltip("대사 내용을 표시할 텍스트 (스피커네임 : 대사 형식)")]
        [SerializeField] private TextMeshProUGUI _dialogueText;

        [Tooltip("다이얼로그 UI 패널 (전체 컨테이너)")]
        [SerializeField] private GameObject _dialoguePanel;

        [Tooltip("다음 대사 표시 아이콘 (선택사항)")]
        [SerializeField] private GameObject _nextIndicator;

        // =============================================================================
        // 스타일 설정
        // =============================================================================

        [Header("스타일 설정")]
        [Tooltip("캐릭터 이름 색상 (기본: 녹색)")]
        [SerializeField] private Color _speakerNameColor = new Color(0.2f, 0.8f, 0.2f, 1f);

        [Tooltip("대사 텍스트 색상")]
        [SerializeField] private Color _dialogueTextColor = Color.white;

        [Tooltip("나레이션 텍스트 색상")]
        [SerializeField] private Color _narrationTextColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        [Tooltip("시스템 메시지 색상")]
        [SerializeField] private Color _systemTextColor = new Color(1f, 0.8f, 0.2f, 1f);

        // =============================================================================
        // 타이핑 효과 설정
        // =============================================================================

        [Header("타이핑 효과")]
        [Tooltip("타이핑 효과 사용 여부")]
        [SerializeField] private bool _useTypingEffect = true;

        [Tooltip("기본 타이핑 속도 (초당 글자 수)")]
        [SerializeField] private float _baseTypingSpeed = 30f;

        [Tooltip("타이핑 효과 스킵 가능 여부")]
        [SerializeField] private bool _canSkipTyping = true;

        // =============================================================================
        // 오디오 설정
        // =============================================================================

        [Header("오디오")]
        [Tooltip("음성 재생용 AudioSource (없으면 자동 생성)")]
        [SerializeField] private AudioSource _voiceAudioSource;

        [Tooltip("타이핑 효과음 (선택사항)")]
        [SerializeField] private AudioClip _typingSound;

        [Tooltip("타이핑 효과음 볼륨")]
        [Range(0f, 1f)]
        [SerializeField] private float _typingSoundVolume = 0.5f;

        // =============================================================================
        // 내부 상태
        // =============================================================================

        // 현재 표시 중인 대사
        private DialogueLine _currentLine;

        // 타이핑 코루틴 참조
        private Coroutine _typingCoroutine;

        // 타이핑 완료 여부
        private bool _isTypingComplete = false;

        // 타이핑 스킵 요청 여부
        private bool _skipRequested = false;

        // =============================================================================
        // 이벤트
        // =============================================================================

        /// <summary>
        /// 대사 표시 완료 시 호출되는 이벤트
        /// </summary>
        public event System.Action OnLineDisplayComplete;

        /// <summary>
        /// 음성 재생 완료 시 호출되는 이벤트
        /// </summary>
        public event System.Action OnVoiceComplete;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// UI가 활성화되어 있는지 확인
        /// </summary>
        public bool IsActive => _dialoguePanel != null && _dialoguePanel.activeSelf;

        /// <summary>
        /// 타이핑이 진행 중인지 확인
        /// </summary>
        public bool IsTyping => !_isTypingComplete;

        /// <summary>
        /// 음성이 재생 중인지 확인
        /// </summary>
        public bool IsVoicePlaying => _voiceAudioSource != null && _voiceAudioSource.isPlaying;

        /// <summary>
        /// 현재 대사 라인
        /// </summary>
        public DialogueLine CurrentLine => _currentLine;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // AudioSource 자동 생성 (없는 경우)
            if (_voiceAudioSource == null)
            {
                _voiceAudioSource = gameObject.AddComponent<AudioSource>();
                _voiceAudioSource.playOnAwake = false;
            }

            // 초기 상태: 숨김
            Hide();
        }

        private void OnDestroy()
        {
            // 코루틴 정리
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
            }
        }

        // =============================================================================
        // 공개 메서드
        // =============================================================================

        /// <summary>
        /// 다이얼로그 UI 표시
        /// </summary>
        public void Show()
        {
            if (_dialoguePanel != null)
            {
                _dialoguePanel.SetActive(true);
            }
        }

        /// <summary>
        /// 다이얼로그 UI 숨김
        /// </summary>
        public void Hide()
        {
            if (_dialoguePanel != null)
            {
                _dialoguePanel.SetActive(false);
            }

            // 음성 정지
            StopVoice();

            // 타이핑 정지
            StopTyping();

            // 다음 표시 아이콘 숨김
            HideNextIndicator();
        }

        /// <summary>
        /// 대사 라인 표시
        /// </summary>
        /// <param name="line">표시할 대사 라인</param>
        public void DisplayLine(DialogueLine line)
        {
            if (line == null)
            {
                Debug.LogWarning("[DialogueUI] 표시할 대사가 null입니다.");
                return;
            }

            // 현재 대사 저장
            _currentLine = line;

            // UI 표시
            Show();

            // 기존 타이핑 정지
            StopTyping();

            // 대사 표시 시작
            if (_useTypingEffect)
            {
                // 타이핑 효과로 표시
                _typingCoroutine = StartCoroutine(TypeText(line));
            }
            else
            {
                // 즉시 표시
                DisplayTextImmediate(line);
            }

            // 음성 재생 (SoundManager TALK 그룹으로 출력)
            if (line.HasVoice)
            {
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayTalk(line.VoiceClip, line.VoiceVolume);
                else
                    PlayVoice(line.VoiceClip, line.VoiceVolume);
            }

            // 다음 표시 아이콘 숨김
            HideNextIndicator();
        }

        /// <summary>
        /// 타이핑 스킵 요청
        /// </summary>
        public void SkipTyping()
        {
            if (!_canSkipTyping) return;

            if (!_isTypingComplete)
            {
                _skipRequested = true;
            }
        }

        /// <summary>
        /// 대사 표시 완료 여부 확인
        /// </summary>
        /// <returns>타이핑이 완료되었으면 true</returns>
        public bool IsDisplayComplete()
        {
            return _isTypingComplete;
        }

        /// <summary>
        /// 음성 정지
        /// </summary>
        public void StopVoice()
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.StopTalk();
            else if (_voiceAudioSource != null && _voiceAudioSource.isPlaying)
                _voiceAudioSource.Stop();
        }

        /// <summary>
        /// 텍스트 직접 설정 (타이핑 효과 없이)
        /// </summary>
        /// <param name="speakerName">캐릭터 이름</param>
        /// <param name="dialogueText">대사 내용</param>
        public void SetTextDirect(string speakerName, string dialogueText)
        {
            if (_dialogueText == null) return;

            // 스피커 이름이 있으면 "이름 : 대사" 형식, 없으면 대사만
            string fullText = string.IsNullOrEmpty(speakerName)
                ? dialogueText
                : $"<color=#{ColorUtility.ToHtmlStringRGB(_speakerNameColor)}>{speakerName}</color> : {dialogueText}";

            _dialogueText.text = fullText;
            _dialogueText.color = _dialogueTextColor;

            _isTypingComplete = true;
            ShowNextIndicator();
        }

        // =============================================================================
        // 내부 메서드
        // =============================================================================

        /// <summary>
        /// 텍스트 즉시 표시
        /// </summary>
        private void DisplayTextImmediate(DialogueLine line)
        {
            if (_dialogueText == null) return;

            // 대사 타입에 따른 색상 설정
            Color textColor = GetTextColor(line.LineType);
            _dialogueText.color = textColor;

            // 나레이션이거나 이름이 없는 경우 대사만, 있으면 "이름 : 대사" 형식
            string displayName = line.DisplayName;
            string fullText;

            if (line.LineType == DialogueLineType.Narration || string.IsNullOrEmpty(displayName))
            {
                fullText = line.DialogueText;
            }
            else
            {
                fullText = $"<color=#{ColorUtility.ToHtmlStringRGB(_speakerNameColor)}>{displayName}</color> : {line.DialogueText}";
            }

            _dialogueText.text = fullText;

            _isTypingComplete = true;
            ShowNextIndicator();
            OnLineDisplayComplete?.Invoke();
        }

        /// <summary>
        /// 타이핑 효과로 텍스트 표시
        /// TMP의 maxVisibleCharacters를 사용하여 Rich Text 태그가 타이핑 도중
        /// 화면에 노출되는 문제를 방지
        /// </summary>
        private IEnumerator TypeText(DialogueLine line)
        {
            if (_dialogueText == null) yield break;

            _isTypingComplete = false;
            _skipRequested = false;

            // 대사 타입에 따른 색상 설정
            Color textColor = GetTextColor(line.LineType);
            _dialogueText.color = textColor;

            // 나레이션이거나 이름이 없는 경우 대사만, 있으면 "이름 : 대사" 형식
            string displayName = line.DisplayName;
            string fullText;

            if (line.LineType == DialogueLineType.Narration || string.IsNullOrEmpty(displayName))
            {
                fullText = line.DialogueText;
            }
            else
            {
                fullText = $"<color=#{ColorUtility.ToHtmlStringRGB(_speakerNameColor)}>{displayName}</color> : {line.DialogueText}";
            }

            // 전체 텍스트를 미리 세팅하고 표시 글자 수를 0으로 시작
            // TMP가 Rich Text 태그를 미리 파싱하므로 태그가 화면에 노출되지 않음
            _dialogueText.text = fullText;
            _dialogueText.maxVisibleCharacters = 0;

            // TMP가 텍스트를 파싱하도록 한 프레임 대기
            yield return null;

            // 타이핑 속도 계산 (속도 배율 적용)
            float typingSpeed = _baseTypingSpeed * line.TextSpeed;
            float delay = 1f / typingSpeed;

            // TMP가 파악한 실제 글자 수 (Rich Text 태그 제외)
            int totalVisibleChars = _dialogueText.textInfo.characterCount;

            // 한 글자씩 표시 (maxVisibleCharacters 증가)
            for (int i = 0; i < totalVisibleChars; i++)
            {
                // 스킵 요청 확인
                if (_skipRequested)
                {
                    _dialogueText.maxVisibleCharacters = totalVisibleChars;
                    break;
                }

                _dialogueText.maxVisibleCharacters = i + 1;

                // 타이핑 효과음 재생 (선택사항)
                if (_typingSound != null && _voiceAudioSource != null && !line.HasVoice)
                {
                    // 현재 표시된 글자가 공백이 아닌 경우만 재생
                    TMP_CharacterInfo charInfo = _dialogueText.textInfo.characterInfo[i];
                    if (!char.IsWhiteSpace(charInfo.character))
                    {
                        _voiceAudioSource.PlayOneShot(_typingSound, _typingSoundVolume);
                    }
                }

                yield return new WaitForSeconds(delay);
            }

            // 타이핑 완료 후 maxVisibleCharacters 제한 해제
            _dialogueText.maxVisibleCharacters = int.MaxValue;

            _isTypingComplete = true;
            _typingCoroutine = null;

            ShowNextIndicator();
            OnLineDisplayComplete?.Invoke();
        }

        /// <summary>
        /// 타이핑 정지
        /// </summary>
        private void StopTyping()
        {
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }

            // maxVisibleCharacters 제한 해제 (다음 대사에서 정상 표시되도록)
            if (_dialogueText != null)
            {
                _dialogueText.maxVisibleCharacters = int.MaxValue;
            }

            _isTypingComplete = true;
            _skipRequested = false;
        }

        /// <summary>
        /// 음성 재생
        /// </summary>
        private void PlayVoice(AudioClip clip, float volume)
        {
            if (_voiceAudioSource == null || clip == null) return;

            _voiceAudioSource.clip = clip;
            _voiceAudioSource.volume = volume;
            _voiceAudioSource.Play();

            // 음성 완료 감지 코루틴 시작
            StartCoroutine(WaitForVoiceComplete(clip.length));
        }

        /// <summary>
        /// 음성 완료 대기
        /// </summary>
        private IEnumerator WaitForVoiceComplete(float duration)
        {
            yield return new WaitForSeconds(duration);

            // 여전히 재생 중이 아니면 완료 이벤트 발생
            if (!_voiceAudioSource.isPlaying)
            {
                OnVoiceComplete?.Invoke();
            }
        }

        /// <summary>
        /// 대사 타입에 따른 텍스트 색상 반환
        /// </summary>
        private Color GetTextColor(DialogueLineType lineType)
        {
            return lineType switch
            {
                DialogueLineType.Normal => _dialogueTextColor,
                DialogueLineType.Narration => _narrationTextColor,
                DialogueLineType.System => _systemTextColor,
                _ => _dialogueTextColor
            };
        }

        /// <summary>
        /// 다음 표시 아이콘 표시
        /// </summary>
        private void ShowNextIndicator()
        {
            if (_nextIndicator != null)
            {
                _nextIndicator.SetActive(true);
            }
        }

        /// <summary>
        /// 다음 표시 아이콘 숨김
        /// </summary>
        private void HideNextIndicator()
        {
            if (_nextIndicator != null)
            {
                _nextIndicator.SetActive(false);
            }
        }
    }
}
