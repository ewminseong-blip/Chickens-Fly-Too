using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 스테이지 환경(맵 디자인): 협곡 절벽 · 지형 바닥 · 절벽 위 풀밭과 나무 · 먼 산 · 구름 · 행성 · 후처리.
// 텍스처와 메시는 모두 유니티 안에서 코드로 만들어 Assets/Art/…/Generated에 저장합니다.
// 색은 시즌1 머티리얼 색(초록 바닥 · 단계별 하늘 파랑 · 갈색 바위 · 우주 네온)을 기준으로 합니다.
public static partial class StageBuilder
{
    const string TexFolder = "Assets/Art/Textures/Generated";
    const string MeshFolder = "Assets/Art/Meshes/Generated";
    const string VolumeFolder = "Assets/Settings/ChickenFly/Volumes";

    enum WallStyle { Rock, Cloud, Crystal }

    class Look
    {
        public WallStyle wall = WallStyle.Rock;
        public Color groundA, groundB;          // 바닥 텍스처 두 색
        public Color rockA, rockB;              // 절벽 · 바위
        public Color grassA, grassB;            // 절벽 위 풀밭
        public Color leafA, leafB;              // 나무 잎
        public Color mountain;                  // 먼 산
        public Color skyTop, skyHorizon, skyGround; // 주변광(Trilight)
        public float treesPer100 = 6f;
        public float tuftsPer100 = 60f;
        public int mountains = 14;
        public int outsideClouds = 0;
        public bool planets;
        public bool flowers;
        public float wallHeight = 0.9f;         // 절벽 높이(복도 높이 비율)
        public float bloom = 0.5f, saturation = 18f, postExposure = 0f, vignette = 0.2f, contrast = 8f;
    }

    static Color C(float r, float g, float b) => new Color(r, g, b);

    // 시즌1 색 기준: 땅 초록(0.10,1,0.16)/(0.51,0.60,0.14), 하늘 (0.62,0.91,1)→(0.20,0.72,0.99)→(0.01,0.33,0.51),
    // 바위 갈색 (0.46,0.29,0)/(0.68,0.43,0.02), 잎 올리브 (0.36,0.43,0.04), 우주 네온 분홍 · 보라 · 초록 · 노랑
    static Look LookFor(string theme)
    {
        switch (theme)
        {
            case "Meadow":
                return new Look
                {
                    groundA = C(0.18f, 0.85f, 0.22f), groundB = C(0.45f, 0.78f, 0.18f),
                    rockA = C(0.46f, 0.29f, 0.05f), rockB = C(0.68f, 0.45f, 0.12f),
                    grassA = C(0.12f, 0.9f, 0.2f), grassB = C(0.51f, 0.7f, 0.14f),
                    leafA = C(0.36f, 0.6f, 0.08f), leafB = C(0.2f, 0.75f, 0.15f),
                    mountain = C(0.45f, 0.7f, 0.85f),
                    skyTop = C(0.62f, 0.91f, 1f), skyHorizon = C(0.8f, 0.95f, 1f), skyGround = C(0.3f, 0.55f, 0.2f),
                    flowers = true, treesPer100 = 7f
                };
            case "Forest":
                return new Look
                {
                    groundA = C(0.15f, 0.7f, 0.18f), groundB = C(0.36f, 0.55f, 0.1f),
                    rockA = C(0.39f, 0.24f, 0.04f), rockB = C(0.58f, 0.38f, 0.1f),
                    grassA = C(0.12f, 0.75f, 0.18f), grassB = C(0.36f, 0.55f, 0.08f),
                    leafA = C(0.36f, 0.43f, 0.04f), leafB = C(0.16f, 0.6f, 0.12f),
                    mountain = C(0.4f, 0.65f, 0.85f),
                    skyTop = C(0.55f, 0.88f, 1f), skyHorizon = C(0.78f, 0.94f, 1f), skyGround = C(0.25f, 0.45f, 0.15f),
                    flowers = true, treesPer100 = 12f, tuftsPer100 = 80f
                };
            case "Cliffs":
                return new Look
                {
                    groundA = C(0.51f, 0.6f, 0.14f), groundB = C(0.58f, 0.45f, 0.25f),
                    rockA = C(0.46f, 0.29f, 0.05f), rockB = C(0.62f, 0.5f, 0.35f),
                    grassA = C(0.45f, 0.65f, 0.14f), grassB = C(0.3f, 0.7f, 0.18f),
                    leafA = C(0.36f, 0.43f, 0.04f), leafB = C(0.25f, 0.55f, 0.1f),
                    mountain = C(0.3f, 0.6f, 0.9f),
                    skyTop = C(0.35f, 0.8f, 1f), skyHorizon = C(0.7f, 0.92f, 1f), skyGround = C(0.4f, 0.35f, 0.25f),
                    treesPer100 = 4f, wallHeight = 1.05f
                };
            case "Mountain":
                return new Look
                {
                    groundA = C(0.82f, 0.86f, 0.9f), groundB = C(0.55f, 0.6f, 0.65f),
                    rockA = C(0.42f, 0.4f, 0.42f), rockB = C(0.62f, 0.6f, 0.62f),
                    grassA = C(0.9f, 0.94f, 1f), grassB = C(0.75f, 0.8f, 0.88f),
                    leafA = C(0.2f, 0.45f, 0.3f), leafB = C(0.3f, 0.55f, 0.35f),
                    mountain = C(0.25f, 0.6f, 0.95f),
                    skyTop = C(0.2f, 0.72f, 0.99f), skyHorizon = C(0.6f, 0.88f, 1f), skyGround = C(0.6f, 0.65f, 0.7f),
                    treesPer100 = 2f, tuftsPer100 = 20f, outsideClouds = 18
                };
            case "CloudLayer":
                return new Look
                {
                    wall = WallStyle.Cloud,
                    groundA = C(1f, 1f, 1f), groundB = C(0.8f, 0.9f, 1f),
                    rockA = C(0.9f, 0.95f, 1f), rockB = C(0.75f, 0.85f, 1f),
                    mountain = C(0.2f, 0.72f, 0.99f),
                    skyTop = C(0.2f, 0.72f, 0.99f), skyHorizon = C(0.85f, 0.95f, 1f), skyGround = C(0.9f, 0.95f, 1f),
                    mountains = 0, outsideClouds = 60, tuftsPer100 = 0f, treesPer100 = 0f, bloom = 0.6f
                };
            case "InsideClouds":
                return new Look
                {
                    wall = WallStyle.Cloud,
                    groundA = C(0.85f, 0.9f, 0.98f), groundB = C(0.6f, 0.7f, 0.85f),
                    rockA = C(0.75f, 0.82f, 0.95f), rockB = C(0.55f, 0.65f, 0.85f),
                    mountain = C(0.1f, 0.5f, 0.8f),
                    skyTop = C(0.1f, 0.55f, 0.85f), skyHorizon = C(0.7f, 0.82f, 0.95f), skyGround = C(0.75f, 0.8f, 0.9f),
                    mountains = 0, outsideClouds = 80, tuftsPer100 = 0f, treesPer100 = 0f, postExposure = -0.1f
                };
            case "AboveClouds":
                // 구름 위: 시즌1 '하늘3' 깊은 파랑 + 구름 바다, 노란 별이 하나둘
                return new Look
                {
                    wall = WallStyle.Cloud,
                    groundA = C(1f, 1f, 1f), groundB = C(0.75f, 0.85f, 1f),
                    rockA = C(1f, 0.97f, 0.9f), rockB = C(0.8f, 0.88f, 1f),
                    mountain = C(0.01f, 0.33f, 0.51f),
                    skyTop = C(0.01f, 0.33f, 0.51f), skyHorizon = C(0.45f, 0.75f, 0.95f), skyGround = C(0.9f, 0.92f, 1f),
                    mountains = 0, outsideClouds = 70, tuftsPer100 = 0f, treesPer100 = 0f, bloom = 0.8f
                };
            case "Storm":
                return new Look
                {
                    wall = WallStyle.Cloud,
                    groundA = C(0.35f, 0.4f, 0.5f), groundB = C(0.2f, 0.25f, 0.35f),
                    rockA = C(0.35f, 0.4f, 0.52f), rockB = C(0.22f, 0.26f, 0.38f),
                    mountain = C(0.01f, 0.2f, 0.33f),
                    skyTop = C(0.01f, 0.2f, 0.33f), skyHorizon = C(0.15f, 0.25f, 0.4f), skyGround = C(0.1f, 0.12f, 0.18f),
                    mountains = 0, outsideClouds = 70, tuftsPer100 = 0f, treesPer100 = 0f, bloom = 1.1f, postExposure = -0.2f, vignette = 0.3f
                };
            case "Space":
                return new Look
                {
                    wall = WallStyle.Crystal,
                    groundA = C(0.12f, 0.06f, 0.2f), groundB = C(0.03f, 0.02f, 0.06f),
                    rockA = C(0.2f, 0.15f, 0.3f), rockB = C(0.08f, 0.06f, 0.12f),
                    mountain = C(0.02f, 0.3f, 0.52f),
                    skyTop = C(0.05f, 0.02f, 0.12f), skyHorizon = C(0.1f, 0.05f, 0.2f), skyGround = C(0.02f, 0.01f, 0.05f),
                    mountains = 0, planets = true, tuftsPer100 = 0f, treesPer100 = 0f, bloom = 1.3f, vignette = 0.3f, saturation = 25f
                };
            default: // DeepSpace
                return new Look
                {
                    wall = WallStyle.Crystal,
                    groundA = C(0.08f, 0.03f, 0.12f), groundB = C(0.02f, 0.01f, 0.04f),
                    rockA = C(0.15f, 0.1f, 0.22f), rockB = C(0.05f, 0.03f, 0.08f),
                    mountain = C(1f, 0.55f, 0f),
                    skyTop = C(0.03f, 0.01f, 0.08f), skyHorizon = C(0.08f, 0.03f, 0.15f), skyGround = C(0.01f, 0.01f, 0.03f),
                    mountains = 0, planets = true, tuftsPer100 = 0f, treesPer100 = 0f, bloom = 1.5f, vignette = 0.32f, saturation = 30f
                };
        }
    }

