using UnityEngine;

/// <summary>
/// ObservationModeController.IsActive 상태에 따라 지정한 UI 오브젝트들을 켜고/끕니다.
/// 관찰 모드에 들어가기 전에는 꺼두고, 관찰 모드에서만 켜고 싶을 때 사용합니다.
/// </summary>
public class ObservationModeUIAutoToggle : MonoBehaviour
{
    [Header("대상 UI")]
    [Tooltip("관찰 모드에 들어가면 켜질 UI들(에임, 관찰 UI 등)")]
    public GameObject[] enableWhenObservationActive;

    [Tooltip("관찰 모드에 들어가면 꺼질 UI들(기본 HUD 등). 필요 없으면 비워두기")]
    public GameObject[] disableWhenObservationActive;

    [Header("동작")]
    [Tooltip("Start에서 즉시 현재 상태로 한 번 맞춥니다.")]
    public bool applyOnStart = true;

    private bool _last;

    private void Start()
    {
        _last = ObservationModeController.IsActive;
        if (applyOnStart)
            Apply(_last);
    }

    private void Update()
    {
        bool cur = ObservationModeController.IsActive;
        if (cur == _last) return;
        _last = cur;
        Apply(cur);
    }

    private void Apply(bool observationActive)
    {
        SetActiveAll(enableWhenObservationActive, observationActive);
        SetActiveAll(disableWhenObservationActive, !observationActive);
    }

    private static void SetActiveAll(GameObject[] targets, bool active)
    {
        if (targets == null) return;
        for (int i = 0; i < targets.Length; i++)
        {
            GameObject go = targets[i];
            if (go == null) continue;
            if (go.activeSelf != active)
                go.SetActive(active);
        }
    }
}

