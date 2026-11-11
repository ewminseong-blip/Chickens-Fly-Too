using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

// 스테이지(구간) 내용과 자연물 기믹.
// 원칙: 그 높이에 있을 법한 자연물만 씁니다(인공물 없음).
//   땅 · 숲 · 절벽 · 산 → 바위 무더기 · 바위 기둥 · 나무 · 쓰러진 통나무 · 바람 · 굴러오는 바위 · 낙석 · 독수리 떼
//   구름층 → 흘러가는 구름 · 솟거나 내려오는 구름 · 갈매기 떼 · 우박 · 바람 고리(꼬리바람) · 상승 기류 · 번개
//   우주(상상) → 소행성 · 혜성 · 수정 · 중력 이상 지대 · 우주 닭 떼 · UFO · 외계 생물 · 펄서 빛줄기 · 오로라
// 충돌 판정은 보이는 모양보다 작게 잡아(보이지 않는 관용) 스치기만 해도 죽는 억울함을 줄입니다.
public static partial class StageBuilder
{
    enum BirdKind { Eagle, Seagull, SpaceChicken, Ufo, Alien }

    // ───────────────────────── 스테이지 정의 ─────────────────────────
    // 난이도: 1→6 상승, 7 · 8은 '어려워 보이지만 한 길(링 · 직진)만 따라가면 다 피해지는' 숨 고르기, 9 · 10은 6보다 높게.
    // x는 그 구간 안의 거리(m). 구간끼리는 끊기지 않고 이어집니다.

