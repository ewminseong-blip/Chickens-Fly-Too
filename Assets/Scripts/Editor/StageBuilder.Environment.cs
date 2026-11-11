using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 월드 환경: 구간마다 지형 바닥 · 벽(바위 절벽 / 구름 벽 / 소행성 벽) · 장식 · 배경,
// 맵 전체에 바리케이드 · 경계 · 위쪽 바람 띠 · 바깥 감시 · 분위기 전환(하늘 · 안개 · 빛 · 후처리).
// 텍스처와 메시는 유니티 안에서 코드로 만들어 Assets/Art/…/Generated에 저장합니다.
// 색은 시즌1 머티리얼 색(초록 바닥 · 단계별 하늘 파랑 · 갈색 바위 · 우주 네온)을 기준으로 합니다.
public static partial class StageBuilder
{
    const string TexFolder = "Assets/Art/Textures/Generated";
    const string MeshFolder = "Assets/Art/Meshes/Generated";
    const string VolumeFolder = "Assets/Settings/ChickenFly/Volumes";

    enum WallStyle { Rock, Cloud, Crystal }

    // 구간(zone)의 모습 · 분위기
    class Look
    {
        public string name;
        public WallStyle wall = WallStyle.Rock;
        public Color groundA, groundB, rockA, rockB, grassA, grassB, leafA, leafB, mountain;
        public float treesPer100 = 6f, tuftsPer100 = 60f, wallHeight = 0.9f;
        public int mountains = 14, outsideClouds = 0;
        public bool planets, flowers, stars, cloudSea;
        // 분위기(ZoneAtmosphere)
        public Color skyTint, skyGround, fog, ambientSky, ambientEquator, ambientGround, light = Color.white;
        public float exposure = 1.2f, atmosphere = 1f, fogStart = 70f, fogEnd = 320f, lightIntensity = 1.2f;
        // 후처리
        public float bloom = 0.5f, saturation = 18f, postExposure = 0f, vignette = 0.2f, contrast = 8f;
    }

    static Color C(float r, float g, float b) => new Color(r, g, b);

