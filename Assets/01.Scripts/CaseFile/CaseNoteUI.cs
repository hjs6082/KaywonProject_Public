using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using GameDatabase.Dialogue;
using GameDatabase.Player;
using GameDatabase.UI;

/// <summary>
/// 사건 수첩 UI — N키(1번 이미지), M키(2번 이미지)
/// 아래에서 위로 슬라이드 인/아웃
/// </summary>
public class CaseNoteUI : MonoBehaviour
{
    // =============================================================================
    // 설정
    // =============================================================================

    [Header("=== 이미지 패널 ===")]
    [Tooltip("N키로 열릴 패널 루트 RectTransform")]
    public RectTransform panel1;

    [Tooltip("M키로 열릴 패널 루트 RectTransform")]
    public RectTransform panel2;

    [Header("=== 애니메이션 ===")]
    [Tooltip("슬라이드 시작 Y 오프셋 (화면 아래)")]
    public float slideOffsetY = -1080f;

    [Tooltip("열리는 시간 (초)")]
    public float openDuration = 0.35f;

    [Tooltip("닫히는 시간 (초)")]
    public float closeDuration = 0.25f;

    public Ease openEase  = Ease.OutQuart;
    public Ease closeEase = Ease.InQuart;

    [Header("=== 입력 키 ===")]
    public KeyCode key1 = KeyCode.N;
    public KeyCode key2 = KeyCode.M;

    [Header("=== 이벤트 (패널 1) ===")]
    public UnityEvent onPanel1Opened;
    public UnityEvent onPanel1Closed;

    [Header("=== 이벤트 (패널 2) ===")]
    public UnityEvent onPanel2Opened;
    public UnityEvent onPanel2Closed;

    [Header("=== 닫힐 때 1회 다이얼로그 ===")]
    [Tooltip("패널 1 닫힐 때 1회만 재생할 다이얼로그")]
    public DialogueData panel1PendingDialogue;

    [Tooltip("패널 2 닫힐 때 1회만 재생할 다이얼로그")]
    public DialogueData panel2PendingDialogue;

    [Header("=== 닫힐 때 1회 UnityEvent ===")]
    [Tooltip("패널 1 닫힐 때 1회만 실행할 이벤트")]
    public UnityEvent panel1OnceEvent;

    [Tooltip("패널 2 닫힐 때 1회만 실행할 이벤트")]
    public UnityEvent panel2OnceEvent;

    // =============================================================================
    // 내부 상태
    // =============================================================================

    private bool _panel1Open = false;
    private bool _panel2Open = false;

    private Vector2 _panel1ClosedPos;
    private Vector2 _panel2ClosedPos;

    private DialogueData _panel1QueuedDialogue;
    private DialogueData _panel2QueuedDialogue;

    private UnityEvent _panel1QueuedEvent;
    private UnityEvent _panel2QueuedEvent;

    // =============================================================================
    // Unity 생명주기
    // =============================================================================

    private void Start()
    {
        if (panel1 != null)
        {
            _panel1ClosedPos = panel1.anchoredPosition;
            panel1.anchoredPosition += new Vector2(0, slideOffsetY);
            panel1.gameObject.SetActive(false);
        }

        if (panel2 != null)
        {
            _panel2ClosedPos = panel2.anchoredPosition;
            panel2.anchoredPosition += new Vector2(0, slideOffsetY);
            panel2.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(key1)) Toggle(1);
        if (Input.GetKeyDown(key2)) Toggle(2);
    }

    // =============================================================================
    // 1회성 다이얼로그 / 이벤트 설정
    // =============================================================================

    public void SetPanel1PendingDialogue(DialogueData dialogue) => _panel1QueuedDialogue = dialogue;
    public void SetPanel2PendingDialogue(DialogueData dialogue) => _panel2QueuedDialogue = dialogue;

    public void SetPanel1OnceEvent(UnityAction action)
    {
        _panel1QueuedEvent = new UnityEvent();
        _panel1QueuedEvent.AddListener(action);
    }

