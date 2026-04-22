// =============================================================================
// CaseBoardManager.cs
// =============================================================================
// 설명: 사건 보드 시스템 매니저 (싱글톤)
// 용도: 보드 열기/닫기, 증거물 배치 검증, 상태 관리, 3D 동기화
// 사용법:
//   1. 씬에 CaseBoardManager 오브젝트 배치
//   2. CaseBoardUI, CaseBoard3DVisualizer 참조 연결
//   3. CaseBoardManager.Instance.OpenBoard(caseBoardData) 호출
// =============================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using GameDatabase.CaseBoard;
using GameDatabase.Evidence;
using GameDatabase.Player;

namespace GameDatabase.UI
{
    /// <summary>
    /// 사건 보드 상태
    /// </summary>
    public enum CaseBoardState
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    /// <summary>
    /// 사건 보드 매니저 - 싱글톤
    /// </summary>
    public class CaseBoardManager : MonoBehaviour
    {
        // =============================================================================
        // 싱글톤
        // =============================================================================

        private static CaseBoardManager _instance;

        public static CaseBoardManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CaseBoardManager>();
                    if (_instance == null)
                    {
                        Debug.LogError("[CaseBoardManager] 씬에 CaseBoardManager가 없습니다!");
                    }
                }
                return _instance;
            }
        }

        // =============================================================================
        // 참조
        // =============================================================================

        [Header("=== UI 참조 ===")]

        [Tooltip("사건 보드 UI 컴포넌트")]
        [SerializeField] private CaseBoardUI _boardUI;

        [Header("=== 3D 시각화 ===")]

        [Tooltip("3D 보드 시각화 컴포넌트 (선택)")]
        [SerializeField] private CaseBoard3DVisualizer _3DVisualizer;

        // =============================================================================
        // 이벤트
        // =============================================================================

        [Header("=== 이벤트 ===")]

        public UnityEvent OnBoardOpened;
        public UnityEvent OnBoardClosed;
        public UnityEvent<string, string> OnEvidencePlaced;  // nodeID, evidenceID
        public UnityEvent<string> OnEvidenceRemoved;         // nodeID
        public UnityEvent OnBoardCompleted;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private CaseBoardState _state = CaseBoardState.Closed;
        private CaseBoardData _currentBoardData;
        private CaseBoardRuntimeState _currentRuntimeState;
        private PlayerState _previousPlayerState;

        // 다중 케이스 런타임 상태
        private Dictionary<string, CaseBoardRuntimeState> _runtimeStates
            = new Dictionary<string, CaseBoardRuntimeState>();

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 현재 보드 상태
        /// </summary>
        public CaseBoardState State => _state;

        /// <summary>
        /// 보드가 열려있는지 여부
        /// </summary>
        public bool IsBoardOpen => _state == CaseBoardState.Open;

        /// <summary>
        /// 현재 보드 데이터
        /// </summary>
        public CaseBoardData CurrentBoardData => _currentBoardData;

        /// <summary>
        /// 현재 런타임 상태
        /// </summary>
        public CaseBoardRuntimeState CurrentRuntimeState => _currentRuntimeState;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (_boardUI == null)
            {
                Debug.LogError("[CaseBoardManager] CaseBoardUI가 할당되지 않았습니다!");
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // =============================================================================
        // 보드 열기/닫기
        // =============================================================================

        /// <summary>
        /// 사건 보드 열기
        /// </summary>
        /// <param name="boardData">열 보드 데이터</param>
        public void OpenBoard(CaseBoardData boardData)
        {
            if (_state != CaseBoardState.Closed)
            {
                Debug.LogWarning("[CaseBoardManager] 보드가 이미 열려있습니다.");
                return;
            }

            if (boardData == null)
            {
                Debug.LogError("[CaseBoardManager] 보드 데이터가 null입니다.");
                return;
            }

            _currentBoardData = boardData;
            _state = CaseBoardState.Opening;

            // 런타임 상태 로드 (없으면 새로 생성)
            if (!_runtimeStates.TryGetValue(boardData.CaseBoardID, out _currentRuntimeState))
            {
                _currentRuntimeState = new CaseBoardRuntimeState();
                _runtimeStates[boardData.CaseBoardID] = _currentRuntimeState;
            }

            // 플레이어 상태 전환
            if (PlayerController.Instance != null)
            {
                _previousPlayerState = PlayerController.Instance.CurrentState;
                PlayerController.Instance.SetState(PlayerState.CaseBoard);
                PlayerController.Instance.SetInputEnabled(false);
            }

            // 커서 표시
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 3D Visualizer가 있으면 카메라 블렌딩 후 UI 열기
            if (_3DVisualizer != null)
            {
                _3DVisualizer.OpenBoardWithCamera(() => OpenBoardUI());
            }
            else
            {
                OpenBoardUI();
            }

            Debug.Log($"[CaseBoardManager] 보드 열기: {_currentBoardData.CaseBoardTitle}");
        }

        /// <summary>
        /// 카메라 블렌딩 완료 후 보드 UI 열기
        /// </summary>
        private void OpenBoardUI()
        {
            if (_boardUI != null)
            {
                _boardUI.Open(_currentBoardData, _currentRuntimeState);
            }

            _state = CaseBoardState.Open;
            OnBoardOpened?.Invoke();
        }

        /// <summary>
        /// 사건 보드 닫기
        /// </summary>
        public void CloseBoard()
        {
            if (_state != CaseBoardState.Open)
            {
                Debug.LogWarning("[CaseBoardManager] 보드가 열려있지 않습니다.");
                return;
            }

            _state = CaseBoardState.Closing;

            // UI 닫기
            if (_boardUI != null)
            {
                _boardUI.Close();
            }

            // 3D 시각화 동기화 + 카메라 복원
            if (_3DVisualizer != null)
            {
                if (_currentRuntimeState != null)
                {
                    _3DVisualizer.SyncFrom2DState(_currentRuntimeState);
                }
                _3DVisualizer.CloseBoardCamera();
            }

            // 플레이어 상태 복원
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetState(_previousPlayerState);
                PlayerController.Instance.SetInputEnabled(true);

                if (PlayerController.Instance.Cursor != null)
                {
                    PlayerController.Instance.Cursor.SetCursorState(CursorState.Hidden);
                }
            }

            _state = CaseBoardState.Closed;
            OnBoardClosed?.Invoke();

            Debug.Log("[CaseBoardManager] 보드 닫기");
        }

        // =============================================================================
        // 증거물 배치
        // =============================================================================

        /// <summary>
        /// 증거물 배치 시도
        /// </summary>
        /// <param name="nodeID">노드 ID</param>
        /// <param name="evidenceID">증거물 ID</param>
        /// <returns>정답이면 true, 오답이면 false</returns>
        public bool TryPlaceEvidence(string nodeID, string evidenceID)
        {
            if (_currentBoardData == null || _currentRuntimeState == null)
            {
                Debug.LogError("[CaseBoardManager] 보드 데이터가 없습니다.");
                return false;
            }

            CaseNodeData node = _currentBoardData.GetNodeByID(nodeID);
            if (node == null)
            {
                Debug.LogWarning($"[CaseBoardManager] 노드를 찾을 수 없습니다: {nodeID}");
                return false;
            }

            // 이미 풀린 노드면 무시
            if (_currentRuntimeState.IsNodeSolved(nodeID))
            {
                Debug.Log($"[CaseBoardManager] 이미 풀린 노드: {nodeID}");
                return false;
            }

            // 정답 검증
            bool isCorrect = _currentBoardData.ValidateEvidencePlacement(nodeID, evidenceID);

            if (isCorrect)
            {
                _currentRuntimeState.PlaceEvidence(nodeID, evidenceID);
                OnEvidencePlaced?.Invoke(nodeID, evidenceID);

                Debug.Log($"[CaseBoardManager] 정답! 노드: {nodeID}, 증거물: {evidenceID}");

                // Red String 연결 체크
                UpdateRedStringConnections(nodeID);

                // 전체 완성 체크
                CheckBoardCompletion();

                return true;
            }

            Debug.Log($"[CaseBoardManager] 오답. 노드: {nodeID}, 증거물: {evidenceID}");
            return false;
        }

        /// <summary>
        /// 노드에서 증거물 제거
        /// </summary>
        public void RemoveEvidence(string nodeID)
        {
            if (_currentRuntimeState == null) return;

            _currentRuntimeState.RemoveEvidence(nodeID);
            OnEvidenceRemoved?.Invoke(nodeID);

            // UI Red String 업데이트
            if (_boardUI != null)
            {
                _boardUI.RefreshRedStrings(_currentRuntimeState);
            }
        }

        // =============================================================================
        // Red String 연결
        // =============================================================================

        private void UpdateRedStringConnections(string solvedNodeID)
        {
            CaseNodeData node = _currentBoardData.GetNodeByID(solvedNodeID);
            if (node == null) return;

            Debug.Log($"[CaseBoardManager] Red String 체크: {solvedNodeID}, 연결 노드 수: {node.ConnectedNodeIDs.Count}");

            foreach (string connectedID in node.ConnectedNodeIDs)
            {
                bool isSolved = _currentRuntimeState.IsNodeSolved(connectedID);
                Debug.Log($"[CaseBoardManager] 연결 대상: {connectedID}, 풀림: {isSolved}");

                // 연결 대상도 풀렸는지 확인
                if (isSolved)
                {
                    _currentRuntimeState.AddConnection(solvedNodeID, connectedID);
                    Debug.Log($"[CaseBoardManager] Red String 추가! {solvedNodeID} ↔ {connectedID}, 총 연결: {_currentRuntimeState.ActiveConnections.Count}");

                    // UI에 Red String 그리기 요청
                    if (_boardUI != null)
                    {
                        _boardUI.DrawRedString(solvedNodeID, connectedID);
                    }
                }
            }
        }

        // =============================================================================
        // 완성 체크
        // =============================================================================

        private void CheckBoardCompletion()
        {
            if (_currentBoardData == null || _currentRuntimeState == null) return;

            foreach (var node in _currentBoardData.Nodes)
            {
                if (!_currentRuntimeState.IsNodeSolved(node.NodeID))
                    return;
            }

            _currentRuntimeState.IsCompleted = true;
            OnBoardCompleted?.Invoke();

            Debug.Log($"[CaseBoardManager] 사건 보드 완성! {_currentBoardData.CaseBoardTitle}");
        }

        // =============================================================================
        // 인벤토리 필터링
        // =============================================================================

        /// <summary>
        /// 현재 보드에서 사용 가능한 (미배치) 증거물 목록 반환
        /// </summary>
        public List<EvidenceData> GetAvailableEvidences()
        {
            if (_currentBoardData == null || _currentRuntimeState == null)
                return new List<EvidenceData>();

            return _currentBoardData.AvailableEvidences
                .Where(e => !_currentRuntimeState.IsEvidencePlaced(e.EvidenceID))
                .ToList();
        }

        // =============================================================================
        // 저장/로드
        // =============================================================================

        /// <summary>
        /// 모든 사건 보드 상태 저장
        /// </summary>
        public void SaveAllStates()
        {
            CaseBoardSaveData saveData = new CaseBoardSaveData();
            foreach (var kvp in _runtimeStates)
            {
                saveData.Entries.Add(new CaseBoardStateEntry
                {
                    CaseBoardID = kvp.Key,
                    State = kvp.Value
                });
            }

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString("CaseBoardSaveData", json);
            PlayerPrefs.Save();

            Debug.Log("[CaseBoardManager] 사건 보드 상태 저장 완료");
        }

        /// <summary>
        /// 모든 사건 보드 상태 로드
        /// </summary>
        public void LoadAllStates()
        {
            string json = PlayerPrefs.GetString("CaseBoardSaveData", "");
            if (string.IsNullOrEmpty(json)) return;

            CaseBoardSaveData saveData = JsonUtility.FromJson<CaseBoardSaveData>(json);
            if (saveData == null || saveData.Entries == null) return;

            _runtimeStates.Clear();
            foreach (var entry in saveData.Entries)
            {
                _runtimeStates[entry.CaseBoardID] = entry.State;
            }

            Debug.Log($"[CaseBoardManager] 사건 보드 상태 로드 완료 ({saveData.Entries.Count}개 케이스)");
        }

        /// <summary>
        /// 특정 케이스의 런타임 상태 반환
        /// </summary>
        public CaseBoardRuntimeState GetRuntimeState(string caseBoardID)
        {
            _runtimeStates.TryGetValue(caseBoardID, out CaseBoardRuntimeState state);
            return state;
        }
    }
}
