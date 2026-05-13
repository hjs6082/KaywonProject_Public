using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using GameDatabase.Player;
using GameDatabase.UI;
using GameDatabase.Dialogue;

public class CaseFileUIManager : MonoBehaviour
{
    // =============================================================================
    // 설정
    // =============================================================================

    [Header("=== 데이터 ===")]
    public CaseFileData caseFileData;

    [Header("=== UI 루트 ===")]
    public GameObject uiRoot;

    [Header("=== 슬롯 (하단 5개) ===")]
    public CaseFileSlotUI[] slots;

    [Tooltip("선택된 슬롯 크기 배율")]
    public float selectedScale = 1.12f;

    [Header("=== 슬롯 이동 버튼 ===")]
    public Button slotLeftButton;
    public Button slotRightButton;

    [Header("=== 3D 오브젝트 ===")]
    public Transform spawnPoint;
    public RawImage rawImage;
    public DragToRotate3D dragToRotate3D;

    [Header("=== 3D 회전 버튼 ===")]
    public Button rotateLeftButton;
    public Button rotateRightButton;

    [Header("=== 글자 스프라이트 ===")]
    public Image textSpriteImage;

    [Header("=== 이벤트 ===")]
    [Tooltip("사건파일 UI가 닫힐 때 호출")]
    public UnityEvent OnClosed;

    private DialogueData _pendingDialogue;

    // =============================================================================
    // 내부 상태
    // =============================================================================

    private int _slotOffset = 0;
    private int _selectedIndex = 0;
    private GameObject _spawned3D;
    private bool _isUnlocked = false;
    private bool _isOpen = false;

    private const int SLOT_COUNT = 5;

    // =============================================================================
    // Unity 생명주기
    // =============================================================================

    private void Start()
    {
        if (uiRoot != null)
            uiRoot.SetActive(false);
        _isUnlocked = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && _isUnlocked)
        {
            if (_isOpen) Close();
            else Open();
        }
    }

    // =============================================================================
    // 공개 API
    // =============================================================================

    /// <summary>
    /// 탭키로 열고 닫을 수 있도록 잠금 해제
    /// </summary>
    public void Unlock()
    {
        _isUnlocked = true;
    }

    /// <summary>
    /// 탭키 잠금
    /// </summary>
    public void Lock()
    {
        _isUnlocked = false;
    }

    /// <summary>
    /// 사건 케이스 교체
    /// </summary>
    public void SetCaseFile(CaseFileData data)
    {
        caseFileData = data;
        if (_isOpen)
        {
            _slotOffset = 0;
            _selectedIndex = 0;
            RefreshSlots();
            SelectItem(_selectedIndex);
        }
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;

        if (uiRoot != null)
            uiRoot.SetActive(true);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetMovementEnabled(false);
            PlayerController.Instance.SetCameraEnabled(false);
        }

        _slotOffset = 0;
        _selectedIndex = 0;

        if (AimCursor.Instance != null)
            AimCursor.Instance.EnterLabelingMode();

        slotLeftButton.onClick.AddListener(OnSlotLeft);
        slotRightButton.onClick.AddListener(OnSlotRight);

        for (int i = 0; i < slots.Length; i++)
        {
            int captured = i;
            if (slots[i].button != null)
                slots[i].button.onClick.AddListener(() => OnSlotClicked(captured));
        }

        RefreshSlots();
        SelectItem(_selectedIndex);
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (uiRoot != null)
            uiRoot.SetActive(false);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetMovementEnabled(true);
            PlayerController.Instance.SetCameraEnabled(true);
        }

        slotLeftButton.onClick.RemoveListener(OnSlotLeft);
        slotRightButton.onClick.RemoveListener(OnSlotRight);

        foreach (var slot in slots)
            if (slot.button != null)
                slot.button.onClick.RemoveAllListeners();

        if (AimCursor.Instance != null)
            AimCursor.Instance.ExitLabelingMode();

        DestroySpawned();
        OnClosed?.Invoke();

        if (_pendingDialogue != null)
        {
            var dialogue = _pendingDialogue;
            _pendingDialogue = null;
            GameDatabase.UI.DialogueManager.Instance.StartDialogue(dialogue);
        }
    }

    public void SetPendingDialogue(DialogueData dialogue)
    {
        _pendingDialogue = dialogue;
    }

    // =============================================================================
    // 슬롯 이동 버튼
    // =============================================================================

    private void OnSlotClicked(int slotDisplayIndex)
    {
        if (caseFileData == null) return;

        int itemIndex = _slotOffset + slotDisplayIndex;
        if (itemIndex >= caseFileData.items.Count) return;

        _selectedIndex = itemIndex;
        RefreshSlots();
        SelectItem(_selectedIndex);
    }

    private void OnSlotLeft()
    {
        if (caseFileData == null || caseFileData.items.Count == 0) return;

        if (_selectedIndex > 0)
        {
            _selectedIndex--;
            if (_selectedIndex < _slotOffset)
                _slotOffset = _selectedIndex;

            RefreshSlots();
            SelectItem(_selectedIndex);
        }
    }

    private void OnSlotRight()
    {
        if (caseFileData == null || caseFileData.items.Count == 0) return;

        if (_selectedIndex < caseFileData.items.Count - 1)
        {
            _selectedIndex++;
            if (_selectedIndex >= _slotOffset + SLOT_COUNT)
                _slotOffset = _selectedIndex - SLOT_COUNT + 1;

            RefreshSlots();
            SelectItem(_selectedIndex);
        }
    }

    // =============================================================================
    // 슬롯 갱신
    // =============================================================================

    private void RefreshSlots()
    {
        if (caseFileData == null) return;

        for (int i = 0; i < SLOT_COUNT; i++)
        {
            int itemIndex = _slotOffset + i;
            bool hasItem = itemIndex < caseFileData.items.Count;
            bool isSelected = itemIndex == _selectedIndex;

            slots[i].bg.gameObject.SetActive(hasItem);
            slots[i].bg.localScale = isSelected ? Vector3.one * selectedScale : Vector3.one;

            if (!hasItem) continue;

            var item = caseFileData.items[itemIndex];
            if (slots[i].spriteImage != null)
                slots[i].spriteImage.sprite = item.itemSprite;
        }
    }

    // =============================================================================
    // 아이템 선택
    // =============================================================================

    private void SelectItem(int index)
    {
        if (caseFileData == null || index >= caseFileData.items.Count) return;

        var item = caseFileData.items[index];

        if (textSpriteImage != null)
            textSpriteImage.sprite = item.textSprite;

        DestroySpawned();
        if (item.prefab3D != null && spawnPoint != null)
        {
            _spawned3D = Instantiate(item.prefab3D, spawnPoint.position, item.prefab3D.transform.rotation);
            if (dragToRotate3D != null)
                dragToRotate3D.targetObject = _spawned3D.transform;
        }
    }

    private void DestroySpawned()
    {
        if (_spawned3D != null)
        {
            Destroy(_spawned3D);
            _spawned3D = null;
        }

        if (dragToRotate3D != null)
            dragToRotate3D.targetObject = null;
    }
}
