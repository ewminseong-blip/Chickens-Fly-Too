using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

// 발판 길 자동 점검(개발용, 플레이 중에 부름): 물리를 직접 한 칸씩 돌려(Physics.Simulate) 빠르게 확인합니다.
//  - 발판 i에 세운 뒤 다음 발판 높이에 맞는 충전량으로 뛰고, 공중에서는 다음 발판 가운데로 WASD(활공 없음 = 보수적).
//  - 다음 발판에 0.5초 서 있으면 성공. 충전량을 바꿔 4번 해도 안 되면 실패.
//  - 시간이 멈춘 채 물리만 돌리므로 움직이는 발판 · 장애물 박자는 반영되지 않음(길 모양만 검사).
public static class ClimbPathCheck
{
    const float Dt = 0.02f;

    public static string Run(int from, int to)
    {
        ClimbController player = Object.FindAnyObjectByType<ClimbController>();
        ClimbPathData data = Object.FindAnyObjectByType<ClimbPathData>();
        if (player == null || data == null) return "no player / path data (play mode?)";
        ClimbSettings set = player.Settings;
        MethodInfo fixedUpdate = typeof(ClimbController).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);

        SimulationMode mode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        movers = Object.FindObjectsByType<ClimbMovingPlatform>();
        simTime = Time.timeSinceLevelLoad;
        if (ClimbGame.Instance != null) ClimbGame.Instance.enabled = false;

        StringBuilder sb = new StringBuilder();
        int pass = 0, fail = 0;
        int last = Mathf.Min(to, data.tops.Length - 2);
        try
        {
            for (int i = from; i <= last; i++)
            {
                Vector3 a = data.Top(i), b = data.Top(i + 1);
                float dh = b.y - a.y;
                float need = Mathf.InverseLerp(set.minJumpHeight, set.maxJumpHeight, dh + 1.4f);
                float[] tries = { Mathf.Clamp01(need + 0.08f), Mathf.Clamp01(need + 0.25f), Mathf.Clamp01(need + 0.45f), 1f };
                string reason = "";
                bool ok = false;
                List<string> reasons = new List<string>();
                foreach (float c in tries)
                {
                    if (Attempt(player, data, set, fixedUpdate, i, c, out reason)) { ok = true; break; }
                    reasons.Add(reason);
                }
                reason = string.Join(" | ", reasons);
                if (ok) { pass++; continue; }
                fail++;
                float gap = new Vector2(b.x - a.x, b.z - a.z).magnitude - data.radii[i] - data.radii[i + 1];
                sb.AppendLine($"FAIL {i}->{i + 1} sec{data.sections[i + 1]} y={a.y:0.0} dh={dh:0.0} gap={gap:0.0} rB={data.radii[i + 1]:0.0} {(data.platforms[i + 1] != null ? data.platforms[i + 1].name : "?")} : {reason}");
            }
        }
        finally
        {
            Physics.simulationMode = mode;
            ClimbMovingPlatform.TimeOverride = null;
        }
        return $"{from}..{last}: passed {pass}, failed {fail}\n{sb}";
    }

    static ClimbMovingPlatform[] movers = new ClimbMovingPlatform[0];
    static float simTime;

    // 물리 한 칸: 시간을 흘려 움직이는 발판 · 장애물도 실제처럼 움직임
    static void Step(ClimbController p, MethodInfo fu)
    {
        simTime += Dt;
        ClimbMovingPlatform.TimeOverride = simTime;
        foreach (ClimbMovingPlatform m in movers) if (m != null) m.Tick();
        fu.Invoke(p, null);
        Physics.Simulate(Dt);
    }

    static bool Attempt(ClimbController p, ClimbPathData data, ClimbSettings set, MethodInfo fu, int i, float charge, out string reason)
    {
        Vector3 a = data.Top(i);
        Vector3 b = data.Top(i + 1);
        Vector3 dir = b - a; dir.y = 0f;
        p.Teleport(a + Vector3.up * 0.3f, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);
        Physics.SyncTransforms();
        for (int k = 0; k < 75; k++)
        {
            Step(p, fu);
            if (k > 5 && p.IsGrounded && p.Velocity.sqrMagnitude < 0.5f) break;
        }
        a = p.transform.position;
        b = data.Top(i + 1);
        dir = b - a; dir.y = 0f;
        float r = data.radii[i + 1];
        // 사람처럼: 다음 발판 바로 아래 가까이에서 뛰면 먼저 위로 솟고(머리 박지 않게) 올라선 뒤 옮겨 감
        bool closeBelow = b.y > a.y + 1f && dir.magnitude - r < 3f;
        p.BotJump(charge, closeBelow ? Vector3.zero : dir, 0.05f);
        typeof(ClimbController).GetProperty("LastHit").SetValue(p, null);

        float stand = 0f;
        float lowest = Mathf.Min(a.y, b.y) - 6f;
        for (int k = 0; k < 350; k++)
        {
            b = data.Top(i + 1);
            Vector3 pos = p.transform.position;
            Vector3 toB = b - pos; toB.y = 0f;
            bool underEdge = pos.y < b.y + 0.4f && toB.magnitude < r + 1.6f && toB.magnitude > r * 0.6f;
            bool rising = p.Velocity.y > 0.5f;
            if (underEdge && rising)
            {
                // 발판 가장자리 바로 아래로 올라가는 중: 옆으로 비켜 서서 솟음(가장자리 밖으로)
                if (toB.magnitude < r + 1.2f) p.BotSteer(-toB.normalized);
            }
            else if (!p.IsGrounded || toB.magnitude > r * 0.5f)
            {
                if (toB.magnitude > Mathf.Min(0.6f, r * 0.3f)) p.BotSteer(toB.normalized);
            }
            Step(p, fu);
            pos = p.transform.position;
            toB = b - pos; toB.y = 0f;
            bool onB = p.IsGrounded && toB.magnitude < r + 0.6f && Mathf.Abs(pos.y - b.y) < 1.8f;   // 기울어진 판은 가장자리 높이가 가운데와 다름
            stand = onB ? stand + Dt : 0f;
            if (stand >= 0.5f) { reason = ""; return true; }
            if (pos.y < lowest) { reason = $"fell (charge {charge:0.00}) lastHit={HitName(p)}"; return false; }
            if (p.IsGrounded && k > 50 && !onB && p.Velocity.sqrMagnitude < 0.2f) { reason = $"stopped at y={pos.y:0.0} on {Below(pos)} (charge {charge:0.00})"; return false; }
        }
        reason = $"timeout (charge {charge:0.00}) lastHit={HitName(p)}";
        return false;
    }

    static string HitName(ClimbController p) => p.LastHit != null ? Path(p.LastHit.transform) : "none";

    static string Below(Vector3 pos)
    {
        return Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.down, out RaycastHit h, 3f, ~(1 << 2), QueryTriggerInteraction.Ignore) ? Path(h.collider.transform) : "nothing";
    }

    static string Path(Transform t)
    {
        string s = t.name;
        if (t.parent != null) s = t.parent.name + "/" + s;
        if (t.parent != null && t.parent.parent != null) s = t.parent.parent.name + "/" + s;
        return s;
    }
}
