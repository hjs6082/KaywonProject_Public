// =============================================================================
// ChoiceUI.cs
// =============================================================================
// 설명: 다이얼로그 선택지 UI 컴포넌트
// 용도: 플레이어가 선택할 수 있는 선택지 버튼들을 표시
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using GameDatabase.Dialogue;

namespace GameDatabase.UI
{
    /// <summary>
    /// 선택지 UI 컴포넌트
    /// 플레이어가 선택할 수 있는 옵션들을 표시
    /// </summary>
    public class ChoiceUI : MonoBehaviour
    {
        // =============================================================================
        // UI 참조
        // =============================================================================

        [Header("UI 요소")]
        [Tooltip("선택지 패널 (전체 컨테이너)")]
        [SerializeField] private GameObject _choicePanel;

        [Tooltip("프롬프트 텍스트 (선택지 전에 표시되는 질문)")]
        [SerializeField] private TextMeshProUGUI _promptText;

        [Tooltip("선택지 버튼들의 부모 컨테이너")]
        [SerializeField] private Transform _choiceButtonContainer;

        [Tooltip("선택지 버튼 프리팹")]
        [SerializeField] private GameObject _choiceButtonPrefab;

        [Tooltip("타이머 표시 UI (선택사항)")]
        [SerializeField] private GameObject _timerUI;

        [Tooltip("타이머 슬라이더 (선택사항)")]
        [SerializeField] private Slider _timerSlider;

        [Tooltip("타이머 텍스트 (선택사항)")]
        [SerializeField] private TextMeshProUGUI _timerText;

        // =============================================================================
        // 스타일 설정
        // =============================================================================

        [Header("스타일 설정")]
        [Tooltip("선택지 버튼 기본 색상")]
        [SerializeField] private Color _buttonNormalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        [Tooltip("선택지 버튼 호버 색상")]
        [SerializeField] private Color _buttonHighlightColor = new Color(0.3f, 0.5f, 0.3f, 0.9f);

        [Tooltip("선택지 텍스트 색상")]
        [SerializeField] private Color _choiceTextColor = Color.white;

        [Tooltip("프롬프트 텍스트 색상")]
        [SerializeField] private Color _promptTextColor = new Color(0.9f, 0.9f, 0.5f, 1f);

        // =============================================================================
        // 내부 상태
        // =============================================================================

        // 현재 표시 중인 선택지
        private DialogueChoice _currentChoice;

        // 생성된 버튼들
        private List<GameObject> _createdButtons = new List<GameObject>();

        // 타이머 관련
        private float _timeRemaining;
        private bool _isTimerRunning;

        // 선택 완료 여부
        private bool _hasSelected;

        // =============================================================================
        // 이벤트
        // =============================================================================

        /// <summary>
        /// 선택지 선택 시 호출되는 이벤트
        /// int: 선택된 옵션 인덱스
        /// ChoiceOption: 선택된 옵션 데이터
        /// </summary>
        public event System.Action<int, ChoiceOption> OnChoiceSelected;

        /// <summary>
        /// 시간 초과 시 호출되는 이벤트
        /// </summary>
        public event System.Action OnTimeExpired;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 선택지 UI가 활성화되어 있는지 확인
        /// </summary>
        public bool IsActive => _choicePanel != null && _choicePanel.activeSelf;

        /// <summary>
        /// 선택이 완료되었는지 확인
        /// </summary>
        public bool HasSelected => _hasSelected;

        /// <summary>
        /// 현재 선택지 데이터
        /// </summary>
        public DialogueChoice CurrentChoice => _currentChoice;

        /// <summary>
        /// 남은 시간
        /// </summary>
        public float TimeRemaining => _timeRemaining;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // 초기 상태: 숨김
            Hide();
        }

        private void Update()
        {
            // 타이머 업데이트
            if (_isTimerRunning)
            {
                UpdateTimer();
            }
        }

        // =============================================================================
        // 공개 메서드
        // =============================================================================

        /// <summary>
        /// 선택지 UI 표시
        /// </summary>
        public void Show()
        {
            if (_choicePanel != null)
            {
                _choicePanel.SetActive(true);
            }
        }

        /// <summary>
        /// 선택지 UI 숨김
        /// </summary>
        public void Hide()
        {
            if (_choicePanel != null)
            {
                _choicePanel.SetActive(false);
            }

            // 타이머 정지
            StopTimer();

            // 타이머 UI 숨김
            if (_timerUI != null)
            {
                _timerUI.SetActive(false);
            }
        }

        /// <summary>
        /// 선택지 표시
        /// </summary>
        /// <param name="choice">표시할 선택지 데이터</param>
        public void DisplayChoice(DialogueChoice choice)
        {
            if (choice == null)
            {
                Debug.LogWarning("[ChoiceUI] 표시할 선택지가 null입니다.");
                return;
            }

            // 상태 초기화
            _currentChoice = choice;
            _hasSelected = false;

            // 기존 버튼 제거
            ClearButtons();

            // 프롬프트 설정
            SetPrompt(choice);

            // 선택지 버튼 생성
            CreateChoiceButtons(choice);

            // 타이머 설정
            if (choice.HasTimeLimit)
            {
                StartTimer(choice.TimeLimit);
            }
            else
            {
                StopTimer();
                if (_timerUI != null)
                {
                    _timerUI.SetActive(false);
                }
            }

            // UI 표시
            Show();
        }