    // ───────────────────────── 환경 전체 ─────────────────────────

    static void BuildEnvironment(Ctx ctx)
    {
        Look look = LookFor(ctx.t.name);
        EnsureFolder(TexFolder);
        EnsureFolder(MeshFolder);

        Transform env = Group(ctx, "Environment");

        // 바닥(지형 메시, 닿으면 죽음)
        Material groundMat = TexMat($"{ctx.t.name}_Ground", look.groundA, look.groundB, 6f, 0f, 0.25f, 0.15f, 1.2f);
        ctx.floorMat = groundMat;
        Mesh ground = GroundMesh(ctx, look);
        GameObject groundGo = MeshObject(env, "Ground", ground, groundMat);
        groundGo.AddComponent<MeshCollider>().sharedMesh = ground;

        // 양옆 · 위 경계: 보이지 않는 막(닿아도 죽지 않음, 설정에서 바꿀 수 있음)
        Boundaries(ctx, env);

        // 앞 · 뒤 바리케이드(시즌1 Barricade처럼 갈색 바위 벽, 닿아도 죽지 않음)
        Material rockMat = TexMat($"{ctx.t.name}_Rock", look.rockA, look.rockB, 4f, 6f, 0.35f, 0.1f, 2f);
        Material barricadeMat = TexMat("Barricade_Rock", C(0.46f, 0.29f, 0f), C(0.68f, 0.43f, 0.02f), 3f, 8f, 0.3f, 0.15f, 2.2f);
        Barricade(ctx, env, -10f, ctx.c.baseY, barricadeMat, "Barricade_Back");
        Barricade(ctx, env, ctx.c.Length + 1f, ctx.c.EndY, barricadeMat, "Barricade_Front");

        // 위쪽 경계 바로 아래: 아래로 미는 바람 띠(단단한 막에 부딪히기 전에 부드럽게 밀어냄)
        TopWindBand(ctx, env);

        // 마지막 안전장치: 어떤 이유로든 맵 밖으로 나가면 다시 시작
        StageBoundsGuard(ctx, env);

        switch (look.wall)
        {
            case WallStyle.Rock:
                foreach (float side in new[] { -1f, 1f })
                {
                    CliffWall(ctx, env, look, side, rockMat);
                    CliffTop(ctx, env, look, side);
                }
                Tufts(ctx, env, look);
                break;
            case WallStyle.Cloud:
                CloudWalls(ctx, env, look);
                break;
            case WallStyle.Crystal:
                CrystalWalls(ctx, env, look, rockMat);
                break;
        }

        Mountains(ctx, env, look);
        OutsideClouds(ctx, env, look);
        if (look.planets) Planets(ctx, env);

        // 장애물 재질: 시즌1 바위 갈색 텍스처(구름 · 우주는 테마색), 움직이는 위험물: 빨강 · 노랑 줄무늬 + 빛
        ctx.obstacleMat = look.wall == WallStyle.Rock
            ? TexMat($"{ctx.t.name}_ObstacleRock", C(0.46f, 0.29f, 0.05f), C(0.68f, 0.43f, 0.02f), 3f, 5f, 0.3f, 0.2f, 2f)
            : look.wall == WallStyle.Cloud
                ? TexMat($"{ctx.t.name}_ObstacleStone", C(0.95f, 0.55f, 0f), C(1f, 0.75f, 0.3f), 3f, 3f, 0.25f, 0.35f, 1.5f)
                : NeonMat("Obstacle_NeonPurple", C(0.45f, 0.2f, 0.99f), 1.2f);
        ctx.hazardMat = StripeMat();

        SetupPostProcessing(ctx, look);
    }