    public void SetPanel2OnceEvent(UnityAction action)
    {
        _panel2QueuedEvent = new UnityEvent();
        _panel2QueuedEvent.AddListener(action);
    }

    // =============================================================================
    // 토글
    // =============================================================================

    private void Toggle(int panelIndex)
    {
        if (panelIndex == 1)
        {
            if (_panel1Open) ClosePanel(1);
            else             OpenPanel(1);
        }
        else
        {
            if (_panel2Open) ClosePanel(2);
            else             OpenPanel(2);
        }
    }

    // =============================================================================
    // 열기 / 닫기
    // =============================================================================

    private void OpenPanel(int panelIndex)
    {
        RectTransform panel = panelIndex == 1 ? panel1 : panel2;
        Vector2 openPos     = panelIndex == 1 ? _panel1ClosedPos : _panel2ClosedPos;

        if (panel == null) return;

        if (panelIndex == 1) _panel1Open = true;
        else                 _panel2Open = true;

        // 커서 전환
        if (AimCursor.Instance != null) AimCursor.Instance.EnterLabelingMode();
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetMovementEnabled(false);
            PlayerController.Instance.SetCameraEnabled(false);
        }

        panel.gameObject.SetActive(true);
        panel.DOKill();
        panel.DOAnchorPos(openPos, openDuration).SetEase(openEase);

        if (panelIndex == 1) onPanel1Opened?.Invoke();
        else                 onPanel2Opened?.Invoke();
    }

    private void ClosePanel(int panelIndex)
    {
        RectTransform panel   = panelIndex == 1 ? panel1 : panel2;
        Vector2 closedPos     = (panelIndex == 1 ? _panel1ClosedPos : _panel2ClosedPos) + new Vector2(0, slideOffsetY);

        if (panel == null) return;

        if (panelIndex == 1) _panel1Open = false;
        else                 _panel2Open = false;

        panel.DOKill();
        panel.DOAnchorPos(closedPos, closeDuration).SetEase(closeEase).OnComplete(() =>
        {
            panel.gameObject.SetActive(false);

            // 두 패널 모두 닫혔을 때만 플레이어 복원
            if (!_panel1Open && !_panel2Open)
            {
                if (AimCursor.Instance != null) AimCursor.Instance.ExitLabelingMode();
                if (PlayerController.Instance != null)
                {
                    PlayerController.Instance.SetMovementEnabled(true);
                    PlayerController.Instance.SetCameraEnabled(true);
                }
            }

            if (panelIndex == 1)
            {
                onPanel1Closed?.Invoke();
                FireOnce(ref _panel1QueuedDialogue, ref _panel1QueuedEvent);
                // 인스펙터 할당 1회 처리
                if (panel1PendingDialogue != null)
                {
                    DialogueManager.Instance?.StartDialogue(panel1PendingDialogue);
                    panel1PendingDialogue = null;
                }
                if (panel1OnceEvent != null)
                {
                    panel1OnceEvent.Invoke();
                    panel1OnceEvent = null;
                }
            }
            else
            {
                onPanel2Closed?.Invoke();
                FireOnce(ref _panel2QueuedDialogue, ref _panel2QueuedEvent);
                if (panel2PendingDialogue != null)
                {
                    DialogueManager.Instance?.StartDialogue(panel2PendingDialogue);
                    panel2PendingDialogue = null;
                }
                if (panel2OnceEvent != null)
                {
                    panel2OnceEvent.Invoke();
                    panel2OnceEvent = null;
                }
            }
        });
    }

    private void FireOnce(ref DialogueData dialogue, ref UnityEvent evt)
    {
        if (dialogue != null)
        {
            DialogueManager.Instance?.StartDialogue(dialogue);
            dialogue = null;
        }
        if (evt != null)
        {
            evt.Invoke();
            evt = null;
        }
    }
}
