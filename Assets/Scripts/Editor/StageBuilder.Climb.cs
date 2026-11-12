using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Can Chicken Fly? 수직 등반 월드 생성기 (핵심: 구간 정의 · 발판 길 · 씬 · 닭 · 카메라)
// 메뉴: Chicken Fly ▸ Build Climb World (Can Chicken Fly?)  →  Assets/Scenes/CanChickenFly.unity
//
// 맵 구조(온리업 · 겟투워크 참고):
//  - 높이 2000m, 12구간. 짚 깔린 닭장 → 천장 구멍 → 평원 언덕 → 숲(거대한 나무) → 절벽 → 설산 → 하늘 → 구름 → 먹구름 → 번개 → 대기권 → 우주 → 행성
//  - 코스 전체가 거의 수직(70~80°): 발판이 '기둥(언덕 · 나무 · 절벽)' 둘레를 나선으로 감으며 올라가고, 기둥 축은 높이 1m당 옆으로 0.25m만 기울어짐(≈76°).
//    → 떨어지면 아래 구간 위로 떨어져 걸리기도 하고, 크게 떨어지면 처음 근처까지 내려감.
//  - 발판 간격은 ClimbSettings의 점프 계산(높이 차별로 갈 수 있는 거리)으로 정해서 '갈 수 있는 길'만 만듦.
//  - 난이도 = 발판 크기 · 높이 차 · 간격 · 기믹. 1 · 2구간 쉬움 → 3구간부터 오름 → 7 · 8구간은 어려워 보이지만 쉬움 → 9구간부터 다시 어려움.
// 파일: StageBuilder.Climb.cs(핵심) · StageBuilder.ClimbArt.cs(발판 · 기둥 · 장식 · 기믹 모양)
public static partial class StageBuilder
{
    const string ClimbScenePath = "Assets/Scenes/CanChickenFly.unity";
    const string ClimbSettingsAssetPath = "Assets/Resources/Climb/ClimbSettings.asset";
    const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

    enum Theme { Coop, Meadow, Forest, Cliffs, Snow, Sky, Clouds, DarkClouds, Thunder, Atmosphere, Space, Planet }

    class Sec
    {
        public int number;
        public string title, hint;
        public Theme theme;
        public float y0, y1;
        public float diff;            // 실제 난이도 1 ~ 10(발판 크기 · 간격 · 기믹)
        public float pathR;           // 기둥 없는 구간: 나선 반지름
        public float ledge = 3f;      // 기둥 있는 구간: 기둥 표면에서 발판 중심까지
        public int turn = 1;          // 나선 방향
        public int checkpoint;        // 0 = 없음, 1 ~ 3
        public string zone;
        public Transform root;
        public Plat start;
        public float D01 => Mathf.Clamp01((diff - 1f) / 9f);
    }

    class Plat
    {
        public Vector3 pos;   // 윗면 가운데
        public float r;       // 윗면 반폭(m)
        public Transform t;
        public Sec sec;
    }

    static ClimbSettings CS;
    static List<Plat> path;
    static readonly Vector3 HillCenter = new Vector3(40f, 0f, 0f);
    static readonly Vector3 LeanDir = new Vector3(1f, 0f, 0.35f).normalized;
    const float Lean = 0.25f;          // 높이 1m당 옆으로(≈ 76°)
    const float CoopHalf = 14f, CoopTop = 30f, SiloR = 2.6f;

