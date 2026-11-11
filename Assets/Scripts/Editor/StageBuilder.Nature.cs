using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

// 스테이지(구간) 내용과 자연물 기믹.
// 원칙: 그 높이에 있을 법한 자연물만(인공물 없음). 우주는 상상력(행성 · 외계인 · 우주 닭 · UFO).
// 위치는 모두 '구간 안 거리 x · 옆 z(+는 길의 로컬 Z쪽) · 바닥에서 높이 h'로 씁니다. 길이 꺾여도 그대로 따라갑니다.
// 충돌 판정은 보이는 모양보다 작게(스치기만 해도 죽는 억울함 줄이기).
public static partial class StageBuilder
{
    enum BirdKind { Eagle, Seagull, SpaceChicken, Ufo }

    // ───────────────────────── 스테이지 정의 ─────────────────────────
    // 난이도: 1→6 상승, 7 · 8은 숨 고르기(어려워 보이지만 한 길만 따라가면 다 피해짐), 9 · 10은 6보다 높게.
    // 폭 · 높이: 넓은 곳은 자유 비행, 구름 구간(5 · 6)처럼 좁은 곳은 좁음 자체가 난이도(시즌1 구름 스테이지).
    // Seg(길이, 오르막°, 좌우 꺾임°): 시즌1처럼 구간마다 왼쪽 · 오른쪽으로 꺾임.

