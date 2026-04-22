// =============================================================================
// YarnDialogueManager.cs
// =============================================================================
// 설명: Yarn Spinner 다이얼로그 재생 싱글톤 매니저
// 용도: DialogueRunner를 중앙에서 관리하고, 노드명만으로 대사 재생
// 사용법:
//   1. 씬에 빈 GameObject를 만들고 이 컴포넌트를 추가
//   2. _dialogueRunner에 씬의 DialogueRunner 할당
//   3. YarnDialogueManager.Instance.Play("NodeName") 으로 재생
// =============================================================================

using UnityEngine;
using Yarn.Unity;

namespace GameDatabase
{
    /// <summary>
    /// Yarn Spinner 다이얼로그 재생 싱글톤 매니저
    /// </summary>
    public class YarnDialogueManager : MonoBehaviour
    {
        // =============================================================================
        // 싱글톤
        // =============================================================================

        private static YarnDialogueManager _instance;
        public static YarnDialogueManager Instance => _instance;

        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== 설정 ===")]

        [Tooltip("씬의 Yarn Spinner DialogueRunner")]
        [SerializeField] private DialogueRunner _dialogueRunner;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>현재 대사가 재생 중인지 여부</summary>
        public bool IsRunning => _dialogueRunner != null && _dialogueRunner.IsDialogueRunning;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        // =============================================================================
        // 공개 API
        // =============================================================================

        /// <summary>
        /// 노드명으로 Yarn 대사 재생
        /// </summary>
        public void Play(string nodeName)
        {
            if (_dialogueRunner == null)
            {
                Debug.LogWarning("[YarnDialogueManager] DialogueRunner가 할당되지 않았습니다.");
                return;
            }

            if (string.IsNullOrEmpty(nodeName))
            {
                Debug.LogWarning("[YarnDialogueManager] 노드명이 비어있습니다.");
                return;
            }

            if (_dialogueRunner.IsDialogueRunning)
            {
                Debug.LogWarning($"[YarnDialogueManager] 이미 대사가 재생 중입니다. 노드: {nodeName}");
                return;
            }

            _dialogueRunner.StartDialogue(nodeName);
        }

        /// <summary>
        /// 현재 재생 중인 대사 중단
        /// </summary>
        public void Stop()
        {
            if (_dialogueRunner != null && _dialogueRunner.IsDialogueRunning)
                _dialogueRunner.Stop();
        }
    }
}