    // 양옆 · 위 경계(보이지 않는 막). 두께 4m, 구간끼리 4m씩 겹쳐 비탈이 꺾이는 곳에 틈이 없게 합니다.
    static void Boundaries(Ctx ctx, Transform env)
    {
        bool deadly = S != null && S.deadlyBoundaries;
        string tag = deadly ? "Untagged" : "Boundary";
        const float thick = 4f;
        const float overlap = 4f;
        Transform g = new GameObject("Boundaries").transform;
        g.SetParent(env, false);

        float x = 0f;
        int i = 0;
        foreach (Vector2 s in ctx.c.segs)
        {
            float y0 = ctx.c.FloorY(x), y1 = ctx.c.FloorY(x + s.x);
            float a = s.y;
            float l = Mathf.Sqrt(s.x * s.x + (y1 - y0) * (y1 - y0));
            Vector3 n = new Vector3(-Mathf.Sin(a * Mathf.Deg2Rad), Mathf.Cos(a * Mathf.Deg2Rad), 0f);
            Vector3 mid = new Vector3(x + s.x / 2f, (y0 + y1) / 2f, 0f);
            foreach (float side in new[] { -1f, 1f })
            {
                // 안쪽 면이 정확히 Z = ±W/2에 오도록
                InvisibleWall(g, $"Boundary_Side_{i}_{(side < 0 ? "L" : "R")}", mid + n * (H / 2f) + new Vector3(0f, 0f, side * (W / 2f + thick / 2f)),
                    new Vector3(l + overlap, H + 12f, thick), a, ctx, tag);
            }
            InvisibleWall(g, $"Boundary_Top_{i}", mid + n * (H + thick / 2f), new Vector3(l + overlap, thick, W + thick * 2f), a, ctx, tag);
            x += s.x;
            i++;
        }

        // 출발 뒤 구간(x -10 ~ 0)
        InvisibleWall(g, "Boundary_Top_Back", new Vector3(-5f, ctx.c.baseY + H + thick / 2f, 0f), new Vector3(12f, thick, W + thick * 2f), 0f, ctx, tag);
        foreach (float side in new[] { -1f, 1f })
        {
            InvisibleWall(g, "Boundary_Side_Back", new Vector3(-5f, ctx.c.baseY + H / 2f, side * (W / 2f + thick / 2f)), new Vector3(12f, H + 12f, thick), 0f, ctx, tag);
        }
    }

    static void InvisibleWall(Transform parent, string name, Vector3 pos, Vector3 scale, float angle, Ctx ctx, string tag)
    {
        GameObject b = Box(parent, name, pos, scale, angle, ctx.triggerMat);
        b.GetComponent<Renderer>().enabled = false;
        b.tag = tag;
    }

    // 시즌1 Barricade처럼 보이는 바위 벽: 큰 판 + 앞쪽에 울퉁불퉁한 바위 몇 개(충돌은 판 하나)
    static void Barricade(Ctx ctx, Transform env, float x, float floorY, Material mat, string name)
    {
        Transform g = new GameObject(name).transform;
        g.SetParent(env, false);
        GameObject wall = Box(g, "Wall", new Vector3(x + (x < 0f ? -2f : 2f), floorY + H * 0.6f, 0f), new Vector3(4f, H * 1.6f, W + 14f), 0f, mat);
        wall.tag = "Boundary";
        for (int k = 0; k < 7; k++)
        {
            GameObject r = MeshObject(g, "BarricadeRock", RockMesh(Random.Range(0, 6)), mat);
            r.transform.position = new Vector3(x + (x < 0f ? -1f : 1f), floorY + Random.Range(1f, H * 1.1f), Random.Range(-W / 2f, W / 2f));
            r.transform.rotation = Random.rotation;
            r.transform.localScale = Vector3.one * Random.Range(4f, 8f);
        }
    }

    static void TopWindBand(Ctx ctx, Transform env)
    {
        Transform g = new GameObject("TopWindBand").transform;
        g.SetParent(env, false);
        float x = 0f;
        int i = 0;
        foreach (Vector2 s in ctx.c.segs)
        {
            float y0 = ctx.c.FloorY(x), y1 = ctx.c.FloorY(x + s.x);
            float a = s.y;
            float l = Mathf.Sqrt(s.x * s.x + (y1 - y0) * (y1 - y0));
            Vector3 n = new Vector3(-Mathf.Sin(a * Mathf.Deg2Rad), Mathf.Cos(a * Mathf.Deg2Rad), 0f);
            Vector3 mid = new Vector3(x + s.x / 2f, (y0 + y1) / 2f, 0f);
            GameObject band = Box(g, $"TopWind_{i}", mid + n * (H - 2f), new Vector3(l + 2f, 4f, W), a, ctx.triggerMat);
            band.GetComponent<Renderer>().enabled = false;
            band.GetComponent<Collider>().isTrigger = true;
            band.tag = "Trigger";
            WindArea wind = band.AddComponent<WindArea>();
            SetField(wind, "acceleration", new Vector3(0f, -32f, 0f));
            x += s.x;
            i++;
        }
    }

    static void StageBoundsGuard(Ctx ctx, Transform env)
    {
        const float step = 2f;
        int count = Mathf.CeilToInt(ctx.c.Length / step) + 1;
        float[] heights = new float[count];
        for (int k = 0; k < count; k++) heights[k] = ctx.c.FloorY(k * step);

        GameObject go = new GameObject("StageBounds");
        go.transform.SetParent(env, false);
        StageBounds bounds = go.AddComponent<StageBounds>();
        bounds.Configure(heights, step, ctx.c.Length, W, H);
        EditorUtility.SetDirty(bounds);
    }

    // ───────────────────────── 메시 ─────────────────────────

