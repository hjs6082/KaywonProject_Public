using UnityEngine;

/// <summary>
/// 우클릭(Mouse1)로 관찰 모드를 토글합니다. (어디서든 발동)\n/// </summary>
public class ObservationModeToggle : MonoBehaviour
{
    [Tooltip("토글 입력 버튼 (기본: 우클릭 = 1)")]
    public int mouseButton = 1;

    [Tooltip("비어 있으면 씬에서 찾거나 자동 생성합니다.")]
    public ObservationModeController controller;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(mouseButton))
            return;

        if (controller == null)
            controller = FindObjectOfType<ObservationModeController>();

        if (controller == null)
        {
            var go = new GameObject("ObservationModeController");
            controller = go.AddComponent<ObservationModeController>();
        }

        controller.Toggle();
    }
}