    static List<StageDef> DefineStages()
    {
        List<StageDef> list = new List<StageDef>();

        list.Add(new StageDef
        {
            number = 1, title = "First Flap", hint = "SPACE = FLAP   W = GO (hold to speed up)   SHIFT (hold) = DASH   S = STOP   MOUSE = TURN", difficulty = 1f, zone = "Meadow",
            width = 30f, height = 45f,
            content = ctx =>
            {
                // 속도 1.5배에 맞춰 구간 길이 ×1.3, 장애물 간격도 넓힘(215m → 280m)
                Sign(ctx, 12f, 12f, 0f, "SPACE : FLAP", 3f);
                Sign(ctx, 28f, 8f, 0f, "HOLD W : FASTER", 2.4f);
                RockGate(ctx, 58f, 2f, 2f + ctx.Hole, -12f, 12f);
                Sign(ctx, 92f, 11f, 0f, "HOLD SHIFT : DASH", 2.6f);
                RockGate(ctx, 145f, 13f, 13f + ctx.Hole, -12f, 12f);
                Sign(ctx, 162f, 15f, 0f, "MOUSE : TURN", 3f);
                RockGate(ctx, 195f, 3f, 25f, -14f, -14f + ctx.Hole);
                RockGate(ctx, 240f, 3f, 25f, 14f - ctx.Hole, 14f);
            }
        }.Seg(40, 0).Seg(80, 12, 12).Seg(50, 0, -22).Seg(65, 18).Seg(45, 0, 14));

        list.Add(new StageDef
        {
            number = 2, title = "Growing Forest", hint = "Trees grow when you pass by", difficulty = 2f, zone = "Forest",
            width = 30f, height = 50f,
            content = ctx =>
            {
                float[] zs = { -7f, 6f, -3f, 8f, -9f, 2f };
                for (int i = 0; i < zs.Length; i++) Tree(ctx, 30f + i * 11f, zs[i], 1.1f);
                Sign(ctx, 108f, 15f, 0f, "WHAT A NICE TREE!", 2.6f);
                foreach (Transform t in new[] { Tree(ctx, 126f, -6f, 1f), Tree(ctx, 134f, 7f, 1f), Tree(ctx, 142f, -1f, 1f) })
                {
                    GrowTriggerFor(ctx, 116f, t, new Vector3(1.6f, 2.4f, 1.6f));
                }
                float[] zs2 = { 7f, -7f, 0f, -10f, 10f, -3f, 5f };
                for (int i = 0; i < zs2.Length; i++) Tree(ctx, 156f + i * 8.5f, zs2[i], 1.2f);
                FallenLog(ctx, 222f, 9f);
            }
        }.Seg(20, 0).Seg(80, 15, 20).Seg(30, 0).Seg(80, 20, -30).Seg(30, 0, 10));

        list.Add(new StageDef
        {
            number = 3, title = "Windy Cliffs", hint = "Ride the wind", difficulty = 3f, zone = "Cliffs",
            width = 34f, height = 55f,
            content = ctx =>
            {
                // 높이가 55m로 늘어난 만큼 '위로 넘어가면 끝'이 되지 않게 높은 곳에도 장애물을 둠.
                // 상승풍을 타고 오르다 → 높이가 다른 틈을 차례로 통과(아래 → 위 → 가운데)
                // (플레이 피드백: 상승풍이 너무 세서 조절이 안 됨 → 상승풍만 30% 약하게, 장애물 간격 ×1.3 · 구간 길이 255 → 333m)
                const float updraft = 0.7f;
                Wind(ctx, 29f, 101f, new Vector3(0f, ctx.Lift * updraft, 0f), false);
                RockGate(ctx, 52f, 6f, 6f + ctx.Hole + 2f, -15f, 15f);
                RockGate(ctx, 81f, 24f, 24f + ctx.Hole + 2f, -15f, 15f);
                Flock(ctx, BirdKind.Eagle, 107f, 14f, true, 3);
                Flock(ctx, BirdKind.Eagle, 117f, 34f, false, 3);
                RockGate(ctx, 130f, 14f, 14f + ctx.Hole + 2f, -10f, 10f);
                Wind(ctx, 140f, 215f, new Vector3(0f, ctx.Lift * 0.9f * updraft, 0f), false);
                // 바위 기둥 숲 + 기둥 사이 높은 곳의 걸린 바위 턱(위로만 피하면 막힘)
                RockSpire(ctx, 163f, -7f, 4f); RockSpire(ctx, 182f, 7f, 4f); RockSpire(ctx, 202f, -2f, 4f);
                NaturalBlock(ctx, Group(ctx, "Ledge_172"), 172f, 30f, 3f, new Vector3(3f, 5f, 22f));
                NaturalBlock(ctx, Group(ctx, "Ledge_192"), 192f, 20f, -4f, new Vector3(3f, 5f, 22f));
                NaturalBlock(ctx, Group(ctx, "Ledge_211"), 211f, 38f, 0f, new Vector3(3f, 6f, 34f));
                Wind(ctx, 224f, 267f, new Vector3(0f, 0f, ctx.CrossWind), true);
                RockSpire(ctx, 241f, 9f, 4f); RockSpire(ctx, 257f, 2f, 4f);
                RockSpire(ctx, 248f, -10f, 4f);
                HeadOnFlock(ctx, BirdKind.Eagle, 262f, 28f, 3);
                Wind(ctx, 276f, 306f, new Vector3(0f, -ctx.Lift, 0f), false);
                RockGate(ctx, 296f, 2f, 2f + ctx.Hole, -14f, 14f);
            }
        }.Seg(26, 0).Seg(78, 28).Seg(33, 0, -20).Seg(78, 30).Seg(33, 0, 25).Seg(52, 15).Seg(33, 0, -5));

        list.Add(new StageDef
        {
            number = 4, title = "Rockfall Ridge", hint = "Boulders roll, eagles cross", difficulty = 4f, zone = "Mountain",
            width = 30f, height = 50f,
            content = ctx =>
            {
                // 굴러오는 바위 5개(좌우 번갈아, 박자를 엇갈리게)
                RollingBoulder(ctx, 34f, 1f, 0f); RollingBoulder(ctx, 46f, -1f, 0.35f); RollingBoulder(ctx, 58f, 1f, 0.7f);
                RollingBoulder(ctx, 70f, -1f, 0.15f); RollingBoulder(ctx, 82f, 1f, 0.55f);
                // 낙석 지대 1: 빠르게 떨어지고 계속 쏟아짐(14개)
                Rockfall(ctx, 86f, 96f, 142f, 14);
                // 높이가 다른 독수리 떼 3무리 + 마주 오는 떼
                Flock(ctx, BirdKind.Eagle, 150f, 10f, true, 4);
                Flock(ctx, BirdKind.Eagle, 162f, 26f, false, 4);
                Flock(ctx, BirdKind.Eagle, 174f, 18f, true, 5);
                HeadOnFlock(ctx, BirdKind.Eagle, 188f, 16f, 3);
                // 낙석 지대 2 + 아래에서 굴러오는 바위(위아래 동시에)
                Rockfall(ctx, 192f, 200f, 226f, 9);
                RollingBoulder(ctx, 205f, -1f, 0.2f); RollingBoulder(ctx, 216f, 1f, 0.7f);
                // 끝 무렵: 구름이 바닥까지 내려와 있음 = 구름 높이에 닿음(시즌1 구성)
                for (float x = 228f; x < ctx.c.Length; x += 9f) CloudFloorRow(ctx, x);
                CloudCluster(ctx, 245f, 18f, -7f, 0.8f, false);
                CloudCluster(ctx, 258f, 22f, 6f, 0.9f, false);
            }
        }.Seg(20, 0).Seg(70, 20, 15).Seg(40, 0, -30).Seg(70, 22).Seg(40, 0, 20).Seg(30, 0));

        list.Add(new StageDef
        {
            // 좁은 구름 통로(시즌1 구름 스테이지처럼): 좁음 자체가 난이도
            number = 5, title = "Drifting Clouds", hint = "A narrow cloud tunnel", difficulty = 5f, zone = "CloudLayer",
            width = 20f, height = 24f,
            content = ctx =>
            {
                DriftingCloud(ctx, 45f, 7f, 1f, 0f); DriftingCloud(ctx, 58f, 15f, -1f, 0.3f); DriftingCloud(ctx, 72f, 10f, 1f, 0.6f);
                HeadOnFlock(ctx, BirdKind.Seagull, 108f, 11f, 3);
                Checkpoint(ctx, 128f);
                DriftingCloud(ctx, 150f, 8f, -1f, 0.2f); DriftingCloud(ctx, 165f, 16f, 1f, 0.5f);
                Flock(ctx, BirdKind.Seagull, 190f, 12f, true, 4);
                DescendingCloud(ctx, 215f, 238f, 0f, -9f);
                DescendingCloud(ctx, 225f, 250f, 5f, -9f);
                DriftingCloud(ctx, 272f, 10f, 1f, 0.1f);
            }
        }.Seg(20, 0).Seg(60, 18, -15).Seg(40, 0).Seg(60, 20, 25).Seg(40, 0, -20).Seg(50, 15).Seg(30, 0, 10));

        list.Add(new StageDef
        {
            number = 6, title = "Hailstorm", hint = "Tightest tunnel. Ice falls from above", difficulty = 6f, zone = "InsideClouds",
            width = 18f, height = 22f,
            content = ctx =>
            {
                Hail(ctx, 40f, 58f, 86f, 7);
                RockGate(ctx, 95f, 5f, 5f + ctx.Hole, -4f, 4f + ctx.Hole * 0.3f);
                Checkpoint(ctx, 112f);
                Hail(ctx, 120f, 136f, 166f, 9);
                DescendingCloud(ctx, 172f, 195f, -4f, -8f);
                DriftingCloud(ctx, 205f, 11f, 1f, 0.3f);
                HeadOnFlock(ctx, BirdKind.Seagull, 214f, 9f, 3);
                Hail(ctx, 222f, 236f, 262f, 8);
                RockGate(ctx, 270f, 3f, 3f + ctx.Hole, 2f - ctx.Hole, 2f);
            }
        }.Seg(20, 0).Seg(60, 22, 20).Seg(30, 0, -20).Seg(60, 24).Seg(30, 0, -15).Seg(60, 20, 15).Seg(25, 0));

        list.Add(new StageDef
        {
            // 숨 고르기: 사방에서 구름이 솟고 내려오지만 가운데 높이의 '바람 고리 길'은 끝까지 비어 있음
            number = 7, title = "Ride the Tailwind", hint = "Follow the wind rings", difficulty = 4.5f, zone = "AboveClouds",
            width = 26f, height = 30f,
            content = ctx =>
            {
                const float laneH = 13f;
                for (float x = 25f; x < ctx.c.Length - 25f; x += 22f)
                {
                    WindRing(ctx, x, laneH, 0f);
                    RisingCloud(ctx, x - 14f, x + 6f, 0f, -5f, 9f);
                    DescendingCloud(ctx, x - 12f, x + 9f, 0f, -6f);
                    RisingCloud(ctx, x - 10f, x + 4f, -ctx.c.Half(x) + 2.5f, 0f, 18f);
                    RisingCloud(ctx, x - 10f, x + 11f, ctx.c.Half(x) - 2.5f, 0f, 18f);
                }
                for (float x = 60f; x < ctx.c.Length - 40f; x += 70f) Thermal(ctx, x, 0f);
                for (float x = 40f; x < ctx.c.Length; x += 90f) DecorFlock(ctx, BirdKind.Seagull, x, 34f, -60f);
            }
        }.Seg(20, 0).Seg(80, 12, -20).Seg(40, 0).Seg(80, 12, 25).Seg(40, 0, -5).Seg(40, 0));

        list.Add(new StageDef
        {
            // 숨 고르기: 번개가 정신없이 치지만 W를 꾹 누르고 직진하면 지날 때마다 꺼져 있음(속도는 게이트에서 실측)
            number = 8, title = "Thunder Storm", hint = "Don't stop. Never stop.", difficulty = 5f, zone = "Storm",
            width = 22f, height = 28f,
            content = ctx =>
            {
                for (float x = 32f; x < ctx.c.Length - 22f; x += 12f)
                {
                    SyncGate(ctx, x - 6f);
                    LightningRow(ctx, x, S.syncedLightningSpeed, 0f);
                }
            }
        }.Seg(20, 0).Seg(120, 8).Seg(40, 0, -20).Seg(100, 8).Seg(30, 0, 20));

        list.Add(new StageDef
        {
            number = 9, title = "Upside-Down Space", hint = "The world turns over", difficulty = 7f, zone = "Space",
            width = 34f, height = 55f,
            content = ctx =>
            {
                GravityAnomaly(ctx, 35f, true);
                Checkpoint(ctx, 80f);
                RockGate(ctx, 95f, 14f, 14f + ctx.Hole, -10f, 10f);
                Comet(ctx, 118f, 18f, 1f);
                AsteroidBelt(ctx, 140f, 6);
                Wind(ctx, 160f, 180f, new Vector3(0f, -ctx.Lift, 0f), false);
                RockGate(ctx, 176f, 2f, 2f + ctx.Hole, -2f, 2f + ctx.Hole * 0.5f);
                Checkpoint(ctx, 205f);
                HeadOnFlock(ctx, BirdKind.SpaceChicken, 232f, 14f, 5);
                Flock(ctx, BirdKind.Ufo, 252f, 20f, true, 2);
                Comet(ctx, 266f, 11f, -1f);
                RockGate(ctx, 290f, 12f, 12f + ctx.Hole, 3f, 3f + ctx.Hole);
                Alien(ctx, 310f, 22f, -9f);
            }
        }.Seg(20, 0).Seg(40, 0, 15).Seg(60, 20).Seg(40, 0, -30).Seg(60, 25).Seg(40, 0, 25).Seg(50, 20).Seg(30, 0, -10));

        list.Add(new StageDef
        {
            // 9스테이지의 뒤집힌 세상 그대로 이어서 시작 → 끝에서 다시 돌아오며 마지막 상승
            number = 10, title = "To the Stars", hint = "Everything you learned", difficulty = 8f, zone = "DeepSpace",
            width = 30f, height = 50f,
            content = ctx =>
            {
                Wind(ctx, 22f, 55f, new Vector3(0f, 0f, -ctx.CrossWind), true);
                RockSpire(ctx, 36f, -8f, 4.5f); RockSpire(ctx, 48f, 4f, 4.5f);
                AsteroidBelt(ctx, 70f, 7);
                HeadOnFlock(ctx, BirdKind.SpaceChicken, 95f, 12f, 5);
                for (int i = 0; i < 4; i++) PulsarRow(ctx, 112f + i * 9f, i * 0.45f);
                Checkpoint(ctx, 160f);
                Flock(ctx, BirdKind.Ufo, 180f, 14f, false, 3);
                Comet(ctx, 198f, 20f, 1f); Comet(ctx, 210f, 9f, -1f);
                WindRing(ctx, 228f, 14f, 0f);
                Alien(ctx, 240f, 12f, 7f);
                RockGate(ctx, 252f, 16f, 16f + ctx.Hole, -13f, -13f + ctx.Hole);
                Checkpoint(ctx, 275f);
                GravityAnomaly(ctx, 300f, false);
                Comet(ctx, 332f, 16f, 1f);
                RockGate(ctx, 352f, 4f, 4f + ctx.Hole, -6f, 6f);
                Aurora(ctx, 362f, ctx.c.Length - 4f);
            }
        }.Seg(20, 0).Seg(30, 0, -15).Seg(60, 22).Seg(40, 0, 30).Seg(60, 25).Seg(40, 0, -30).Seg(60, 28).Seg(40, 0, 20).Seg(50, 15).Seg(30, 0, -10).Seg(40, 0));

        return list;
    }

