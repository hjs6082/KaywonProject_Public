// =============================================================================
// CaseBoard3DVisualizer.cs
// =============================================================================
// 설명: 3D 공간에서 사건 보드의 시각적 표현을 관리
// 용도: 2D UI에서의 배치 상태를 3D 보드에 동기화
// =============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using GameDatabase.Evidence;
using GameDatabase.Player;
using GameDatabase.UI;

namespace GameDatabase.CaseBoard
{
    /// <summary>
    /// 3D 사건 보드 시각화 관리자
    /// </summary>
    public class CaseBoard3DVisualizer : MonoBehaviour
    {
        // =============================================================================
        // 참조
        // =============================================================================

        [Header("=== 보드 참조 ===")]

        [Tooltip("사건 보드 데이터")]
        [SerializeField] private CaseBoardData _caseBoardData;

        [Tooltip("보드 평면 Transform (코르크보드 Quad 등)")]
        [SerializeField] private Transform _boardPlane;

        [Tooltip("보드 가로 크기 (월드 단위)")]
        [SerializeField] private float _boardWidth = 2f;

        [Tooltip("보드 세로 크기 (월드 단위)")]
        [SerializeField] private float _boardHeight = 1.5f;

        // =============================================================================
        // 카메라
        // =============================================================================

        [Header("=== 카메라 ===")]

        [Tooltip("보드 Cinemachine 카메라")]
        [SerializeField] private CinemachineVirtualCamera _boardCamera;

        [Tooltip("보드 상호작용 시 카메라 Priority")]
        [SerializeField] private int _activePriority = 20;

        [Tooltip("비활성 시 카메라 Priority")]
        [SerializeField] private int _inactivePriority = 0;

        // =============================================================================
        // 프리팹
        // =============================================================================

        [Header("=== 프리팹 ===")]

        [Tooltip("3D 노드 프리팹")]
        [SerializeField] private GameObject _node3DPrefab;

        [Tooltip("3D Red String 프리팹")]
        [SerializeField] private GameObject _redString3DPrefab;

        // =============================================================================
        // 상호작용
        // =============================================================================

        [Header("=== 상호작용 ===")]

        [Tooltip("플레이어가 보드를 열 수 있는 거리")]
        [SerializeField] private float _interactionRange = 3f;

        [Tooltip("상호작용 키")]
        [SerializeField] private KeyCode _interactKey = KeyCode.E;

        // =============================================================================
        // 내부 변수
        // =============================================================================

        private Dictionary<string, CaseNode3D> _node3DMap = new Dictionary<string, CaseNode3D>();
        private List<RedString3D> _activeRedStrings = new List<RedString3D>();
        private bool _isInitialized = false;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 사건 보드 데이터
        /// </summary>
        public CaseBoardData CaseBoardData => _caseBoardData;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Start()
        {
            InitializeBoard();

            // 카메라 비활성화
            if (_boardCamera != null)
            {
                _boardCamera.Priority = _inactivePriority;
            }
        }

        private void Update()
        {
            // 보드가 이미 열려있으면 무시
            if (CaseBoardManager.Instance != null && CaseBoardManager.Instance.IsBoardOpen)
                return;

            // 상호작용 키 입력 감지
            if (Input.GetKeyDown(_interactKey))
            {
                TryOpenBoard();
            }
        }

        /// <summary>
        /// 플레이어 거리 체크 후 보드 열기
        /// </summary>
        private void TryOpenBoard()
        {
            if (_caseBoardData == null || _boardPlane == null) return;

            // 플레이어와의 거리 체크
            Transform player = PlayerController.Instance?.transform;
            if (player == null) return;

            float distance = Vector3.Distance(player.position, _boardPlane.position);
            if (distance > _interactionRange) return;

            // 보드 열기
            CaseBoardManager.Instance?.OpenBoard(_caseBoardData);
        }

        // =============================================================================
        // 카메라 블렌딩
        // =============================================================================

        /// <summary>
        /// 카메라를 보드 정면으로 블렌딩한 후 콜백 호출
        /// </summary>
        public void OpenBoardWithCamera(System.Action onCameraReady)
        {
            ActivateBoardCamera();
            StartCoroutine(WaitForBlendComplete(onCameraReady));
        }