    static List<StageDef> DefineStages()
    {
        List<StageDef> list = new List<StageDef>();

        list.Add(new StageDef
        {
            number = 1, title = "First Flap", hint = "SPACE = FLAP   W = GO   S = STOP   MOUSE = TURN", difficulty = 1f, zone = "Meadow",
            content = ctx =>
            {
                Sign(ctx, 10f, 12f, 0f, "SPACE : FLAP", 3f);
                Sign(ctx, 22f, 8f, 0f, "W : GO     S : STOP", 2.2f);
                RockGate(ctx, 45f, 2f, 2f + ctx.Hole, -10f, 10f);          // 아래 틈
                RockGate(ctx, 110f, 11f, 11f + ctx.Hole, -10f, 10f);       // 위 틈
                Sign(ctx, 125f, 15f, 0f, "MOUSE : TURN", 3f);
                RockGate(ctx, 150f, 3f, 21f, -11f, -11f + ctx.Hole);       // 왼쪽 틈
                RockGate(ctx, 178f, 3f, 21f, 11f - ctx.Hole, 11f);         // 오른쪽 틈
            }
        }.Seg(30, 0).Seg(60, 12).Seg(40, 0).Seg(50, 18).Seg(35, 0));

        list.Add(new StageDef
        {
            number = 2, title = "Growing Forest", hint = "Trees grow when you pass by", difficulty = 2f, zone = "Forest",
            content = ctx =>
            {
                float[] zs = { -6f, 5f, -3f, 7f, -7f, 2f };
                for (int i = 0; i < zs.Length; i++) Tree(ctx, 30f + i * 11f, zs[i], 1f);
                Sign(ctx, 108f, 15f, 0f, "WHAT A NICE TREE!", 2.6f);   // 시즌1 표지판
                foreach (Transform t in new[] { Tree(ctx, 126f, -5f, 0.9f), Tree(ctx, 134f, 6f, 0.9f), Tree(ctx, 142f, -1f, 0.9f) })
                {
                    GrowTriggerFor(ctx, 116f, t, new Vector3(1.5f, 2f, 1.5f));
                }
                float[] zs2 = { 6f, -6f, 0f, -8f, 8f, -2f, 4f };
                for (int i = 0; i < zs2.Length; i++) Tree(ctx, 156f + i * 8.5f, zs2[i], 1.1f);
                FallenLog(ctx, 222f, 8f);
            }
        }.Seg(20, 0).Seg(80, 15).Seg(30, 0).Seg(80, 20).Seg(30, 0));

        list.Add(new StageDef
        {
            number = 3, title = "Windy Cliffs", hint = "Ride the wind", difficulty = 3f, zone = "Cliffs",
            content = ctx =>
            {
                Wind(ctx, 22f, 78f, new Vector3(0f, ctx.Lift, 0f));           // 비탈을 도와주는 상승풍
                Flock(ctx, BirdKind.Eagle, 95f, 15f, true, 3);                  // 까마귀 · 독수리가 가로질러 감
                Wind(ctx, 108f, 165f, new Vector3(0f, ctx.Lift * 0.9f, 0f));
                RockSpire(ctx, 125f, -6f, 3.5f); RockSpire(ctx, 140f, 6f, 3.5f); RockSpire(ctx, 155f, -2f, 3.5f);
                Wind(ctx, 172f, 205f, new Vector3(0f, 0f, ctx.CrossWind));      // 옆바람
                RockSpire(ctx, 185f, 7f, 3.5f); RockSpire(ctx, 198f, 2f, 3.5f);
                Wind(ctx, 212f, 235f, new Vector3(0f, -ctx.Lift, 0f));          // 하강풍 + 아래 틈
                RockGate(ctx, 228f, 2f, 2f + ctx.Hole, -10f, 10f);
            }
        }.Seg(20, 0).Seg(60, 28).Seg(25, 0).Seg(60, 30).Seg(25, 0).Seg(40, 15).Seg(25, 0));

        list.Add(new StageDef
        {
            number = 4, title = "Rockfall Ridge", hint = "Boulders roll, eagles cross", difficulty = 4f, zone = "Mountain",
            content = ctx =>
            {
                RollingBoulder(ctx, 40f, 1f, 0f); RollingBoulder(ctx, 56f, -1f, 0.4f); RollingBoulder(ctx, 72f, 1f, 0.8f);
                Rockfall(ctx, 88f, 100f, 132f, 6);
                Flock(ctx, BirdKind.Eagle, 152f, 12f, true, 4);
                Flock(ctx, BirdKind.Eagle, 174f, 18f, false, 4);
                RollingBoulder(ctx, 205f, -1f, 0.2f); RollingBoulder(ctx, 216f, 1f, 0.7f);
                // 끝 무렵: 구름이 바닥까지 내려와 있음 = 구름 높이에 닿음(시즌1 구성)
                for (float x = 228f; x < ctx.c.Length; x += 9f) CloudFloorRow(ctx, x);
                CloudCluster(ctx, 245f, 18f, -7f, 0.8f, false);
                CloudCluster(ctx, 258f, 22f, 6f, 0.9f, false);
            }
        }.Seg(20, 0).Seg(70, 20).Seg(40, 0).Seg(70, 22).Seg(40, 0).Seg(30, 0));

        list.Add(new StageDef
        {
            number = 5, title = "Drifting Clouds", hint = "Clouds drift, gulls fly", difficulty = 5f, zone = "CloudLayer",
            content = ctx =>
            {
                DriftingCloud(ctx, 45f, 8f, 1f, 0f); DriftingCloud(ctx, 58f, 16f, -1f, 0.3f); DriftingCloud(ctx, 72f, 11f, 1f, 0.6f);
                HeadOnFlock(ctx, BirdKind.Seagull, 108f, 12f, 3);
                Checkpoint(ctx, 128f);
                DriftingCloud(ctx, 150f, 9f, -1f, 0.2f); DriftingCloud(ctx, 165f, 17f, 1f, 0.5f);
                Flock(ctx, BirdKind.Seagull, 190f, 13f, true, 5);
                DescendingCloud(ctx, 215f, 238f, 0f, -11f);   // 위에서 내려와 위쪽 길을 막음 → 아래로
                DescendingCloud(ctx, 225f, 250f, 6f, -11f);
                DriftingCloud(ctx, 272f, 10f, 1f, 0.1f);
            }
        }.Seg(20, 0).Seg(60, 18).Seg(40, 0).Seg(60, 20).Seg(40, 0).Seg(50, 15).Seg(30, 0));

        list.Add(new StageDef
        {
            number = 6, title = "Hailstorm", hint = "Ice falls from above", difficulty = 6f, zone = "InsideClouds",
            content = ctx =>
            {
                Hail(ctx, 40f, 58f, 86f, 7);
                CloudGate(ctx, 95f, 6f, 6f + ctx.Hole, -4f, 4f + ctx.Hole * 0.3f);
                Checkpoint(ctx, 112f);
                Hail(ctx, 120f, 136f, 166f, 9);
                DescendingCloud(ctx, 172f, 195f, -5f, -10f);
                DriftingCloud(ctx, 205f, 12f, 1f, 0.3f);
                HeadOnFlock(ctx, BirdKind.Seagull, 214f, 9f, 3);
                Hail(ctx, 222f, 236f, 262f, 8);
                CloudGate(ctx, 270f, 3f, 3f + ctx.Hole, 2f - ctx.Hole, 2f);
            }
        }.Seg(20, 0).Seg(60, 22).Seg(30, 0).Seg(60, 24).Seg(30, 0).Seg(60, 20).Seg(25, 0));

        list.Add(new StageDef
        {
            // 숨 고르기: 사방에서 구름이 솟고 내려오지만 가운데 높이의 '바람 고리 길'은 끝까지 비어 있음
            number = 7, title = "Ride the Tailwind", hint = "Follow the wind rings", difficulty = 4.5f, zone = "AboveClouds",
            content = ctx =>
            {
                const float laneH = 13f;   // 안전한 길: 바닥에서 13m, Z 0 (±5m)
                for (float x = 25f; x < ctx.c.Length - 25f; x += 22f)
                {
                    WindRing(ctx, x, laneH, 0f);
                    RisingCloud(ctx, x - 14f, x + 6f, 0f, -5f, 9f);
                    DescendingCloud(ctx, x - 12f, x + 9f, 0f, -4f);
                    RisingCloud(ctx, x - 10f, x + 4f, -10.5f, 0f, 16f);
                    RisingCloud(ctx, x - 10f, x + 11f, 10.5f, 0f, 16f);
                }
                for (float x = 60f; x < ctx.c.Length - 40f; x += 70f) Thermal(ctx, x, 0f);
                // 멀리 기러기 떼(장식, 충돌 없음)
                for (float x = 40f; x < ctx.c.Length; x += 90f) DecorFlock(ctx, BirdKind.Seagull, x, 30f, -60f);
            }
        }.Seg(20, 0).Seg(80, 12).Seg(40, 0).Seg(80, 12).Seg(40, 0).Seg(40, 0));

        list.Add(new StageDef
        {
            // 숨 고르기: 번개가 정신없이 치지만 W를 꾹 누르고 직진하면 지나가는 순간마다 꺼져 있음
            number = 8, title = "Thunder Storm", hint = "Don't stop. Never stop.", difficulty = 5f, zone = "Storm",
            content = ctx =>
            {
                for (float x = 32f; x < ctx.c.Length - 22f; x += 12f)
                {
                    SyncGate(ctx, x - 6f);
                    LightningRow(ctx, x, S.syncedLightningSpeed, 0f);
                }
            }
        }.Seg(20, 0).Seg(120, 8).Seg(40, 0).Seg(100, 8).Seg(30, 0));

        list.Add(new StageDef
        {
            number = 9, title = "Upside-Down Space", hint = "The world turns over", difficulty = 7f, zone = "Space",
            content = ctx =>
            {
                GravityAnomaly(ctx, 35f, true);
                Checkpoint(ctx, 80f);
                AsteroidGate(ctx, 95f, 12f, 12f + ctx.Hole, -9f, 9f);
                Comet(ctx, 118f, 15f, 1f);
                AsteroidBelt(ctx, 140f, 5);
                Wind(ctx, 160f, 180f, new Vector3(0f, -ctx.Lift, 0f));             // 중력 우물
                AsteroidGate(ctx, 176f, 2f, 2f + ctx.Hole, -2f, 2f + ctx.Hole * 0.5f);
                Checkpoint(ctx, 205f);
                HeadOnFlock(ctx, BirdKind.SpaceChicken, 232f, 13f, 4);              // 시즌1 기획의 우주 닭
                Flock(ctx, BirdKind.Ufo, 252f, 16f, true, 2);
                Comet(ctx, 266f, 9f, -1f);
                AsteroidGate(ctx, 290f, 10f, 10f + ctx.Hole, 3f, 3f + ctx.Hole);
                Alien(ctx, 310f, 18f, -8f);
            }
        }.Seg(20, 0).Seg(40, 0).Seg(60, 20).Seg(40, 0).Seg(60, 25).Seg(40, 0).Seg(50, 20).Seg(30, 0));

        list.Add(new StageDef
        {
            // 9스테이지에서 뒤집힌 세상 그대로 이어서 시작 → 끝에서 다시 돌아오며 마지막 상승
            number = 10, title = "To the Stars", hint = "Everything you learned", difficulty = 8f, zone = "DeepSpace",
            content = ctx =>
            {
                Wind(ctx, 22f, 55f, new Vector3(0f, 0f, -ctx.CrossWind));          // 태양풍
                AsteroidSpire(ctx, 36f, -7f); AsteroidSpire(ctx, 48f, 3f);
                AsteroidBelt(ctx, 70f, 6);
                HeadOnFlock(ctx, BirdKind.SpaceChicken, 95f, 12f, 5);
                for (int i = 0; i < 4; i++) PulsarRow(ctx, 112f + i * 9f, i * 0.45f);
                Checkpoint(ctx, 160f);
                Flock(ctx, BirdKind.Ufo, 180f, 12f, false, 3);
                Comet(ctx, 198f, 18f, 1f); Comet(ctx, 210f, 8f, -1f);
                WindRing(ctx, 228f, 13f, 0f);
                Alien(ctx, 240f, 10f, 6f);
                AsteroidGate(ctx, 252f, 14f, 14f + ctx.Hole, -10f, -10f + ctx.Hole);
                Checkpoint(ctx, 275f);
                GravityAnomaly(ctx, 300f, false);   // 세상이 다시 돌아옴 → 마지막 상승
                Comet(ctx, 332f, 14f, 1f);
                AsteroidGate(ctx, 352f, 4f, 4f + ctx.Hole, -5f, 5f);
                Aurora(ctx, 362f, ctx.c.Length - 4f);   // 시즌1 마지막 네온(분홍 · 노랑)을 오로라로
            }
        }.Seg(20, 0).Seg(30, 0).Seg(60, 22).Seg(40, 0).Seg(60, 25).Seg(40, 0).Seg(60, 28).Seg(40, 0).Seg(50, 15).Seg(30, 0).Seg(40, 0));

        return list;
    }

    // ───────────────────────── 출발 · 도착 · 체크포인트 ─────────────────────────

    // 출발 둥지(닿아도 안전: 이름 FirstGroundStart)
    static void StartNest(Ctx ctx)
    {
        Material bark = TexMat("Bark_Generated", C(0.39f, 0.24f, 0f), C(0.68f, 0.43f, 0.02f), 8f, 12f, 0.15f, 0.1f, 1.5f);
        GameObject pad = Prim(PrimitiveType.Cylinder, null, "FirstGroundStart", bark, false);
        pad.transform.position = ctx.c.P(0f, 0.25f, 0f);
        pad.transform.localScale = new Vector3(6f, 0.75f, 6f);
        BoxCollider col = pad.AddComponent<BoxCollider>();   // 위가 평평한 상자 충돌
        col.size = new Vector3(1f, 2f, 1f);
        NestRing(null, pad.transform.position + Vector3.up * 0.5f, 2.9f, 1f);   // 크기가 바뀐 발판의 자식으로 두면 모양이 찌그러짐
    }

