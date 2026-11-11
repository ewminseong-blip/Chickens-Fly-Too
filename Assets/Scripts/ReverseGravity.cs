using UnityEngine;

// 중력 반전 트리거: 지나가면 맵이 통째로 180° 도는 연출(GravityFlipDirector) 뒤
// 화면 기준 조작이 반대가 됩니다. reverse를 끄면 원래대로 되돌리는 트리거가 됩니다.
// (태그는 "Trigger"여야 닿아도 죽지 않습니다)
public class ReverseGravity : PlayerTrigger
{
    // 체크: 화면을 뒤집습니다. 해제: 원래대로 되돌립니다.
    [SerializeField] bool reverse = true;

    protected override void OnPlayerEnter(Mover mover)
    {
        GravityFlipDirector.Get().Flip(mover, reverse);
        Debug.Log($"중력 {(reverse ? "반전" : "복구")}: {name}");
    }
}