    // ───────────────────────── 출발 · 도착 · 체크포인트 ─────────────────────────

    static void StartNest(Ctx ctx)
    {
        Material bark = TexMat("Bark_Generated", C(0.39f, 0.24f, 0f), C(0.68f, 0.43f, 0.02f), 8f, 12f, 0.15f, 0.1f, 1.5f);
        GameObject pad = Prim(PrimitiveType.Cylinder, null, "FirstGroundStart", bark, false);
        pad.transform.position = ctx.c.P(0f, 0.25f, 0f);
        pad.transform.localScale = new Vector3(6f, 0.75f, 6f);
        pad.AddComponent<BoxCollider>().size = new Vector3(1f, 2f, 1f);
        NestRing(null, pad.transform.position + Vector3.up * 0.5f, 2.9f, 1f);   // 크기가 바뀐 발판의 자식으로 두면 찌그러짐
    }

    static void GoalNest(Ctx ctx)
    {
        float gx = ctx.c.Length - 16f;
        Transform goal = Group(ctx, "Goal");

        float top = ctx.c.VerticalHeight(gx);
        GameObject trigger = Box(goal, "Ending_Trigger", ctx.c.P(gx, top / 2f, 0f), new Vector3(3f, top, ctx.c.Width(gx)), ctx.c.Rot(gx), ctx.triggerMat);
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
        NestRing(goal, nestPos, 4.5f, 2f).gameObject.tag = "Ending";

        Material gold = Mat("Goal_GoldenEgg", C(1f, 0.84f, 0.3f), 0.9f, C(1f, 0.75f, 0.2f) * 1.8f);
        GameObject egg = Prim(PrimitiveType.Sphere, goal, "GoldenEgg", gold, false);
        egg.transform.position = nestPos + Vector3.up * 1.6f;
        egg.transform.localScale = new Vector3(2.4f, 3.1f, 2.4f);
        egg.tag = "Ending";

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

        Sign(ctx, gx - 1f, Mathf.Min(top - 6f, 20f), 0f, "GOAL", 4f).tag = "Ending";
    }

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

    // 체크포인트: 옆 바위 위 작은 둥지와 알(지나가면 알이 초록으로 빛남)
    static void Checkpoint(Ctx ctx, float x, int order = -1)
    {
        // 스테이지 중간 체크포인트(order 없음)는 설정에서 켰을 때만
        if (order < 0 && !S.midStageCheckpoints)
        {
            return;
        }

        ctx.checkpointCount++;
        if (order < 0) order = ctx.index * 100 + ctx.checkpointCount;

        Transform g = Group(ctx, $"Checkpoint_{ctx.checkpointCount}");
        GameObject trig = FullTrigger(ctx, "CheckpointTrigger", x, 3f);
        trig.transform.SetParent(g, true);
        Checkpoint cp = trig.AddComponent<Checkpoint>();
        SetField(cp, "order", order);

        GameObject spawn = new GameObject("RespawnPoint");
        spawn.transform.SetParent(g, false);
        spawn.transform.position = ctx.c.P(x + 4f, Mathf.Min(12f, ctx.c.Height(x) * 0.45f), 0f);
        SetField(cp, "respawnPoint", spawn.transform);

        float z = ctx.c.Half(x) - 2.5f;
        float ph = Mathf.Min(10f, ctx.c.Height(x) * 0.3f);
        GameObject perch = MeshObject(g, "Perch", RockMesh(3), ctx.obstacleMat);
        perch.transform.position = ctx.c.P(x, ph, z);
        perch.transform.localScale = new Vector3(4f, 3f, 3.5f);
        perch.tag = "Trigger";
        Vector3 nest = ctx.c.P(x, ph + 1.4f, z);
        NestRing(g, nest, 1.1f, 0.6f).gameObject.tag = "Trigger";
        GameObject egg = Prim(PrimitiveType.Sphere, g, "Egg", Mat("Checkpoint_Egg", C(1f, 0.97f, 0.9f), 0.6f, C(0.25f, 0.25f, 0.2f)), false);
        egg.transform.position = nest + Vector3.up * 0.45f;
        egg.transform.localScale = new Vector3(0.9f, 1.15f, 0.9f);
        egg.tag = "Trigger";
        SetField(cp, "flagRenderer", egg.GetComponent<Renderer>());
    }

    // ───────────────────────── 땅 · 숲 · 절벽 · 산 ─────────────────────────

