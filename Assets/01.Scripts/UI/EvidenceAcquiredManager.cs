// =============================================================================
// EvidenceAcquiredManager.cs
// =============================================================================
// 설명: 증거물 획득 UI 매니저 (싱글톤)
// 용도: 증거물 획득 시 UI를 표시하는 중앙 관리자
// 사용법:
//   1. 씬에 EvidenceAcquiredManager 프리팹 배치
//   2. 코드에서 EvidenceAcquiredManager.Instance.ShowEvidence(evidenceData) 호출
// =============================================================================

using UnityEngine;
using GameDatabase.Evidence;

namespace GameDatabase.UI
{
    /// <summary>
    /// 증거물 획득 UI 매니저 - 싱글톤
    /// </summary>
    public class EvidenceAcquiredManager : MonoBehaviour
    {
        // =============================================================================
        // 싱글톤
        // =============================================================================

        private static EvidenceAcquiredManager _instance;

        public static EvidenceAcquiredManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EvidenceAcquiredManager>();

                    if (_instance == null)
                    {
                        Debug.LogError("[EvidenceAcquiredManager] 씬에 EvidenceAcquiredManager가 없습니다!");
                    }
                }
                return _instance;
            }
        }

        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== UI 참조 ===")]

        [Tooltip("증거물 획득 UI 컴포넌트")]
        [SerializeField] private EvidenceAcquiredUI _evidenceUI;

        [Header("=== 증거물 데이터베이스 ===")]

        [Tooltip("증거물 데이터베이스 (ID로 증거물 찾기용)")]
        [SerializeField] private EvidenceDatabase _evidenceDatabase;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // 싱글톤 설정
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // UI 체크
            if (_evidenceUI == null)
            {
                Debug.LogError("[EvidenceAcquiredManager] EvidenceAcquiredUI가 할당되지 않았습니다!");
            }
        }

        // =============================================================================
        // 증거물 표시
        // =============================================================================

        /// <summary>
        /// 증거물 획득 UI 표시 (EvidenceData로)
        /// </summary>
        /// <param name="evidence">증거물 데이터</param>
        public void ShowEvidence(EvidenceData evidence)
        {
            if (_evidenceUI == null)
            {
                Debug.LogError("[EvidenceAcquiredManager] EvidenceAcquiredUI가 없습니다!");
                return;
            }

            if (evidence == null)
            {
                Debug.LogWarning("[EvidenceAcquiredManager] 증거물 데이터가 null입니다.");
                return;
            }

            _evidenceUI.ShowEvidenceAcquired(evidence);

            Debug.Log($"[EvidenceAcquiredManager] 증거물 표시: {evidence.EvidenceName}");
        }

        /// <summary>
        /// 증거물 획득 UI 표시 (ID로)
        /// </summary>
        /// <param name="evidenceID">증거물 ID</param>
        public void ShowEvidenceByID(string evidenceID)
        {
            if (_evidenceDatabase == null)
            {
                Debug.LogError("[EvidenceAcquiredManager] EvidenceDatabase가 할당되지 않았습니다!");
                return;
            }

            EvidenceData evidence = _evidenceDatabase.GetEvidenceByID(evidenceID);

            if (evidence != null)
            {
                ShowEvidence(evidence);
            }
            else
            {
                Debug.LogWarning($"[EvidenceAcquiredManager] 증거물을 찾을 수 없습니다: {evidenceID}");
            }
        }

        /// <summary>
        /// 증거물 획득 UI 표시 (개별 데이터로)
        /// </summary>
        public void ShowEvidence(string evidenceName, string evidenceDescription, Sprite evidenceImage)
        {
            if (_evidenceUI == null)
            {
                Debug.LogError("[EvidenceAcquiredManager] EvidenceAcquiredUI가 없습니다!");
                return;
            }

            _evidenceUI.ShowEvidenceAcquired(evidenceName, evidenceDescription, evidenceImage);

            Debug.Log($"[EvidenceAcquiredManager] 증거물 표시: {evidenceName}");
        }

        // =============================================================================
        // UI 제어
        // =============================================================================

        /// <summary>
        /// 현재 표시 중인 UI 숨기기
        /// </summary>
        public void HideCurrentEvidence()
        {
            if (_evidenceUI != null)
            {
                _evidenceUI.Hide();
            }
        }

        /// <summary>
        /// 헤더 텍스트 변경 (기본: "증거 획득")
        /// </summary>
        public void SetHeaderText(string text)
        {
            if (_evidenceUI != null)
            {
                _evidenceUI.SetHeaderText(text);
            }
        }

        /// <summary>
        /// 표시 시간 변경 (기본: 3초)
        /// </summary>
        public void SetDisplayDuration(float duration)
        {
            if (_evidenceUI != null)
            {
                _evidenceUI.SetDisplayDuration(duration);
            }
        }
    }
}
