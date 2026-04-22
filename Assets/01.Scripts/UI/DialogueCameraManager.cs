// =============================================================================
// DialogueCameraManager.cs
// =============================================================================
// 설명: 다이얼로그 카메라 시스템의 중앙 관리자
// 용도: 대화 중 시네머신 가상 카메라 전환 및 카메라 이동 제어
// 의존: Cinemachine 2.10.3, DialogueManager
// =============================================================================
//
// 사용법:
//   1. 씬에 빈 게임오브젝트를 만들고 이 컴포넌트를 추가
//   2. 카메라 엔트리 목록에 CameraId + CinemachineVirtualCamera 쌍을 등록
//   3. DialogueManager의 _cameraManager 필드에 이 오브젝트를 할당
//   4. DialogueData의 각 대사 라인에서 CameraId를 지정하면 자동으로 카메라 전환
//
// =============================================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using GameDatabase.Dialogue;
using GameDatabase.Player;

namespace GameDatabase.UI
{
    /// <summary>
    /// 대화 카메라 엔트리
    /// CameraId와 씬의 가상 카메라를 매핑
    /// </summary>
    [System.Serializable]
    public class DialogueCameraEntry
    {
        [Tooltip("카메라 식별자 (DialogueLine의 CameraId와 매칭)")]
        public string CameraId;

        [Tooltip("씬에 배치된 시네머신 가상 카메라")]
        public CinemachineVirtualCamera VirtualCamera;
    }

    /// <summary>
    /// 다이얼로그 카메라 매니저
    /// 대화 중 시네머신 가상 카메라 전환 및 이동을 관리하는 싱글톤 클래스
    /// </summary>
    public class DialogueCameraManager : MonoBehaviour
    {
        // =============================================================================
        // 싱글톤
        // =============================================================================

        private static DialogueCameraManager _instance;