    static Mesh GroundMesh(Ctx ctx, Look look)
    {
        float x0 = -10f, x1 = ctx.c.Length + 4f;
        float half = W / 2f + 3f;
        int nx = Mathf.CeilToInt((x1 - x0) / 2f);
        int nz = Mathf.CeilToInt(half * 2f / 1.5f);
        float seed = ctx.def.number * 13.1f;

        return GridMesh($"Stage{ctx.def.number:00}_Ground", nx, nz,
            (i, j) =>
            {
                float x = x0 + (x1 - x0) * i / nx;
                float z = -half + half * 2f * j / nz;
                float edge = Mathf.InverseLerp(W / 2f - 4f, half, Mathf.Abs(z));
                float bump = (Fbm(x * 0.12f + seed, z * 0.12f, 3) - 0.5f) * Mathf.Lerp(0.2f, 1.6f, edge * edge);
                return new Vector3(x, ctx.c.FloorY(Mathf.Max(0f, x)) + bump - 0.05f, z);
            },
            (i, j) => new Vector2((x0 + (x1 - x0) * i / nx) / 8f, (-half + half * 2f * j / nz) / 8f),
            Vector3.up);
    }

    static float WallTop(Ctx ctx, Look look, float x, float side)
    {
        return H * look.wallHeight + (Fbm(x * 0.025f + side * 31f, 4.2f, 3) - 0.5f) * 10f;
    }

    static void CliffWall(Ctx ctx, Transform env, Look look, float side, Material rockMat)
    {
        float x0 = -12f, x1 = ctx.c.Length + 6f;
        int nx = Mathf.CeilToInt((x1 - x0) / 2.2f);
        const int ny = 14;
        float seed = ctx.def.number * 7.7f + side * 50f;

        Mesh m = GridMesh($"Stage{ctx.def.number:00}_Cliff_{(side < 0 ? "L" : "R")}", nx, ny,
            (i, j) =>
            {
                float x = x0 + (x1 - x0) * i / nx;
                float t = j / (float)ny;
                float top = WallTop(ctx, look, x, side);
                float v = -2f + t * (top + 2f);
                // 바깥쪽으로만 울퉁불퉁(경계 안으로 들어오지 않음) + 위로 갈수록 살짝 뒤로 기울어짐
                float outward = Fbm(x * 0.07f + seed, t * 3.5f, 4) * 3.2f + Fbm(x * 0.3f + seed, t * 9f, 2) * 0.9f + t * t * 3f;
                // 지층: 높이에 따라 층이 살짝 튀어나옴
                outward += Mathf.Abs(Mathf.Sin(v * 0.9f + Fbm(x * 0.05f, 1f, 2) * 4f)) * 0.5f;
                return new Vector3(x, ctx.c.FloorY(Mathf.Clamp(x, 0f, ctx.c.Length)) + v, side * (W / 2f + 1.2f + outward));
            },
            (i, j) => new Vector2((x0 + (x1 - x0) * i / nx) / 9f, (j / (float)ny) * (H * look.wallHeight) / 9f),
            new Vector3(0f, 0f, -side));
        MeshObject(env, $"Cliff_{(side < 0 ? "Left" : "Right")}", m, rockMat);
    }

