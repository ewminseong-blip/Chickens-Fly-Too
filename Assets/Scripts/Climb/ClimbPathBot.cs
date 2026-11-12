using System.Collections;
using System.Text;
using UnityEngine;

// 자동 점검 봇(개발용): 발판 길을 처음부터 끝까지 '실제 물리로' 뛰어 보며 갈 수 있는지 확인합니다.
//  - 발판 i 위에 세운 뒤, 다음 발판 높이에 맞는 충전량으로 뛰고, 공중에서는 다음 발판 가운데로 WASD를 누름(활공 없음 = 보수적).
//  - 다음 발판에 0.5초 이상 서 있으면 성공. 세 번(충전량을 바꿔) 해도 안 되면 실패로 기록.
// 결과는 Report(문자열)와 콘솔에 남깁니다. 플레이 중에 붙이면 바로 시작합니다.
public class ClimbPathBot : MonoBehaviour
{
    public int from = 0;
    public int to = -1;
    public float timeScale = 6f;
    public bool Done { get; private set; }
    public string Report { get; private set; } = "";
    public int Passed { get; private set; }
    public int Failed { get; private set; }
    public int Current { get; private set; }

    ClimbController player;
    ClimbPathData data;
    ClimbSettings set;
    readonly StringBuilder log = new StringBuilder();

    void Start()
    {
        player = FindAnyObjectByType<ClimbController>();
        data = FindAnyObjectByType<ClimbPathData>();
        set = player.Settings;
        if (ClimbGame.Instance != null) ClimbGame.Instance.enabled = false;   // 체크포인트 복귀 · 저장이 끼어들지 않게
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        Time.timeScale = timeScale;
        int last = to < 0 ? data.tops.Length - 2 : Mathf.Min(to, data.tops.Length - 2);
        for (int i = from; i <= last; i++)
        {
            Current = i;
            bool ok = false;
            float bestCharge = -1f;
            Vector3 a = data.Top(i), b = data.Top(i + 1);
            float dh = b.y - a.y;
            float need = Mathf.InverseLerp(set.minJumpHeight, set.maxJumpHeight, dh + 1.4f);
            float[] tries = { Mathf.Clamp01(need + 0.08f), Mathf.Clamp01(need + 0.25f), Mathf.Clamp01(need + 0.45f), 1f };
            foreach (float c in tries)
            {
                yield return Attempt(i, c);
                if (lastResult) { ok = true; bestCharge = c; break; }
            }
            if (ok)
            {
                Passed++;
            }
            else
            {
                Failed++;
                float gap = new Vector2(b.x - a.x, b.z - a.z).magnitude - data.radii[i] - data.radii[i + 1];
                string line = $"FAIL {i}->{i + 1} sec{data.sections[i + 1]} y={a.y:0.0} dh={dh:0.0} gap={gap:0.0} rB={data.radii[i + 1]:0.0} name={(data.platforms[i + 1] != null ? data.platforms[i + 1].name : "?")} ({lastReason})";
                log.AppendLine(line);
                Debug.LogWarning("[ClimbPathBot] " + line);
            }
        }
        Time.timeScale = 1f;
        Report = $"passed {Passed}, failed {Failed}\n{log}";
        Debug.Log("[ClimbPathBot] " + Report);
        Done = true;
    }

    bool lastResult;
    string lastReason = "";

    IEnumerator Attempt(int i, float charge)
    {
        lastResult = false;
        Vector3 a = data.Top(i);
        Vector3 b = data.Top(i + 1);
        Vector3 dir = b - a; dir.y = 0f;
        player.Teleport(a + Vector3.up * 0.3f, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);
        // 서 있을 때까지
        float t = 0f;
        while (t < 1.5f && !(player.IsGrounded && player.Velocity.sqrMagnitude < 0.5f))
        {
            t += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }
        a = player.transform.position;
        b = data.Top(i + 1);
        dir = b - a; dir.y = 0f;
        player.BotJump(charge, dir, 0.05f);

        float stand = 0f;
        t = 0f;
        float lowest = Mathf.Min(a.y, b.y) - 6f;
        while (t < 7f)
        {
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
            b = data.Top(i + 1);
            Vector3 p = player.transform.position;
            Vector3 to = b - p; to.y = 0f;
            float r = data.radii[i + 1];
            // 공중: 다음 발판 가운데 쪽으로 WASD(가까우면 손을 뗌) / 땅: 다음 발판 위면 가만히
            bool airborne = !player.IsGrounded;
            if (airborne || to.magnitude > r * 0.5f)
            {
                if (to.magnitude > Mathf.Min(0.6f, r * 0.3f)) player.BotSteer(to.normalized);
            }
            bool onB = player.IsGrounded && to.magnitude < r + 0.6f && Mathf.Abs(p.y - b.y) < 1.2f;
            stand = onB ? stand + Time.fixedDeltaTime : 0f;
            if (stand >= 0.5f)
            {
                lastResult = true;
                yield break;
            }
            if (p.y < lowest)
            {
                lastReason = $"fell (charge {charge:0.00})";
                yield break;
            }
            // 다른 곳(이전 발판 등)에 서서 멈춤
            if (player.IsGrounded && t > 1f && !onB && player.Velocity.sqrMagnitude < 0.2f)
            {
                lastReason = $"stopped short at {p.y:0.0} (charge {charge:0.00})";
                yield break;
            }
        }
        lastReason = $"timeout (charge {charge:0.00})";
    }
}
