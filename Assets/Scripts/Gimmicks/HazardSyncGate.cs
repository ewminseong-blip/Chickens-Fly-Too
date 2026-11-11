using UnityEngine;

// 점멸 위험물의 '초록 신호' 기준점입니다. 플레이어가 지나간 시각 · 길 위 위치 · 그때 속도를 기록합니다.
// 번개 줄마다 바로 앞에 하나씩 두면, 속도가 바뀌어도(가속 · 감속) 다음 줄에서 박자가 다시 맞춰집니다.
public class HazardSyncGate : PlayerTrigger
{
    float progress;

    void Start()
    {
        CoursePath path = CoursePath.Get();
        progress = path != null ? path.Progress(transform.position) : transform.position.x;
    }

    protected override void OnPlayerEnter(Mover mover)
    {
        Vector3 v = mover.Velocity;
        HazardSync.Mark(Time.timeSinceLevelLoad, progress, new Vector2(v.x, v.z).magnitude);
    }
}

public static class HazardSync
{
    public static bool HasGate { get; private set; }
    public static float StartTime { get; private set; }
    public static float OriginX { get; private set; }   // 길 위 거리(m)
    public static float Speed { get; private set; }     // 지날 때 수평 속도(m/s)

    public static void Mark(float time, float progress, float speed)
    {
        HasGate = true;
        StartTime = time;
        OriginX = progress;
        Speed = speed;
    }

    public static void Clear()
    {
        HasGate = false;
        StartTime = 0f;
        OriginX = 0f;
        Speed = 0f;
    }
}