    static void CliffTop(Ctx ctx, Transform env, Look look, float side)
    {
        float x0 = -12f, x1 = ctx.c.Length + 6f;
        int nx = Mathf.CeilToInt((x1 - x0) / 3f);
        const int nz = 12;
        float inner = W / 2f + 4f, outer = W / 2f + 70f;
        Material grass = TexMat($"{ctx.t.name}_CliffGrass", look.grassA, look.grassB, 5f, 0f, 0.2f, 0.12f, 1f);

        Mesh m = GridMesh($"Stage{ctx.def.number:00}_CliffTop_{(side < 0 ? "L" : "R")}", nx, nz,
            (i, j) =>
            {
                float x = x0 + (x1 - x0) * i / nx;
                float u = j / (float)nz;
                float z = side * Mathf.Lerp(inner, outer, u);
                float top = WallTop(ctx, look, x, side);
                float roll = (Fbm(x * 0.05f, u * 4f + side, 3) - 0.5f) * 4f * u;
                return new Vector3(x, ctx.c.FloorY(Mathf.Clamp(x, 0f, ctx.c.Length)) + top - 0.4f + roll, z);
            },
            (i, j) => new Vector2((x0 + (x1 - x0) * i / nx) / 8f, (j / (float)nz) * (outer - inner) / 8f),
            Vector3.up);
        MeshObject(env, $"CliffTop_{(side < 0 ? "Left" : "Right")}", m, grass);

        // 절벽 위 나무
        int trees = Mathf.RoundToInt(look.treesPer100 * ctx.c.Length / 100f);
        Material bark = TexMat("Bark_Generated", C(0.39f, 0.24f, 0f), C(0.68f, 0.43f, 0.02f), 8f, 12f, 0.15f, 0.1f, 1.5f);
        Material leafA = Mat($"{ctx.t.name}_LeafA", look.leafA, 0.15f);
        Material leafB = Mat($"{ctx.t.name}_LeafB", look.leafB, 0.15f);
        Transform tg = new GameObject($"Trees_{(side < 0 ? "L" : "R")}").transform;
        tg.SetParent(env, false);
        for (int k = 0; k < trees; k++)
        {
            float x = Random.Range(-5f, ctx.c.Length);
            float z = side * Random.Range(inner + 3f, inner + 35f);
            float y = ctx.c.FloorY(Mathf.Clamp(x, 0f, ctx.c.Length)) + WallTop(ctx, look, x, side) - 0.6f;
            DecorTree(tg, new Vector3(x, y, z), Random.Range(0.8f, 1.6f), bark, Random.value < 0.5f ? leafA : leafB);
        }

        // 절벽 위 돌
        Transform rg = new GameObject($"Rocks_{(side < 0 ? "L" : "R")}").transform;
        rg.SetParent(env, false);
        Material rockMat = TexMat($"{ctx.t.name}_Rock", look.rockA, look.rockB, 4f, 6f, 0.35f, 0.1f, 2f);
        for (int k = 0; k < trees / 2 + 4; k++)
        {
            float x = Random.Range(-5f, ctx.c.Length);
            float z = side * Random.Range(inner + 1f, inner + 30f);
            float y = ctx.c.FloorY(Mathf.Clamp(x, 0f, ctx.c.Length)) + WallTop(ctx, look, x, side) - 0.5f;
            GameObject r = MeshObject(rg, "Rock", RockMesh(Random.Range(0, 6)), rockMat);
            r.transform.position = new Vector3(x, y, z);
            r.transform.localScale = new Vector3(Random.Range(1.5f, 4f), Random.Range(1f, 2.5f), Random.Range(1.5f, 4f));
            r.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
    }

    static void DecorTree(Transform parent, Vector3 pos, float scale, Material bark, Material leaf)
    {
        GameObject tree = new GameObject("DecorTree");
        tree.transform.SetParent(parent, false);
        tree.transform.position = pos;
        tree.transform.localScale = Vector3.one * scale;
        tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject trunk = RemoveCollider(GameObject.CreatePrimitive(PrimitiveType.Cylinder));
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        trunk.transform.localScale = new Vector3(0.9f, 2.5f, 0.9f);
        trunk.GetComponent<Renderer>().sharedMaterial = bark;

        int blobs = Random.Range(2, 4);
        for (int b = 0; b < blobs; b++)
        {
            GameObject crown = MeshObject(tree.transform, "Crown", RockMesh(Random.Range(0, 6)), leaf);
            crown.transform.localPosition = new Vector3(Random.Range(-0.8f, 0.8f), 5.5f + b * 1.6f, Random.Range(-0.8f, 0.8f));
            float s = Random.Range(3.2f, 4.6f) - b * 0.6f;
            crown.transform.localScale = new Vector3(s, s * 0.85f, s);
        }
    }

    // 바닥 가장자리 풀포기(합친 메시 하나, 충돌 없음)
    static void Tufts(Ctx ctx, Transform env, Look look)
    {
        int count = Mathf.RoundToInt(look.tuftsPer100 * ctx.c.Length / 100f);
        if (count <= 0) return;

        List<CombineInstance> parts = new List<CombineInstance>();
        Mesh quad = CrossQuadMesh();
        for (int k = 0; k < count; k++)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            float x = Random.Range(2f, ctx.c.Length - 2f);
            float z = side * Random.Range(W / 2f - 3.5f, W / 2f + 1f);
            Vector3 p = new Vector3(x, ctx.c.FloorY(x) - 0.05f, z);
            float s = Random.Range(0.7f, 1.4f);
            parts.Add(new CombineInstance
            {
                mesh = quad,
                transform = Matrix4x4.TRS(p, Quaternion.Euler(0f, Random.Range(0f, 180f), 0f), new Vector3(s, s * Random.Range(0.8f, 1.3f), s))
            });
        }

        Mesh combined = new Mesh { name = $"Stage{ctx.def.number:00}_Tufts", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        combined.CombineMeshes(parts.ToArray(), true, true);
        combined = SaveMesh(combined);
        MeshObject(env, "GrassTufts", combined, GrassTuftMat(look));
        Object.DestroyImmediate(quad);

        if (look.flowers)
        {
            Material[] petals =
            {
                Mat("Flower_Yellow", C(0.94f, 0.9f, 0f), 0.3f, C(0.3f, 0.28f, 0f)),
                Mat("Flower_Pink", C(0.99f, 0.2f, 0.46f), 0.3f, C(0.3f, 0.05f, 0.12f)),
                Mat("Flower_White", C(1f, 1f, 1f), 0.3f)
            };
            Transform fg = new GameObject("Flowers").transform;
            fg.SetParent(env, false);
            for (int k = 0; k < count / 4; k++)
            {
                float side = Random.value < 0.5f ? -1f : 1f;
                float x = Random.Range(2f, ctx.c.Length - 2f);
                float z = side * Random.Range(W / 2f - 3f, W / 2f + 0.5f);
                GameObject f = RemoveCollider(GameObject.CreatePrimitive(PrimitiveType.Sphere));
                f.name = "Flower";
                f.transform.SetParent(fg, false);
                f.transform.position = new Vector3(x, ctx.c.FloorY(x) + 0.45f, z);
                f.transform.localScale = Vector3.one * Random.Range(0.3f, 0.5f);
                f.GetComponent<Renderer>().sharedMaterial = petals[Random.Range(0, petals.Length)];
            }
        }
    }

    // 구름 벽: 양옆을 큰 구름 덩어리로 채움(충돌 없음, 경계는 보이지 않는 막)
    static void CloudWalls(Ctx ctx, Transform env, Look look)
    {
        if (ctx.cloudPrefab == null) return;
        Transform g = new GameObject("CloudWalls").transform;
        g.SetParent(env, false);
        foreach (float side in new[] { -1f, 1f })
        {
            for (float x = -8f; x < ctx.c.Length + 6f; x += 6.5f)
            {
                for (float h = -2f; h < H + 4f; h += 7f)
                {
                    Vector3 p = ctx.c.P(Mathf.Clamp(x, 0f, ctx.c.Length), h, side * (W / 2f + Random.Range(4.5f, 8f)));
                    p.x = x + Random.Range(-1.5f, 1.5f);
                    SpawnCloud(g, p, Random.Range(6.5f, 10f), false);
                }
            }
        }
    }

    // 우주 벽: 떠다니는 소행성 + 시즌1 네온색 수정(충돌 없음)
    static void CrystalWalls(Ctx ctx, Transform env, Look look, Material rockMat)
    {
        Transform g = new GameObject("AsteroidWalls").transform;
        g.SetParent(env, false);
        Material[] neon =
        {
            NeonMat("Crystal_NeonPink", C(0.99f, 0.2f, 0.46f), 2.2f),
            NeonMat("Crystal_NeonPurple", C(0.45f, 0.2f, 0.99f), 2.2f),
            NeonMat("Crystal_NeonGreen", C(0.48f, 0.99f, 0.2f), 2.2f),
            NeonMat("Crystal_StarYellow", C(0.94f, 0.9f, 0f), 2.2f)
        };
        foreach (float side in new[] { -1f, 1f })
        {
            for (float x = -8f; x < ctx.c.Length + 6f; x += 5f)
            {
                for (float h = 0f; h < H + 6f; h += 8f)
                {
                    Vector3 p = ctx.c.P(Mathf.Clamp(x, 0f, ctx.c.Length), h + Random.Range(-2f, 2f), side * (W / 2f + Random.Range(3.5f, 9f)));
                    p.x = x + Random.Range(-2f, 2f);
                    GameObject r = MeshObject(g, "Asteroid", RockMesh(Random.Range(0, 6)), rockMat);
                    r.transform.position = p;
                    r.transform.rotation = Random.rotation;
                    r.transform.localScale = Vector3.one * Random.Range(2.5f, 6f);
                    Spinner sp = r.AddComponent<Spinner>();
                    SetField(sp, "xAngle", Random.Range(-8f, 8f));
                    SetField(sp, "yAngle", Random.Range(-12f, 12f));
                    SetField(sp, "zAngle", Random.Range(-8f, 8f));

                    if (Random.value < 0.35f)
                    {
                        GameObject c = MeshObject(g, "Crystal", CrystalMesh(), neon[Random.Range(0, neon.Length)]);
                        c.transform.position = p + new Vector3(0f, Random.Range(2f, 4f), -side * 1.5f);
                        c.transform.rotation = Quaternion.Euler(Random.Range(-25f, 25f), Random.Range(0f, 360f), Random.Range(-25f, 25f));
                        c.transform.localScale = new Vector3(1f, Random.Range(2.5f, 5f), 1f) * Random.Range(0.6f, 1.2f);
                    }
                }
            }
        }
    }

    static void Mountains(Ctx ctx, Transform env, Look look)
    {
        if (look.mountains <= 0) return;
        Transform g = new GameObject("Mountains").transform;
        g.SetParent(env, false);
        Material mat = Mat($"{ctx.t.name}_Mountain", look.mountain, 0.05f);
        Material snow = Mat("Mountain_Snow", C(0.95f, 0.98f, 1f), 0.2f);
        for (int k = 0; k < look.mountains; k++)
        {
            float side = k % 2 == 0 ? -1f : 1f;
            float x = Random.Range(-80f, ctx.c.Length + 120f);
            float z = side * Random.Range(110f, 260f);
            float baseY = ctx.c.FloorY(Mathf.Clamp(x, 0f, ctx.c.Length)) - 60f;
            GameObject m = MeshObject(g, "Mountain", RockMesh(Random.Range(0, 6)), mat);
            float w = Random.Range(60f, 140f);
            float h = Random.Range(140f, 300f);
            m.transform.position = new Vector3(x, baseY + h * 0.35f, z);
            m.transform.localScale = new Vector3(w, h, w);
            m.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            if (h > 220f)
            {
                GameObject cap = MeshObject(m.transform, "SnowCap", RockMesh(Random.Range(0, 6)), snow);
                cap.transform.localPosition = new Vector3(0f, 0.38f, 0f);
                cap.transform.localScale = new Vector3(0.45f, 0.3f, 0.45f);
            }
        }
    }

    static void OutsideClouds(Ctx ctx, Transform env, Look look)
    {
        if (look.outsideClouds <= 0 || ctx.cloudPrefab == null) return;
        Transform g = new GameObject("OutsideClouds").transform;
        g.SetParent(env, false);
        for (int k = 0; k < look.outsideClouds; k++)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            float x = Random.Range(-60f, ctx.c.Length + 80f);
            float z = side * Random.Range(W / 2f + 15f, 160f);
            float y = ctx.c.FloorY(Mathf.Clamp(x, 0f, ctx.c.Length)) + Random.Range(-25f, 10f);
            SpawnCloud(g, new Vector3(x, y, z), Random.Range(10f, 26f), false);
        }
    }

