// =============================================================================
// GameStateManager.cs
// =============================================================================
// 설명: 씬 간 유지되는 글로벌 플래그 저장소
// 용도: "대화 1 완료 → 다른 씬 갔다 돌아와도 대화 2 재생" 등 조건부 로직의 기반
// 특징: DontDestroyOnLoad + 싱글톤. 향후 세이브/로드 시 이 딕셔너리만 직렬화
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace GameDatabase
{
    /// <summary>
    /// 글로벌 게임 상태 플래그 매니저
    /// 씬 전환 후에도 플래그가 유지되는 싱글톤
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        // =============================================================================
        // 싱글톤
        // =============================================================================

        private static GameStateManager _instance;

        public static GameStateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameStateManager>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("GameStateManager");
                        _instance = obj.AddComponent<GameStateManager>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return _instance;
            }
        }

        // =============================================================================
        // 내부 상태
        // =============================================================================

        // 전역 플래그 저장소 (key: 플래그 이름, value: bool 값)
        private Dictionary<string, bool> _flags = new Dictionary<string, bool>();

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        // =============================================================================
        // 플래그 관리
        // =============================================================================

        /// <summary>
        /// 플래그 설정
        /// </summary>
        /// <param name="key">플래그 키</param>
        /// <param name="value">설정할 값</param>
        public void SetFlag(string key, bool value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _flags[key] = value;
            Debug.Log($"[GameStateManager] 플래그 설정: '{key}' = {value}");
        }

        /// <summary>
        /// 플래그 값 가져오기
        /// </summary>
        /// <param name="key">플래그 키</param>
        /// <param name="defaultValue">키가 없을 때 반환할 기본값</param>
        public bool GetFlag(string key, bool defaultValue = false)
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;
            return _flags.TryGetValue(key, out bool value) ? value : defaultValue;
        }

        /// <summary>
        /// 플래그가 존재하는지 확인
        /// </summary>
        /// <param name="key">플래그 키</param>
        public bool HasFlag(string key)
        {
            return !string.IsNullOrEmpty(key) && _flags.ContainsKey(key);
        }

        /// <summary>
        /// 플래그 제거
        /// </summary>
        /// <param name="key">플래그 키</param>
        public void RemoveFlag(string key)
        {
            if (!string.IsNullOrEmpty(key))
                _flags.Remove(key);
        }

        /// <summary>
        /// 모든 플래그 초기화 (새 게임 시작 등에 사용)
        /// </summary>
        public void ClearAllFlags()
        {
            _flags.Clear();
            Debug.Log("[GameStateManager] 모든 플래그 초기화");
        }

        // =============================================================================
        // 디버그
        // =============================================================================

        /// <summary>
        /// 현재 설정된 모든 플래그를 콘솔에 출력
        /// </summary>
        public void DebugPrintAllFlags()
        {
            if (_flags.Count == 0)
            {
                Debug.Log("[GameStateManager] 설정된 플래그 없음");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[GameStateManager] 전체 플래그 목록 ({_flags.Count}개):");
            foreach (var kv in _flags)
            {
                sb.AppendLine($"  '{kv.Key}' = {kv.Value}");
            }
            Debug.Log(sb.ToString());
        }
    }
}
