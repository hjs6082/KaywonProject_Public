// =============================================================================
// PlayerState.cs
// =============================================================================
// 설명: 플레이어의 상태를 정의하는 열거형
// 용도: PlayerController에서 현재 플레이어 상태를 관리할 때 사용
// =============================================================================

namespace GameDatabase.Player
{
    /// <summary>
    /// 플레이어의 현재 상태를 나타내는 열거형
    /// </summary>
    public enum PlayerState
    {
        /// <summary>
        /// 대기 상태 - 움직이지 않고 서 있는 상태
        /// </summary>
        Idle,

        /// <summary>
        /// 걷기 상태 - 일반 속도로 이동 중
        /// </summary>
        Walking,

        /// <summary>
        /// 달리기 상태 - 빠른 속도로 이동 중
        /// </summary>
        Running,

        /// <summary>
        /// 대화 상태 - NPC와 대화 중 (이동 및 카메라 조작 불가)
        /// </summary>
        InDialogue,

        /// <summary>
        /// 조사 상태 - 마우스로 오브젝트를 조사하는 모드
        /// 커서가 표시되고, 클릭으로 오브젝트 상호작용 가능
        /// </summary>
        Inspecting,

        /// <summary>
        /// 오브젝트 회전 상태 - 마우스 드래그로 오브젝트를 회전시키는 중
        /// </summary>
        RotatingObject,

        /// <summary>
        /// 조사 구역 모드 - 포인트앤클릭으로 단서를 조사하는 상태
        /// </summary>
        Investigating,

        /// <summary>
        /// 사건 보드 상태 - 사건 보드 UI를 조작 중 (이동/카메라 불가)
        /// </summary>
        CaseBoard,

        /// <summary>
        /// 일시정지 상태 - 게임이 일시정지된 상태 (메뉴, 설정 등)
        /// </summary>
        Paused
    }

    /// <summary>
    /// 커서의 현재 상태를 나타내는 열거형
    /// </summary>
    public enum CursorState
    {
        /// <summary>
        /// 기본 - 숨김 및 잠금 상태 (1인칭 모드)
        /// </summary>
        Hidden,

        /// <summary>
        /// 일반 포인터 - 기본 커서 표시
        /// </summary>
        Normal,

        /// <summary>
        /// 잡기 가능 - 오브젝트를 잡을 수 있음을 표시
        /// </summary>
        Grab,

        /// <summary>
        /// 잡는 중 - 오브젝트를 잡고 있는 상태
        /// </summary>
        Grabbing,

        /// <summary>
        /// 조사 가능 - 오브젝트를 조사할 수 있음을 표시
        /// </summary>
        Inspect,

        /// <summary>
        /// 상호작용 가능 - E키 등으로 상호작용 가능
        /// </summary>
        Interact
    }
}