    static void Planets(Ctx ctx, Transform env)
    {
        Transform g = new GameObject("Planets").transform;
        g.SetParent(env, false);
        // 시즌1 행성 색: 파랑(0.02,0.30,0.52) · 주황(1,0.55,0), 고리 주황
        (Color col, float size, Vector3 off, bool ring)[] planets =
        {
            (C(0.02f, 0.3f, 0.52f), 90f, new Vector3(0.35f, 120f, 260f), false),
            (C(1f, 0.55f, 0f), 60f, new Vector3(0.7f, 70f, -220f), true),
            (C(0.45f, 0.2f, 0.99f), 35f, new Vector3(0.15f, 40f, -160f), false)
        };
        foreach (var p in planets)
        {
            Material m = Mat($"Planet_{p.col.r:0.00}_{p.col.g:0.00}", p.col, 0.4f, p.col * 0.35f);
            GameObject s = RemoveCollider(GameObject.CreatePrimitive(PrimitiveType.Sphere));
            s.name = "Planet";
            s.transform.SetParent(g, false);
            float x = ctx.c.Length * p.off.x;
            s.transform.position = new Vector3(x, ctx.c.FloorY(x) + p.off.y, p.off.z);
            s.transform.localScale = Vector3.one * p.size;
            s.GetComponent<Renderer>().sharedMaterial = m;
            if (p.ring)
            {
                GameObject ring = RemoveCollider(GameObject.CreatePrimitive(PrimitiveType.Cylinder));
                ring.name = "Ring";
                ring.transform.SetParent(s.transform, false);
                ring.transform.localScale = new Vector3(1.9f, 0.01f, 1.9f);
                ring.transform.localRotation = Quaternion.Euler(18f, 0f, 12f);
                ring.GetComponent<Renderer>().sharedMaterial = Mat("Planet_Ring", C(1f, 0.55f, 0f), 0.5f, C(0.5f, 0.25f, 0f));
            }
        }
    }