    // 시즌1 색 기준: 땅 초록(0.10,1,0.16)/(0.51,0.60,0.14) · 하늘 (0.62,0.91,1)→(0.20,0.72,0.99)→(0.01,0.33,0.51)→검정
    //               바위 갈색 (0.46,0.29,0)/(0.68,0.43,0.02) · 잎 올리브 (0.36,0.43,0.04) · 우주 네온 분홍 · 보라 · 초록 · 노랑
    static Look LookFor(string zone)
    {
        switch (zone)
        {
            case "Meadow":
                return new Look
                {
                    name = zone, groundA = C(0.18f, 0.85f, 0.22f), groundB = C(0.45f, 0.78f, 0.18f),
                    rockA = C(0.46f, 0.29f, 0.05f), rockB = C(0.68f, 0.45f, 0.12f), grassA = C(0.12f, 0.9f, 0.2f), grassB = C(0.51f, 0.7f, 0.14f),
                    leafA = C(0.36f, 0.6f, 0.08f), leafB = C(0.2f, 0.75f, 0.15f), mountain = C(0.45f, 0.72f, 0.88f), flowers = true, treesPer100 = 7f,
                    skyTint = C(0.62f, 0.91f, 1f), skyGround = C(0.4f, 0.6f, 0.3f), fog = C(0.8f, 0.94f, 1f),
                    ambientSky = C(0.62f, 0.91f, 1f), ambientEquator = C(0.8f, 0.95f, 1f), ambientGround = C(0.3f, 0.55f, 0.2f),
                    light = C(1f, 0.96f, 0.88f), lightIntensity = 1.25f
                };
            case "Forest":
                return new Look
                {
                    name = zone, groundA = C(0.15f, 0.7f, 0.18f), groundB = C(0.36f, 0.55f, 0.1f),
                    rockA = C(0.39f, 0.24f, 0.04f), rockB = C(0.58f, 0.38f, 0.1f), grassA = C(0.12f, 0.75f, 0.18f), grassB = C(0.36f, 0.55f, 0.08f),
                    leafA = C(0.36f, 0.43f, 0.04f), leafB = C(0.16f, 0.6f, 0.12f), mountain = C(0.4f, 0.68f, 0.88f), flowers = true, treesPer100 = 12f, tuftsPer100 = 80f,
                    skyTint = C(0.58f, 0.88f, 1f), skyGround = C(0.3f, 0.5f, 0.25f), fog = C(0.75f, 0.92f, 1f),
                    ambientSky = C(0.58f, 0.88f, 1f), ambientEquator = C(0.75f, 0.92f, 1f), ambientGround = C(0.25f, 0.45f, 0.15f),
                    light = C(1f, 0.95f, 0.85f)
                };
            case "Cliffs":
                return new Look
                {
                    name = zone, groundA = C(0.51f, 0.6f, 0.14f), groundB = C(0.58f, 0.45f, 0.25f),
                    rockA = C(0.46f, 0.29f, 0.05f), rockB = C(0.62f, 0.5f, 0.35f), grassA = C(0.45f, 0.65f, 0.14f), grassB = C(0.3f, 0.7f, 0.18f),
                    leafA = C(0.36f, 0.43f, 0.04f), leafB = C(0.25f, 0.55f, 0.1f), mountain = C(0.3f, 0.62f, 0.92f), treesPer100 = 4f, wallHeight = 1.05f,
                    skyTint = C(0.42f, 0.82f, 1f), skyGround = C(0.5f, 0.45f, 0.4f), fog = C(0.68f, 0.9f, 1f),
                    ambientSky = C(0.42f, 0.82f, 1f), ambientEquator = C(0.7f, 0.9f, 1f), ambientGround = C(0.4f, 0.35f, 0.25f)
                };
            case "Mountain":
                return new Look
                {
                    name = zone, groundA = C(0.82f, 0.86f, 0.9f), groundB = C(0.55f, 0.6f, 0.65f),
                    rockA = C(0.42f, 0.4f, 0.42f), rockB = C(0.62f, 0.6f, 0.62f), grassA = C(0.9f, 0.94f, 1f), grassB = C(0.75f, 0.8f, 0.88f),
                    leafA = C(0.2f, 0.45f, 0.3f), leafB = C(0.3f, 0.55f, 0.35f), mountain = C(0.25f, 0.62f, 0.95f), treesPer100 = 2f, tuftsPer100 = 20f, outsideClouds = 18,
                    skyTint = C(0.2f, 0.72f, 0.99f), skyGround = C(0.7f, 0.7f, 0.75f), fog = C(0.6f, 0.85f, 1f),
                    ambientSky = C(0.2f, 0.72f, 0.99f), ambientEquator = C(0.6f, 0.88f, 1f), ambientGround = C(0.6f, 0.65f, 0.7f),
                    light = C(1f, 0.97f, 0.92f)
                };
            case "CloudLayer":
                return new Look
                {
                    name = zone, wall = WallStyle.Cloud, cloudSea = true, groundA = C(1f, 1f, 1f), groundB = C(0.8f, 0.9f, 1f),
                    rockA = C(0.9f, 0.95f, 1f), rockB = C(0.75f, 0.85f, 1f), mountain = C(0.2f, 0.72f, 0.99f), mountains = 0, outsideClouds = 60, tuftsPer100 = 0f, treesPer100 = 0f,
                    skyTint = C(0.2f, 0.72f, 0.99f), skyGround = C(0.9f, 0.92f, 1f), fog = C(0.85f, 0.94f, 1f), fogStart = 50f, fogEnd = 260f,
                    ambientSky = C(0.2f, 0.72f, 0.99f), ambientEquator = C(0.85f, 0.95f, 1f), ambientGround = C(0.9f, 0.95f, 1f), bloom = 0.6f
                };
            case "InsideClouds":
                return new Look
                {
                    name = zone, wall = WallStyle.Cloud, cloudSea = true, groundA = C(0.85f, 0.9f, 0.98f), groundB = C(0.6f, 0.7f, 0.85f),
                    rockA = C(0.75f, 0.82f, 0.95f), rockB = C(0.55f, 0.65f, 0.85f), mountain = C(0.1f, 0.5f, 0.8f), mountains = 0, outsideClouds = 80, tuftsPer100 = 0f, treesPer100 = 0f,
                    skyTint = C(0.1f, 0.5f, 0.8f), skyGround = C(0.7f, 0.72f, 0.8f), fog = C(0.7f, 0.8f, 0.92f), fogStart = 30f, fogEnd = 190f,
                    ambientSky = C(0.1f, 0.55f, 0.85f), ambientEquator = C(0.7f, 0.82f, 0.95f), ambientGround = C(0.75f, 0.8f, 0.9f),
                    light = C(0.9f, 0.93f, 1f), lightIntensity = 0.95f, postExposure = -0.1f
                };
            case "AboveClouds":
                // 구름 위: 시즌1 '하늘3' 짙은 파랑 + 구름 바다
                return new Look
                {
                    name = zone, wall = WallStyle.Cloud, cloudSea = true, groundA = C(1f, 1f, 1f), groundB = C(0.75f, 0.85f, 1f),
                    rockA = C(1f, 0.97f, 0.9f), rockB = C(0.8f, 0.88f, 1f), mountain = C(0.01f, 0.33f, 0.51f), mountains = 0, outsideClouds = 70, tuftsPer100 = 0f, treesPer100 = 0f,
                    skyTint = C(0.01f, 0.33f, 0.51f), skyGround = C(0.9f, 0.92f, 1f), fog = C(0.45f, 0.75f, 0.95f), exposure = 1f,
                    ambientSky = C(0.01f, 0.33f, 0.51f), ambientEquator = C(0.45f, 0.75f, 0.95f), ambientGround = C(0.9f, 0.92f, 1f), bloom = 0.8f
                };
            case "Storm":
                return new Look
                {
                    name = zone, wall = WallStyle.Cloud, cloudSea = true, groundA = C(0.35f, 0.4f, 0.5f), groundB = C(0.2f, 0.25f, 0.35f),
                    rockA = C(0.35f, 0.4f, 0.52f), rockB = C(0.22f, 0.26f, 0.38f), mountain = C(0.01f, 0.2f, 0.33f), mountains = 0, outsideClouds = 70, tuftsPer100 = 0f, treesPer100 = 0f,
                    skyTint = C(0.01f, 0.2f, 0.33f), skyGround = C(0.2f, 0.2f, 0.25f), fog = C(0.14f, 0.18f, 0.28f), exposure = 0.6f, fogStart = 25f, fogEnd = 180f,
                    ambientSky = C(0.05f, 0.2f, 0.33f), ambientEquator = C(0.18f, 0.25f, 0.4f), ambientGround = C(0.1f, 0.12f, 0.18f),
                    light = C(0.7f, 0.78f, 1f), lightIntensity = 0.75f, bloom = 1.1f, postExposure = -0.15f, vignette = 0.3f
                };
            case "Space":
                return new Look
                {
                    name = zone, wall = WallStyle.Crystal, stars = true, planets = true, groundA = C(0.14f, 0.07f, 0.24f), groundB = C(0.04f, 0.02f, 0.08f),
                    rockA = C(0.24f, 0.18f, 0.34f), rockB = C(0.1f, 0.07f, 0.15f), mountain = C(0.02f, 0.3f, 0.52f), mountains = 0, tuftsPer100 = 0f, treesPer100 = 0f,
                    skyTint = C(0.05f, 0.03f, 0.12f), skyGround = C(0.02f, 0.02f, 0.05f), exposure = 0.25f, atmosphere = 0.2f, fog = C(0.05f, 0.03f, 0.12f), fogStart = 120f, fogEnd = 420f,
                    ambientSky = C(0.32f, 0.22f, 0.55f), ambientEquator = C(0.22f, 0.15f, 0.38f), ambientGround = C(0.08f, 0.05f, 0.14f),
                    light = C(0.88f, 0.84f, 1f), lightIntensity = 1.05f, bloom = 1.3f, vignette = 0.28f, saturation = 25f
                };
            default: // DeepSpace
                return new Look
                {
                    name = "DeepSpace", wall = WallStyle.Crystal, stars = true, planets = true, groundA = C(0.1f, 0.04f, 0.16f), groundB = C(0.03f, 0.01f, 0.05f),
                    rockA = C(0.2f, 0.13f, 0.28f), rockB = C(0.07f, 0.04f, 0.1f), mountain = C(1f, 0.55f, 0f), mountains = 0, tuftsPer100 = 0f, treesPer100 = 0f,
                    skyTint = C(0.03f, 0.01f, 0.08f), skyGround = C(0.01f, 0.01f, 0.03f), exposure = 0.2f, atmosphere = 0.15f, fog = C(0.03f, 0.01f, 0.08f), fogStart = 140f, fogEnd = 460f,
                    ambientSky = C(0.35f, 0.2f, 0.5f), ambientEquator = C(0.25f, 0.12f, 0.35f), ambientGround = C(0.06f, 0.03f, 0.1f),
                    light = C(0.9f, 0.82f, 1f), lightIntensity = 1.05f, bloom = 1.5f, vignette = 0.3f, saturation = 30f
                };
        }
    }