    // 도착: 금빛 알이 있는 큰 둥지 + 빛기둥
    static void GoalNest(Ctx ctx)
    {
        float gx = ctx.c.Length - 16f;
        Transform goal = Group(ctx, "Goal");

        GameObject trigger = Box(goal, "Ending_Trigger", ctx.c.P(gx, ctx.c.VerticalHeight(gx) / 2f, 0f), new Vector3(3f, ctx.c.VerticalHeight(gx), W), 0f, ctx.triggerMat);
        trigger.GetComponent<Collider>().isTrigger = true;
        trigger.GetComponent<Renderer>().enabled = false;
        trigger.tag = "Ending";
        trigger.AddComponent<EndingCredits>();

        Vector3 nestPos = ctx.c.P(gx + 7f, 6f, 0f);
        Material rock = TexMat("Barricade_Rock", C(0.46f, 0.29f, 0f), C(0.68f, 0.43f, 0.02f), 3f, 8f, 0.3f, 0.15f, 2.2f);
        GameObject pedestal = MeshObject(goal, "NestRock", RockMesh(2), rock);
        pedestal.transform.position = ctx.c.P(gx + 7f, 2f, 0f);
        pedestal.transform.localScale = new Vector3(10f, 9f, 10f);
        pedestal.tag = "Ending";
        Transform ring = NestRing(goal, nestPos, 4.5f, 2f);
        ring.gameObject.tag = "Ending";

        Material gold = Mat("Goal_GoldenEgg", C(1f, 0.84f, 0.3f), 0.9f, C(1f, 0.75f, 0.2f) * 1.8f);
        GameObject egg = Prim(PrimitiveType.Sphere, goal, "GoldenEgg", gold, false);
        egg.transform.position = nestPos + Vector3.up * 1.6f;
        egg.transform.localScale = new Vector3(2.4f, 3.1f, 2.4f);
        egg.tag = "Ending";

        // 빛기둥(파티클)
        GameObject beam = new GameObject("LightBeam");
        beam.transform.SetParent(goal, false);
        beam.transform.position = nestPos;
        ParticleSystem ps = beam.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = 3f;
        main.startSpeed = 6f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(C(1f, 0.9f, 0.5f), C(1f, 1f, 1f));
        main.maxParticles = 300;
        main.prewarm = true;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 60f;
        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = 2f;
        sh.rotation = new Vector3(-90f, 0f, 0f);
        beam.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat;

        Sign(ctx, gx - 1f, ctx.c.VerticalHeight(gx) - 6f, 0f, "GOAL", 4f).tag = "Ending";
    }