    static void SpawnCloud(Transform parent, Vector3 pos, float scale, bool keepColliders)
    {
        GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(ctxCloudPrefab, parent.gameObject.scene);
        c.transform.SetParent(parent, false);
        c.transform.position = pos;
        c.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        c.transform.localScale = Vector3.one * scale;
        if (!keepColliders)
        {
            foreach (Collider col in c.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        }
    }

    static GameObject ctxCloudPrefab => AssetDatabase.LoadAssetAtPath<GameObject>(CloudPrefabPath);

    // ───────────────────────── 메시 도구 ─────────────────────────

    static Mesh GridMesh(string name, int nx, int ny, System.Func<int, int, Vector3> pos, System.Func<int, int, Vector2> uv, Vector3 facing)
    {
        int w = nx + 1;
        Vector3[] v = new Vector3[w * (ny + 1)];
        Vector2[] u = new Vector2[v.Length];
        for (int j = 0; j <= ny; j++)
        {
            for (int i = 0; i <= nx; i++)
            {
                v[j * w + i] = pos(i, j);
                u[j * w + i] = uv(i, j);
            }
        }

        int[] t = new int[nx * ny * 6];
        int k = 0;
        for (int j = 0; j < ny; j++)
        {
            for (int i = 0; i < nx; i++)
            {
                int a = j * w + i, b = a + 1, c = a + w, d = c + 1;
                t[k++] = a; t[k++] = c; t[k++] = b;
                t[k++] = b; t[k++] = c; t[k++] = d;
            }
        }

        // 면이 원하는 쪽(facing)을 보도록 감기 방향을 맞춥니다.
        Vector3 n = Vector3.Cross(v[t[1]] - v[t[0]], v[t[2]] - v[t[0]]);
        if (Vector3.Dot(n, facing) < 0f)
        {
            for (int q = 0; q < t.Length; q += 3)
            {
                (t[q + 1], t[q + 2]) = (t[q + 2], t[q + 1]);
            }
        }

        Mesh m = new Mesh { name = name };
        if (v.Length > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.vertices = v;
        m.uv = u;
        m.triangles = t;
        m.RecalculateNormals();
        m.RecalculateTangents();
        m.RecalculateBounds();
        return SaveMesh(m);
    }

    static Mesh SaveMesh(Mesh m)
    {
        string path = $"{MeshFolder}/{m.name}.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            existing.Clear();
            EditorUtility.CopySerialized(m, existing);
            Object.DestroyImmediate(m);
            EditorUtility.SetDirty(existing);
            return existing;
        }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // 울퉁불퉁한 바위 · 나무 수관 · 산에 쓰는 메시(변형 6가지, 한 번 만들어 재사용)
    static Mesh RockMesh(int variant)
    {
        string path = $"{MeshFolder}/Rock_{variant}.asset";
        Mesh cached = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (cached != null) return cached;

        Mesh m = Icosphere(2);
        Vector3[] v = m.vertices;
        float seed = variant * 19.7f;
        for (int i = 0; i < v.Length; i++)
        {
            Vector3 d = v[i].normalized;
            float n = Fbm(d.x * 1.8f + seed, d.y * 1.8f + d.z * 1.3f, 3);
            v[i] = d * (0.5f + (n - 0.5f) * 0.35f);
            v[i].y *= 0.85f;
        }
        m.vertices = v;
        m.RecalculateNormals();
        m.RecalculateBounds();
        m.name = $"Rock_{variant}";
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static Mesh CrystalMesh()
    {
        string path = $"{MeshFolder}/Crystal.asset";
        Mesh cached = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (cached != null) return cached;

        // 위아래로 뾰족한 육각 수정
        List<Vector3> v = new List<Vector3>();
        List<int> t = new List<int>();
        Vector3 top = new Vector3(0f, 0.5f, 0f), bottom = new Vector3(0f, -0.5f, 0f);
        for (int i = 0; i < 6; i++)
        {
            float a0 = i * Mathf.PI / 3f, a1 = (i + 1) * Mathf.PI / 3f;
            Vector3 p0 = new Vector3(Mathf.Cos(a0) * 0.25f, 0.15f, Mathf.Sin(a0) * 0.25f);
            Vector3 p1 = new Vector3(Mathf.Cos(a1) * 0.25f, 0.15f, Mathf.Sin(a1) * 0.25f);
            Vector3 q0 = new Vector3(p0.x, -0.15f, p0.z), q1 = new Vector3(p1.x, -0.15f, p1.z);
            AddTri(v, t, top, p1, p0);
            AddTri(v, t, p0, p1, q1); AddTri(v, t, p0, q1, q0);
            AddTri(v, t, bottom, q0, q1);
        }
        Mesh m = new Mesh { name = "Crystal" };
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void AddTri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c);
        t.Add(i); t.Add(i + 1); t.Add(i + 2);
    }

    static Mesh CrossQuadMesh()
    {
        // 풀포기: 서로 교차한 세로 사각형 2장(양면 머티리얼)
        Mesh m = new Mesh { name = "CrossQuad" };
        Vector3[] v =
        {
            new Vector3(-0.6f, 0f, 0f), new Vector3(0.6f, 0f, 0f), new Vector3(-0.6f, 1.2f, 0f), new Vector3(0.6f, 1.2f, 0f),
            new Vector3(0f, 0f, -0.6f), new Vector3(0f, 0f, 0.6f), new Vector3(0f, 1.2f, -0.6f), new Vector3(0f, 1.2f, 0.6f)
        };
        Vector2[] u = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        int[] t = { 0, 2, 1, 1, 2, 3, 4, 6, 5, 5, 6, 7 };
        m.vertices = v; m.uv = u; m.triangles = t;
        m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        m.RecalculateBounds();
        return m;
    }

    static Mesh Icosphere(int subdivisions)
    {
        float tt = (1f + Mathf.Sqrt(5f)) / 2f;
        List<Vector3> v = new List<Vector3>
        {
            new Vector3(-1, tt, 0), new Vector3(1, tt, 0), new Vector3(-1, -tt, 0), new Vector3(1, -tt, 0),
            new Vector3(0, -1, tt), new Vector3(0, 1, tt), new Vector3(0, -1, -tt), new Vector3(0, 1, -tt),
            new Vector3(tt, 0, -1), new Vector3(tt, 0, 1), new Vector3(-tt, 0, -1), new Vector3(-tt, 0, 1)
        };
        List<int> f = new List<int>
        {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
        };
        for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized * 0.5f;

        Dictionary<long, int> cache = new Dictionary<long, int>();
        int Mid(int a, int b)
        {
            long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
            if (cache.TryGetValue(key, out int idx)) return idx;
            v.Add(((v[a] + v[b]) * 0.5f).normalized * 0.5f);
            cache[key] = v.Count - 1;
            return v.Count - 1;
        }

        for (int s = 0; s < subdivisions; s++)
        {
            List<int> nf = new List<int>();
            for (int i = 0; i < f.Count; i += 3)
            {
                int a = Mid(f[i], f[i + 1]), b = Mid(f[i + 1], f[i + 2]), c = Mid(f[i + 2], f[i]);
                nf.AddRange(new[] { f[i], a, c, f[i + 1], b, a, f[i + 2], c, b, a, b, c });
            }
            f = nf;
        }

        Mesh m = new Mesh();
        m.SetVertices(v);
        m.SetTriangles(f, 0);
        // 바깥을 보도록(노멀 확인)
        Vector3 n = Vector3.Cross(v[f[1]] - v[f[0]], v[f[2]] - v[f[0]]);
        if (Vector3.Dot(n, v[f[0]]) < 0f)
        {
            for (int q = 0; q < f.Count; q += 3) (f[q + 1], f[q + 2]) = (f[q + 2], f[q + 1]);
            m.SetTriangles(f, 0);
        }
        Vector2[] uv = new Vector2[v.Count];
        for (int i = 0; i < v.Count; i++) uv[i] = new Vector2(Mathf.Atan2(v[i].z, v[i].x) / (Mathf.PI * 2f) + 0.5f, v[i].y + 0.5f);
        m.uv = uv;
        m.RecalculateNormals();
        return m;
    }

    static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material mat)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        g.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return g;
    }

    // ───────────────────────── 텍스처 · 머티리얼 ─────────────────────────