    // ───────────────────────── 구간 하나의 환경 ─────────────────────────

    static void BuildSection(Ctx ctx)
    {
        EnsureFolder(TexFolder);
        EnsureFolder(MeshFolder);
        Look look = ctx.look;
        Transform env = Group(ctx, "Environment");

        // 바닥(지형 메시, 닿으면 죽음)
        Material groundMat = TexMat($"{look.name}_Ground", look.groundA, look.groundB, 6f, 0f, 0.25f, 0.15f, 1.2f);
        ctx.floorMat = groundMat;
        Mesh ground = GroundMesh(ctx);
        GameObject groundGo = MeshObject(env, "Ground", ground, groundMat);
        groundGo.AddComponent<MeshCollider>().sharedMesh = ground;

        Material rockMat = TexMat($"{look.name}_Rock", look.rockA, look.rockB, 4f, 6f, 0.35f, 0.1f, 2f);
        switch (look.wall)
        {
            case WallStyle.Rock:
                foreach (float side in new[] { -1f, 1f })
                {
                    CliffWall(ctx, env, side, rockMat);
                    CliffTop(ctx, env, side, rockMat);
                }
                Tufts(ctx, env);
                break;
            case WallStyle.Cloud:
                CloudWalls(ctx, env);
                break;
            case WallStyle.Crystal:
                CrystalWalls(ctx, env, rockMat);
                break;
        }

        if (look.cloudSea) CloudSea(ctx);
        Mountains(ctx, env);
        OutsideClouds(ctx, env);
        if (look.planets) Planets(ctx, env);
        if (look.stars) StarField(ctx);

        // 장애물 재질: 땅 = 시즌1 갈색 바위, 구름 = 구름, 우주 = 소행성
        ctx.obstacleMat = look.wall == WallStyle.Rock
            ? TexMat($"{look.name}_ObstacleRock", C(0.46f, 0.29f, 0.05f), C(0.68f, 0.43f, 0.02f), 3f, 5f, 0.3f, 0.2f, 2f)
            : rockMat;
    }

    // 구간 바닥: 가운데는 거의 평평(공정함), 가장자리로 갈수록 울퉁불퉁
    static Mesh GroundMesh(Ctx ctx)
    {
        float x0 = ctx.isFirst ? -14f : -1.5f;
        float x1 = ctx.c.Length + (ctx.isLast ? 8f : 1.5f);
        float half = W / 2f + 3f;
        int nx = Mathf.CeilToInt((x1 - x0) / 2f);
        int nz = Mathf.CeilToInt(half * 2f / 1.5f);

        return GridMesh($"World_Ground_{ctx.def.number:00}", nx, nz,
            (i, j) =>
            {
                float x = x0 + (x1 - x0) * i / nx;
                float z = -half + half * 2f * j / nz;
                float gx = ctx.c.X(x);
                float edge = Mathf.InverseLerp(W / 2f - 4f, half, Mathf.Abs(z));
                float bump = (Fbm(gx * 0.12f, z * 0.12f, 3) - 0.5f) * Mathf.Lerp(0.2f, 1.6f, edge * edge);
                return new Vector3(gx, ctx.c.FloorY(x) + bump - 0.05f, z);
            },
            (i, j) => new Vector2(ctx.c.X(x0 + (x1 - x0) * i / nx) / 8f, (-half + half * 2f * j / nz) / 8f),
            Vector3.up);
    }