        /// <summary>
        /// 보드 카메라 비활성화 (원래 카메라로 복귀)
        /// </summary>
        public void CloseBoardCamera()
        {
            DeactivateBoardCamera();
        }

        private IEnumerator WaitForBlendComplete(System.Action onComplete)
        {
            var brain = Camera.main?.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                // 블렌딩 시작 대기 (1프레임)
                yield return null;
                // 블렌딩 완료 대기
                while (brain.IsBlending)
                    yield return null;
            }
            onComplete?.Invoke();
        }

        // =============================================================================
        // 초기화
        // =============================================================================

        private void InitializeBoard()
        {
            if (_caseBoardData == null || _node3DPrefab == null || _boardPlane == null)
            {
                Debug.LogWarning("[CaseBoard3DVisualizer] 필수 참조가 누락되었습니다.");
                return;
            }

            // 기존 노드 정리
            ClearNodes();

            // 노드 생성 (보드 평면의 자식으로 로컬 좌표 배치)
            foreach (CaseNodeData nodeData in _caseBoardData.Nodes)
            {
                float x = (nodeData.BoardPosition.x - 0.5f) * _boardWidth;
                float y = (nodeData.BoardPosition.y - 0.5f) * _boardHeight;
                Vector3 localPos = new Vector3(x, y, -0.01f);

                GameObject nodeObj = Instantiate(_node3DPrefab, _boardPlane);
                nodeObj.transform.localPosition = localPos;
                nodeObj.transform.localRotation = Quaternion.identity;
                nodeObj.name = $"Node3D_{nodeData.NodeID}";

                CaseNode3D node3D = nodeObj.GetComponent<CaseNode3D>();
                if (node3D != null)
                {
                    node3D.Initialize(nodeData);
                    _node3DMap[nodeData.NodeID] = node3D;
                }
            }

            _isInitialized = true;

            // 기존 저장 상태 복원
            CaseBoardRuntimeState savedState = CaseBoardManager.Instance?.GetRuntimeState(_caseBoardData.CaseBoardID);
            if (savedState != null)
            {
                SyncFrom2DState(savedState);
            }
        }

        private void ClearNodes()
        {
            foreach (var kvp in _node3DMap)
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
            }
            _node3DMap.Clear();