    static float Fbm(float x, float y, int octaves)
    {
        float sum = 0f, amp = 0.5f, freq = 1f, norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += amp * Mathf.PerlinNoise(x * freq + 101.3f + i * 17.1f, y * freq + 57.7f + i * 9.3f);
            norm += amp;
            amp *= 0.5f;
            freq *= 2f;
        }
        return sum / norm;
    }

    // 이어 붙여도 이음새가 없는 노이즈(0~1 좌표)
    static float SeamlessFbm(float u, float v, float scale, int octaves)
    {
        float a = Fbm(u * scale, v * scale, octaves);
        float b = Fbm((u - 1f) * scale, v * scale, octaves);
        float c = Fbm(u * scale, (v - 1f) * scale, octaves);
        float d = Fbm((u - 1f) * scale, (v - 1f) * scale, octaves);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    // 텍스처(색 + 노멀맵)를 만들어 저장하고 머티리얼을 돌려줍니다.
    // strata: 가로 지층 무늬 세기(바위), speckle: 잔점 세기, smoothness, normalStrength
    static Material TexMat(string name, Color a, Color b, float scale, float strata, float speckle, float smoothness, float normalStrength)
    {
        const int size = 256;
        string albedoPath = $"{TexFolder}/{name}_Albedo.png";
        string normalPath = $"{TexFolder}/{name}_Normal.png";

        if (!File.Exists(albedoPath) || !File.Exists(normalPath))
        {
            float[] h = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    float n = SeamlessFbm(u, v, scale, 5);
                    if (strata > 0f)
                    {
                        float layer = Mathf.Sin((v * strata + n * 1.5f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                        n = Mathf.Lerp(n, layer, 0.35f);
                    }
                    h[y * size + x] = n;
                }
            }

            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Texture2D normal = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = h[y * size + x];
                    float sp = SeamlessFbm(x / (float)size, y / (float)size, scale * 8f, 2);
                    Color col = Color.Lerp(a, b, Mathf.SmoothStep(0.2f, 0.8f, n));
                    col *= 1f + (sp - 0.5f) * speckle * 2f;
                    col.a = 1f;
                    albedo.SetPixel(x, y, col);

                    float hl = h[y * size + (x + size - 1) % size], hr = h[y * size + (x + 1) % size];
                    float hd = h[((y + size - 1) % size) * size + x], hu = h[((y + 1) % size) * size + x];
                    Vector3 nn = new Vector3((hl - hr) * normalStrength * 4f, (hd - hu) * normalStrength * 4f, 1f).normalized;
                    normal.SetPixel(x, y, new Color(nn.x * 0.5f + 0.5f, nn.y * 0.5f + 0.5f, nn.z * 0.5f + 0.5f, 1f));
                }
            }

            File.WriteAllBytes(albedoPath, albedo.EncodeToPNG());
            File.WriteAllBytes(normalPath, normal.EncodeToPNG());
            Object.DestroyImmediate(albedo);
            Object.DestroyImmediate(normal);
            AssetDatabase.ImportAsset(albedoPath);
            AssetDatabase.ImportAsset(normalPath);
            TextureImporter ni = (TextureImporter)AssetImporter.GetAtPath(normalPath);
            ni.textureType = TextureImporterType.NormalMap;
            ni.SaveAndReimport();
        }

        Material m = Mat(name, Color.white, smoothness);
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath));
        m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
        m.SetFloat("_BumpScale", 1f);
        m.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material NeonMat(string name, Color c, float glow)
    {
        return Mat(name, c, 0.85f, c * glow);
    }

    // 움직이는 위험물: 시즌1 빨강 계열 + 노랑 사선 줄무늬, 은은하게 빛남
    static Material StripeMat()
    {
        const int size = 128;
        string path = $"{TexFolder}/Hazard_Stripes.png";
        if (!File.Exists(path))
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color red = C(0.95f, 0.15f, 0.12f), yellow = C(1f, 0.85f, 0.1f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, ((x + y) / (size / 4)) % 2 == 0 ? red : yellow);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }
        Material m = Mat("Hazard_Striped", Color.white, 0.6f, C(0.6f, 0.12f, 0.08f));
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        m.SetColor("_EmissionColor", Color.white * 0.6f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material GrassTuftMat(Look look)
    {
        const int size = 128;
        string path = $"{TexFolder}/GrassTuft.png";
        if (!File.Exists(path))
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) tex.SetPixel(x, y, clear);
            Random.State st = Random.state;
            Random.InitState(42);
            for (int b = 0; b < 14; b++)
            {
                float bx = Random.Range(10f, size - 10f), lean = Random.Range(-20f, 20f), bh = Random.Range(size * 0.5f, size * 0.98f), bw = Random.Range(3f, 6f);
                for (int y = 0; y < bh; y++)
                {
                    float k = y / bh;
                    float cx = bx + lean * k * k;
                    float w = bw * (1f - k);
                    for (int x = Mathf.FloorToInt(cx - w); x <= Mathf.CeilToInt(cx + w); x++)
                    {
                        if (x < 0 || x >= size) continue;
                        tex.SetPixel(x, y, Color.Lerp(new Color(0.55f, 0.6f, 0.55f, 1f), Color.white, k));
                    }
                }
            }
            Random.state = st;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }

        Material m = Mat($"GrassTuft_{look.grassA.r:0.00}{look.grassA.g:0.00}", Color.Lerp(look.grassA, look.grassB, 0.4f), 0.1f);
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        m.SetFloat("_AlphaClip", 1f);
        m.SetFloat("_Cutoff", 0.5f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Cull", 0f);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    // ───────────────────────── 후처리 · 주변광 ─────────────────────────

    static void SetupPostProcessing(Ctx ctx, Look look)
    {
        EnsureFolder(VolumeFolder);
        string path = $"{VolumeFolder}/{ctx.t.name}.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.intensity.Override(look.bloom);
        bloom.threshold.Override(0.95f);
        bloom.scatter.Override(0.7f);

        Tonemapping tone = GetOrAdd<Tonemapping>(profile);
        tone.mode.Override(TonemappingMode.Neutral);

        ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
        color.saturation.Override(look.saturation);
        color.contrast.Override(look.contrast);
        color.postExposure.Override(look.postExposure);

        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.intensity.Override(look.vignette);
        vignette.smoothness.Override(0.45f);

        EditorUtility.SetDirty(profile);

        Volume volume = Object.FindAnyObjectByType<Volume>();
        if (volume == null)
        {
            volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
        }
        volume.sharedProfile = profile;

        Camera cam = Camera.main;
        if (cam != null)
        {
            UniversalAdditionalCameraData data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data != null) data.renderPostProcessing = true;
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = look.skyTop;
        RenderSettings.ambientEquatorColor = look.skyHorizon;
        RenderSettings.ambientGroundColor = look.skyGround;
        RenderSettings.fogColor = look.skyHorizon;
    }

    static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet(out T component))
        {
            return component;
        }

        component = profile.Add<T>(false);
        component.name = typeof(T).Name;
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }
}