        /// <summary>
        /// 싱글톤 인스턴스
        /// </summary>
        public static DialogueCameraManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DialogueCameraManager>();
                    if (_instance == null)
                    {
                        Debug.LogError("[DialogueCameraManager] 씬에 DialogueCameraManager가 없습니다.");
                    }
                }
                return _instance;
            }
        }

        // =============================================================================
        // 카메라 설정
        // =============================================================================

        [Header("카메라 목록")]
        [Tooltip("대화에서 사용할 카메라 엔트리 목록\nCameraId와 VirtualCamera 쌍으로 등록")]
        [SerializeField] private List<DialogueCameraEntry> _cameraEntries = new List<DialogueCameraEntry>();

        [Header("블렌딩 설정")]
        [Tooltip("카메라 전환 블렌드 시간 (초)")]
        [SerializeField] private float _blendDuration = 0.5f;

        [Header("플레이어 참조")]
        [Tooltip("플레이어 컨트롤러 (대화 시 카메라 고정)")]
        [SerializeField] private PlayerController _playerController;

        [Header("선택지 카메라 설정 (런타임 자동 생성)")]
        [Tooltip("NPC 뒤쪽에서 얼마나 떨어질지 (NPC→카메라 거리)")]
        [SerializeField] private float _choiceCamDistance = 2.5f;

        [Tooltip("NPC 기준 카메라 높이 오프셋")]
        [SerializeField] private float _choiceCamHeightOffset = 1.2f;

        [Tooltip("플레이어 방향 기준 오른쪽으로 얼마나 옆으로 이동할지 (Over-the-shoulder 각도)")]
        [SerializeField] private float _choiceCamSideOffset = 1.0f;

        [Tooltip("선택지 카메라 FOV")]
        [SerializeField] private float _choiceCamFov = 50f;

        // =============================================================================
        // 상수
        // =============================================================================

        // 시네머신 Priority 값
        // CinemachineBrain은 가장 높은 Priority의 카메라를 활성화
        private const int DIALOGUE_CAMERA_PRIORITY = 20;
        private const int INACTIVE_PRIORITY = 0;

        // =============================================================================
        // 내부 상태
        // =============================================================================

        // CameraId -> VirtualCamera 룩업 딕셔너리
        private Dictionary<string, CinemachineVirtualCamera> _cameraLookup;

        // VirtualCamera -> 원래 위치 (이동 후 복원용)
        private Dictionary<CinemachineVirtualCamera, Vector3> _originalPositions
            = new Dictionary<CinemachineVirtualCamera, Vector3>();

        // 현재 활성 대화 카메라
        private CinemachineVirtualCamera _currentDialogueCamera;

        // 카메라 이동 코루틴
        private Coroutine _cameraMovementCoroutine;

        // 대화 카메라 활성 여부
        private bool _isDialogueCameraActive = false;

        // 런타임 생성된 선택지 전용 가상 카메라
        private CinemachineVirtualCamera _choiceCamera;

        // 선택지 카메라 활성 여부
        private bool _isChoiceCameraActive = false;

        // =============================================================================
        // 프로퍼티
        // =============================================================================

        /// <summary>
        /// 대화 카메라가 활성화되어 있는지
        /// </summary>
        public bool IsDialogueCameraActive => _isDialogueCameraActive;

        /// <summary>
        /// 현재 활성 대화 카메라
        /// </summary>
        public CinemachineVirtualCamera CurrentDialogueCamera => _currentDialogueCamera;

        /// <summary>
        /// 등록된 카메라 엔트리 목록 (읽기 전용)
        /// </summary>
        public IReadOnlyList<DialogueCameraEntry> CameraEntries => _cameraEntries;

        // =============================================================================
        // Unity 생명주기
        // =============================================================================

        private void Awake()
        {
            // 싱글톤 설정
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // 카메라 룩업 테이블 구축
            BuildCameraLookup();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        // =============================================================================
        // 공개 메서드 - 대화 이벤트 핸들러
        // =============================================================================

        /// <summary>
        /// 대화 시작 시 호출
        /// 플레이어 카메라를 비활성화하고 대화 카메라 모드 진입
        /// </summary>
        public void OnDialogueStarted()
        {
            _isDialogueCameraActive = true;

            // 플레이어 카메라 고정
            if (_playerController != null)
            {
                _playerController.LockCameraPosition = true;
            }

            Debug.Log("[DialogueCameraManager] 대화 카메라 모드 시작");
        }

        /// <summary>
        /// 대화 종료 시 호출
        /// 모든 대화 카메라를 비활성화하고 플레이어 카메라 복원
        /// </summary>
        public void OnDialogueEnded()
        {
            // 카메라 이동 중지
            StopCameraMovement();

            // 선택지 카메라 제거
            DestroyChoiceCamera();

            // 현재 대화 카메라 비활성화
            DeactivateCurrentDialogueCamera();

            // 모든 대화 카메라의 원래 위치 복원
            RestoreAllCameraPositions();

            _isDialogueCameraActive = false;

            // 플레이어 카메라 복원
            if (_playerController != null)
            {
                _playerController.LockCameraPosition = false;
            }

            Debug.Log("[DialogueCameraManager] 대화 카메라 모드 종료");
        }

        /// <summary>
        /// 선택지 표시 시 호출
        /// NPC와 플레이어 위치를 기반으로 Over-the-shoulder 카메라를 런타임 생성 및 활성화
        /// </summary>
        /// <param name="npcTransform">선택지 대상 NPC Transform</param>
        public void OnChoiceDisplayed(Transform npcTransform)
        {
            if (npcTransform == null) return;

            // 기존 선택지 카메라 정리
            DestroyChoiceCamera();

            // 플레이어 위치 확인
            Transform playerTr = _playerController != null ? _playerController.transform : null;
            if (playerTr == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) playerTr = playerObj.transform;
            }

            // 카메라 위치 계산
            Vector3 camPos = CalculateChoiceCameraPosition(npcTransform, playerTr);

            // 런타임 가상 카메라 생성
            GameObject camObj = new GameObject("ChoiceCamera_Runtime");
            camObj.transform.position = camPos;

            _choiceCamera = camObj.AddComponent<CinemachineVirtualCamera>();
            _choiceCamera.m_Lens.FieldOfView = _choiceCamFov;

            // LookAt: NPC 목 위치 (높이 오프셋 적용)
            GameObject lookAtTarget = new GameObject("ChoiceCam_LookAtTarget");
            lookAtTarget.transform.SetParent(npcTransform);
            lookAtTarget.transform.localPosition = Vector3.up * (_choiceCamHeightOffset * 0.6f);
            _choiceCamera.LookAt = lookAtTarget.transform;

            // Body/Aim: 고정 위치에서 LookAt만 동작하도록
            // Cinemachine의 기본 상태(컴포넌트 없음)가 고정 위치 = Do Nothing Body

            // 이전 대화 카메라 비활성화 후 선택지 카메라 활성화
            DeactivateCurrentDialogueCamera();
            _choiceCamera.Priority = DIALOGUE_CAMERA_PRIORITY + 5; // 대화 카메라보다 높은 Priority
            _isChoiceCameraActive = true;

            Debug.Log($"[DialogueCameraManager] 선택지 카메라 활성화: 위치={camPos}");
        }

        /// <summary>
        /// 선택지 종료 시 호출 (선택 완료 또는 대화 종료)
        /// 선택지 카메라를 제거하고 이전 상태로 복원
        /// </summary>
        public void OnChoiceEnded()
        {
            if (!_isChoiceCameraActive) return;
            DestroyChoiceCamera();
            Debug.Log("[DialogueCameraManager] 선택지 카메라 해제");
        }

        /// <summary>
        /// 대사 라인 표시 시 호출
        /// 해당 라인에 지정된 카메라로 전환하고 이동 애니메이션 시작
        /// </summary>
        /// <param name="line">표시할 대사 라인</param>
        public void OnLineDisplayed(DialogueLine line)
        {
            if (line == null) return;

            // 카메라가 지정되지 않은 경우 현재 카메라 유지
            if (!line.HasCamera)
            {
                return;
            }

            // CameraId로 가상 카메라 찾기
            CinemachineVirtualCamera targetCamera = GetCamera(line.CameraId);
            if (targetCamera == null)
            {
                Debug.LogWarning($"[DialogueCameraManager] 카메라 ID '{line.CameraId}'를 찾을 수 없습니다.");
                return;
            }

            // 이전 카메라 이동 중지
            StopCameraMovement();

            // 이전 대화 카메라 비활성화
            DeactivateCurrentDialogueCamera();

            // 새 카메라의 원래 위치 저장 (이미 저장되어 있지 않은 경우)
            SaveOriginalPosition(targetCamera);

            // 원래 위치로 리셋 (이전에 이동된 경우 복원)
            RestoreCameraPosition(targetCamera);

            // 새 카메라 활성화
            _currentDialogueCamera = targetCamera;
            ActivateDialogueCamera(_currentDialogueCamera);

            // 카메라 이동 애니메이션 시작
            if (line.CameraMovement != CameraMovementType.None)
            {
                _cameraMovementCoroutine = StartCoroutine(
                    AnimateCameraMovement(
                        _currentDialogueCamera,
                        line.CameraMovement,
                        line.CameraMovementDistance,
                        line.CameraMovementDuration
                    )
                );
            }
        }

        // =============================================================================
        // 공개 메서드 - 카메라 관리
        // =============================================================================

        /// <summary>
        /// CameraId로 가상 카메라를 찾아 반환
        /// </summary>
        /// <param name="cameraId">카메라 식별자</param>
        /// <returns>해당 가상 카메라, 없으면 null</returns>
        public CinemachineVirtualCamera GetCamera(string cameraId)
        {
            if (string.IsNullOrEmpty(cameraId)) return null;

            if (_cameraLookup == null)
            {
                BuildCameraLookup();
            }

            _cameraLookup.TryGetValue(cameraId, out var vcam);
            return vcam;
        }

        /// <summary>
        /// 등록된 모든 카메라 ID 목록 반환
        /// 에디터에서 드롭다운 등에 활용
        /// </summary>
        /// <returns>카메라 ID 배열</returns>
        public string[] GetAllCameraIds()
        {
            if (_cameraEntries == null || _cameraEntries.Count == 0)
                return new string[0];

            var ids = new List<string>();
            foreach (var entry in _cameraEntries)
            {
                if (!string.IsNullOrEmpty(entry.CameraId))
                {
                    ids.Add(entry.CameraId);
                }
            }
            return ids.ToArray();
        }

        /// <summary>
        /// 카메라 룩업 테이블 재구축
        /// 런타임에 카메라 엔트리가 변경된 경우 호출
        /// </summary>
        public void RefreshCameraLookup()
        {
            BuildCameraLookup();
        }

        // =============================================================================
        // 내부 메서드 - 카메라 전환
        // =============================================================================

        /// <summary>
        /// 카메라 룩업 딕셔너리 구축
        /// </summary>
        private void BuildCameraLookup()
        {
            _cameraLookup = new Dictionary<string, CinemachineVirtualCamera>();

            foreach (var entry in _cameraEntries)
            {
                if (!string.IsNullOrEmpty(entry.CameraId) && entry.VirtualCamera != null)
                {
                    if (_cameraLookup.ContainsKey(entry.CameraId))
                    {
                        Debug.LogWarning($"[DialogueCameraManager] 중복된 카메라 ID: '{entry.CameraId}'");
                        continue;
                    }
                    _cameraLookup[entry.CameraId] = entry.VirtualCamera;
                }
            }

            // 모든 등록된 카메라를 비활성 Priority로 초기화
            foreach (var entry in _cameraEntries)
            {
                if (entry.VirtualCamera != null)
                {
                    entry.VirtualCamera.Priority = INACTIVE_PRIORITY;
                }
            }

            Debug.Log($"[DialogueCameraManager] {_cameraLookup.Count}개의 대화 카메라 등록 완료");
        }

        /// <summary>
        /// 대화 카메라 활성화 (Priority 설정)
        /// CinemachineBrain이 자동으로 블렌딩 처리
        /// </summary>
        /// <param name="vcam">활성화할 가상 카메라</param>
        private void ActivateDialogueCamera(CinemachineVirtualCamera vcam)
        {
            if (vcam == null) return;
            vcam.Priority = DIALOGUE_CAMERA_PRIORITY;
        }

        /// <summary>
        /// 현재 대화 카메라 비활성화
        /// </summary>
        private void DeactivateCurrentDialogueCamera()
        {
            if (_currentDialogueCamera != null)
            {
                _currentDialogueCamera.Priority = INACTIVE_PRIORITY;
                _currentDialogueCamera = null;
            }
        }

        // =============================================================================
        // 내부 메서드 - 위치 저장/복원
        // =============================================================================

        /// <summary>
        /// 가상 카메라의 원래 위치 저장
        /// </summary>
        /// <param name="vcam">위치를 저장할 카메라</param>
        private void SaveOriginalPosition(CinemachineVirtualCamera vcam)
        {
            if (vcam == null) return;

            if (!_originalPositions.ContainsKey(vcam))
            {
                _originalPositions[vcam] = vcam.transform.position;
            }
        }

        /// <summary>
        /// 특정 카메라의 원래 위치 복원
        /// </summary>
        /// <param name="vcam">복원할 카메라</param>
        private void RestoreCameraPosition(CinemachineVirtualCamera vcam)
        {
            if (vcam == null) return;

            if (_originalPositions.TryGetValue(vcam, out Vector3 originalPos))
            {
                vcam.transform.position = originalPos;
            }
        }

        /// <summary>
        /// 모든 카메라의 원래 위치 복원
        /// </summary>
        private void RestoreAllCameraPositions()
        {
            foreach (var kvp in _originalPositions)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.transform.position = kvp.Value;
                }
            }
            _originalPositions.Clear();
        }

        // =============================================================================
        // 내부 메서드 - 카메라 이동 애니메이션
        // =============================================================================

        /// <summary>
        /// Over-the-shoulder 선택지 카메라 위치 계산
        /// 플레이어 뒤-오른쪽에서 NPC를 바라보는 구도
        /// </summary>
        private Vector3 CalculateChoiceCameraPosition(Transform npcTr, Transform playerTr)
        {
            Vector3 npcPos = npcTr.position + Vector3.up * _choiceCamHeightOffset;

            // 플레이어→NPC 방향
            Vector3 toNpc = (npcTr.position - (playerTr != null ? playerTr.position : npcTr.position)).normalized;
            if (toNpc == Vector3.zero) toNpc = Vector3.forward;

            // 카메라는 NPC 뒤에서 플레이어 쪽으로 _choiceCamDistance 거리
            // 즉, NPC 기준 플레이어 방향으로 이동 + 오른쪽으로 sideOffset
            Vector3 backward = -toNpc;
            Vector3 right    = Vector3.Cross(Vector3.up, toNpc).normalized; // toNpc의 오른쪽

            return npcPos
                + backward * _choiceCamDistance
                + right    * _choiceCamSideOffset
                + Vector3.up * 0.3f; // 살짝 위에서 내려다보는 각도
        }

        /// <summary>
        /// 런타임 생성된 선택지 카메라 및 LookAt 타겟 제거
        /// </summary>
        private void DestroyChoiceCamera()
        {
            if (_choiceCamera != null)
            {
                // LookAt 타겟도 함께 제거
                if (_choiceCamera.LookAt != null)
                    Destroy(_choiceCamera.LookAt.gameObject);

                Destroy(_choiceCamera.gameObject);
                _choiceCamera = null;
            }
            _isChoiceCameraActive = false;
        }

        /// <summary>
        /// 카메라 이동 코루틴 중지
        /// </summary>
        private void StopCameraMovement()
        {
            if (_cameraMovementCoroutine != null)
            {
                StopCoroutine(_cameraMovementCoroutine);
                _cameraMovementCoroutine = null;
            }
        }

        /// <summary>
        /// 카메라 이동 애니메이션 코루틴
        /// 지정된 방향으로 SmoothStep 기반 부드러운 이동
        /// </summary>
        /// <param name="vcam">이동할 가상 카메라</param>
        /// <param name="movementType">이동 방향 타입</param>
        /// <param name="distance">이동 거리 (월드 단위)</param>
        /// <param name="duration">이동 지속시간 (초)</param>
        private IEnumerator AnimateCameraMovement(
            CinemachineVirtualCamera vcam,
            CameraMovementType movementType,
            float distance,
            float duration)
        {
            if (vcam == null) yield break;

            // 이동 방향 계산 (카메라의 로컬 축 기준)
            Vector3 direction = GetMovementDirection(vcam.transform, movementType);

            // 시작 위치
            Vector3 startPosition = vcam.transform.position;
            // 종료 위치
            Vector3 endPosition = startPosition + direction * distance;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // SmoothStep으로 부드러운 가감속 이동
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                vcam.transform.position = Vector3.Lerp(startPosition, endPosition, smoothT);

                yield return null;
            }

            // 최종 위치 보정
            vcam.transform.position = endPosition;
            _cameraMovementCoroutine = null;
        }

        /// <summary>
        /// 이동 방향 벡터 계산
        /// 카메라의 로컬 축을 기준으로 방향을 결정
        /// </summary>
        /// <param name="cameraTransform">카메라 Transform</param>
        /// <param name="type">이동 방향 타입</param>
        /// <returns>월드 공간 이동 방향 벡터 (정규화)</returns>
        private Vector3 GetMovementDirection(Transform cameraTransform, CameraMovementType type)
        {
            switch (type)
            {
                case CameraMovementType.LeftToRight:
                    return cameraTransform.right;           // 카메라 기준 오른쪽
                case CameraMovementType.RightToLeft:
                    return -cameraTransform.right;          // 카메라 기준 왼쪽
                case CameraMovementType.UpToDown:
                    return -cameraTransform.up;             // 카메라 기준 아래쪽
                case CameraMovementType.DownToUp:
                    return cameraTransform.up;              // 카메라 기준 위쪽
                default:
                    return Vector3.zero;
            }
        }
    }
}