    static List<Sec> DefineClimb()
    {
        return new List<Sec>
        {
            new Sec { number = 1, title = "Straw Coop", hint = "Hold SPACE to charge, let go to fly", theme = Theme.Coop, y0 = 0f, y1 = 60f, diff = 2f, pathR = 8.5f, zone = "Meadow" },
            new Sec { number = 2, title = "Green Meadow", hint = "Big falls slide far. Land softly", theme = Theme.Meadow, y0 = 60f, y1 = 180f, diff = 2.8f, ledge = 3.4f, turn = -1, zone = "Meadow" },
            new Sec { number = 3, title = "The Giant Oak", hint = "Mushrooms bounce", theme = Theme.Forest, y0 = 180f, y1 = 360f, diff = 3.5f, ledge = 3f, turn = 1, zone = "Forest" },
            new Sec { number = 4, title = "Windy Cliffs", hint = "Updrafts lift you. Gusts push you", theme = Theme.Cliffs, y0 = 360f, y1 = 540f, diff = 4.3f, ledge = 3f, turn = -1, checkpoint = 1, zone = "Cliffs" },
            new Sec { number = 5, title = "Snowy Peak", hint = "Ice is slippery. Watch the icicles", theme = Theme.Snow, y0 = 540f, y1 = 720f, diff = 5.2f, ledge = 3f, turn = 1, zone = "Mountain" },
            new Sec { number = 6, title = "Open Sky", hint = "Floating islands. Birds knock you off", theme = Theme.Sky, y0 = 720f, y1 = 880f, diff = 6.2f, pathR = 16f, turn = -1, zone = "CloudLayer" },
            new Sec { number = 7, title = "Cloud Steps", hint = "Only bright clouds hold you", theme = Theme.Clouds, y0 = 880f, y1 = 1040f, diff = 4.5f, pathR = 15f, turn = 1, checkpoint = 2, zone = "AboveClouds" },
            new Sec { number = 8, title = "Dark Clouds", hint = "Looks scary. Follow the bright path", theme = Theme.DarkClouds, y0 = 1040f, y1 = 1200f, diff = 5.2f, pathR = 15f, turn = -1, zone = "InsideClouds" },
            new Sec { number = 9, title = "Thunder", hint = "Bright yellow = shock. Wait for the dark", theme = Theme.Thunder, y0 = 1200f, y1 = 1400f, diff = 7.2f, pathR = 15f, turn = 1, zone = "Storm" },
            new Sec { number = 10, title = "Edge of the Sky", hint = "The wind is wild up here", theme = Theme.Atmosphere, y0 = 1400f, y1 = 1600f, diff = 8.2f, pathR = 16f, turn = -1, checkpoint = 3, zone = "Atmosphere" },
            new Sec { number = 11, title = "Space", hint = "Low gravity. Rocks spin", theme = Theme.Space, y0 = 1600f, y1 = 1800f, diff = 9.1f, pathR = 17f, turn = 1, zone = "Space" },
            new Sec { number = 12, title = "Planet Ring", hint = "The golden egg is on top", theme = Theme.Planet, y0 = 1800f, y1 = 2000f, diff = 10f, pathR = 17f, turn = -1, zone = "DeepSpace" },
        };
    }

    // ───────────── 기둥(축) ─────────────

    // 그 높이의 코스 중심: 1구간은 닭장 가운데, 2구간 언덕은 닭장 옆, 3구간부터는 위로 갈수록 LeanDir로 기울어짐
    static Vector3 Axis(float y)
    {
        if (y < 60f) return new Vector3(0f, y, 0f);
        Vector3 c = HillCenter + LeanDir * Mathf.Max(0f, y - 180f) * Lean;
        c.y = y;
        return c;
    }

    // 기둥 반지름(0 = 기둥 없음)
    static float CoreRadius(Sec s, float y)
    {
        switch (s.theme)
        {
            case Theme.Coop: return SiloR;
            case Theme.Meadow: return Mathf.Lerp(24f, 10f, Mathf.InverseLerp(0f, 180f, y));
            case Theme.Forest: return 6.5f + Mathf.Max(0f, 186f - y) * 0.45f;   // 밑동은 넓게 퍼짐
            case Theme.Cliffs: return 9.5f;
            case Theme.Snow: return Mathf.Lerp(9.5f, 7f, Mathf.InverseLerp(540f, 722f, y));
            default: return 0f;
        }
    }

    static float PathRadius(Sec s, float y)
    {
        float core = CoreRadius(s, y);
        return s.theme == Theme.Coop || core <= 0f ? s.pathR : core + s.ledge;
    }

    // ───────────── 메뉴 ─────────────

    [MenuItem("Chicken Fly/Build Climb World (Can Chicken Fly?)")]
    public static void BuildClimbWorld()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureTags();
        EnsureFolder(MatFolder);
        EnsureFolder(FxFolder);
        EnsureFolder(MeshFolder);
        EnsureFolder(TexFolder);
        BuildFxAssets();
        LoadOrCreateFeedbackSettings();
        S = LoadOrCreateSettings();
        CS = LoadOrCreateClimbSettings();
        CleanClimbMeshes();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Random.InitState(20261112);

        List<Sec> secs = DefineClimb();
        Transform world = new GameObject("ClimbWorld").transform;
        foreach (Sec s in secs)
        {
            s.root = new GameObject($"Section{s.number:00}_{s.title.Replace(' ', '_')}").transform;
            s.root.SetParent(world, false);
        }

        // 1) 바닥 · 배경 · 닭장
        Ground(world);
        Coop(secs[0]);

        // 2) 발판 길(구간마다 나선) + 기둥
        path = new List<Plat>();
        Plat start = new Plat { pos = new Vector3(-4f, 0f, -7f), r = 3f, sec = secs[0] };
        path.Add(start);
        secs[0].start = start;
        Plat last = start;
        float theta = Mathf.Atan2(start.pos.z, start.pos.x);
        foreach (Sec s in secs)
        {
            Core(s);
            last = BuildSectionPath(s, last, ref theta);
        }