    // 둥지 테두리: 짚 색 캡슐을 원으로
    static Transform NestRing(Transform parent, Vector3 center, float radius, float thickness)
    {
        Transform g = new GameObject("Nest").transform;
        g.SetParent(parent, true);
        g.position = center;
        Material straw = Mat("Nest_Straw", C(0.94f, 0.8f, 0.35f), 0.1f);
        Material straw2 = Mat("Nest_Straw_Dark", C(0.68f, 0.5f, 0.2f), 0.1f);
        int n = Mathf.RoundToInt(radius * 5f);
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n + Random.Range(-0.1f, 0.1f);
            GameObject twig = Prim(PrimitiveType.Capsule, g, "Twig", i % 3 == 0 ? straw2 : straw, false);
            twig.transform.position = center + new Vector3(Mathf.Cos(a) * radius, Random.Range(-0.15f, 0.15f) * thickness, Mathf.Sin(a) * radius);
            twig.transform.rotation = Quaternion.Euler(90f + Random.Range(-15f, 15f), -a * Mathf.Rad2Deg + Random.Range(-20f, 20f), 0f);
            twig.transform.localScale = new Vector3(0.18f, radius * 0.35f, 0.18f) * thickness;
        }
        return g;
    }

    // 체크포인트: 옆 바위 위 작은 둥지와 알(지나가면 알이 초록으로 빛남). order를 안 주면 구간 번호 기준으로 자동.
    static void Checkpoint(Ctx ctx, float x, int order = -1)
    {
        ctx.checkpointCount++;
        if (order < 0) order = ctx.index * 100 + ctx.checkpointCount;

        Transform g = Group(ctx, $"Checkpoint_{ctx.checkpointCount}");
        GameObject trig = FullTrigger(ctx, "CheckpointTrigger", x, 3f);
        trig.transform.SetParent(g, true);
        Checkpoint cp = trig.AddComponent<Checkpoint>();
        SetField(cp, "order", order);

        GameObject spawn = new GameObject("RespawnPoint");
        spawn.transform.SetParent(g, false);
        spawn.transform.position = ctx.c.P(x + 4f, H * 0.45f, 0f);
        SetField(cp, "respawnPoint", spawn.transform);

        // 장식(충돌 없음): 벽 쪽 바위 받침 + 둥지 + 알
        float z = W / 2f - 2.5f;
        Material rockMat = ctx.obstacleMat;
        GameObject perch = MeshObject(g, "Perch", RockMesh(3), rockMat);
        perch.transform.position = ctx.c.P(x, H * 0.3f, z);
        perch.transform.localScale = new Vector3(4f, 3f, 3.5f);
        perch.tag = "Trigger";
        Vector3 nest = ctx.c.P(x, H * 0.3f + 1.4f, z);
        NestRing(g, nest, 1.1f, 0.6f).gameObject.tag = "Trigger";
        GameObject egg = Prim(PrimitiveType.Sphere, g, "Egg", Mat("Checkpoint_Egg", C(1f, 0.97f, 0.9f), 0.6f, C(0.25f, 0.25f, 0.2f)), false);
        egg.transform.position = nest + Vector3.up * 0.45f;
        egg.transform.localScale = new Vector3(0.9f, 1.15f, 0.9f);
        egg.tag = "Trigger";
        SetField(cp, "flagRenderer", egg.GetComponent<Renderer>());
    }

    // ───────────────────────── 땅 · 숲 · 절벽 · 산 ─────────────────────────

    // 자연 장애물 덩어리: 보이지 않는 상자 충돌(작게) + 바위 / 구름 / 소행성 모양(크게)
    static void NaturalBlock(Ctx ctx, Transform parent, Vector3 center, Vector3 size)
    {
        GameObject block = new GameObject("Block");
        block.transform.SetParent(parent, false);
        block.transform.position = center;
        block.AddComponent<BoxCollider>().size = size;

        float s = Mathf.Clamp(Mathf.Min(size.y, size.z), 2.5f, 7f);
        int ny = Mathf.Max(1, Mathf.CeilToInt(size.y / (s * 0.8f)));
        int nz = Mathf.Max(1, Mathf.CeilToInt(size.z / (s * 0.8f)));
        for (int j = 0; j < ny; j++)
        {
            for (int k = 0; k < nz; k++)
            {
                Vector3 p = center + new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    (ny == 1 ? 0f : -size.y / 2f + size.y * (j + 0.5f) / ny) + Random.Range(-0.4f, 0.4f),
                    (nz == 1 ? 0f : -size.z / 2f + size.z * (k + 0.5f) / nz) + Random.Range(-0.4f, 0.4f));
                float sc = s * Random.Range(1.05f, 1.3f);

                switch (ctx.look.wall)
                {
                    case WallStyle.Cloud:
                        SpawnCloud(ctx, block.transform, p, sc * 0.75f, false);
                        break;
                    case WallStyle.Crystal:
                    {
                        GameObject r = MeshObject(block.transform, "Asteroid", RockMesh(Random.Range(0, 6)), ctx.obstacleMat);
                        r.transform.position = p;
                        r.transform.rotation = Random.rotation;
                        r.transform.localScale = new Vector3(Mathf.Max(size.x * 1.6f, sc * 0.8f), sc, sc);
                        if (Random.value < 0.25f)
                        {
                            Material[] neon = NeonSet();
                            GameObject c = MeshObject(block.transform, "Crystal", CrystalMesh(), neon[Random.Range(0, neon.Length)]);
                            c.transform.position = p + Random.onUnitSphere * sc * 0.4f;
                            c.transform.rotation = Random.rotation;
                            c.transform.localScale = new Vector3(1f, 3f, 1f) * Random.Range(0.6f, 1f);
                        }
                        break;
                    }
                    default:
                    {
                        GameObject r = MeshObject(block.transform, "Rock", RockMesh(Random.Range(0, 6)), ctx.obstacleMat);
                        r.transform.position = p;
                        r.transform.rotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
                        r.transform.localScale = new Vector3(Mathf.Max(size.x * 1.6f, sc * 0.8f), sc, sc);
                        break;
                    }
                }
            }
        }
    }

    // 틈이 있는 자연 벽: (y0~y1: 바닥에서 높이, z0~z1) 자리만 비어 있음
    static void RockGate(Ctx ctx, float x, float y0, float y1, float z0, float z1)
    {
        Transform g = Group(ctx, $"Gate_{x:0}");
        float top = ctx.c.VerticalHeight(x);
        const float th = 2f;
        y0 = Mathf.Clamp(y0, 0f, top); y1 = Mathf.Clamp(y1, y0, top);
        z0 = Mathf.Clamp(z0, -W / 2f, W / 2f); z1 = Mathf.Clamp(z1, z0, W / 2f);
        float hy = y1 - y0;

        if (y0 > 0.01f) NaturalBlock(ctx, g, ctx.c.P(x, y0 / 2f, 0f), new Vector3(th, y0, W));
        if (y1 < top - 0.01f) NaturalBlock(ctx, g, ctx.c.P(x, (y1 + top) / 2f, 0f), new Vector3(th, top - y1, W));
        if (z0 > -W / 2f + 0.01f) NaturalBlock(ctx, g, ctx.c.P(x, (y0 + y1) / 2f, (-W / 2f + z0) / 2f), new Vector3(th, hy, z0 + W / 2f));
        if (z1 < W / 2f - 0.01f) NaturalBlock(ctx, g, ctx.c.P(x, (y0 + y1) / 2f, (z1 + W / 2f) / 2f), new Vector3(th, hy, W / 2f - z1));
    }

    static void CloudGate(Ctx ctx, float x, float y0, float y1, float z0, float z1) => RockGate(ctx, x, y0, y1, z0, z1);
    static void AsteroidGate(Ctx ctx, float x, float y0, float y1, float z0, float z1) => RockGate(ctx, x, y0, y1, z0, z1);

    // 바닥에서 위 경계까지 솟은 바위 기둥
    static void RockSpire(Ctx ctx, float x, float z, float size)
    {
        float top = ctx.c.VerticalHeight(x);
        NaturalBlock(ctx, Group(ctx, $"Spire_{x:0}"), ctx.c.P(x, top / 2f, z), new Vector3(size, top, size));
    }

    static void AsteroidSpire(Ctx ctx, float x, float z) => RockSpire(ctx, x, z, 4f);

    // 코스 안 나무(닿으면 죽음): 줄기 · 수관
    static Transform Tree(Ctx ctx, float x, float z, float scale)
    {
        Material bark = TexMat("Bark_Generated", C(0.39f, 0.24f, 0f), C(0.68f, 0.43f, 0.02f), 8f, 12f, 0.15f, 0.1f, 1.5f);
        Material leaf = Mat($"{ctx.look.name}_LeafA", ctx.look.leafA, 0.15f);
        Material leaf2 = Mat($"{ctx.look.name}_LeafB", ctx.look.leafB, 0.15f);
        GameObject tree = new GameObject($"Tree_{x:0}");
        tree.transform.SetParent(Group(ctx, "Trees"), false);
        tree.transform.position = ctx.c.P(x, 0f, z);
        tree.transform.localScale = Vector3.one * scale;

        GameObject trunk = Prim(PrimitiveType.Cylinder, tree.transform, "Trunk", bark, true);
        trunk.transform.localPosition = new Vector3(0f, 4f, 0f);
        trunk.transform.localScale = new Vector3(1.4f, 4f, 1.4f);

        for (int b = 0; b < 3; b++)
        {
            GameObject crown = MeshObject(tree.transform, "Crown", RockMesh(Random.Range(0, 6)), b == 2 ? leaf2 : leaf);
            crown.transform.localPosition = new Vector3(Random.Range(-0.7f, 0.7f), 9f + b * 1.8f, Random.Range(-0.7f, 0.7f));
            float s = 6.5f - b * 1.4f;
            crown.transform.localScale = new Vector3(s, s * 0.85f, s);
        }
        // 수관 충돌은 보이는 것보다 작게
        SphereCollider sc = tree.AddComponent<SphereCollider>();
        sc.center = new Vector3(0f, 10f, 0f);
        sc.radius = 2.6f;
        return tree.transform;
    }

    static void GrowTriggerFor(Ctx ctx, float x, Transform target, Vector3 multiplier)
    {
        GameObject trig = FullTrigger(ctx, $"GrowTrigger_{target.name}", x, 2f);
        GrowTrigger grow = trig.AddComponent<GrowTrigger>();
        SetField(grow, "targetObject", target.gameObject);
        SetField(grow, "scaleMultiplier", multiplier);
    }

    // 쓰러진 통나무: 복도를 가로질러 걸쳐 있음(위나 아래로 지나감)
    static void FallenLog(Ctx ctx, float x, float h)
    {
        Material bark = TexMat("Bark_Generated", C(0.39f, 0.24f, 0f), C(0.68f, 0.43f, 0.02f), 8f, 12f, 0.15f, 0.1f, 1.5f);
        GameObject log = Prim(PrimitiveType.Cylinder, Group(ctx, $"Log_{x:0}"), "FallenLog", bark, false);
        log.transform.position = ctx.c.P(x, h, 0f);
        log.transform.rotation = Quaternion.Euler(90f, 0f, Random.Range(-6f, 6f));
        log.transform.localScale = new Vector3(2.4f, W / 2f + 4f, 2.4f);
        CapsuleCollider cc = log.AddComponent<CapsuleCollider>();
        cc.direction = 1;
        cc.radius = 0.42f;
        cc.height = 2f;
    }

    // 바람 구역 + 바람 방향 줄무늬(파티클)
    static void Wind(Ctx ctx, float xa, float xb, Vector3 accel)
    {
        float len = xb - xa;
        float xm = (xa + xb) / 2f;
        GameObject trig = FullTrigger(ctx, $"Wind_{xa:0}", xm, len);
        SetField(trig.AddComponent<WindArea>(), "acceleration", accel);

        GameObject fx = new GameObject("WindStreaks");
        fx.transform.SetParent(trig.transform.parent, false);
        fx.transform.position = ctx.c.P(xm, H / 2f, 0f);
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = 1.6f;
        main.startSpeed = 0f;
        main.startSize = 0.25f;
        main.startColor = new Color(1f, 1f, 1f, 0.55f);
        main.maxParticles = 400;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.prewarm = true;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = Mathf.Clamp(len * 3f, 40f, 220f);
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(len, H - 2f, W - 2f);
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        Vector3 dir = accel.normalized * 9f;
        vel.x = dir.x; vel.y = dir.y; vel.z = dir.z;
        ParticleSystemRenderer r = fx.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.35f;
        r.lengthScale = 2f;
        r.sharedMaterial = ParticleMat;
    }

    // 굴러오는 바위: 복도를 가로질러 굴러가고, 사라졌다가 다시 굴러옴(산사태)
    static void RollingBoulder(Ctx ctx, float x, float dir, float phase)
    {
        GameObject b = MeshObject(Group(ctx, $"Boulder_{x:0}"), "RollingBoulder", RockMesh(Random.Range(0, 6)), ctx.obstacleMat);
        float size = 5f;
        b.transform.position = ctx.c.P(x, size * 0.45f, -dir * (W / 2f + 4f));
        b.transform.localScale = Vector3.one * size;
        b.AddComponent<SphereCollider>().radius = 0.4f;   // 보이는 것(0.5)보다 작게
        PathMover pm = b.AddComponent<PathMover>();
        SetField(pm, "travel", new Vector3(0f, 0f, dir * (W + 8f)));
        SetField(pm, "speed", ctx.CrossSpeed);
        SetField(pm, "interval", ctx.CrossWait);
        SetField(pm, "startDelay", phase * (W + 8f) / ctx.CrossSpeed);
        SetField(pm, "rollRadius", size * 0.45f);
    }

    // 낙석: 지나가면 앞쪽 절벽 위에서 바위가 차례로 떨어짐(흔들림으로 예고)
    static void Rockfall(Ctx ctx, float triggerX, float fromX, float toX, int count)
    {
        Transform g = Group(ctx, $"Rockfall_{triggerX:0}");
        GameObject trig = FullTrigger(ctx, "RockfallTrigger", triggerX, 2f);
        trig.transform.SetParent(g, true);
        DropOnTrigger drop = trig.AddComponent<DropOnTrigger>();
        List<Transform> rocks = new List<Transform>();
        float[] zs = { -7f, 4f, -2f, 8f, -9f, 1f, 6f, -4f };
        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Lerp(fromX, toX, count == 1 ? 0f : i / (float)(count - 1));
            GameObject r = MeshObject(g, $"FallingRock_{i}", RockMesh(Random.Range(0, 6)), ctx.obstacleMat);
            r.transform.position = ctx.c.P(x, ctx.c.VerticalHeight(x) - 1f, zs[i % zs.Length]);
            r.transform.localScale = Vector3.one * Random.Range(3f, 4.2f);
            r.AddComponent<SphereCollider>().radius = 0.4f;
            rocks.Add(r.transform);
        }
        SetField(drop, "targets", rocks.ToArray());
        SetField(drop, "interval", R(S.hailInterval, ctx.D01) * 1.5f);
    }

    // 우박: 구름 속에서 얼음 조각이 차례로 떨어짐
    static void Hail(Ctx ctx, float triggerX, float fromX, float toX, int count)
    {
        Transform g = Group(ctx, $"Hail_{triggerX:0}");
        GameObject trig = FullTrigger(ctx, "HailTrigger", triggerX, 2f);
        trig.transform.SetParent(g, true);
        DropOnTrigger drop = trig.AddComponent<DropOnTrigger>();
        Material ice = Mat("Hail_Ice", C(0.8f, 0.92f, 1f), 0.95f, C(0.2f, 0.35f, 0.5f));
        List<Transform> shards = new List<Transform>();
        float[] zs = { -8f, 4f, -2f, 9f, -6f, 1f, 7f, -10f, 3f };
        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Lerp(fromX, toX, count == 1 ? 0f : i / (float)(count - 1));
            GameObject s = MeshObject(g, $"Hailstone_{i}", CrystalMesh(), ice);
            s.transform.position = ctx.c.P(x, ctx.c.VerticalHeight(x) - 3f, zs[i % zs.Length]);
            s.transform.rotation = Quaternion.Euler(180f, Random.Range(0f, 360f), 0f);   // 뾰족한 쪽이 아래
            s.transform.localScale = new Vector3(2.2f, 5f, 2.2f);
            CapsuleCollider cc = s.AddComponent<CapsuleCollider>();
            cc.radius = 0.18f;
            cc.height = 0.9f;
            shards.Add(s.transform);
        }
        SetField(drop, "targets", shards.ToArray());
        SetField(drop, "interval", R(S.hailInterval, ctx.D01));
    }

    // ───────────────────────── 새 · 우주 생물 ─────────────────────────

    // 가로지르는 떼: crossing=true면 왼쪽 → 오른쪽, false면 반대
    static void Flock(Ctx ctx, BirdKind kind, float x, float h, bool leftToRight, int count)
    {
        float dir = leftToRight ? 1f : -1f;
        Transform flock = new GameObject($"Flock_{kind}_{x:0}").transform;
        flock.SetParent(Group(ctx, "Flocks"), false);
        flock.position = ctx.c.P(x, h, -dir * (W / 2f + 8f));
        for (int i = 0; i < count; i++)
        {
            // V자 대형(진행 방향이 +Z인 로컬 기준)
            float row = (i + 1) / 2;
            float side = i % 2 == 0 ? 1f : -1f;
            Bird(flock, new Vector3(side * row * 2.6f, Random.Range(-0.6f, 0.6f), -row * 2.8f), kind, 1f, true);
        }
        PathMover pm = flock.gameObject.AddComponent<PathMover>();
        SetField(pm, "travel", new Vector3(0f, 0f, dir * (W + 16f)));
        SetField(pm, "speed", ctx.CrossSpeed * (kind == BirdKind.Ufo ? 1.3f : 1f));
        SetField(pm, "interval", ctx.CrossWait);
        SetField(pm, "startDelay", Random.Range(0f, 2f));
        SetField(pm, "faceDirection", true);
    }

    // 정면으로 날아오는 떼(앞에서 닭 쪽으로): 레인을 비워 둬 피할 길이 있음
    static void HeadOnFlock(Ctx ctx, BirdKind kind, float x, float h, int count)
    {
        Transform flock = new GameObject($"HeadOn_{kind}_{x:0}").transform;
        flock.SetParent(Group(ctx, "Flocks"), false);
        Vector3 from = ctx.c.P(x + 45f, h, 0f);
        Vector3 to = ctx.c.P(x - 15f, h, 0f);
        flock.position = from;
        float[] zs = { -6f, 6f, 0f, -9f, 9f };
        float[] ys = { 0f, 2f, -3f, 4f, -1f };
        for (int i = 0; i < count; i++)
        {
            Bird(flock, new Vector3(zs[i % zs.Length], ys[i % ys.Length], -i * 3f), kind, 1f, true);
        }
        PathMover pm = flock.gameObject.AddComponent<PathMover>();
        SetField(pm, "travel", to - from);
        SetField(pm, "speed", ctx.CrossSpeed * 0.9f);
        SetField(pm, "interval", ctx.CrossWait + 1f);
        SetField(pm, "faceDirection", true);
    }

    // 장식용 먼 새 떼(충돌 없음)
    static void DecorFlock(Ctx ctx, BirdKind kind, float x, float h, float z)
    {
        Transform flock = new GameObject($"DecorFlock_{x:0}").transform;
        flock.SetParent(Group(ctx, "Flocks"), false);
        flock.position = ctx.c.P(x, h, z);
        for (int i = 0; i < 7; i++)
        {
            float row = (i + 1) / 2;
            float side = i % 2 == 0 ? 1f : -1f;
            Bird(flock, new Vector3(side * row * 3f, 0f, -row * 3f), kind, 1.2f, false);
        }
        PathMover pm = flock.gameObject.AddComponent<PathMover>();
        SetField(pm, "travel", new Vector3(80f, 6f, 0f));
        SetField(pm, "speed", 6f);
        SetField(pm, "interval", 4f);
        SetField(pm, "faceDirection", true);
    }

    static GameObject Bird(Transform parent, Vector3 localPos, BirdKind kind, float size, bool hazard)
    {
        GameObject bird = new GameObject(kind.ToString());
        bird.transform.SetParent(parent, false);
        bird.transform.localPosition = localPos;
        bird.transform.localScale = Vector3.one * size;

        Color body, wing, head, beak;
        switch (kind)
        {
            case BirdKind.Seagull: body = C(1f, 1f, 1f); wing = C(0.62f, 0.66f, 0.72f); head = C(1f, 1f, 1f); beak = C(0.94f, 0.9f, 0f); break;
            case BirdKind.SpaceChicken: body = C(1f, 1f, 1f); wing = C(1f, 0.89f, 0.17f); head = C(1f, 1f, 1f); beak = C(1f, 0.55f, 0f); break;
            case BirdKind.Eagle: body = C(0.39f, 0.24f, 0f); wing = C(0.28f, 0.18f, 0f); head = C(0.95f, 0.95f, 0.9f); beak = C(0.94f, 0.75f, 0f); break;
            default: body = C(0.8f, 0.82f, 0.88f); wing = C(0.6f, 0.62f, 0.7f); head = C(0.48f, 0.99f, 0.2f); beak = C(0.48f, 0.99f, 0.2f); break;
        }

        if (kind == BirdKind.Ufo)
        {
            GameObject saucer = Prim(PrimitiveType.Sphere, bird.transform, "Saucer", Mat("Ufo_Metal", body, 0.9f), false);
            saucer.transform.localScale = new Vector3(4f, 0.9f, 4f);
            GameObject dome = Prim(PrimitiveType.Sphere, bird.transform, "Dome", NeonMat("Ufo_Dome", head, 1.2f), false);
            dome.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            dome.transform.localScale = new Vector3(1.8f, 1.4f, 1.8f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                GameObject l = Prim(PrimitiveType.Sphere, bird.transform, "Light", NeonMat(i % 2 == 0 ? "Ufo_LightPink" : "Ufo_LightYellow", i % 2 == 0 ? C(0.99f, 0.2f, 0.46f) : C(0.94f, 0.9f, 0f), 2.5f), false);
                l.transform.localPosition = new Vector3(Mathf.Cos(a) * 1.8f, -0.05f, Mathf.Sin(a) * 1.8f);
                l.transform.localScale = Vector3.one * 0.35f;
            }
            if (hazard) bird.AddComponent<SphereCollider>().radius = 1.6f;
            Spinner sp = bird.AddComponent<Spinner>();
            SetField(sp, "xAngle", 0f); SetField(sp, "yAngle", 90f); SetField(sp, "zAngle", 0f);
            return bird;
        }

        GameObject b = MeshObject(bird.transform, "Body", RockMesh(1), Mat($"Bird_{kind}_Body", body, 0.3f));
        b.transform.localScale = new Vector3(1.1f, 0.9f, 2f);
        GameObject h = Prim(PrimitiveType.Sphere, bird.transform, "Head", Mat($"Bird_{kind}_Head", head, 0.3f), false);
        h.transform.localPosition = new Vector3(0f, 0.4f, 1.05f);
        h.transform.localScale = Vector3.one * 0.75f;
        GameObject bk = Prim(PrimitiveType.Cube, bird.transform, "Beak", Mat($"Bird_{kind}_Beak", beak, 0.4f), false);
        bk.transform.localPosition = new Vector3(0f, 0.35f, 1.55f);
        bk.transform.localScale = new Vector3(0.2f, 0.16f, 0.45f);
        foreach (float ex in new[] { -0.22f, 0.22f })
        {
            GameObject eye = Prim(PrimitiveType.Sphere, bird.transform, "Eye", Mat("Bird_Eye", C(0.05f, 0.05f, 0.08f), 0.8f), false);
            eye.transform.localPosition = new Vector3(ex, 0.55f, 1.32f);
            eye.transform.localScale = Vector3.one * 0.14f;
        }
        if (kind == BirdKind.SpaceChicken)
        {
            GameObject comb = Prim(PrimitiveType.Sphere, bird.transform, "Comb", Mat("Bird_Comb", C(0.9f, 0.12f, 0.1f), 0.4f), false);
            comb.transform.localPosition = new Vector3(0f, 0.85f, 1.05f);
            comb.transform.localScale = new Vector3(0.18f, 0.35f, 0.45f);
        }

        Material wingMat = Mat($"Bird_{kind}_Wing", wing, 0.3f);
        foreach (float side in new[] { -1f, 1f })
        {
            GameObject pivot = new GameObject(side < 0 ? "WingPivot_L" : "WingPivot_R");
            pivot.transform.SetParent(bird.transform, false);
            pivot.transform.localPosition = new Vector3(side * 0.45f, 0.25f, 0.1f);
            GameObject w = Prim(PrimitiveType.Cube, pivot.transform, "Wing", wingMat, false);
            w.transform.localPosition = new Vector3(side * 1.25f, 0f, 0f);
            w.transform.localScale = new Vector3(2.5f, 0.08f, 1f);
        }
        bird.AddComponent<BirdFlap>();

        if (hazard) bird.AddComponent<SphereCollider>().radius = 1.0f;   // 날개 끝까지는 판정 없음
        return bird;
    }

    // 외계 생물: 초록 몸에 눈 달린 촉수가 둥둥 떠다님(위아래로 출렁)
    static void Alien(Ctx ctx, float x, float h, float z)
    {
        Transform g = Group(ctx, $"Alien_{x:0}");
        GameObject alien = new GameObject("Alien");
        alien.transform.SetParent(g, false);
        alien.transform.position = ctx.c.P(x, h, z);
        Material skin = NeonMat("Alien_Skin", C(0.48f, 0.99f, 0.2f), 0.6f);
        GameObject body = MeshObject(alien.transform, "Body", RockMesh(4), skin);
        body.transform.localScale = new Vector3(4f, 3.4f, 4f);
        foreach (float ex in new[] { -0.9f, 0.9f })
        {
            GameObject stalk = Prim(PrimitiveType.Cylinder, alien.transform, "Stalk", skin, false);
            stalk.transform.localPosition = new Vector3(ex, 2.2f, -0.4f);
            stalk.transform.localScale = new Vector3(0.25f, 0.9f, 0.25f);
            GameObject eye = Prim(PrimitiveType.Sphere, alien.transform, "Eye", Mat("Alien_Eye", C(1f, 1f, 1f), 0.9f), false);
            eye.transform.localPosition = new Vector3(ex, 3.1f, -0.4f);
            eye.transform.localScale = Vector3.one * 0.9f;
            GameObject pupil = Prim(PrimitiveType.Sphere, eye.transform, "Pupil", Mat("Bird_Eye", C(0.05f, 0.05f, 0.08f), 0.8f), false);
            pupil.transform.localPosition = new Vector3(0f, 0f, -0.4f);
            pupil.transform.localScale = Vector3.one * 0.45f;
        }
        alien.AddComponent<SphereCollider>().radius = 1.5f;
        Oscillator o = alien.AddComponent<Oscillator>();
        SetField(o, "offset", new Vector3(0f, -8f, -z * 1.2f));
        SetField(o, "period", 4f);
        SetField(o, "phase", Random.value);
    }

    // ───────────────────────── 구름 ─────────────────────────

    static void DriftingCloud(Ctx ctx, float x, float h, float dir, float phase)
    {
        Transform cloud = CloudCluster(ctx, x, h, -dir * (W / 2f + 6f), 1f, true);
        PathMover pm = cloud.gameObject.AddComponent<PathMover>();
        float dist = W + 12f;
        SetField(pm, "travel", new Vector3(0f, 0f, dir * dist));
        SetField(pm, "speed", ctx.Drift);
        SetField(pm, "interval", 0.5f);
        SetField(pm, "startDelay", phase * dist / ctx.Drift);
        SetField(pm, "fade", 0.15f);
    }

    static void RisingCloud(Ctx ctx, float triggerX, float cloudX, float z, float startH, float rise)
    {
        Transform cloud = CloudCluster(ctx, cloudX, startH, z, 1f, true);
        CloudTrigger(ctx, triggerX, cloud, new Vector3(0f, rise, 0f));
    }

    static void DescendingCloud(Ctx ctx, float triggerX, float cloudX, float z, float drop)
    {
        Transform cloud = CloudCluster(ctx, cloudX, ctx.c.VerticalHeight(cloudX) - 2f, z, 1f, true);
        CloudTrigger(ctx, triggerX, cloud, new Vector3(0f, drop, 0f));
    }

    static void CloudTrigger(Ctx ctx, float x, Transform cloud, Vector3 move)
    {
        GameObject trig = FullTrigger(ctx, $"CloudTrigger_{cloud.name}", x, 2f);
        RisingCloudTrigger rc = trig.AddComponent<RisingCloudTrigger>();
        SetField(rc, "targetObject", cloud.gameObject);
        SetField(rc, "positionPlus", move);
        SetField(rc, "targetTime", R(S.cloudMoveTime, ctx.D01));
    }

    static Transform CloudCluster(Ctx ctx, float x, float heightAboveFloor, float z, float scale, bool hazard)
    {
        GameObject group = new GameObject($"Cloud_{x:0}_{z:0}");
        group.transform.SetParent(Group(ctx, "Clouds"), false);
        group.transform.position = ctx.c.P(x, heightAboveFloor, z);
        SpawnCloud(ctx, group.transform, group.transform.position, S.cloudScale * scale, hazard);
        return group.transform;
    }

    // 구름 바다: 바닥을 구름으로 덮음(= 구름 위로 올라왔다)
    static void CloudSea(Ctx ctx)
    {
        for (float x = 0f; x < ctx.c.Length; x += 9f) CloudFloorRow(ctx, x);
    }

    static void CloudFloorRow(Ctx ctx, float x)
    {
        // 바닥보다 살짝 아래에 깔아 위로 조금만 솟게(시야를 가리지 않게)
        foreach (float z in new[] { -8f, 0f, 8f })
        {
            CloudCluster(ctx, x + Random.Range(-2f, 2f), -3.5f, z + Random.Range(-1.5f, 1.5f), 0.85f, false);
        }
    }

    // 바람 고리(꼬리바람): 소용돌이치는 바람 입자 고리. 지나가면 잠깐 빨라짐
    static void WindRing(Ctx ctx, float x, float h, float z)
    {
        Transform g = Group(ctx, $"WindRing_{x:0}");
        GameObject trig = new GameObject("WindRing");
        trig.transform.SetParent(g, false);
        trig.transform.position = ctx.c.P(x, h, z);
        trig.tag = "Trigger";
        BoxCollider box = trig.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2f, 6f, 6f);
        BoostRing ring = trig.AddComponent<BoostRing>();
        SetField(ring, "boostSpeed", S.boostRingSpeed);

        GameObject visual = new GameObject("WindSwirl");
        visual.transform.SetParent(trig.transform, false);
        ParticleSystem ps = visual.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = 1.2f;
        main.startSpeed = 0.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
        main.startColor = new ParticleSystem.MinMaxGradient(C(0.8f, 0.95f, 1f), C(1f, 0.95f, 0.7f));
        main.maxParticles = 200;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 90f;
        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Donut;
        sh.radius = 3.3f;
        sh.donutRadius = 0.35f;
        sh.rotation = new Vector3(0f, 90f, 0f);
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalX = 2.5f;
        visual.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat;
        SetField(ring, "visual", visual.transform);
    }

    // 상승 기류(열기둥): 바닥 구름에서 따뜻한 공기가 솟아 닭을 위로 올려 줌
    static void Thermal(Ctx ctx, float x, float z)
    {
        Transform g = Group(ctx, $"Thermal_{x:0}");
        GameObject trig = Box(g, "Thermal", ctx.c.P(x, 3f, z), new Vector3(6f, 6f, 6f), 0f, ctx.triggerMat);
        trig.GetComponent<Collider>().isTrigger = true;
        trig.GetComponent<Renderer>().enabled = false;
        trig.tag = "Trigger";
        BouncePad pad = trig.AddComponent<BouncePad>();
        SetField(pad, "launchSpeed", S.bounceLaunchSpeed);

        Transform puff = CloudCluster(ctx, x, 0.5f, z, 0.45f, false);
        puff.SetParent(g, true);
        SetField(pad, "visual", puff);

        GameObject fx = new GameObject("WarmAir");
        fx.transform.SetParent(g, false);
        fx.transform.position = ctx.c.P(x, 0.5f, z);
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = 1.8f;
        main.startSpeed = 9f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startColor = new Color(1f, 0.95f, 0.85f, 0.6f);
        main.prewarm = true;
        main.maxParticles = 200;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 45f;
        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = 2.5f;
        sh.rotation = new Vector3(-90f, 0f, 0f);
        fx.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat;
    }

    // ───────────────────────── 폭풍 · 우주 ─────────────────────────

    static void SyncGate(Ctx ctx, float x)
    {
        FullTrigger(ctx, $"HazardSyncGate_{x:0}", x, 1f).AddComponent<HazardSyncGate>();
    }

    // 번개 한 줄: 위에서 아래로 내리치는 지그재그 번개 3개 + 가로 번개 1개(같은 박자로 깜빡임)
    static void LightningRow(Ctx ctx, float x, float syncSpeed, float phase)
    {
        Transform g = Group(ctx, $"Lightning_{x:0}");
        float top = ctx.c.VerticalHeight(x);
        Material bolt = Mat("Lightning_Bolt", C(1f, 0.97f, 0.85f), 0.9f, C(1f, 0.92f, 0.65f) * 2.2f);
        List<Transform> bolts = new List<Transform>();
        foreach (float z in new[] { -8f, 0f, 8f })
        {
            List<Vector3> pts = new List<Vector3>();
            int n = 6;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                pts.Add(ctx.c.P(x, top * (1f - t), z + (i == 0 || i == n ? 0f : Random.Range(-1.8f, 1.8f))));
            }
            bolts.Add(ZigZag(g, "Bolt", pts, bolt, 0.45f));
        }
        {
            float hh = Random.Range(6f, top - 6f);
            List<Vector3> pts = new List<Vector3>();
            int n = 6;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                pts.Add(ctx.c.P(x, hh + (i == 0 || i == n ? 0f : Random.Range(-1.5f, 1.5f)), Mathf.Lerp(-W / 2f, W / 2f, t)));
            }
            bolts.Add(ZigZag(g, "SheetLightning", pts, bolt, 0.4f));
        }

        foreach (Transform b in bolts)
        {
            TimedHazard th = b.gameObject.AddComponent<TimedHazard>();
            SetField(th, "offTime", syncSpeed > 0f ? 1.0f : R(S.lightningOff, ctx.D01));
            SetField(th, "warnTime", 0.4f);
            SetField(th, "onTime", syncSpeed > 0f ? 0.8f : R(S.lightningOn, ctx.D01));
            SetField(th, "syncSpeed", syncSpeed);
            SetField(th, "phaseOffset", phase + Random.Range(0f, 0.3f));
        }
    }

    // 점들을 잇는 굵은 선(조각마다 트리거 충돌)
    static Transform ZigZag(Transform parent, string name, List<Vector3> pts, Material mat, float thickness)
    {
        Transform g = new GameObject(name).transform;
        g.SetParent(parent, false);
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            Vector3 a = pts[i], b = pts[i + 1];
            GameObject seg = Prim(PrimitiveType.Cube, g, "Segment", mat, true);
            seg.transform.position = (a + b) / 2f;
            seg.transform.rotation = Quaternion.LookRotation(b - a);
            seg.transform.localScale = new Vector3(thickness, thickness, Vector3.Distance(a, b) + thickness);
            seg.GetComponent<Collider>().isTrigger = true;
        }
        return g;
    }

    // 펄서 빛줄기(우주판 번개): 양옆 수정에서 가로로 쏘는 빛
    static void PulsarRow(Ctx ctx, float x, float phase)
    {
        Transform g = Group(ctx, $"Pulsar_{x:0}");
        float hh = Random.Range(5f, ctx.c.VerticalHeight(x) - 5f);
        Material beam = NeonMat("Pulsar_Beam", C(0.45f, 0.2f, 0.99f), 2.5f);
        foreach (float side in new[] { -1f, 1f })
        {
            GameObject c = MeshObject(g, "PulsarCrystal", CrystalMesh(), NeonMat("Crystal_NeonPurple", C(0.45f, 0.2f, 0.99f), 2.2f));
            c.transform.position = ctx.c.P(x, hh, side * (W / 2f + 1f));
            c.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            c.transform.localScale = new Vector3(2f, 4f, 2f);
        }
        List<Vector3> pts = new List<Vector3> { ctx.c.P(x, hh, -W / 2f), ctx.c.P(x, hh, W / 2f) };
        Transform b = ZigZag(g, "PulsarBeam", pts, beam, 0.7f);
        TimedHazard th = b.gameObject.AddComponent<TimedHazard>();
        SetField(th, "offTime", R(S.lightningOff, ctx.D01));
        SetField(th, "warnTime", 0.45f);
        SetField(th, "onTime", R(S.lightningOn, ctx.D01));
        SetField(th, "phaseOffset", phase);
        SetField(th, "onColor", C(0.75f, 0.45f, 1f));
        SetField(th, "warnColor", C(0.99f, 0.2f, 0.46f));
        SetField(th, "offColor", C(0.2f, 0.12f, 0.35f));
    }

    // 중력 이상 지대: 보라 소용돌이 + 수정 고리. 지나가면 세상이 뒤집힘(reverse=false면 원래대로)
    static void GravityAnomaly(Ctx ctx, float x, bool reverse)
    {
        Transform g = Group(ctx, $"GravityAnomaly_{x:0}");
        GameObject trig = FullTrigger(ctx, reverse ? "ReverseGravityTrigger" : "RestoreGravityTrigger", x, 2f);
        trig.transform.SetParent(g, true);
        SetField(trig.AddComponent<ReverseGravity>(), "reverse", reverse);

        float top = ctx.c.VerticalHeight(x);
        Material[] neon = NeonSet();
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16f;
            GameObject c = MeshObject(g, "AnomalyCrystal", CrystalMesh(), neon[i % 2 == 0 ? 1 : 0]);
            c.transform.position = ctx.c.P(x, top / 2f + Mathf.Sin(a) * (top / 2f - 1.5f), Mathf.Cos(a) * (W / 2f - 1f));
            c.transform.rotation = Quaternion.LookRotation(Vector3.right, new Vector3(0f, Mathf.Sin(a), Mathf.Cos(a)));
            c.transform.localScale = new Vector3(1.2f, 3.2f, 1.2f);
        }

        GameObject fx = new GameObject("Swirl");
        fx.transform.SetParent(g, false);
        fx.transform.position = ctx.c.P(x, top / 2f, 0f);
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = 2.5f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        main.startColor = new ParticleSystem.MinMaxGradient(C(0.6f, 0.35f, 1f), C(0.99f, 0.4f, 0.7f));
        main.prewarm = true;
        main.maxParticles = 500;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 160f;
        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = W / 2f - 1f;
        sh.rotation = new Vector3(0f, 90f, 0f);
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalX = 1.2f;
        vel.radial = -2f;
        fx.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat;

        Sign(ctx, x - 12f, H * 0.7f, 0f, reverse ? "GRAVITY FLIP AHEAD" : "BACK TO NORMAL", 2.6f, g);
    }

    // 혜성: 꼬리를 끌며 비스듬히 가로질러 날아감
    static void Comet(Ctx ctx, float x, float h, float dir)
    {
        Transform g = Group(ctx, $"Comet_{x:0}");
        GameObject comet = MeshObject(g, "Comet", RockMesh(5), ctx.obstacleMat);
        comet.transform.position = ctx.c.P(x + 10f, h + 6f, -dir * (W / 2f + 10f));
        comet.transform.localScale = Vector3.one * 3.2f;
        comet.AddComponent<SphereCollider>().radius = 0.4f;
        PathMover pm = comet.AddComponent<PathMover>();
        SetField(pm, "travel", new Vector3(-20f, -12f, dir * (W + 20f)));
        SetField(pm, "speed", ctx.CrossSpeed * 1.4f);
        SetField(pm, "interval", ctx.CrossWait + 0.5f);
        SetField(pm, "startDelay", Random.Range(0f, 1.5f));

        GameObject tail = new GameObject("Tail");
        tail.transform.SetParent(comet.transform, false);
        ParticleSystem ps = tail.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = 0.9f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
        main.startColor = new ParticleSystem.MinMaxGradient(C(0.6f, 0.85f, 1f), C(1f, 0.6f, 0.9f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.rateOverDistance = 6f;
        tail.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat;
    }

    // 소행성 띠: 여러 소행성이 천천히 가로질러 흘러감
    static void AsteroidBelt(Ctx ctx, float x, int count)
    {
        Transform g = Group(ctx, $"AsteroidBelt_{x:0}");
        for (int i = 0; i < count; i++)
        {
            float dir = i % 2 == 0 ? 1f : -1f;
            GameObject a = MeshObject(g, "DriftingAsteroid", RockMesh(Random.Range(0, 6)), ctx.obstacleMat);
            float size = Random.Range(3.5f, 5.5f);
            a.transform.position = ctx.c.P(x + i * 4f, 4f + i * (H - 8f) / Mathf.Max(1, count - 1), -dir * (W / 2f + 6f));
            a.transform.localScale = Vector3.one * size;
            a.transform.rotation = Random.rotation;
            a.AddComponent<SphereCollider>().radius = 0.4f;
            PathMover pm = a.AddComponent<PathMover>();
            float dist = W + 12f;
            SetField(pm, "travel", new Vector3(0f, 0f, dir * dist));
            SetField(pm, "speed", ctx.Drift * 1.2f);
            SetField(pm, "interval", 0.3f);
            SetField(pm, "startDelay", i * 0.9f);
            SetField(pm, "rollRadius", size * 0.6f);
        }
    }

    // 오로라: 시즌1 마지막 구간의 네온(분홍 · 노랑 · 초록)을 우주 오로라 커튼으로
    static void Aurora(Ctx ctx, float xa, float xb)
    {
        Transform g = Group(ctx, "Aurora");
        Material[] mats =
        {
            NeonMat("Aurora_Pink", C(0.99f, 0.2f, 0.46f), 2.6f),
            NeonMat("Aurora_Yellow", C(0.94f, 0.9f, 0f), 2.6f),
            NeonMat("Aurora_Green", C(0.48f, 0.99f, 0.2f), 2.6f)
        };
        int i = 0;
        for (float x = xa; x < xb; x += 3.5f, i++)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                float wave = Mathf.Sin(x * 0.15f) * 1.5f;
                GameObject strip = Prim(PrimitiveType.Cube, g, "AuroraStrip", mats[i % mats.Length], false);
                strip.transform.position = ctx.c.P(x, H * 0.55f + wave, side * (W / 2f + 1.5f + Mathf.Cos(x * 0.2f)));
                strip.transform.localScale = new Vector3(0.35f, H * 0.7f + Random.Range(-3f, 3f), 0.15f);
                Oscillator o = strip.AddComponent<Oscillator>();
                SetField(o, "offset", new Vector3(0f, 2.5f, 0f));
                SetField(o, "period", 3f);
                SetField(o, "phase", (x * 0.03f) % 1f);
            }
        }
    }

    static void StarField(Ctx ctx)
    {
        GameObject fx = new GameObject("StarField");
        fx.transform.SetParent(ctx.root, false);
        float len = ctx.c.Length;
        fx.transform.position = ctx.c.P(len / 2f, H / 2f, 0f);
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.duration = 5f;
        main.startLifetime = 100000f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.6f);
        main.startColor = new ParticleSystem.MinMaxGradient(C(1f, 1f, 1f), C(0.94f, 0.9f, 0.4f));
        main.maxParticles = 1500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1500) });
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(len + 60f, (ctx.c.FloorY(len) - ctx.c.FloorY(0f)) + H + 80f, 160f);
        fx.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat;
    }

    static GameObject Sign(Ctx ctx, float x, float h, float z, string text, float size, Transform parent = null)
    {
        GameObject go = new GameObject($"Sign_{text}");
        go.transform.SetParent(parent != null ? parent : Group(ctx, "Signs"), false);
        go.transform.position = ctx.c.P(x, h, z);
        go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        go.tag = "Text";
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size * 10f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.rectTransform.sizeDelta = new Vector2(W * 1.6f, 8f);
        return go;
    }
}
