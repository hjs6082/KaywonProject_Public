// =============================================================================
// EyelidBlackoutTester.cs
// =============================================================================
// 설명: EyelidBlackout 테스트 전용 스크립트 (빌드 전 제거)
// 키 입력:
//   W : WakeUp (기절에서 깨어나는 연출)
//   F : FadeToBlack (블랙아웃)
//   R : FadeFromBlack (블랙아웃 해제)
//   T : 전체 시퀀스 테스트 (FadeToBlack → 대기 → FadeFromBlack → WakeUp)
//   I : 즉시 블랙 (SetInstantBlack)
//   C : 즉시 클리어 (SetInstantClear)
// =============================================================================

using System.Collections;
using UnityEngine;

namespace GameDatabase.UI
{
    public class EyelidBlackoutTester : MonoBehaviour
    {
        [Header("=== 테스트 설정 ===")]
        [Tooltip("전체 시퀀스 테스트 시 FadeToBlack 후 대기 시간 (초)")]
        [SerializeField] private float _sequenceHoldDuration = 1.0f;

        [Tooltip("FadeToBlack 지속 시간")]
        [SerializeField] private float _fadeToBlackDuration = 0.5f;

        [Tooltip("FadeFromBlack 지속 시간")]
        [SerializeField] private float _fadeFromBlackDuration = 0.8f;

        private void Update()
        {
            if (EyelidBlackout.Instance == null)
            {
                Debug.LogWarning("[Tester] EyelidBlackout 인스턴스가 없습니다.");
                return;
            }

            // W : WakeUp
            if (Input.GetKeyDown(KeyCode.W))
            {
                Debug.Log("[Tester] WakeUp 시작");
                EyelidBlackout.Instance.SetInstantBlack();
                EyelidBlackout.Instance.WakeUp(() =>
                    Debug.Log("[Tester] WakeUp 완료"));
            }

            // F : FadeToBlack
            if (Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("[Tester] FadeToBlack 시작");
                EyelidBlackout.Instance.FadeToBlack(_fadeToBlackDuration, () =>
                    Debug.Log("[Tester] FadeToBlack 완료"));
            }

            // R : FadeFromBlack
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("[Tester] FadeFromBlack 시작");
                EyelidBlackout.Instance.FadeFromBlack(_fadeFromBlackDuration, () =>
                    Debug.Log("[Tester] FadeFromBlack 완료"));
            }

            // T : 전체 시퀀스
            if (Input.GetKeyDown(KeyCode.T))
            {
                Debug.Log("[Tester] 전체 시퀀스 시작");
                StartCoroutine(FullSequenceTest());
            }

            // I : 즉시 블랙
            if (Input.GetKeyDown(KeyCode.I))
            {
                Debug.Log("[Tester] SetInstantBlack");
                EyelidBlackout.Instance.SetInstantBlack();
            }

            // C : 즉시 클리어
            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log("[Tester] SetInstantClear");
                EyelidBlackout.Instance.SetInstantClear();
            }
        }

        /// <summary>
        /// 전체 시퀀스: FadeToBlack → 대기 → FadeFromBlack → WakeUp
        /// 실제 게임 흐름과 유사하게 테스트
        /// </summary>
        private IEnumerator FullSequenceTest()
        {
            if (EyelidBlackout.Instance.IsBusy)
            {
                Debug.LogWarning("[Tester] 이미 연출 중 - 시퀀스 취소");
                yield break;
            }

            // 1. 블랙아웃
            Debug.Log("[Tester] 1단계: FadeToBlack");
            bool fadeDone = false;
            EyelidBlackout.Instance.FadeToBlack(_fadeToBlackDuration, () => fadeDone = true);
            yield return new WaitUntil(() => fadeDone);

            // 2. 블랙 상태 유지 (씬 전환 대기 시뮬레이션)
            Debug.Log($"[Tester] 2단계: {_sequenceHoldDuration}초 대기 (씬 전환 구간)");
            yield return new WaitForSeconds(_sequenceHoldDuration);

            // 3. 눈 뜨기 (FullBlack 끄고 눈꺼풀 닫힌 상태로 전환 후 WakeUp)
            Debug.Log("[Tester] 3단계: WakeUp");
            EyelidBlackout.Instance.PrepareWakeUp();
            EyelidBlackout.Instance.WakeUp(() =>
                Debug.Log("[Tester] 전체 시퀀스 완료"));
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 280, 200));
            GUILayout.Label("=== EyelidBlackout 테스트 ===");
            GUILayout.Label("[W] WakeUp");
            GUILayout.Label("[F] FadeToBlack");
            GUILayout.Label("[R] FadeFromBlack");
            GUILayout.Label("[T] 전체 시퀀스 (Fade→Wait→WakeUp)");
            GUILayout.Label("[I] 즉시 블랙");
            GUILayout.Label("[C] 즉시 클리어");

            if (EyelidBlackout.Instance != null)
                GUILayout.Label($"IsBusy: {EyelidBlackout.Instance.IsBusy}");
            GUILayout.EndArea();
        }
    }
}