    // 절벽 높이(이웃 구간과 이어지도록 맵 전체 x로 계산)
    static float WallTop(Ctx ctx, float gx, float side)
    {
        return H * ctx.look.wallHeight + (Fbm(gx * 0.025f + side * 31f, 4.2f, 3) - 0.5f) * 10f;
    }

    static void CliffWall(Ctx ctx, Transform env, float side, Material rockMat)
    {
        float x0 = ctx.isFirst ? -16f : -1.5f;
        float x1 = ctx.c.Length + (ctx.isLast ? 10f : 1.5f);
        int nx = Mathf.CeilToInt((x1 - x0) / 2.2f);
        const int ny = 14;

        Mesh m = GridMesh($"World_Cliff_{ctx.def.number:00}_{(side < 0 ? "L" : "R")}", nx, ny,
            (i, j) =>
            {
                float x = x0 + (x1 - x0) * i / nx;
                float gx = ctx.c.X(x);
                float t = j / (float)ny;
                float top = WallTop(ctx, gx, side);
                float v = -2f + t * (top + 2f);
                // 바깥쪽으로만 울퉁불퉁(경계 안으로 들어오지 않음) + 위로 갈수록 살짝 뒤로
                float outward = Fbm(gx * 0.07f + side * 50f, t * 3.5f, 4) * 3.2f + Fbm(gx * 0.3f + side * 50f, t * 9f, 2) * 0.9f + t * t * 3f;
                outward += Mathf.Abs(Mathf.Sin(v * 0.9f + Fbm(gx * 0.05f, 1f, 2) * 4f)) * 0.5f;   // 지층
                return new Vector3(gx, ctx.c.FloorY(x) + v, side * (W / 2f + 1.2f + outward));
            },
            (i, j) => new Vector2(ctx.c.X(x0 + (x1 - x0) * i / nx) / 9f, (j / (float)ny) * (H * ctx.look.wallHeight) / 9f),
            new Vector3(0f, 0f, -side));
        MeshObject(env, $"Cliff_{(side < 0 ? "Left" : "Right")}", m, rockMat);
    }

