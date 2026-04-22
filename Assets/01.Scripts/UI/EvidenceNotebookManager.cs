// =============================================================================
// EvidenceNotebookManager.cs
// =============================================================================
// 설명: 증거물 노트북 매니저 (싱글톤)
// 용도: 증거물을 노트북에 추가하고 관리하는 중앙 관리자
// 사용법:
//   1. 씬에 EvidenceNotebookManager 프리팹 배치
//   2. 코드에서 EvidenceNotebookManager.Instance.AddEvidence(evidenceData) 호출
// =============================================================================

using UnityEngine;
using GameDatabase.Evidence;

namespace GameDatabase.UI
{
    /// <summary>
    /// 증거물 노트북 매니저 - 싱글톤
    /// </summary>
    public class EvidenceNotebookManager : MonoBehaviour
    {
        // =============================================================================
        // 싱글톤
        // =============================================================================

        private static EvidenceNotebookManager _instance;

        public static EvidenceNotebookManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EvidenceNotebookManager>();

                    if (_instance == null)
                    {
                        //Debug.LogError("[EvidenceNotebookManager] 씬에 EvidenceNotebookManager가 없습니다!");
                    }
                }
                return _instance;
            }
        }

        // =============================================================================
        // 설정
        // =============================================================================

        [Header("=== UI 참조 ===")]

        [Tooltip("증거물 노트북 UI 컴포넌트")]
        [SerializeField] private EvidenceNotebookUI _notebookUI;

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
            if (_notebookUI == null)
            {
                Debug.LogError("[EvidenceNotebookManager] EvidenceNotebookUI가 할당되지 않았습니다!");
            }
        }

        // =============================================================================
        // 증거물 추가
        // =============================================================================

        /// <summary>
        /// 증거물을 노트북에 추가
        /// </summary>
        /// <param name="evidence">증거물 데이터</param>
        public void AddEvidence(EvidenceData evidence)
        {
            if (_notebookUI == null)
            {
                Debug.LogError("[EvidenceNotebookManager] EvidenceNotebookUI가 없습니다!");
                return;
            }

            if (evidence == null)
            {
                Debug.LogWarning("[EvidenceNotebookManager] 증거물 데이터가 null입니다.");
                return;
            }

            _notebookUI.AddEvidence(evidence);

            Debug.Log($"[EvidenceNotebookManager] 증거물 노트북에 추가: {evidence.EvidenceName}");
        }

        // =============================================================================
        // 노트북 제어
        // =============================================================================

        /// <summary>
        /// 노트북 열기
        /// </summary>
        public void OpenNotebook()
        {
            if (_notebookUI != null)
            {
                _notebookUI.Open();
            }
        }

        /// <summary>
        /// 노트북 닫기
        /// </summary>
        public void CloseNotebook()
        {
            if (_notebookUI != null)
            {
                _notebookUI.Close();
            }
        }

        /// <summary>
        /// 노트북 열림 여부
        /// </summary>
        public bool IsNotebookOpen
        {
            get
            {
                if (_notebookUI != null)
                {
                    return _notebookUI.IsOpen;
                }
                return false;
            }
        }

        /// <summary>
        /// 모든 증거물 초기화
        /// </summary>
        public void ClearAllEvidences()
        {
            if (_notebookUI != null)
            {
                _notebookUI.ClearAllEvidences();
            }
        }

        /// <summary>
        /// 특정 증거물을 획득했는지 확인
        /// </summary>
        /// <param name="evidenceID">확인할 증거물 ID</param>
        public bool HasEvidence(string evidenceID)
        {
            if (_notebookUI == null) return false;
            return _notebookUI.HasEvidence(evidenceID);
        }
    }
}