    // 자연 장애물 덩어리: 바위 / 구름 / 소행성을 size(앞뒤, 높이, 옆) 자리에 빽빽이 쌓음.
    // 판정 = 보이는 모양: 덩어리 하나하나에 그 모양 그대로의 충돌을 붙임(살짝 작게 = 스치기만 하면 안 죽는 아주 작은 관용).
    // (예전에는 보이지 않는 상자 하나가 판정이라 바위가 상자보다 평균 1.4 ~ 2.6m 튀어나와 '닿았는데 지나가지는' 곳이 있었음.
    //  한때 바위를 상자 크기로 줄여 맞췄다가 바위가 작아 보여서, 크기는 원래대로 두고 판정을 바위에 맞추는 방식으로 바꿈)
    static void NaturalBlock(Ctx ctx, Transform parent, float x, float h, float z, Vector3 size)
    {
        Quaternion rot = ctx.c.Rot(x);
        Vector3 center = ctx.c.P(x, h, z);
        GameObject block = new GameObject("Block");
        block.transform.SetParent(parent, false);
        block.transform.position = center;
        block.transform.rotation = rot;

        float s = Mathf.Clamp(Mathf.Min(size.y, size.z), 2.5f, 7f);
        int ny = Mathf.Max(1, Mathf.CeilToInt(size.y / (s * 0.8f)));
        int nz = Mathf.Max(1, Mathf.CeilToInt(size.z / (s * 0.8f)));
        for (int j = 0; j < ny; j++)
        {
            for (int k = 0; k < nz; k++)
            {
                Vector3 local = new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    (ny == 1 ? 0f : -size.y / 2f + size.y * (j + 0.5f) / ny) + Random.Range(-0.4f, 0.4f),
                    (nz == 1 ? 0f : -size.z / 2f + size.z * (k + 0.5f) / nz) + Random.Range(-0.4f, 0.4f));
                Vector3 p = center + rot * local;
                float sc = s * Random.Range(1.05f, 1.3f);

                switch (ctx.look.wall)
                {
                    case WallStyle.Cloud:
                    {
                        // 구름은 프리팹의 구 충돌을 그대로 살려 보이는 구름 = 판정
                        GameObject cloud = SpawnCloud(ctx, block.transform, p, sc * 0.75f, true);
                        if (cloud != null)
                        {
                            foreach (SphereCollider sph in cloud.GetComponentsInChildren<SphereCollider>()) sph.radius *= S.hitShrink;
                        }
                        break;
                    }
                    case WallStyle.Crystal:
                    {
                        Mesh mesh = RockMesh(Random.Range(0, 6));
                        GameObject r = MeshObject(block.transform, "Asteroid", mesh, ctx.obstacleMat);
                        r.transform.position = p;
                        r.transform.rotation = Random.rotation;
                        r.transform.localScale = new Vector3(Mathf.Max(size.x * 1.6f, sc * 0.8f), sc, sc);
                        ShapeHit(r, mesh);
                        if (Random.value < 0.25f)
                        {
                            Material[] neon = NeonSet();
                            Mesh crystal = CrystalMesh();
                            GameObject c = MeshObject(block.transform, "Crystal", crystal, neon[Random.Range(0, neon.Length)]);
                            c.transform.position = p + Random.onUnitSphere * sc * 0.4f;
                            c.transform.rotation = Random.rotation;
                            c.transform.localScale = new Vector3(1f, 3f, 1f) * Random.Range(0.6f, 1f);
                            ShapeHit(c, crystal);
                        }
                        break;
                    }
                    default:
                    {
                        Mesh mesh = RockMesh(Random.Range(0, 6));
                        GameObject r = MeshObject(block.transform, "Rock", mesh, ctx.obstacleMat);
                        r.transform.position = p;
                        r.transform.rotation = rot * Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
                        r.transform.localScale = new Vector3(Mathf.Max(size.x * 1.6f, sc * 0.8f), sc, sc);
                        ShapeHit(r, mesh);
                        break;
                    }
                }
            }
        }
    }

    // 보이는 메시 모양 그대로의 충돌(볼록 메시)을 살짝 작게(S.hitShrink배) 붙임
    static void ShapeHit(GameObject visual, Mesh mesh)
    {
        GameObject hit = new GameObject("Hit");
        hit.transform.SetParent(visual.transform, false);
        hit.transform.localScale = Vector3.one * S.hitShrink;
        MeshCollider mc = hit.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = true;
    }

    // 틈이 있는 자연 벽: (y0~y1: 바닥에서 높이, z0~z1: 옆) 자리만 비어 있음. 범위는 그 위치의 폭 · 높이로 자름.
    static void RockGate(Ctx ctx, float x, float y0, float y1, float z0, float z1)
    {
        Transform g = Group(ctx, $"Gate_{x:0}");
        float top = ctx.c.VerticalHeight(x);
        float half = ctx.c.Half(x);
        const float th = 2f;
        y0 = Mathf.Clamp(y0, 0f, top); y1 = Mathf.Clamp(y1, y0, top);
        z0 = Mathf.Clamp(z0, -half, half); z1 = Mathf.Clamp(z1, z0, half);
        float hy = y1 - y0;

        if (y0 > 0.01f) NaturalBlock(ctx, g, x, y0 / 2f, 0f, new Vector3(th, y0, half * 2f));
        if (y1 < top - 0.01f) NaturalBlock(ctx, g, x, (y1 + top) / 2f, 0f, new Vector3(th, top - y1, half * 2f));
        if (z0 > -half + 0.01f) NaturalBlock(ctx, g, x, (y0 + y1) / 2f, (-half + z0) / 2f, new Vector3(th, hy, z0 + half));
        if (z1 < half - 0.01f) NaturalBlock(ctx, g, x, (y0 + y1) / 2f, (z1 + half) / 2f, new Vector3(th, hy, half - z1));
    }

    static void RockSpire(Ctx ctx, float x, float z, float size)
    {
        float top = ctx.c.VerticalHeight(x);
        NaturalBlock(ctx, Group(ctx, $"Spire_{x:0}"), x, top / 2f, ctx.c.ClampZ(x, z, size / 2f + 1f), new Vector3(size, top, size));
    }

    static Transform Tree(Ctx ctx, float x, float z, float scale)
    {
        Material bark = TexMat("Bark_Generated", C(0.39f, 0.24f, 0f), C(0.68f, 0.43f, 0.02f), 8f, 12f, 0.15f, 0.1f, 1.5f);
        Material leaf = Mat($"{ctx.look.name}_LeafA", ctx.look.leafA, 0.15f);
        Material leaf2 = Mat($"{ctx.look.name}_LeafB", ctx.look.leafB, 0.15f);
        GameObject tree = new GameObject($"Tree_{x:0}");
        tree.transform.SetParent(Group(ctx, "Trees"), false);
        tree.transform.position = ctx.c.P(x, 0f, ctx.c.ClampZ(x, z, 3f));
        tree.transform.localScale = Vector3.one * scale;

        tree.transform.rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up);

        GameObject trunk = Prim(PrimitiveType.Cylinder, tree.transform, "Trunk", bark, true);
        trunk.transform.localPosition = new Vector3(0f, 4f, 0f);
        trunk.transform.localScale = new Vector3(1.3f, 4f, 1.3f);

        const float k = 0.55f;
        RoundCrown(tree.transform, k, 1.2f, leaf, leaf2);

        // 보이는 잎보다 조금 작은 충돌(보이지 않는 관용): 잎 덩어리를 세로 캡슐 하나로.
        // 구(Sphere)는 자라는 나무(가로 1.6 · 세로 2.4배)에서 반지름이 '가장 큰 배율(2.4)'로 커져
        // 잎보다 훨씬 넓게 길을 막았음 → 캡슐은 반지름이 가로 배율, 높이가 세로 배율을 따름.
        CapsuleCollider cc = tree.AddComponent<CapsuleCollider>();
        cc.direction = 1;
        cc.center = new Vector3(0f, 21f * k, 0f);
        cc.radius = 3f;   // 잎 반지름 약 3.1 ~ 3.7m보다 조금 작게
        cc.height = 8.5f;
        return tree.transform;
    }

    // 시즌1 Tree_1 프리팹의 잎 배치(매끈한 구 5개를 위로 쌓은 동글동글한 뭉게 모양)를 그대로 옮긴 값.
    // 프리팹 루트 배율(2.38, 1.79, 2.01)을 곱해 둔 좌표 · 크기이고, k로 전체 크기, widen으로 옆 통통함을 조절합니다.
    static readonly Vector3[] CrownPos =
    {
        new Vector3(0.93f, 17.45f, 0.44f),   // 아래 큰 덩어리
        new Vector3(0.93f, 18.44f, -1.51f),
        new Vector3(-1.52f, 20.26f, -0.58f),
        new Vector3(0.93f, 21.30f, 0.76f),
        new Vector3(-1.52f, 24.58f, -1.33f), // 맨 위 둥근 머리
    };
    static readonly Vector3[] CrownScale =
    {
        new Vector3(7.60f, 9.36f, 6.4f),
        new Vector3(6.50f, 8.00f, 5.6f),
        new Vector3(5.21f, 6.43f, 4.6f),
        new Vector3(5.64f, 6.95f, 4.9f),
        new Vector3(8.16f, 10.06f, 7.0f),
    };

    static void RoundCrown(Transform tree, float k, float widen, Material leaf, Material leaf2)
    {
        for (int b = 0; b < CrownPos.Length; b++)
        {
            // 구 기본 메시는 매끈해서 시즌1처럼 둥근 실루엣이 나옴(울퉁불퉁한 바위 메시 대신)
            GameObject puff = Prim(PrimitiveType.Sphere, tree, $"Crown_{b}", b == 3 ? leaf2 : leaf, false);
            Vector3 p = CrownPos[b] * k;
            puff.transform.localPosition = new Vector3(p.x * widen, p.y, p.z * widen);
            float jitter = Random.Range(0.92f, 1.08f);
            Vector3 s = CrownScale[b] * k * jitter;
            puff.transform.localScale = new Vector3(s.x * widen, s.y, s.z * widen);
            puff.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }

    static void GrowTriggerFor(Ctx ctx, float x, Transform target, Vector3 multiplier)
    {
        GameObject trig = FullTrigger(ctx, $"GrowTrigger_{target.name}", x, 2f);
        GrowTrigger grow = trig.AddComponent<GrowTrigger>();
        SetField(grow, "targetObject", target.gameObject);
        SetField(grow, "scaleMultiplier", multiplier);
    }

    static void FallenLog(Ctx ctx, float x, float h)
    {
        Material bark = TexMat("Bark_Generated", C(0.39f, 0.24f, 0f), C(0.68f, 0.43f, 0.02f), 8f, 12f, 0.15f, 0.1f, 1.5f);
        GameObject log = Prim(PrimitiveType.Cylinder, Group(ctx, $"Log_{x:0}"), "FallenLog", bark, false);
        log.transform.position = ctx.c.P(x, h, 0f);
        log.transform.rotation = ctx.c.Rot(x) * Quaternion.Euler(90f, 0f, Random.Range(-6f, 6f));
        log.transform.localScale = new Vector3(2.4f, ctx.c.Half(x) + 4f, 2.4f);
        CapsuleCollider cc = log.AddComponent<CapsuleCollider>();
        cc.direction = 1;
        cc.radius = 0.42f;
        cc.height = 2f;
    }

    // 바람 구역 + 바람 방향 줄무늬. accel은 길 기준(x 앞, y 위, z 옆). lateral이면 옆바람.
    static void Wind(Ctx ctx, float xa, float xb, Vector3 accel, bool lateral)
    {
        float len = xb - xa;
        float xm = (xa + xb) / 2f;
        Vector3 world = ctx.c.Fwd(xm) * accel.x + Vector3.up * accel.y + ctx.c.Lat(xm) * accel.z;
        GameObject trig = FullTrigger(ctx, $"Wind_{xa:0}", xm, len);
        SetField(trig.AddComponent<WindArea>(), "acceleration", world);

        GameObject fx = new GameObject("WindStreaks");
        fx.transform.SetParent(trig.transform.parent, false);
        fx.transform.position = ctx.c.P(xm, ctx.c.Height(xm) / 2f, 0f);
        fx.transform.rotation = ctx.c.Rot(xm);
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
        shape.scale = new Vector3(len, ctx.c.Height(xm) - 2f, ctx.c.Width(xm) - 2f);
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        Vector3 dir = world.normalized * 9f;
        vel.x = dir.x; vel.y = dir.y; vel.z = dir.z;
        ParticleSystemRenderer r = fx.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.35f;
        r.lengthScale = 2f;
        r.sharedMaterial = ParticleMat;
    }

    // 옆으로 가로지르는 자연물(굴러오는 바위 · 구름 · 소행성)의 이동: 옆(-dir) → 옆(+dir)
    static void Cross(Ctx ctx, GameObject go, float x, float dir, float speed, float wait, float phase, float roll, float fade)
    {
        float dist = ctx.c.Width(x) + 12f;
        PathMover pm = go.AddComponent<PathMover>();
        SetField(pm, "travel", ctx.c.Lat(x) * dir * dist);
        SetField(pm, "speed", speed);
        SetField(pm, "interval", wait);
        SetField(pm, "startDelay", phase * dist / Mathf.Max(0.1f, speed));
        SetField(pm, "rollRadius", roll);
        SetField(pm, "fade", fade);
    }

    static void RollingBoulder(Ctx ctx, float x, float dir, float phase)
    {
        GameObject b = MeshObject(Group(ctx, $"Boulder_{x:0}"), "RollingBoulder", RockMesh(Random.Range(0, 6)), ctx.obstacleMat);
        float size = 5f;
        b.transform.position = ctx.c.P(x, size * 0.45f, -dir * (ctx.c.Half(x) + 6f));
        b.transform.localScale = Vector3.one * size;
        b.AddComponent<SphereCollider>().radius = 0.55f;   // 보이는 크기와 거의 같게(예전 0.4 → 스쳐도 지나감)
        Cross(ctx, b, x, dir, ctx.CrossSpeed, ctx.CrossWait, phase, size * 0.45f, 0.08f);
    }

    static void Rockfall(Ctx ctx, float triggerX, float fromX, float toX, int count)
    {
        Transform g = Group(ctx, $"Rockfall_{triggerX:0}");
        GameObject trig = FullTrigger(ctx, "RockfallTrigger", triggerX, 2f);
        trig.transform.SetParent(g, true);
        DropOnTrigger drop = trig.AddComponent<DropOnTrigger>();
        List<Transform> rocks = new List<Transform>();
        float[] zs = { -0.5f, 0.3f, -0.1f, 0.55f, -0.65f, 0.05f, 0.4f, -0.3f };
        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Lerp(fromX, toX, count == 1 ? 0f : i / (float)(count - 1));
            GameObject r = MeshObject(g, $"FallingRock_{i}", RockMesh(Random.Range(0, 6)), ctx.obstacleMat);
            r.transform.position = ctx.c.P(x, ctx.c.VerticalHeight(x) - 1f, zs[i % zs.Length] * ctx.c.Width(x));
            r.transform.localScale = Vector3.one * Random.Range(3f, 4.2f);
            // 바위 메시 반지름 0.5 ~ 0.68 → 0.55(보이는 크기와 거의 같게, 예전 0.4는 너무 작아 스쳐도 지나감)
            r.AddComponent<SphereCollider>().radius = 0.55f;
            rocks.Add(r.transform);
        }
        SetField(drop, "targets", rocks.ToArray());
        SetField(drop, "interval", R(S.hailInterval, ctx.D01) * 1.5f);
        SetField(drop, "fallDistance", ctx.c.Height(fromX) + 6f);
        // 낙석: 빠르게 시작해 점점 빨라지고, 다 떨어지면 다시 위에서 떨어짐(계속 쏟아지는 지대)
        SetField(drop, "fallSpeed", S.rockfallSpeed);
        SetField(drop, "fallAcceleration", S.rockfallAcceleration);
        SetField(drop, "loop", true);
    }

    static void Hail(Ctx ctx, float triggerX, float fromX, float toX, int count)
    {
        Transform g = Group(ctx, $"Hail_{triggerX:0}");
        GameObject trig = FullTrigger(ctx, "HailTrigger", triggerX, 2f);
        trig.transform.SetParent(g, true);
        DropOnTrigger drop = trig.AddComponent<DropOnTrigger>();
        Material ice = Mat("Hail_Ice", C(0.8f, 0.92f, 1f), 0.95f, C(0.2f, 0.35f, 0.5f));
        List<Transform> shards = new List<Transform>();
        float[] zs = { -0.4f, 0.2f, -0.1f, 0.42f, -0.3f, 0.05f, 0.33f, -0.45f, 0.15f };
        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Lerp(fromX, toX, count == 1 ? 0f : i / (float)(count - 1));
            GameObject s = MeshObject(g, $"Hailstone_{i}", CrystalMesh(), ice);
            s.transform.position = ctx.c.P(x, ctx.c.VerticalHeight(x) - 3f, zs[i % zs.Length] * ctx.c.Width(x));
            s.transform.rotation = Quaternion.Euler(180f, Random.Range(0f, 360f), 0f);
            s.transform.localScale = new Vector3(2.2f, 5f, 2.2f);
            CapsuleCollider cc = s.AddComponent<CapsuleCollider>();
            cc.radius = 0.18f;
            cc.height = 0.9f;
            shards.Add(s.transform);
        }
        SetField(drop, "targets", shards.ToArray());
        SetField(drop, "interval", R(S.hailInterval, ctx.D01));
        SetField(drop, "fallDistance", ctx.c.Height(fromX) + 6f);
    }

    // ───────────────────────── 새 · 우주 생물 ─────────────────────────

    static void Flock(Ctx ctx, BirdKind kind, float x, float h, bool leftToRight, int count)
    {
        float dir = leftToRight ? 1f : -1f;
        Transform flock = new GameObject($"Flock_{kind}_{x:0}").transform;
        flock.SetParent(Group(ctx, "Flocks"), false);
        flock.position = ctx.c.P(x, Mathf.Min(h, ctx.c.Height(x) - 4f), -dir * (ctx.c.Half(x) + 8f));
        for (int i = 0; i < count; i++)
        {
            float row = (i + 1) / 2;
            float side = i % 2 == 0 ? 1f : -1f;
            Bird(flock, new Vector3(side * row * 2.6f, Random.Range(-0.6f, 0.6f), -row * 2.8f), kind, 1f, true);
        }
        float dist = ctx.c.Width(x) + 16f;
        PathMover pm = flock.gameObject.AddComponent<PathMover>();
        SetField(pm, "travel", ctx.c.Lat(x) * dir * dist);
        SetField(pm, "speed", ctx.CrossSpeed * (kind == BirdKind.Ufo ? 1.3f : 1f));
        SetField(pm, "interval", ctx.CrossWait);
        SetField(pm, "startDelay", Random.Range(0f, 2f));
        SetField(pm, "faceDirection", true);
    }

    static void HeadOnFlock(Ctx ctx, BirdKind kind, float x, float h, int count)
    {
        Transform flock = new GameObject($"HeadOn_{kind}_{x:0}").transform;
        flock.SetParent(Group(ctx, "Flocks"), false);
        float hh = Mathf.Min(h, ctx.c.Height(x) - 4f);
        Vector3 from = ctx.c.P(x + 45f, hh, 0f);
        Vector3 to = ctx.c.P(x - 15f, hh, 0f);
        flock.position = from;
        float half = ctx.c.Half(x) - 2f;
        float[] zs = { -0.55f, 0.55f, 0f, -0.85f, 0.85f };
        float[] ys = { 0f, 2f, -3f, 4f, -1f };
        for (int i = 0; i < count; i++)
        {
            Bird(flock, new Vector3(zs[i % zs.Length] * half, ys[i % ys.Length], -i * 3f), kind, 1f, true);
        }
        PathMover pm = flock.gameObject.AddComponent<PathMover>();
        SetField(pm, "travel", to - from);
        SetField(pm, "speed", ctx.CrossSpeed * 0.9f);
        SetField(pm, "interval", ctx.CrossWait + 1f);
        SetField(pm, "faceDirection", true);
    }

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
        SetField(pm, "travel", ctx.c.Fwd(x) * 80f + Vector3.up * 6f);
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

        GameObject b = Prim(PrimitiveType.Sphere, bird.transform, "Body", Mat($"Bird_{kind}_Body", body, 0.3f), false);
        b.transform.localScale = new Vector3(1.1f, 0.95f, 1.9f);
        GameObject h = Prim(PrimitiveType.Sphere, bird.transform, "Head", Mat($"Bird_{kind}_Head", head, 0.3f), false);
        h.transform.localPosition = new Vector3(0f, 0.4f, 1.0f);
        h.transform.localScale = Vector3.one * 0.85f;
        GameObject bk = Prim(PrimitiveType.Sphere, bird.transform, "Beak", Mat($"Bird_{kind}_Beak", beak, 0.4f), false);
        bk.transform.localPosition = new Vector3(0f, 0.32f, 1.48f);
        bk.transform.localScale = new Vector3(0.22f, 0.16f, 0.36f);
        foreach (float ex in new[] { -0.24f, 0.24f })
        {
            GameObject eye = Prim(PrimitiveType.Sphere, bird.transform, "Eye", Mat("Bird_Eye", C(0.05f, 0.05f, 0.08f), 0.6f), false);
            eye.transform.localPosition = new Vector3(ex, 0.52f, 1.3f);
            eye.transform.localScale = new Vector3(0.14f, 0.16f, 0.08f);
        }
        if (kind == BirdKind.SpaceChicken)
        {
            GameObject comb = Prim(PrimitiveType.Sphere, bird.transform, "Comb", Mat("Bird_Comb", C(0.93f, 0.13f, 0.12f), 0.4f), false);
            comb.transform.localPosition = new Vector3(0f, 0.85f, 1.0f);
            comb.transform.localScale = new Vector3(0.16f, 0.3f, 0.42f);
        }

        Material wingMat = Mat($"Bird_{kind}_Wing", wing, 0.3f);
        foreach (float side in new[] { -1f, 1f })
        {
            GameObject pivot = new GameObject(side < 0 ? "WingPivot_L" : "WingPivot_R");
            pivot.transform.SetParent(bird.transform, false);
            pivot.transform.localPosition = new Vector3(side * 0.45f, 0.25f, 0.1f);
            GameObject w = Prim(PrimitiveType.Sphere, pivot.transform, "Wing", wingMat, false);
            w.transform.localPosition = new Vector3(side * 1.2f, 0f, 0f);
            w.transform.localScale = new Vector3(2.4f, 0.12f, 1f);
        }
        bird.AddComponent<BirdFlap>();

        if (hazard)
        {
            // 몸통 + 머리만 감싸는 캡슐(날개 · 위아래 여백은 빼서 '안 닿은 것 같은데 죽는' 일이 없게, 보이지 않는 관용)
            CapsuleCollider cap = bird.AddComponent<CapsuleCollider>();
            cap.direction = 2;
            cap.radius = 0.5f;
            cap.height = 2.4f;
            cap.center = new Vector3(0f, 0.15f, 0.25f);
        }
        return bird;
    }

    static void Alien(Ctx ctx, float x, float h, float z)
    {
        Transform g = Group(ctx, $"Alien_{x:0}");
        GameObject alien = new GameObject("Alien");
        alien.transform.SetParent(g, false);
        alien.transform.position = ctx.c.P(x, Mathf.Min(h, ctx.c.Height(x) - 4f), ctx.c.ClampZ(x, z, 3f));
        Material skin = NeonMat("Alien_Skin", C(0.48f, 0.99f, 0.2f), 0.6f);
        GameObject body = MeshObject(alien.transform, "Body", RockMesh(4), skin);
        body.transform.localScale = new Vector3(4f, 3.4f, 4f);
        foreach (float ex in new[] { -0.9f, 0.9f })
        {
            GameObject stalk = Prim(PrimitiveType.Cylinder, alien.transform, "Stalk", skin, false);
            stalk.transform.localPosition = new Vector3(ex, 2.2f, -0.4f);
            stalk.transform.localScale = new Vector3(0.25f, 0.9f, 0.25f);
            GameObject eye = Prim(PrimitiveType.Sphere, alien.transform, "Eye", Mat("Alien_Eye", C(1f, 1f, 1f), 0.6f), false);
            eye.transform.localPosition = new Vector3(ex, 3.1f, -0.4f);
            eye.transform.localScale = Vector3.one * 0.9f;
            GameObject pupil = Prim(PrimitiveType.Sphere, alien.transform, "Pupil", Mat("Bird_Eye", C(0.05f, 0.05f, 0.08f), 0.6f), false);
            pupil.transform.localPosition = new Vector3(ex, 3.1f, -0.72f);
            pupil.transform.localScale = new Vector3(0.45f, 0.45f, 0.2f);
        }
        alien.AddComponent<SphereCollider>().radius = 1.5f;
        Oscillator o = alien.AddComponent<Oscillator>();
        SetField(o, "offset", Vector3.down * 8f - ctx.c.Lat(x) * z * 1.2f);
        SetField(o, "period", 4f);
        SetField(o, "phase", Random.value);
    }

    // ───────────────────────── 구름 ─────────────────────────

    static void DriftingCloud(Ctx ctx, float x, float h, float dir, float phase)
    {
        Transform cloud = CloudCluster(ctx, x, Mathf.Min(h, ctx.c.Height(x) - 4f), -dir * (ctx.c.Half(x) + 6f), 1f, true);
        Cross(ctx, cloud.gameObject, x, dir, ctx.Drift, 0.5f, phase, 0f, 0.15f);
    }

    static void RisingCloud(Ctx ctx, float triggerX, float cloudX, float z, float startH, float rise)
    {
        Transform cloud = CloudCluster(ctx, cloudX, startH, z, 1f, true);
        CloudTrigger(ctx, triggerX, cloud, new Vector3(0f, rise, 0f));
    }

    // 위에서 내려오는 구름: 높이가 높은 구간에서도 길을 막도록 시작 높이를 24m 이하로
    static void DescendingCloud(Ctx ctx, float triggerX, float cloudX, float z, float drop)
    {
        float start = Mathf.Min(ctx.c.VerticalHeight(cloudX) - 2f, 24f);
        Transform cloud = CloudCluster(ctx, cloudX, start, z, 1f, true);
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

    static void CloudSea(Ctx ctx)
    {
        for (float x = 0f; x < ctx.c.Length; x += 9f) CloudFloorRow(ctx, x);
    }

    static void CloudFloorRow(Ctx ctx, float x)
    {
        float half = ctx.c.Half(x);
        foreach (float f in new[] { -0.65f, 0f, 0.65f })
        {
            CloudCluster(ctx, x + Random.Range(-2f, 2f), -3.5f, f * half + Random.Range(-1.5f, 1.5f), 0.85f, false);
        }
    }

    // 바람 고리(꼬리바람): 소용돌이치는 바람 입자 고리, 지나가면 빨라짐
    static void WindRing(Ctx ctx, float x, float h, float z)
    {
        Transform g = Group(ctx, $"WindRing_{x:0}");
        GameObject trig = new GameObject("WindRing");
        trig.transform.SetParent(g, false);
        trig.transform.position = ctx.c.P(x, h, z);
        trig.transform.rotation = ctx.c.Rot(x);
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

    static void Thermal(Ctx ctx, float x, float z)
    {
        Transform g = Group(ctx, $"Thermal_{x:0}");
        GameObject trig = Box(g, "Thermal", ctx.c.P(x, 3f, z), new Vector3(6f, 6f, 6f), ctx.c.Rot(x), ctx.triggerMat);
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

    static void LightningRow(Ctx ctx, float x, float syncSpeed, float phase)
    {
        Transform g = Group(ctx, $"Lightning_{x:0}");
        float top = ctx.c.VerticalHeight(x);
        float half = ctx.c.Half(x);
        Material bolt = Mat("Lightning_Bolt", C(1f, 0.97f, 0.85f), 0.9f, C(1f, 0.92f, 0.65f) * 2.2f);
        List<Transform> bolts = new List<Transform>();
        foreach (float f in new[] { -0.65f, 0f, 0.65f })
        {
            float z = f * half;
            List<Vector3> pts = new List<Vector3>();
            const int n = 6;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                pts.Add(ctx.c.P(x, top * (1f - t), z + (i == 0 || i == n ? 0f : Random.Range(-1.8f, 1.8f))));
            }
            bolts.Add(ZigZag(g, "Bolt", pts, bolt, 0.45f));
        }
        {
            float hh = Random.Range(5f, top - 5f);
            List<Vector3> pts = new List<Vector3>();
            const int n = 6;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                pts.Add(ctx.c.P(x, hh + (i == 0 || i == n ? 0f : Random.Range(-1.5f, 1.5f)), Mathf.Lerp(-half, half, t)));
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

    static Transform ZigZag(Transform parent, string name, List<Vector3> pts, Material mat, float thickness)
    {
        Transform g = new GameObject(name).transform;
        g.SetParent(parent, false);
        g.position = pts[0];
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

    static void PulsarRow(Ctx ctx, float x, float phase)
    {
        Transform g = Group(ctx, $"Pulsar_{x:0}");
        float half = ctx.c.Half(x);
        float hh = Random.Range(5f, ctx.c.VerticalHeight(x) - 5f);
        Material beam = NeonMat("Pulsar_Beam", C(0.45f, 0.2f, 0.99f), 2.5f);
        foreach (float side in new[] { -1f, 1f })
        {
            GameObject c = MeshObject(g, "PulsarCrystal", CrystalMesh(), NeonMat("Crystal_NeonPurple", C(0.45f, 0.2f, 0.99f), 2.2f));
            c.transform.position = ctx.c.P(x, hh, side * (half + 1f));
            c.transform.rotation = ctx.c.Rot(x) * Quaternion.Euler(90f, 0f, 0f);
            c.transform.localScale = new Vector3(2f, 4f, 2f);
        }
        Transform b = ZigZag(g, "PulsarBeam", new List<Vector3> { ctx.c.P(x, hh, -half), ctx.c.P(x, hh, half) }, beam, 0.7f);
        TimedHazard th = b.gameObject.AddComponent<TimedHazard>();
        SetField(th, "offTime", R(S.lightningOff, ctx.D01));
        SetField(th, "warnTime", 0.45f);
        SetField(th, "onTime", R(S.lightningOn, ctx.D01));
        SetField(th, "phaseOffset", phase);
        SetField(th, "onColor", C(0.75f, 0.45f, 1f));
        SetField(th, "warnColor", C(0.99f, 0.2f, 0.46f));
        SetField(th, "offColor", C(0.2f, 0.12f, 0.35f));
    }

    static void GravityAnomaly(Ctx ctx, float x, bool reverse)
    {
        Transform g = Group(ctx, $"GravityAnomaly_{x:0}");
        GameObject trig = FullTrigger(ctx, reverse ? "ReverseGravityTrigger" : "RestoreGravityTrigger", x, 2f);
        trig.transform.SetParent(g, true);
        SetField(trig.AddComponent<ReverseGravity>(), "reverse", reverse);

        float top = ctx.c.VerticalHeight(x);
        float half = ctx.c.Half(x);
        Material[] neon = NeonSet();
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16f;
            GameObject c = MeshObject(g, "AnomalyCrystal", CrystalMesh(), neon[i % 2 == 0 ? 1 : 0]);
            c.transform.position = ctx.c.P(x, top / 2f + Mathf.Sin(a) * (top / 2f - 1.5f), Mathf.Cos(a) * (half - 1f));
            c.transform.rotation = Quaternion.LookRotation(ctx.c.Fwd(x), Vector3.up * Mathf.Sin(a) + ctx.c.Lat(x) * Mathf.Cos(a));
            c.transform.localScale = new Vector3(1.2f, 3.2f, 1.2f);
        }

        GameObject fx = new GameObject("Swirl");
        fx.transform.SetParent(g, false);
        fx.transform.position = ctx.c.P(x, top / 2f, 0f);
        fx.transform.rotation = ctx.c.Rot(x);
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
        sh.radius = Mathf.Min(half, top / 2f) - 1f;
        sh.rotation = new Vector3(0f, 90f, 0f);
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalX = 1.2f;
        vel.radial = -2f;
        fx.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat;

        Sign(ctx, x - 12f, Mathf.Min(top * 0.6f, 20f), 0f, reverse ? "GRAVITY FLIP AHEAD" : "BACK TO NORMAL", 2.6f, g);
    }

    static void Comet(Ctx ctx, float x, float h, float dir)
    {
        Transform g = Group(ctx, $"Comet_{x:0}");
        GameObject comet = MeshObject(g, "Comet", RockMesh(5), ctx.obstacleMat);
        float half = ctx.c.Half(x);
        comet.transform.position = ctx.c.P(x + 10f, Mathf.Min(h, ctx.c.Height(x) - 6f) + 6f, -dir * (half + 10f));
        comet.transform.localScale = Vector3.one * 3.2f;
        comet.AddComponent<SphereCollider>().radius = 0.4f;
        PathMover pm = comet.AddComponent<PathMover>();
        SetField(pm, "travel", -ctx.c.Fwd(x) * 20f + Vector3.down * 12f + ctx.c.Lat(x) * dir * (half * 2f + 20f));
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

    static void AsteroidBelt(Ctx ctx, float x, int count)
    {
        Transform g = Group(ctx, $"AsteroidBelt_{x:0}");
        float top = ctx.c.Height(x);
        for (int i = 0; i < count; i++)
        {
            float dir = i % 2 == 0 ? 1f : -1f;
            float xi = x + i * 4f;
            GameObject a = MeshObject(g, "DriftingAsteroid", RockMesh(Random.Range(0, 6)), ctx.obstacleMat);
            float size = Random.Range(3.5f, 5.5f);
            a.transform.position = ctx.c.P(xi, 4f + i * (top - 8f) / Mathf.Max(1, count - 1), -dir * (ctx.c.Half(xi) + 6f));
            a.transform.localScale = Vector3.one * size;
            a.transform.rotation = Random.rotation;
            a.AddComponent<SphereCollider>().radius = 0.4f;
            Cross(ctx, a, xi, dir, ctx.Drift * 1.2f, 0.3f, i * 0.15f, size * 0.6f, 0.08f);
        }
    }

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
            float top = ctx.c.Height(x);
            foreach (float side in new[] { -1f, 1f })
            {
                GameObject strip = Prim(PrimitiveType.Cube, g, "AuroraStrip", mats[i % mats.Length], false);
                strip.transform.position = ctx.c.P(x, top * 0.55f + Mathf.Sin(x * 0.15f) * 1.5f, side * (ctx.c.Half(x) + 1.5f + Mathf.Cos(x * 0.2f)));
                strip.transform.rotation = ctx.c.Rot(x);
                strip.transform.localScale = new Vector3(0.35f, top * 0.7f + Random.Range(-3f, 3f), 0.15f);
                Oscillator o = strip.AddComponent<Oscillator>();
                SetField(o, "offset", new Vector3(0f, 2.5f, 0f));
                SetField(o, "period", 3f);
                SetField(o, "phase", (x * 0.03f) % 1f);
            }
        }
    }

    // 별: 구간을 감싸는 상자(꺾인 길의 범위)에 고정된 반짝이
    static void StarField(Ctx ctx)
    {
        Vector3 min = Vector3.one * float.MaxValue, max = Vector3.one * float.MinValue;
        for (float x = 0f; x <= ctx.c.Length; x += 10f)
        {
            Vector3 p = ctx.c.P(x, 0f, 0f);
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        Vector3 size = max - min + new Vector3(160f, ctx.c.Height(0f) + 80f, 160f);

        GameObject fx = new GameObject("StarField");
        fx.transform.SetParent(ctx.root, false);
        fx.transform.position = (min + max) / 2f + Vector3.up * ctx.c.Height(0f) / 2f;
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.duration = 5f;
        main.startLifetime = 100000f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.6f);
        main.startColor = new ParticleSystem.MinMaxGradient(C(1f, 1f, 1f), C(0.94f, 0.9f, 0.4f));
        main.maxParticles = 1800;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1800) });
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = size;
        fx.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat;
    }

    static GameObject Sign(Ctx ctx, float x, float h, float z, string text, float size, Transform parent = null)
    {
        GameObject go = new GameObject($"Sign_{text}");
        go.transform.SetParent(parent != null ? parent : Group(ctx, "Signs"), false);
        go.transform.position = ctx.c.P(x, h, z);
        go.transform.rotation = ctx.c.Rot(x) * Quaternion.Euler(0f, 90f, 0f);
        go.tag = "Text";
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size * 10f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.rectTransform.sizeDelta = new Vector2(ctx.c.Width(x) * 1.4f, 8f);
        return go;
    }
}