        /// <summary>
        /// 선택지 강제 선택 (시간 초과 시 사용)
        /// </summary>
        /// <param name="optionIndex">선택할 옵션 인덱스</param>
        public void ForceSelect(int optionIndex)
        {
            if (_hasSelected) return;

            SelectOption(optionIndex);
        }

        // =============================================================================
        // 내부 메서드
        // =============================================================================

        /// <summary>
        /// 프롬프트 텍스트 설정
        /// </summary>
        private void SetPrompt(DialogueChoice choice)
        {
            if (_promptText == null) return;

            if (choice.HasPrompt)
            {
                _promptText.gameObject.SetActive(true);
                _promptText.text = choice.PromptText;
                _promptText.color = _promptTextColor;
            }
            else
            {
                _promptText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 선택지 버튼 생성
        /// </summary>
        private void CreateChoiceButtons(DialogueChoice choice)
        {
            if (_choiceButtonContainer == null || _choiceButtonPrefab == null)
            {
                Debug.LogError("[ChoiceUI] 버튼 컨테이너 또는 프리팹이 설정되지 않았습니다.");
                return;
            }

            // 옵션 배열 가져오기 (셔플 옵션 고려)
            ChoiceOption[] options = choice.Options;
            int[] indices = new int[options.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            // 셔플 옵션
            if (choice.ShuffleOptions)
            {
                ShuffleArray(indices);
            }

            // 버튼 생성
            for (int i = 0; i < indices.Length; i++)
            {
                int originalIndex = indices[i];
                ChoiceOption option = options[originalIndex];

                if (option == null || !option.IsValid) continue;

                // 버튼 인스턴스 생성
                GameObject buttonObj = Instantiate(_choiceButtonPrefab, _choiceButtonContainer);
                _createdButtons.Add(buttonObj);

                // 버튼 텍스트 설정
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = option.ChoiceText;
                    buttonText.color = _choiceTextColor;
                }

                // 버튼 색상 설정
                Button button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    // 색상 블록 설정
                    ColorBlock colors = button.colors;
                    colors.normalColor = _buttonNormalColor;
                    colors.highlightedColor = _buttonHighlightColor;
                    colors.pressedColor = _buttonHighlightColor;
                    button.colors = colors;

                    // 클릭 이벤트 연결
                    int capturedIndex = originalIndex; // 클로저를 위한 캡처
                    button.onClick.AddListener(() => OnButtonClicked(capturedIndex));
                }
            }
        }

        /// <summary>
        /// 버튼 클릭 처리
        /// </summary>
        private void OnButtonClicked(int optionIndex)
        {
            if (_hasSelected) return;

            SelectOption(optionIndex);
        }

        /// <summary>
        /// 옵션 선택 처리
        /// </summary>
        private void SelectOption(int optionIndex)
        {
            _hasSelected = true;

            // 타이머 정지
            StopTimer();

            // 선택된 옵션 가져오기
            ChoiceOption selectedOption = _currentChoice?.GetOption(optionIndex);

            // 이벤트 발생
            OnChoiceSelected?.Invoke(optionIndex, selectedOption);

            // UI 숨김
            Hide();
        }

        /// <summary>
        /// 기존 버튼 제거
        /// </summary>
        private void ClearButtons()
        {
            foreach (GameObject button in _createdButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }
            _createdButtons.Clear();
        }

        /// <summary>
        /// 배열 셔플 (Fisher-Yates 알고리즘)
        /// </summary>
        private void ShuffleArray<T>(T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }

        // =============================================================================
        // 타이머 관련
        // =============================================================================

        /// <summary>
        /// 타이머 시작
        /// </summary>
        private void StartTimer(float duration)
        {
            _timeRemaining = duration;
            _isTimerRunning = true;

            // 타이머 UI 표시
            if (_timerUI != null)
            {
                _timerUI.SetActive(true);
            }

            // 슬라이더 초기화
            if (_timerSlider != null)
            {
                _timerSlider.maxValue = duration;
                _timerSlider.value = duration;
            }

            UpdateTimerDisplay();
        }

        /// <summary>
        /// 타이머 정지
        /// </summary>
        private void StopTimer()
        {
            _isTimerRunning = false;
        }

        /// <summary>
        /// 타이머 업데이트
        /// </summary>
        private void UpdateTimer()
        {
            _timeRemaining -= Time.deltaTime;

            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                _isTimerRunning = false;

                // 시간 초과 처리
                HandleTimeExpired();
            }

            UpdateTimerDisplay();
        }

        /// <summary>
        /// 타이머 표시 업데이트
        /// </summary>
        private void UpdateTimerDisplay()
        {
            // 슬라이더 업데이트
            if (_timerSlider != null)
            {
                _timerSlider.value = _timeRemaining;
            }

            // 텍스트 업데이트
            if (_timerText != null)
            {
                _timerText.text = Mathf.CeilToInt(_timeRemaining).ToString();
            }
        }

        /// <summary>
        /// 시간 초과 처리
        /// </summary>
        private void HandleTimeExpired()
        {
            if (_hasSelected) return;

            // 이벤트 발생
            OnTimeExpired?.Invoke();

            // 기본 옵션 자동 선택
            if (_currentChoice != null)
            {
                ChoiceOption defaultOption = _currentChoice.GetDefaultOption();
                int defaultIndex = _currentChoice.DefaultOptionIndex >= 0
                    ? _currentChoice.DefaultOptionIndex
                    : 0;

                SelectOption(defaultIndex);
            }
        }
    }
}
