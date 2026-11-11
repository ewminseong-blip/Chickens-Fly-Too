using UnityEngine;

// 코스 길(중심선) 정보: 시즌1처럼 위로 올라가면서 왼쪽 · 오른쪽으로 꺾이는 길입니다.
// StageBuilder가 2m마다 중심점(바닥 높이) · 진행 방향(yaw) · 폭 · 높이를 미리 계산해 넣어 둡니다.
// 다른 스크립트는 여기서 '길을 따라 몇 m 왔는지(progress)'를 물어봅니다
// (분위기 전환 · 스테이지 배너 · 맵 밖 감시 · 번개 박자 · 마우스 방향 기준).
public class CoursePath : MonoBehaviour
{
    [SerializeField] Vector3[] points = new Vector3[0];   // 바닥 중심점
    [SerializeField] float[] yaws = new float[0];         // 진행 방향(°, 로컬 +X가 앞)
    [SerializeField] float[] widths = new float[0];       // 복도 폭(m)
    [SerializeField] float[] heights = new float[0];      // 바닥에서 위 경계까지(m)
    [SerializeField] float step = 2f;

    static CoursePath instance;

    public static CoursePath Get()
    {
        if (instance == null) instance = FindAnyObjectByType<CoursePath>();
        return instance;
    }

    void Awake()
    {
        instance = this;
    }

    public void Configure(Vector3[] centerPoints, float[] yawDegrees, float[] corridorWidths, float[] corridorHeights, float sampleStep)
    {
        points = centerPoints;
        yaws = yawDegrees;
        widths = corridorWidths;
        heights = corridorHeights;
        step = sampleStep;
    }

    public bool IsValid => points != null && points.Length > 1;
    public float Length => IsValid ? (points.Length - 1) * step : 0f;

    void Index(float s, out int i, out float t)
    {
        float f = Mathf.Clamp(s, 0f, Length) / step;
        i = Mathf.Clamp(Mathf.FloorToInt(f), 0, points.Length - 2);
        t = f - i;
    }

    public Vector3 Center(float s)
    {
        if (!IsValid) return Vector3.zero;
        if (s < 0f) return points[0] + Forward(0f) * s;
        if (s > Length) return points[points.Length - 1] + Forward(Length) * (s - Length);
        Index(s, out int i, out float t);
        return Vector3.Lerp(points[i], points[i + 1], t);
    }

    public float Yaw(float s)
    {
        if (!IsValid) return 0f;
        Index(s, out int i, out float t);
        return Mathf.LerpAngle(yaws[i], yaws[i + 1], t);
    }

    public float Width(float s)
    {
        if (!IsValid) return 24f;
        Index(s, out int i, out float t);
        return Mathf.Lerp(widths[i], widths[i + 1], t);
    }

    public float Height(float s)
    {
        if (!IsValid) return 26f;
        Index(s, out int i, out float t);
        return Mathf.Lerp(heights[i], heights[i + 1], t);
    }

    public Vector3 Forward(float s) => Quaternion.Euler(0f, Yaw(s), 0f) * Vector3.right;
    public Vector3 Lateral(float s) => Quaternion.Euler(0f, Yaw(s), 0f) * Vector3.forward;

    // 위치에서 가장 가까운 길 위 거리(m). hint를 쓰면 지난 위치 근처만 찾아 빠름.
    public float Progress(Vector3 p, ref int hint, int window = 40)
    {
        if (!IsValid) return p.x;
        int from = 0, to = points.Length - 2;
        if (hint >= 0)
        {
            from = Mathf.Max(0, hint - window);
            to = Mathf.Min(points.Length - 2, hint + window);
        }

        float best = float.MaxValue;
        float bestS = 0f;
        int bestI = 0;
        for (int i = from; i <= to; i++)
        {
            Vector3 a = points[i], b = points[i + 1];
            Vector2 ab = new Vector2(b.x - a.x, b.z - a.z);
            Vector2 ap = new Vector2(p.x - a.x, p.z - a.z);
            float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            float d = (ap - ab * t).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestS = (i + t) * step;
                bestI = i;
            }
        }

        // 근처에서 못 찾을 만큼 멀리 갔으면(순간이동 · 부활) 전체를 다시 찾습니다.
        if (hint >= 0 && best > 900f)
        {
            int none = -1;
            float s = Progress(p, ref none);
            hint = Mathf.Clamp(Mathf.RoundToInt(s / step), 0, points.Length - 2);
            return s;
        }

        hint = bestI;
        // 시작 전 · 끝 뒤는 앞 방향으로 늘려서 계산
        if (bestI == 0) bestS = Mathf.Min(bestS, Vector3.Dot(p - points[0], Forward(0f)));
        return bestS;
    }

    public float Progress(Vector3 p)
    {
        int none = -1;
        return Progress(p, ref none);
    }

    // 길 기준 좌표: s(앞), lateral(옆, +가 로컬 Z), up(바닥에서 높이)
    public void Local(Vector3 p, float s, out float lateral, out float up)
    {
        Vector3 c = Center(s);
        lateral = Vector3.Dot(p - c, Lateral(s));
        up = p.y - c.y;
    }
}