    static void CliffTop(Ctx ctx, Transform env, float side, Material rockMat)
    {
        Look look = ctx.look;
        float x0 = ctx.isFirst ? -16f : -1.5f;
        float x1 = ctx.c.Length + (ctx.isLast ? 10f : 1.5f);
        int nx = Mathf.CeilToInt((x1 - x0) / 3f);
        const int nz = 12;
        float inner = W / 2f + 4f, outer = W / 2f + 70f;
        Material grass = TexMat($"{look.name}_CliffGrass", look.grassA, look.grassB, 5f, 0f, 0.2f, 0.12f, 1f);

        Mesh m = GridMesh($"World_CliffTop_{ctx.def.number:00}_{(side < 0 ? "L" : "R")}", nx, nz,
            (i, j) =>
            {
                float x = x0 + (x1 - x0) * i / nx;
                float gx = ctx.c.X(x);
                float u = j / (float)nz;
                float roll = (Fbm(gx * 0.05f, u * 4f + side, 3) - 0.5f) * 4f * u;
                return new Vector3(gx, ctx.c.FloorY(x) + WallTop(ctx, gx, side) - 0.4f + roll, side * Mathf.Lerp(inner, outer, u));
            },
            (i, j) => new Vector2(ctx.c.X(x0 + (x1 - x0) * i / nx) / 8f, (j / (float)nz) * (outer - inner) / 8f),
            Vector3.up);
        MeshObject(env, $"CliffTop_{(side < 0 ? "Left" : "Right")}", m, grass);

        // 절벽 위 나무 · 돌(충돌 없음, 복도 밖)
        int trees = Mathf.RoundToInt(look.treesPer100 * ctx.c.Length / 100f);
        Material bark = TexMat("Bark_Generated", C(0.39f, 0.24f, 0f), C(0.68f, 0.43f, 0.02f), 8f, 12f, 0.15f, 0.1f, 1.5f);
        Material leafA = Mat($"{look.name}_LeafA", look.leafA, 0.15f);
        Material leafB = Mat($"{look.name}_LeafB", look.leafB, 0.15f);
        Transform tg = new GameObject($"Trees_{(side < 0 ? "L" : "R")}").transform;
        tg.SetParent(env, false);
        for (int k = 0; k < trees; k++)
        {
            float x = Random.Range(0f, ctx.c.Length);
            float z = side * Random.Range(inner + 3f, inner + 35f);
            float y = ctx.c.FloorY(x) + WallTop(ctx, ctx.c.X(x), side) - 0.6f;
            DecorTree(tg, new Vector3(ctx.c.X(x), y, z), Random.Range(0.8f, 1.6f), bark, Random.value < 0.5f ? leafA : leafB);
        }

        Transform rg = new GameObject($"Rocks_{(side < 0 ? "L" : "R")}").transform;
        rg.SetParent(env, false);
        for (int k = 0; k < trees / 2 + 4; k++)
        {
            float x = Random.Range(0f, ctx.c.Length);
            float z = side * Random.Range(inner + 1f, inner + 30f);
            float y = ctx.c.FloorY(x) + WallTop(ctx, ctx.c.X(x), side) - 0.5f;
            GameObject r = MeshObject(rg, "Rock", RockMesh(Random.Range(0, 6)), rockMat);
            r.transform.position = new Vector3(ctx.c.X(x), y, z);
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

        GameObject trunk = Prim(PrimitiveType.Cylinder, tree.transform, "Trunk", bark, false);
        trunk.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        trunk.transform.localScale = new Vector3(0.9f, 2.5f, 0.9f);

        int blobs = Random.Range(2, 4);
        for (int b = 0; b < blobs; b++)
        {
            GameObject crown = MeshObject(tree.transform, "Crown", RockMesh(Random.Range(0, 6)), leaf);
            crown.transform.localPosition = new Vector3(Random.Range(-0.8f, 0.8f), 5.5f + b * 1.6f, Random.Range(-0.8f, 0.8f));
            float s = Random.Range(3.2f, 4.6f) - b * 0.6f;
            crown.transform.localScale = new Vector3(s, s * 0.85f, s);
        }
    }

    // 바닥 가장자리 풀포기 + 꽃(충돌 없음)
    static void Tufts(Ctx ctx, Transform env)
    {
        Look look = ctx.look;
        int count = Mathf.RoundToInt(look.tuftsPer100 * ctx.c.Length / 100f);
        if (count <= 0) return;

        List<CombineInstance> parts = new List<CombineInstance>();
        Mesh quad = CrossQuadMesh();
        for (int k = 0; k < count; k++)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            float x = Random.Range(1f, ctx.c.Length - 1f);
            float z = side * Random.Range(W / 2f - 3.5f, W / 2f + 1f);
            float s = Random.Range(0.7f, 1.4f);
            parts.Add(new CombineInstance
            {
                mesh = quad,
                transform = Matrix4x4.TRS(ctx.c.P(x, -0.05f, z), Quaternion.Euler(0f, Random.Range(0f, 180f), 0f), new Vector3(s, s * Random.Range(0.8f, 1.3f), s))
            });
        }

        Mesh combined = new Mesh { name = $"World_Tufts_{ctx.def.number:00}", indexFormat = IndexFormat.UInt32 };
        combined.CombineMeshes(parts.ToArray(), true, true);
        MeshObject(env, "GrassTufts", SaveMesh(combined), GrassTuftMat(look));
        Object.DestroyImmediate(quad);

        if (!look.flowers) return;
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
            float x = Random.Range(1f, ctx.c.Length - 1f);
            GameObject f = Prim(PrimitiveType.Sphere, fg, "Flower", petals[Random.Range(0, petals.Length)], false);
            f.transform.position = ctx.c.P(x, 0.45f, side * Random.Range(W / 2f - 3f, W / 2f + 0.5f));
            f.transform.localScale = Vector3.one * Random.Range(0.3f, 0.5f);
        }
    }

    // 구름 벽: 양옆을 큰 구름으로 빈틈없이(충돌 없음, 경계는 보이지 않는 막)
    static void CloudWalls(Ctx ctx, Transform env)
    {
        if (ctx.cloudPrefab == null) return;
        Transform g = new GameObject("CloudWalls").transform;
        g.SetParent(env, false);
        foreach (float side in new[] { -1f, 1f })
        {
            for (float x = -1f; x < ctx.c.Length + 1f; x += 7.5f)
            {
                for (float h = -2f; h < H + 6f; h += 8f)
                {
                    Vector3 p = ctx.c.P(x + Random.Range(-1.5f, 1.5f), h, side * (W / 2f + Random.Range(5f, 8f)));
                    SpawnCloud(ctx, g, p, Random.Range(S.cloudWallScale.x, S.cloudWallScale.y), false);
                }
            }
        }
    }

    // 우주 벽: 떠다니는 소행성 + 시즌1 네온색 수정(충돌 없음)
    static void CrystalWalls(Ctx ctx, Transform env, Material rockMat)
    {
        Transform g = new GameObject("AsteroidWalls").transform;
        g.SetParent(env, false);
        Material[] neon = NeonSet();
        foreach (float side in new[] { -1f, 1f })
        {
            for (float x = -1f; x < ctx.c.Length + 1f; x += 5f)
            {
                for (float h = 0f; h < H + 6f; h += 7f)
                {
                    Vector3 p = ctx.c.P(x + Random.Range(-2f, 2f), h + Random.Range(-2f, 2f), side * (W / 2f + Random.Range(4f, 9f)));
                    GameObject r = MeshObject(g, "Asteroid", RockMesh(Random.Range(0, 6)), rockMat);
                    r.transform.position = p;
                    r.transform.rotation = Random.rotation;
                    r.transform.localScale = Vector3.one * Random.Range(3f, 6.5f);
                    Spinner sp = r.AddComponent<Spinner>();
                    SetField(sp, "xAngle", Random.Range(-8f, 8f));
                    SetField(sp, "yAngle", Random.Range(-12f, 12f));
                    SetField(sp, "zAngle", Random.Range(-8f, 8f));

                    if (Random.value < 0.3f)
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

    static Material[] NeonSet() => new[]
    {
        NeonMat("Crystal_NeonPink", C(0.99f, 0.2f, 0.46f), 2.2f),
        NeonMat("Crystal_NeonPurple", C(0.45f, 0.2f, 0.99f), 2.2f),
        NeonMat("Crystal_NeonGreen", C(0.48f, 0.99f, 0.2f), 2.2f),
        NeonMat("Crystal_StarYellow", C(0.94f, 0.9f, 0f), 2.2f)
    };

    static void Mountains(Ctx ctx, Transform env)
    {
        Look look = ctx.look;
        int count = Mathf.RoundToInt(look.mountains * ctx.c.Length / 250f);
        if (count <= 0) return;
        Transform g = new GameObject("Mountains").transform;
        g.SetParent(env, false);
        Material mat = Mat($"{look.name}_Mountain", look.mountain, 0.05f);
        Material snow = Mat("Mountain_Snow", C(0.95f, 0.98f, 1f), 0.2f);
        for (int k = 0; k < count; k++)
        {
            float side = k % 2 == 0 ? -1f : 1f;
            float x = Random.Range(0f, ctx.c.Length);
            float z = side * Random.Range(120f, 280f);
            GameObject m = MeshObject(g, "Mountain", RockMesh(Random.Range(0, 6)), mat);
            float w = Random.Range(60f, 140f);
            float h = Random.Range(160f, 320f);
            m.transform.position = new Vector3(ctx.c.X(x), ctx.c.FloorY(x) - 60f + h * 0.35f, z);
            m.transform.localScale = new Vector3(w, h, w);
            m.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            if (h > 240f)
            {
                GameObject cap = MeshObject(m.transform, "SnowCap", RockMesh(Random.Range(0, 6)), snow);
                cap.transform.localPosition = new Vector3(0f, 0.38f, 0f);
                cap.transform.localScale = new Vector3(0.45f, 0.3f, 0.45f);
            }
        }
    }

    static void OutsideClouds(Ctx ctx, Transform env)
    {
        int count = Mathf.RoundToInt(ctx.look.outsideClouds * ctx.c.Length / 300f);
        if (count <= 0 || ctx.cloudPrefab == null) return;
        Transform g = new GameObject("OutsideClouds").transform;
        g.SetParent(env, false);
        for (int k = 0; k < count; k++)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            float x = Random.Range(0f, ctx.c.Length);
            Vector3 p = ctx.c.P(x, Random.Range(-30f, 12f), side * Random.Range(W / 2f + 16f, 170f));
            SpawnCloud(ctx, g, p, Random.Range(10f, 26f), false);
        }
    }

    // 우주: 시즌1 행성 색(파랑 · 주황 + 고리) + 상상 속 행성들
    static void Planets(Ctx ctx, Transform env)
    {
        Transform g = new GameObject("Planets").transform;
        g.SetParent(env, false);
        (Color col, float size, float at, float up, float z, bool ring)[] planets =
        {
            (C(0.02f, 0.3f, 0.52f), 95f, 0.3f, 130f, 280f, false),
            (C(1f, 0.55f, 0f), 62f, 0.75f, 80f, -230f, true),
            (C(0.45f, 0.2f, 0.99f), 34f, 0.1f, 45f, -170f, false),
            (C(0.48f, 0.99f, 0.2f), 22f, 0.55f, 160f, 210f, true)
        };
        foreach (var p in planets)
        {
            Material m = Mat($"Planet_{p.col.r:0.00}_{p.col.g:0.00}_{p.col.b:0.00}", p.col, 0.4f, p.col * 0.35f);
            GameObject s = Prim(PrimitiveType.Sphere, g, "Planet", m, false);
            float x = ctx.c.Length * p.at;
            s.transform.position = ctx.c.P(x, p.up, p.z);
            s.transform.localScale = Vector3.one * p.size;
            if (p.ring)
            {
                GameObject ring = Prim(PrimitiveType.Cylinder, s.transform, "Ring", Mat("Planet_Ring", C(1f, 0.55f, 0f), 0.5f, C(0.5f, 0.25f, 0f)), false);
                ring.transform.localScale = new Vector3(1.9f, 0.01f, 1.9f);
                ring.transform.localRotation = Quaternion.Euler(18f, 0f, 12f);
            }
        }
    }

    static void SpawnCloud(Ctx ctx, Transform parent, Vector3 pos, float scale, bool keepColliders)
    {
        if (ctx.cloudPrefab == null) return;
        GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(ctx.cloudPrefab, parent.gameObject.scene);
        c.transform.SetParent(parent, false);
        c.transform.position = pos;
        c.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        c.transform.localScale = Vector3.one * scale;
        if (!keepColliders)
        {
            foreach (Collider col in c.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        }
    }

    // ───────────────────────── 맵 전체 ─────────────────────────

    static void BuildWorldWide(Transform world, Course course, List<Ctx> contexts)
    {
        Transform g = new GameObject("WorldWide").transform;
        g.SetParent(world, false);
        Ctx first = contexts[0];
        Ctx last = contexts[contexts.Count - 1];

        StartNest(first);
        GoalNest(last);

        // 앞 · 뒤 바리케이드(시즌1 Barricade처럼 갈색 바위 벽, 닿아도 죽지 않음)
        Material barricadeMat = TexMat("Barricade_Rock", C(0.46f, 0.29f, 0f), C(0.68f, 0.43f, 0.02f), 3f, 8f, 0.3f, 0.15f, 2.2f);
        Barricade(g, -12f, course.baseY, barricadeMat, "Barricade_Back");
        Barricade(g, course.Length + 1f, course.EndY, barricadeMat, "Barricade_Front");

        Boundaries(g, course, first.triggerMat);
        TopWindBand(g, course, first.triggerMat);
        StageBoundsGuard(g, course);
        Atmosphere(g, contexts);
    }

    // 양옆 · 위 경계(보이지 않는 막). 두께 4m, 구간끼리 4m씩 겹쳐 비탈이 꺾이는 곳에 틈이 없게.
    static void Boundaries(Transform parent, Course course, Material inv)
    {
        bool deadly = S != null && S.deadlyBoundaries;
        string tag = deadly ? "Untagged" : "Boundary";
        const float thick = 4f, overlap = 4f;
        Transform g = new GameObject("Boundaries").transform;
        g.SetParent(parent, false);

        float x = 0f;
        int i = 0;
        foreach (Vector2 s in course.segs)
        {
            float y0 = course.FloorY(x), y1 = course.FloorY(x + s.x);
            float a = s.y;
            float l = Mathf.Sqrt(s.x * s.x + (y1 - y0) * (y1 - y0));
            Vector3 n = new Vector3(-Mathf.Sin(a * Mathf.Deg2Rad), Mathf.Cos(a * Mathf.Deg2Rad), 0f);
            Vector3 mid = new Vector3(x + s.x / 2f, (y0 + y1) / 2f, 0f);
            foreach (float side in new[] { -1f, 1f })
            {
                InvisibleWall(g, $"Side_{i}_{(side < 0 ? "L" : "R")}", mid + n * (H / 2f) + new Vector3(0f, 0f, side * (W / 2f + thick / 2f)), new Vector3(l + overlap, H + 12f, thick), a, inv, tag);
            }
            InvisibleWall(g, $"Top_{i}", mid + n * (H + thick / 2f), new Vector3(l + overlap, thick, W + thick * 2f), a, inv, tag);
            x += s.x;
            i++;
        }

        InvisibleWall(g, "Top_Back", new Vector3(-6f, course.baseY + H + thick / 2f, 0f), new Vector3(14f, thick, W + thick * 2f), 0f, inv, tag);
        foreach (float side in new[] { -1f, 1f })
        {
            InvisibleWall(g, "Side_Back", new Vector3(-6f, course.baseY + H / 2f, side * (W / 2f + thick / 2f)), new Vector3(14f, H + 12f, thick), 0f, inv, tag);
        }
    }

    static void InvisibleWall(Transform parent, string name, Vector3 pos, Vector3 scale, float angle, Material inv, string tag)
    {
        GameObject b = Box(parent, name, pos, scale, angle, inv);
        b.GetComponent<Renderer>().enabled = false;
        b.tag = tag;
    }

    // 시즌1 Barricade 같은 바위 벽: 큰 판(충돌) + 울퉁불퉁한 바위들(모양)
    static void Barricade(Transform parent, float x, float floorY, Material mat, string name)
    {
        Transform g = new GameObject(name).transform;
        g.SetParent(parent, false);
        float dir = x < 1f ? -1f : 1f;
        GameObject wall = Box(g, "Wall", new Vector3(x + dir * 2f, floorY + H * 0.6f, 0f), new Vector3(4f, H * 1.6f, W + 14f), 0f, mat);
        wall.tag = "Boundary";
        for (int k = 0; k < 9; k++)
        {
            GameObject r = MeshObject(g, "BarricadeRock", RockMesh(Random.Range(0, 6)), mat);
            r.transform.position = new Vector3(x + dir * 1f, floorY + Random.Range(1f, H * 1.1f), Random.Range(-W / 2f, W / 2f));
            r.transform.rotation = Random.rotation;
            r.transform.localScale = Vector3.one * Random.Range(4f, 8f);
        }
    }

    // 위쪽 경계 바로 아래: 아래로 미는 바람 띠(단단한 막에 닿기 전에 부드럽게 밀어냄)
    static void TopWindBand(Transform parent, Course course, Material inv)
    {
        Transform g = new GameObject("TopWindBand").transform;
        g.SetParent(parent, false);
        float x = 0f;
        int i = 0;
        foreach (Vector2 s in course.segs)
        {
            float y0 = course.FloorY(x), y1 = course.FloorY(x + s.x);
            float a = s.y;
            float l = Mathf.Sqrt(s.x * s.x + (y1 - y0) * (y1 - y0));
            Vector3 n = new Vector3(-Mathf.Sin(a * Mathf.Deg2Rad), Mathf.Cos(a * Mathf.Deg2Rad), 0f);
            Vector3 mid = new Vector3(x + s.x / 2f, (y0 + y1) / 2f, 0f);
            GameObject band = Box(g, $"TopWind_{i}", mid + n * (H - 2f), new Vector3(l + 2f, 4f, W), a, inv);
            band.GetComponent<Renderer>().enabled = false;
            band.GetComponent<Collider>().isTrigger = true;
            band.tag = "Trigger";
            SetField(band.AddComponent<WindArea>(), "acceleration", new Vector3(0f, -32f, 0f));
            x += s.x;
            i++;
        }
    }

    // 마지막 안전장치: 어떤 이유로든 맵 밖으로 나가면 다시 시작
    static void StageBoundsGuard(Transform parent, Course course)
    {
        const float step = 2f;
        int count = Mathf.CeilToInt(course.Length / step) + 1;
        float[] heights = new float[count];
        for (int k = 0; k < count; k++) heights[k] = course.FloorY(k * step);

        GameObject go = new GameObject("StageBounds");
        go.transform.SetParent(parent, false);
        StageBounds bounds = go.AddComponent<StageBounds>();
        bounds.Configure(heights, step, course.Length, W, H);
        EditorUtility.SetDirty(bounds);
    }

    // 분위기 전환: 구간마다 하늘 · 안개 · 빛 · 후처리 값을 ZoneAtmosphere에 넣습니다.
    static void Atmosphere(Transform parent, List<Ctx> contexts)
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

        SerializedObject so = new SerializedObject(atmo);
        so.FindProperty("blend").floatValue = S.atmosphereBlend;
        so.FindProperty("sun").objectReferenceValue = Object.FindAnyObjectByType<Light>();
        so.FindProperty("skyboxTemplate").objectReferenceValue = skyTemplate;
        SerializedProperty zones = so.FindProperty("zones");
        zones.arraySize = contexts.Count;

        for (int i = 0; i < contexts.Count; i++)
        {
            Look l = contexts[i].look;
            Volume vol = new GameObject($"Volume_{i + 1:00}_{l.name}").AddComponent<Volume>();
            vol.transform.SetParent(go.transform, false);
            vol.isGlobal = true;
            vol.weight = i == 0 ? 1f : 0f;
            vol.sharedProfile = VolumeProfileFor(l);

            SerializedProperty z = zones.GetArrayElementAtIndex(i);
            z.FindPropertyRelative("name").stringValue = $"{i + 1}. {contexts[i].def.title}";
            z.FindPropertyRelative("startX").floatValue = contexts[i].c.x0;
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

        // 에디터에서 보이는 첫 모습(플레이하면 ZoneAtmosphere가 이어받음)
        Look f = contexts[0].look;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = f.fog;
        RenderSettings.fogStartDistance = f.fogStart;
        RenderSettings.fogEndDistance = f.fogEnd;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = f.ambientSky;
        RenderSettings.ambientEquatorColor = f.ambientEquator;
        RenderSettings.ambientGroundColor = f.ambientGround;

        Camera cam = Camera.main;
        if (cam != null)
        {
            UniversalAdditionalCameraData data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data != null) data.renderPostProcessing = true;
        }
    }

    static VolumeProfile VolumeProfileFor(Look look)
    {
        string path = $"{VolumeFolder}/{look.name}.asset";
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
        GetOrAdd<Tonemapping>(profile).mode.Override(TonemappingMode.Neutral);
        ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
        color.saturation.Override(look.saturation);
        color.contrast.Override(look.contrast);
        color.postExposure.Override(look.postExposure);
        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.intensity.Override(look.vignette);
        vignette.smoothness.Override(0.45f);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet(out T component)) return component;
        component = profile.Add<T>(false);
        component.name = typeof(T).Name;
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

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
            for (int q = 0; q < t.Length; q += 3) (t[q + 1], t[q + 2]) = (t[q + 2], t[q + 1]);
        }

        Mesh m = new Mesh { name = name };
        if (v.Length > 65000) m.indexFormat = IndexFormat.UInt32;
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

    // 울퉁불퉁한 바위 · 나무 수관 · 산 · 소행성 메시(6가지, 한 번 만들어 재사용)
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
        Mesh m = new Mesh { name = "CrossQuad" };
        m.vertices = new[]
        {
            new Vector3(-0.6f, 0f, 0f), new Vector3(0.6f, 0f, 0f), new Vector3(-0.6f, 1.2f, 0f), new Vector3(0.6f, 1.2f, 0f),
            new Vector3(0f, 0f, -0.6f), new Vector3(0f, 0f, 0.6f), new Vector3(0f, 1.2f, -0.6f), new Vector3(0f, 1.2f, 0.6f)
        };
        m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        m.triangles = new[] { 0, 2, 1, 1, 2, 3, 4, 6, 5, 5, 6, 7 };
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

        Vector3 n = Vector3.Cross(v[f[1]] - v[f[0]], v[f[2]] - v[f[0]]);
        if (Vector3.Dot(n, v[f[0]]) < 0f)
        {
            for (int q = 0; q < f.Count; q += 3) (f[q + 1], f[q + 2]) = (f[q + 2], f[q + 1]);
        }

        Mesh m = new Mesh();
        m.SetVertices(v);
        m.SetTriangles(f, 0);
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

    static float SeamlessFbm(float u, float v, float scale, int octaves)
    {
        float a = Fbm(u * scale, v * scale, octaves);
        float b = Fbm((u - 1f) * scale, v * scale, octaves);
        float c = Fbm(u * scale, (v - 1f) * scale, octaves);
        float d = Fbm((u - 1f) * scale, (v - 1f) * scale, octaves);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    // 텍스처(색 + 노멀맵)를 만들어 저장하고 머티리얼을 돌려줍니다. strata = 지층 무늬, speckle = 잔점
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

    static Material NeonMat(string name, Color c, float glow) => Mat(name, c, 0.85f, c * glow);

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

        Material m = Mat($"GrassTuft_{look.name}", Color.Lerp(look.grassA, look.grassB, 0.4f), 0.1f);
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        m.SetFloat("_AlphaClip", 1f);
        m.SetFloat("_Cutoff", 0.5f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Cull", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }
}
