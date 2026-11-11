using UnityEngine;

// 씬을 다시 불러와도 남아 있어야 하는 '한 스테이지 도전' 기록입니다.
// (체크포인트, 죽기 전까지 흐른 시간, 사망 횟수, 출발 위치)
// 다른 스테이지로 넘어가거나 Restart를 누르면 ResetRun()으로 지웁니다.
public static class GameSession
{
    // 기록이 속한 스테이지(씬 이름). 다른 씬에서 쓰려 하면 자동으로 초기화합니다.
    public static string StageName { get; private set; } = "";

    public static bool HasCheckpoint { get; private set; }
    public static int CheckpointOrder { get; private set; }
    public static Vector3 CheckpointPosition { get; private set; }
    // 체크포인트가 중력 반전 구간 안이면 부활할 때도 반전 상태로 시작합니다.
    public static bool CheckpointReversed { get; private set; }

    public static float AccumulatedTime { get; private set; }
    public static int Deaths { get; private set; }

    public static bool HasStartX { get; private set; }
    public static float StartX { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLaunch()
    {
        StageName = "";
        ResetRun();
    }

    // 지금 씬의 기록인지 확인하고, 아니면 새로 시작합니다. 여러 번 불러도 안전합니다.
    public static void EnsureStage(string sceneName)
    {
        if (StageName == sceneName)
        {
            return;
        }

        ResetRun();
        StageName = sceneName;
    }

    public static void ResetRun()
    {
        HasCheckpoint = false;
        CheckpointOrder = int.MinValue;
        CheckpointPosition = Vector3.zero;
        CheckpointReversed = false;
        AccumulatedTime = 0f;
        Deaths = 0;
        HasStartX = false;
        StartX = 0f;
    }

    // 더 뒤(order가 큰) 체크포인트만 덮어씁니다. 같은 체크포인트를 다시 지나면 무시합니다.
    public static bool SetCheckpoint(int order, Vector3 position, bool reversed)
    {
        if (HasCheckpoint && order <= CheckpointOrder)
        {
            return false;
        }

        HasCheckpoint = true;
        CheckpointOrder = order;
        CheckpointPosition = position;
        CheckpointReversed = reversed;
        return true;
    }

    public static void RecordStartX(float x)
    {
        if (HasStartX)
        {
            return;
        }

        HasStartX = true;
        StartX = x;
    }

    public static void RecordDeath(float timeThisLife)
    {
        Deaths++;
        AccumulatedTime += timeThisLife;
    }
}