        // 3) 구간별 장식 · 기믹(발판 길이 정해진 뒤 길을 피해서)
        foreach (Sec s in secs) Dress(s);
        Goal(secs[secs.Count - 1], last);

        // 길 정보(자동 점검 봇 · 개발용)
        ClimbPathData pd = world.gameObject.AddComponent<ClimbPathData>();
        pd.platforms = path.ConvertAll(p => p.t).ToArray();
        pd.tops = path.ConvertAll(p => p.pos).ToArray();
        pd.radii = path.ConvertAll(p => p.r).ToArray();
        pd.sections = path.ConvertAll(p => p.sec != null ? p.sec.number : 0).ToArray();

        // 4) 닭 · 카메라 · 빛 · 분위기 · 게임 관리 · 화면
        GameObject player = BuildClimbPlayer(start.pos + Vector3.up * 0.2f);
        Camera cam = BuildClimbCamera(player.transform);
        Light sun = BuildSun();
        ClimbAtmosphere(world, secs, sun);
        BuildGameObjects(secs, player, cam);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ClimbScenePath);
        AddClimbSceneToBuild();
        AssetDatabase.SaveAssets();
        Debug.Log($"[ClimbBuilder] 완료: 발판 {path.Count}개, 꼭대기 {last.pos.y:0}m, 점프 최대 {CS.maxJumpHeight}m · 충전 {CS.chargeTime}초");
    }

    static ClimbSettings LoadOrCreateClimbSettings()
    {
        ClimbSettings s = AssetDatabase.LoadAssetAtPath<ClimbSettings>(ClimbSettingsAssetPath);
        if (s == null)
        {
            EnsureFolder("Assets/Resources/Climb");
            s = ScriptableObject.CreateInstance<ClimbSettings>();
            AssetDatabase.CreateAsset(s, ClimbSettingsAssetPath);
            AssetDatabase.SaveAssets();
        }
        return s;
    }

    static void CleanClimbMeshes()
    {
        if (!AssetDatabase.IsValidFolder(MeshFolder)) return;
        foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { MeshFolder }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileName(p).StartsWith("Climb_")) AssetDatabase.DeleteAsset(p);
        }
    }

    // 빌드 목록에는 '꺼진 채로' 뒤에 추가만 함: 게임 이름 · 빌드 방식이 정해질 때까지 기존 Chickens Fly Too 빌드를 바꾸지 않음
    static void AddClimbSceneToBuild()
    {
        List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (list.Exists(e => e.path == ClimbScenePath)) return;
        list.Add(new EditorBuildSettingsScene(ClimbScenePath, false));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ───────────── 발판 길 ─────────────

    static Plat BuildSectionPath(Sec s, Plat prev, ref float theta)
    {
        float d = s.D01;
        bool lowGravity = s.theme == Theme.Space;
        float gravityBackup = CS.gravity;
        if (lowGravity) CS.gravity *= SpaceGravity;   // 낮은 중력 구간은 그만큼 더 멀리 · 높이 갈 수 있게 계산

        try
        {
            // 구간 시작 발판(넓은 쉼터 + 이름 표지판 · 체크포인트)
            if (s.theme != Theme.Coop)
            {
                // 이전 발판과 겹치지 않게: 높이는 3.5m 이상 위, 가장자리 사이 2m 이상 떨어진 자리(나선 방향)
                float sy = Mathf.Max(s.y0, prev.pos.y + 3.5f);
                float rStart = 3.6f;
                float ang = AngleToward(s, sy, prev.pos);
                Vector3 target = RingPoint(s, sy, ang);
                Vector2 flatGap = new Vector2(target.x - prev.pos.x, target.z - prev.pos.z);
                if (flatGap.magnitude < prev.r + rStart + 2f && TryAngle(s, prev, sy, prev.r + 2.5f + rStart, rStart, ang, out float a2, out Vector3 p2))
                {
                    ang = a2;
                    target = p2;
                }
                // 최근 발판들과 겹치거나 그 위를 덮지 않을 때까지 나선 방향으로 조금씩 옮김
                // (쉼터는 커서, 바로 앞 발판 위를 덮지 않게 더 넉넉히: 6.5m 위이거나 가장자리 3m 이상 떨어짐)
                bool StartOk(Vector3 t)
                {
                    float hd = new Vector2(t.x - prev.pos.x, t.z - prev.pos.z).magnitude;
                    return Clear(t, rStart, prev) && (t.y - prev.pos.y >= 6.5f || hd >= prev.r + rStart + 3f);
                }
                int tries = 0;
                for (; tries < 120 && !StartOk(target); tries++)
                {
                    ang += s.turn * 0.12f;
                    if (tries % 30 == 29) sy += 2f;   // 한 바퀴 돌아도 자리가 없으면 조금 더 위로
                    target = RingPoint(s, sy, ang);
                }
                if (tries >= 120) Debug.LogWarning($"[ClimbBuilder] {s.title} 시작 쉼터 자리를 깔끔하게 못 찾음({target.y:0}m)");
                prev = Bridge(s, prev, target, rStart, ref theta);
                Plat sp = MakePlat(s, target, rStart, PlatKind.Rest, OutwardAt(s, target));
                s.start = sp;
                theta = ang;
                prev = sp;
                SectionSign(s, sp);
                if (s.checkpoint > 0) CheckpointNest(s, sp);
            }

            int sinceRest = 0;
            int guard = 0;
            bool coop = s.theme == Theme.Coop;
            float endY = coop ? 21f : s.y1 - 4f;
            float capY = coop ? CoopTop - 5.6f : float.MaxValue;   // 천장(30m) 아래 닭 키 + 여유
            while (prev.pos.y < endY && guard++ < 400)
            {
                // 구간 안에서도 뒤로 갈수록 조금 더 어렵게
                float prog = Mathf.InverseLerp(s.y0, s.y1, prev.pos.y);
                float dd = Mathf.Clamp01(d + (prog - 0.5f) * 0.12f);
                bool rest = ++sinceRest >= Mathf.RoundToInt(Mathf.Lerp(6f, 10f, dd));
                if (rest) sinceRest = 0;

                Plat next = Step(s, prev, dd, rest, ref theta, endY, capY);
                if (next == null) break;
                prev = next;
            }

            if (s.theme == Theme.Coop)
            {
                // 닭장 천장 구멍(마지막 실내 발판 바로 위) → 지붕 → 사일로 둘레
                CoopCeiling(s, prev);
                // 지붕 착지 자리: 구멍 바로 옆(사일로 쪽) 지붕 바닥
                Vector3 holeC = new Vector3(Mathf.Clamp(prev.pos.x, -CoopHalf + 3.25f, CoopHalf - 3.25f), CoopTop + 0.3f, Mathf.Clamp(prev.pos.z, -CoopHalf + 3.25f, CoopHalf - 3.25f));
                Vector3 toSilo = new Vector3(-holeC.x, 0f, -holeC.z).normalized;
                Plat roof = new Plat { pos = holeC + toSilo * 4.3f, r = 1.5f, sec = s };
                path.Add(roof);
                prev = roof;
                guard = 0;
                while (prev.pos.y < s.y1 - 4f && guard++ < 200)
                {
                    Plat next = Step(s, prev, d, false, ref theta, s.y1 - 4f, s.y1 - 2f);   // 60m부터는 언덕 축이라 닭장 구간은 58m까지
                    if (next == null) break;
                    prev = next;
                }
            }
        }
        finally
        {
            CS.gravity = gravityBackup;
        }
        return prev;
    }

    const float SpaceGravity = 0.6f;

    static float AngleToward(Sec s, float y, Vector3 p)
    {
        Vector3 c = Axis(y);
        return Mathf.Atan2(p.z - c.z, p.x - c.x);
    }

    static Vector3 RingPoint(Sec s, float y, float ang)
    {
        Vector3 c = Axis(y);
        float r = PathRadius(s, y);
        return new Vector3(c.x + Mathf.Cos(ang) * r, y, c.z + Mathf.Sin(ang) * r);
    }

    static Vector3 OutwardAt(Sec s, Vector3 p)
    {
        Vector3 o = p - Axis(p.y);
        o.y = 0f;
        return o.sqrMagnitude > 0.001f ? o.normalized : Vector3.forward;
    }

    // 이 높이 차(dh)에서 안전하게 갈 수 있는 가장자리 간격(m)
    static float SafeGap(float dh, float d)
    {
        float reach = CS.MaxReach(dh);
        if (reach < 0f) return -1f;
        return reach * Mathf.Lerp(0.5f, 0.82f, d) - 0.6f;
    }

    // 다음 발판 하나: 높이 차 · 크기 · 간격을 난이도로 고르고, 나선 위에서 간격이 맞는 각도를 찾음
    // 난이도별 범위(2026-11-12 "처음이 너무 쉬움, 발판이 크고 낮음" → 처음부터 작고 높게):
    //  발판 반폭 쉬움 1.6~2.4m · 어려움 0.9~1.5m / 높이 차 쉬움 3~6m · 어려움 5.5~10m / 간격 쉬움 1.2~3.2m · 어려움 3~7m
    static Plat Step(Sec s, Plat prev, float d, bool rest, ref float theta, float endY, float capY = float.MaxValue)
    {
        float hMax = CS.maxJumpHeight * (CS.gravity < 23f ? 1f / SpaceGravity : 1f);
        for (int attempt = 0; attempt < 40; attempt++)
        {
            float relax = attempt / 40f;   // 실패하면 점점 쉽게
            float dh;
            if (!rest && Random.value < Mathf.Lerp(0.08f, 0.16f, d))
            {
                dh = Random.Range(-1.5f, 1.5f);   // 옆으로 건너가기
            }
            else
            {
                dh = Random.Range(Mathf.Lerp(3f, 5.5f, d), Mathf.Lerp(6f, 10f, d)) * (CS.gravity < 23f ? 1.25f : 1f);
            }
            dh = Mathf.Min(dh * (1f - relax * 0.5f), hMax * 0.83f);
            if (prev.pos.y + dh > endY + 6f) dh = Mathf.Max(1f, endY + 4f - prev.pos.y);
            if (prev.pos.y + dh > capY) dh = capY - prev.pos.y;   // 닭장 천장 아래 머리 공간

            float r = rest ? Random.Range(2.6f, 3.2f) : Random.Range(Mathf.Lerp(1.6f, 0.9f, d), Mathf.Lerp(2.4f, 1.5f, d));
            r = Mathf.Lerp(r, Mathf.Max(r, 2f), relax);

            float safe = SafeGap(dh, d);
            if (safe < 0.4f) continue;
            float gap = Random.Range(Mathf.Lerp(1.2f, 3f, d), Mathf.Lerp(3.2f, 7f, d));
            gap = Mathf.Min(gap, safe) * (1f - relax * 0.5f);
            gap = Mathf.Max(gap, 0.4f);
            float want = prev.r + gap + r;

            float y = prev.pos.y + dh;
            if (!TryAngle(s, prev, y, want, r, theta, out float ang, out Vector3 pos)) continue;
            if (!Clear(pos, r, prev)) continue;

            theta = ang;
            PlatKind kind = rest ? PlatKind.Rest : PickKind(s, d);
            return MakePlat(s, pos, r, kind, OutwardAt(s, pos));
        }
        Debug.LogWarning($"[ClimbBuilder] {s.title}: {prev.pos.y:0}m에서 다음 발판을 못 찾음");
        return null;
    }

    // 나선 방향으로 각도를 늘려 가며 '가운데 사이 거리 = want'인 자리를 찾음(이분 탐색). 기둥을 가로지르면 안 됨.
    static bool TryAngle(Sec s, Plat prev, float y, float want, float r, float theta, out float ang, out Vector3 pos)
    {
        float lo = 0.01f, hi = 2.2f;
        ang = theta;
        pos = Vector3.zero;
        Vector3 Point(float a) => RingPoint(s, y, theta + s.turn * a);
        float Dist(float a) { Vector3 p = Point(a); return new Vector2(p.x - prev.pos.x, p.z - prev.pos.z).magnitude; }
        if (Dist(hi) < want) hi = 3.1f;
        if (Dist(hi) < want * 0.8f) return false;
        for (int i = 0; i < 30; i++)
        {
            float mid = (lo + hi) * 0.5f;
            if (Dist(mid) < want) lo = mid; else hi = mid;
        }
        float da = (lo + hi) * 0.5f;
        ang = theta + s.turn * da;
        pos = Point(da);

        // 기둥을 가로지르는지(두 발판 사이 직선이 기둥 표면 + 1.2m 안으로 들어가면 안 됨)
        float core = CoreRadius(s, y);
        if (core > 0f && s.theme != Theme.Coop)
        {
            Vector3 c = Axis(y);
            float dist = SegmentPointDistance2D(prev.pos, pos, c);
            if (dist < core + 1.2f) return false;
        }
        return true;
    }

    static float SegmentPointDistance2D(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector2 A = new Vector2(a.x, a.z), B = new Vector2(b.x, b.z), P = new Vector2(p.x, p.z);
        Vector2 ab = B - A;
        float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(P - A, ab) / ab.sqrMagnitude) : 0f;
        return Vector2.Distance(A + ab * t, P);
    }

    // 다른 발판과 겹치지 않고, 바로 아래 발판에서 뛰어오를 때 머리에 걸리지 않는지
    static bool Clear(Vector3 pos, float r, Plat prev)
    {
        int from = Mathf.Max(0, path.Count - 60);
        for (int i = from; i < path.Count; i++)
        {
            Plat o = path[i];
            float hd = new Vector2(o.pos.x - pos.x, o.pos.z - pos.z).magnitude;
            float dy = pos.y - o.pos.y;
            // 겹침 · 머리 걸림: 바로 앞 발판(prev)도 마찬가지(그 위로 내려앉는 길을 덮으면 안 됨)
            if (hd < o.r + r + (o == prev ? 1f : 1.2f) && Mathf.Abs(dy) < 6.5f) return false;
        }
        // 바로 앞 점프(그 전 발판 → prev)가 지나가는 공중 길 위를 덮으면 안 됨(뛰다가 머리를 박음)
        int pi = path.IndexOf(prev);
        if (pi > 0)
        {
            Plat q = path[pi - 1];
            float side = SegmentPointDistance2D(q.pos, prev.pos, pos);
            float low = Mathf.Min(q.pos.y, prev.pos.y) - 1f;
            float high = Mathf.Max(q.pos.y, prev.pos.y) + 6f;
            if (side < r + 2.6f && pos.y > low && pos.y < high) return false;
        }
        return true;
    }

    // 두 지점 사이가 멀면 중간 발판을 놓아 이어 줌(구간이 바뀌며 기둥 위치가 바뀌는 곳)
    static Plat Bridge(Sec s, Plat prev, Vector3 target, float targetR, ref float theta)
    {
        for (int k = 0; k < 12; k++)
        {
            Vector3 flat = new Vector3(target.x - prev.pos.x, 0f, target.z - prev.pos.z);
            float dh = target.y - prev.pos.y;
            float gapNow = flat.magnitude - prev.r - targetR;
            float safe = SafeGap(Mathf.Clamp(dh, -2f, CS.maxJumpHeight * 0.75f), 0.15f);
            if (dh <= CS.maxJumpHeight * 0.7f && gapNow <= Mathf.Max(0.5f, safe)) return prev;

            float stepH = Mathf.Clamp(dh, -2f, CS.maxJumpHeight * 0.55f);
            float stepGap = Mathf.Max(0.5f, SafeGap(stepH, 0.15f) * 0.8f);
            float r = 2.6f;
            float move = Mathf.Min(flat.magnitude - targetR - r - 0.5f, prev.r + stepGap + r);
            if (move < 0.5f) move = 0f;
            Vector3 p = prev.pos + flat.normalized * move + Vector3.up * stepH;
            // 이어 주는 발판도 다른 발판 · 앞 점프 길과 겹치지 않게(겹치면 조금씩 위로)
            for (int up = 0; up < 6 && !Clear(p, r, prev) && p.y - prev.pos.y < CS.maxJumpHeight * 0.7f; up++) p += Vector3.up * 1.2f;
            prev = MakePlat(s, p, r, PlatKind.Rest, flat.sqrMagnitude > 0f ? flat.normalized : Vector3.forward);
        }
        return prev;
    }

    // ───────────── 닭 · 카메라 · 빛 ─────────────

    static GameObject BuildClimbPlayer(Vector3 at)
    {
        GameObject root = new GameObject("Player");
        root.tag = "Player";
        root.layer = 2;   // Ignore Raycast: 카메라가 닭에 막혀 당겨지지 않게
        root.transform.position = at;

        // 기존 닭 모델(Player.prefab의 Model)을 복사해 옴: 앞 = +X인 모델을 루트 앞(+Z)으로 돌림
        GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (src != null)
        {
            GameObject tmp = (GameObject)PrefabUtility.InstantiatePrefab(src);
            PrefabUtility.UnpackPrefabInstance(tmp, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Transform model = tmp.transform.Find("Model");
            if (model != null)
            {
                float scale = model.lossyScale.x;
                model.SetParent(root.transform, false);
                model.localRotation = Quaternion.Euler(0f, -90f, 0f);
                model.localScale = Vector3.one * scale;
                model.localPosition = Vector3.zero;
                // 발이 루트 높이 0에 오게
                Bounds b = new Bounds(root.transform.position, Vector3.zero);
                bool first = true;
                foreach (Renderer rr in model.GetComponentsInChildren<Renderer>())
                {
                    if (first) { b = rr.bounds; first = false; } else b.Encapsulate(rr.bounds);
                }
                model.localPosition = new Vector3(0f, at.y - b.min.y, 0f);
                foreach (Transform t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2;
                Debug.Log($"[ClimbBuilder] 닭 크기: 높이 {b.size.y:0.00}m, 폭 {b.size.x:0.00} × {b.size.z:0.00}m (모델 배율 {scale:0.###})");
            }
            Object.DestroyImmediate(tmp);
        }

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        CapsuleCollider cap = root.AddComponent<CapsuleCollider>();
        // 몸통 폭 1.3m · 머리 꼭대기 약 2.4m(볏 제외)에 맞춘 캡슐. 날개 · 다리 끝은 판정 밖
        cap.radius = 0.62f;
        cap.height = 2.2f;
        cap.center = new Vector3(0f, 1.1f, 0f);
        cap.sharedMaterial = NoFrictionMaterial();

        ClimbController ctrl = root.AddComponent<ClimbController>();
        SetField(ctrl, "settings", CS);
        SetField(ctrl, "radius", cap.radius);
        SetField(ctrl, "height", cap.height);
        root.AddComponent<ClimbChickenAnimator>();
        return root;
    }

    static Camera BuildClimbCamera(Transform player)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        Camera cam = go.AddComponent<Camera>();
        cam.fieldOfView = 62f;
        cam.nearClipPlane = 0.2f;
        cam.farClipPlane = 3000f;
        go.AddComponent<AudioListener>();
        UniversalAdditionalCameraData data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        go.transform.position = player.position + new Vector3(0f, 4f, -8f);
        go.transform.LookAt(player.position + Vector3.up * 1.4f);
        ClimbCamera cc = go.AddComponent<ClimbCamera>();
        SetField(cc, "target", player);
        SetField(player.GetComponent<ClimbController>(), "cameraTransform", go.transform);
        return cam;
    }

    static Light BuildSun()
    {
        GameObject go = new GameObject("Directional Light");
        Light l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.2f;
        l.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        go.AddComponent<UniversalAdditionalLightData>();
        return l;
    }

    // 하늘 · 안개 · 빛 · 후처리: 높이로 구간 사이를 섞음(ZoneAtmosphere useHeight)
    static void ClimbAtmosphere(Transform parent, List<Sec> secs, Light sun)
    {
        EnsureFolder(VolumeFolder);
        GameObject go = new GameObject("ZoneAtmosphere");
        go.transform.SetParent(parent, false);
        ZoneAtmosphere atmo = go.AddComponent<ZoneAtmosphere>();

        Material skyTemplate = AssetDatabase.LoadAssetAtPath<Material>($"{MatFolder}/Sky_Procedural.mat");
        if (skyTemplate == null)
        {
            skyTemplate = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(skyTemplate, $"{MatFolder}/Sky_Procedural.mat");
        }
        RenderSettings.skybox = skyTemplate;
        RenderSettings.sun = sun;

        SerializedObject so = new SerializedObject(atmo);
        so.FindProperty("blend").floatValue = 30f;
        so.FindProperty("useHeight").boolValue = true;
        so.FindProperty("sun").objectReferenceValue = sun;
        so.FindProperty("skyboxTemplate").objectReferenceValue = skyTemplate;
        // 분위기 구역: 닭장 안(따뜻한 빛) → 닭장 지붕 위(평원 하늘) → 구간마다
        List<(string name, float y, Look look)> list = new List<(string, float, Look)>();
        list.Add(("1. Straw Coop (inside)", 0f, CoopInsideLook()));
        list.Add(("1. Straw Coop (roof)", CoopTop + 1f, ClimbLook(secs[0])));
        for (int i = 1; i < secs.Count; i++) list.Add(($"{secs[i].number}. {secs[i].title}", secs[i].y0, ClimbLook(secs[i])));

        SerializedProperty zones = so.FindProperty("zones");
        zones.arraySize = list.Count;
        for (int i = 0; i < list.Count; i++)
        {
            Look l = list[i].look;
            Volume vol = new GameObject($"Volume_{i:00}_{l.name}").AddComponent<Volume>();
            vol.transform.SetParent(go.transform, false);
            vol.isGlobal = true;
            vol.weight = i == 0 ? 1f : 0f;
            vol.sharedProfile = VolumeProfileFor(l);

            SerializedProperty z = zones.GetArrayElementAtIndex(i);
            z.FindPropertyRelative("name").stringValue = list[i].name;
            z.FindPropertyRelative("startX").floatValue = list[i].y;
            z.FindPropertyRelative("skyTint").colorValue = l.skyTint;
            z.FindPropertyRelative("skyGround").colorValue = l.skyGround;
            z.FindPropertyRelative("exposure").floatValue = l.exposure;
            z.FindPropertyRelative("atmosphereThickness").floatValue = l.atmosphere;
            z.FindPropertyRelative("fog").colorValue = l.fog;
            z.FindPropertyRelative("fogStart").floatValue = l.fogStart;
            z.FindPropertyRelative("fogEnd").floatValue = l.fogEnd;
            z.FindPropertyRelative("ambientSky").colorValue = l.ambientSky;
            z.FindPropertyRelative("ambientEquator").colorValue = l.ambientEquator;
            z.FindPropertyRelative("ambientGround").colorValue = l.ambientGround;
            z.FindPropertyRelative("light").colorValue = l.light;
            z.FindPropertyRelative("lightIntensity").floatValue = l.lightIntensity;
            z.FindPropertyRelative("volume").objectReferenceValue = vol;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        Look f = CoopInsideLook();
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = f.fog;
        RenderSettings.fogStartDistance = f.fogStart;
        RenderSettings.fogEndDistance = f.fogEnd;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = f.ambientSky;
        RenderSettings.ambientEquatorColor = f.ambientEquator;
        RenderSettings.ambientGroundColor = f.ambientGround;
    }

    // 닭장 안: 짚 색 따뜻한 빛(햇빛은 벽에 가려 주변광이 주인공)
    static Look CoopInsideLook()
    {
        Look l = LookFor("Meadow");
        l.name = "Climb_CoopInside";
        l.ambientSky = C(1f, 0.86f, 0.62f);
        l.ambientEquator = C(0.92f, 0.74f, 0.5f);
        l.ambientGround = C(0.62f, 0.46f, 0.26f);
        l.fog = C(0.95f, 0.82f, 0.6f);
        l.fogStart = 30f;
        l.fogEnd = 160f;
        l.light = C(1f, 0.9f, 0.75f);
        l.lightIntensity = 0.9f;
        l.saturation = 14f;
        l.vignette = 0.28f;
        return l;
    }

    // 구간 분위기: 기존 게임의 구간 색을 바탕으로, 등반 구간에 맞게 조금씩 바꿈(멀리까지 보이게 안개를 뒤로)
    static Look ClimbLook(Sec s)
    {
        Look l;
        switch (s.zone)
        {
            case "Atmosphere":
                l = LookFor("AboveClouds");
                l.name = "Climb_Atmosphere";
                l.skyTint = C(0.03f, 0.1f, 0.28f); l.exposure = 0.55f; l.atmosphere = 0.45f;
                l.fog = C(0.12f, 0.2f, 0.42f); l.ambientSky = C(0.2f, 0.3f, 0.6f); l.ambientEquator = C(0.25f, 0.35f, 0.6f);
                l.bloom = 1f; l.saturation = 22f;
                break;
            default:
                l = LookFor(s.zone);
                l.name = "Climb_" + l.name;
                break;
        }
        l.fogStart = Mathf.Max(l.fogStart, 90f);
        l.fogEnd = Mathf.Max(l.fogEnd, 420f);
        if (s.theme == Theme.DarkClouds) { l.fogStart = 40f; l.fogEnd = 260f; }
        if (s.theme == Theme.Thunder) { l.fogStart = 45f; l.fogEnd = 280f; }
        return l;
    }

    // ───────────── 게임 관리 · 화면 ─────────────

    static void BuildGameObjects(List<Sec> secs, GameObject player, Camera cam)
    {
        GameObject g = new GameObject("ClimbGame");
        ClimbGame game = g.AddComponent<ClimbGame>();
        g.AddComponent<ClimbHud>();

        SerializedObject so = new SerializedObject(game);
        so.FindProperty("player").objectReferenceValue = player.GetComponent<ClimbController>();
        so.FindProperty("cameraRig").objectReferenceValue = cam.GetComponent<ClimbCamera>();
        so.FindProperty("startPosition").vector3Value = player.transform.position;
        Vector3 first = path.Count > 1 ? path[1].pos - path[0].pos : Vector3.forward;
        so.FindProperty("startYaw").floatValue = Mathf.Atan2(first.x, first.z) * Mathf.Rad2Deg;   // 첫 발판을 보고 시작
        so.FindProperty("goalHeight").floatValue = secs[secs.Count - 1].y1;

        SerializedProperty titles = so.FindProperty("sectionTitles");
        SerializedProperty hints = so.FindProperty("sectionHints");
        SerializedProperty starts = so.FindProperty("sectionStartHeights");
        SerializedProperty spawns = so.FindProperty("sectionSpawns");
        titles.arraySize = hints.arraySize = starts.arraySize = spawns.arraySize = secs.Count;
        for (int i = 0; i < secs.Count; i++)
        {
            titles.GetArrayElementAtIndex(i).stringValue = secs[i].title;
            hints.GetArrayElementAtIndex(i).stringValue = secs[i].hint;
            starts.GetArrayElementAtIndex(i).floatValue = secs[i].y0;
            Vector3 sp = secs[i].start != null ? secs[i].start.pos + Vector3.up * 0.3f : player.transform.position;
            spawns.GetArrayElementAtIndex(i).vector3Value = sp;
        }

        ClimbCheckpoint[] cps = Object.FindObjectsByType<ClimbCheckpoint>();
        System.Array.Sort(cps, (a, b) => a.Index.CompareTo(b.Index));
        SerializedProperty cpArr = so.FindProperty("checkpoints");
        cpArr.arraySize = cps.Length;
        for (int i = 0; i < cps.Length; i++) cpArr.GetArrayElementAtIndex(i).objectReferenceValue = cps[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