            foreach (var rs in _activeRedStrings)
            {
                if (rs != null) Destroy(rs.gameObject);
            }
            _activeRedStrings.Clear();
        }

        // =============================================================================
        // 2D → 3D 동기화
        // =============================================================================

        /// <summary>
        /// 2D UI 상태를 3D 보드에 동기화
        /// </summary>
        public void SyncFrom2DState(CaseBoardRuntimeState state)
        {
            Debug.Log($"[CaseBoard3D] SyncFrom2DState 호출 - initialized: {_isInitialized}, state null: {state == null}, connections: {state?.ActiveConnections.Count ?? -1}");
            if (!_isInitialized || state == null) return;

            // 노드별 증거물 표시 업데이트
            foreach (var nodeData in _caseBoardData.Nodes)
            {
                string placedID = state.GetPlacedEvidenceID(nodeData.NodeID);

                if (_node3DMap.TryGetValue(nodeData.NodeID, out CaseNode3D node3D))
                {
                    if (!string.IsNullOrEmpty(placedID))
                    {
                        EvidenceData evidence = _caseBoardData.GetEvidenceByID(placedID);
                        if (evidence != null)
                        {
                            node3D.ShowEvidence(evidence);
                        }
                    }
                    else
                    {
                        node3D.ClearEvidence();
                    }
                }
            }

            // Red String 업데이트
            UpdateRedStrings3D(state);
        }

        // =============================================================================
        // Red String 3D
        // =============================================================================

        private void UpdateRedStrings3D(CaseBoardRuntimeState state)
        {
            Debug.Log($"[CaseBoard3D] UpdateRedStrings3D - prefab null: {_redString3DPrefab == null}, boardPlane null: {_boardPlane == null}, connections: {state.ActiveConnections.Count}");

            // 기존 Red String 제거
            foreach (var rs in _activeRedStrings)
            {
                if (rs != null) Destroy(rs.gameObject);
            }
            _activeRedStrings.Clear();

            if (_redString3DPrefab == null || _boardPlane == null) return;

            // 활성 연결에 대해 Red String 생성
            foreach (var conn in state.ActiveConnections)
            {
                Debug.Log($"[CaseBoard3D] 연결 처리: {conn.FromNodeID} → {conn.ToNodeID}");

                CaseNodeData fromData = _caseBoardData.GetNodeByID(conn.FromNodeID);
                CaseNodeData toData = _caseBoardData.GetNodeByID(conn.ToNodeID);

                Debug.Log($"[CaseBoard3D] fromData null: {fromData == null}, toData null: {toData == null}");

                if (fromData == null || toData == null) continue;

                // 노드 데이터에서 직접 월드 좌표 계산 (transform.position 의존 제거)
                Vector3 fromPos = NodeDataToWorldPosition(fromData.BoardPosition);
                Vector3 toPos = NodeDataToWorldPosition(toData.BoardPosition);

                GameObject rsObj = Instantiate(_redString3DPrefab);
                rsObj.name = $"RedString_{conn.FromNodeID}_{conn.ToNodeID}";

                RedString3D rs = rsObj.GetComponent<RedString3D>();
                Debug.Log($"[CaseBoard3D] RedString3D 컴포넌트 null: {rs == null}, LineRenderer null: {rsObj.GetComponent<LineRenderer>() == null}");

                if (rs == null)
                {
                    // RedString3D 컴포넌트가 없으면 추가
                    rs = rsObj.AddComponent<RedString3D>();
                    Debug.LogWarning("[CaseBoard3D] RedString3D 프리팹에 스크립트가 누락됨! 자동 추가.");
                }

                Debug.Log($"[RedString3D] {conn.FromNodeID}({fromPos}) → {conn.ToNodeID}({toPos})");
                rs.SetConnection(fromPos, toPos);
                _activeRedStrings.Add(rs);
            }
        }

        /// <summary>
        /// 노드 정규화 좌표를 보드 기준 월드 좌표로 변환
        /// </summary>
        private Vector3 NodeDataToWorldPosition(Vector2 boardPosition)
        {
            float x = (boardPosition.x - 0.5f) * _boardWidth;
            float y = (boardPosition.y - 0.5f) * _boardHeight;
            Vector3 localPos = new Vector3(x, y, -0.02f);
            return _boardPlane.TransformPoint(localPos);
        }

        // =============================================================================
        // 좌표 변환
        // =============================================================================

        /// <summary>
        /// 정규화 좌표(0~1)를 보드 평면의 월드 좌표로 변환
        /// </summary>
        private Vector3 NormalizedToBoardPosition(Vector2 normalizedPos)
        {
            if (_boardPlane == null) return Vector3.zero;

            // 보드 중심 기준으로 오프셋 계산
            float x = (normalizedPos.x - 0.5f) * _boardWidth;
            float y = (normalizedPos.y - 0.5f) * _boardHeight;

            // 보드 평면의 로컬 좌표 → 월드 좌표
            Vector3 localPos = new Vector3(x, y, -0.01f); // 보드 표면 앞쪽
            return _boardPlane.TransformPoint(localPos);
        }

        // =============================================================================
        // 카메라 제어
        // =============================================================================

        /// <summary>
        /// 보드 카메라 활성화 (보드 상호작용 시작)
        /// </summary>
        public void ActivateBoardCamera()
        {
            if (_boardCamera != null)
            {
                _boardCamera.Priority = _activePriority;
            }
        }

        /// <summary>
        /// 보드 카메라 비활성화
        /// </summary>
        public void DeactivateBoardCamera()
        {
            if (_boardCamera != null)
            {
                _boardCamera.Priority = _inactivePriority;
            }
        }

        // =============================================================================
        // Gizmo
        // =============================================================================

        private void OnDrawGizmosSelected()
        {
            if (_boardPlane == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.matrix = _boardPlane.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(_boardWidth, _boardHeight, 0.01f));

            // 노드 위치 미리보기
            if (_caseBoardData != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var node in _caseBoardData.Nodes)
                {
                    Vector3 localPos = new Vector3(
                        (node.BoardPosition.x - 0.5f) * _boardWidth,
                        (node.BoardPosition.y - 0.5f) * _boardHeight,
                        -0.01f);
                    Gizmos.DrawWireSphere(localPos, 0.05f);
                }
            }
        }
    }
}
