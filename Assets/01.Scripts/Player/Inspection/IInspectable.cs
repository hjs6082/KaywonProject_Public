// =============================================================================
// IInspectable.cs
// =============================================================================
// 설명: 조사 모드에서 상호작용 가능한 오브젝트를 위한 인터페이스
// 용도: 마우스 클릭으로 조사할 수 있는 오브젝트가 구현해야 함
// 사용법:
//   1. 조사할 오브젝트에 이 인터페이스를 구현하는 스크립트 추가
//   2. 조사 모드에서 마우스로 클릭 시 OnInspect() 호출됨
// =============================================================================

using UnityEngine;

namespace GameDatabase.Player
{
    /// <summary>
    /// 조사 가능한 오브젝트 인터페이스
    /// 조사 모드에서 마우스로 클릭하여 상호작용할 수 있는 오브젝트가 구현합니다.
    /// </summary>
    public interface IInspectable
    {
        /// <summary>
        /// 조사 제목/이름
        /// UI에 표시될 오브젝트 이름
        /// </summary>
        string InspectTitle { get; }

        /// <summary>
        /// 조사 설명
        /// 오브젝트를 조사했을 때 표시될 설명 텍스트
        /// </summary>
        string InspectDescription { get; }

        /// <summary>
        /// 조사 가능 여부
        /// 조건에 따라 조사를 막을 수 있습니다.
        /// </summary>
        bool CanInspect { get; }

        /// <summary>
        /// 조사 실행
        /// 플레이어가 조사 모드에서 이 오브젝트를 클릭했을 때 호출됩니다.
        /// </summary>
        /// <param name="player">조사하는 플레이어</param>
        void OnInspect(PlayerController player);

        /// <summary>
        /// 마우스 호버 시작
        /// 조사 모드에서 마우스가 오브젝트 위로 올라갈 때 호출됩니다.
        /// </summary>
        void OnHoverEnter();

        /// <summary>
        /// 마우스 호버 종료
        /// 조사 모드에서 마우스가 오브젝트에서 벗어날 때 호출됩니다.
        /// </summary>
        void OnHoverExit();
    }

    /// <summary>
    /// 회전 가능한 오브젝트 인터페이스
    /// 조사 모드에서 마우스 드래그로 회전시킬 수 있는 오브젝트가 구현합니다.
    /// </summary>
    public interface IRotatable : IInspectable
    {
        /// <summary>
        /// 현재 회전 각도
        /// </summary>
        Vector3 CurrentRotation { get; }

        /// <summary>
        /// 회전 가능 여부
        /// </summary>
        bool CanRotate { get; }

        /// <summary>
        /// 회전 시작
        /// 마우스 드래그 시작 시 호출됩니다.
        /// </summary>
        void OnRotateStart();

        /// <summary>
        /// 회전 중
        /// 마우스 드래그 중 호출됩니다.
        /// </summary>
        /// <param name="delta">마우스 이동량</param>
        void OnRotate(Vector2 delta);

        /// <summary>
        /// 회전 종료
        /// 마우스 드래그 종료 시 호출됩니다.
        /// </summary>
        void OnRotateEnd();

        /// <summary>
        /// 회전 초기화
        /// 원래 회전 상태로 되돌립니다.
        /// </summary>
        void ResetRotation();
    }
}
