using UnityEngine;

// 점멸 위험물의 '초록 신호' 기준점입니다. 플레이어가 지나간 시각과 위치를 기록합니다.
// 30m 정도마다 하나씩 두면, 속도가 조금 달라도 다음 게이트에서 박자가 다시 맞춰집니다.
public class HazardSyncGate : PlayerTrigger
{
    protected override void OnPlayerEnter(Mover mover)
    {
        HazardSync.Mark(Time.timeSinceLevelLoad, transform.position.x);
    }
}

public static class HazardSync
{
    public static bool HasGate { get; private set; }
    public static float StartTime { get; private set; }
    public static float OriginX { get; private set; }

    public static void Mark(float time, float x)
    {
        HasGate = true;
        StartTime = time;
        OriginX = x;
    }

    public static void Clear()
    {
        HasGate = false;
        StartTime = 0f;
        OriginX = 0f;
    }
}
