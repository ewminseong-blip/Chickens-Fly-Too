using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

// Can Chicken Fly? 모양 만들기: 발판(구간별 모양 × 종류) · 기둥(사일로 · 언덕 · 거대한 나무 · 절벽 · 설산) · 닭장 · 장식 · 기믹.
// 원칙
//  - 발판 윗면 = 판정 윗면(보이는 대로 선다). 둥근 바위 · 구름은 윗면 판정을 따로 평평하게 두고, 가장자리는 둥글게 보여 모서리 미끄러짐과 맞음.
//  - 길이 되는 발판은 밝고 선명하게, 장식(밟을 수 없는 것)은 흐리거나 멀리 → 7 · 8구간처럼 '어려워 보이는' 곳도 길은 읽힘.
//  - 장애물은 죽이지 않고 밀어냄(ClimbHazard).
public static partial class StageBuilder
{
    enum PlatKind { Rest, Normal, Small, Tilted, Moving, Spinning, Bouncy, Slippery }

    // ───────────── 재질 ─────────────

    static Material MStraw => TexMat("Climb_Straw", C(0.82f, 0.62f, 0.22f), C(1f, 0.88f, 0.48f), 9f, 38f, 0.35f, 0.05f, 1.4f);
    static Material MWood => TexMat("Climb_Wood", C(0.42f, 0.26f, 0.12f), C(0.62f, 0.42f, 0.22f), 4f, 9f, 0.2f, 0.12f, 1.1f);
    static Material MBarn => TexMat("Climb_BarnWall", C(0.62f, 0.2f, 0.12f), C(0.78f, 0.32f, 0.18f), 3f, 14f, 0.18f, 0.08f, 1.3f);
    static Material MGrass => TexMat("Climb_Grass", C(0.2f, 0.62f, 0.16f), C(0.48f, 0.82f, 0.22f), 7f, 0f, 0.35f, 0.08f, 1.2f);
    static Material MEarth => TexMat("Climb_Earth", C(0.36f, 0.24f, 0.12f), C(0.5f, 0.36f, 0.18f), 5f, 6f, 0.3f, 0.06f, 1.6f);
    static Material MBark => TexMat("Bark_Generated", C(0.39f, 0.24f, 0f), C(0.68f, 0.43f, 0.02f), 8f, 12f, 0.15f, 0.1f, 1.5f);
    static Material MRock => TexMat("Climb_Rock", C(0.42f, 0.33f, 0.24f), C(0.66f, 0.56f, 0.44f), 3f, 9f, 0.3f, 0.12f, 2.2f);
    static Material MSnow => TexMat("Climb_Snow", C(0.86f, 0.9f, 0.97f), C(1f, 1f, 1f), 6f, 0f, 0.12f, 0.35f, 0.7f);
    static Material MIce => Mat("Climb_Ice", C(0.62f, 0.86f, 1f), 0.92f, C(0.05f, 0.12f, 0.18f));
    static Material MMoss => Mat("Climb_Moss", C(0.3f, 0.58f, 0.16f), 0.1f);
    static Material MLeaf => Mat("Climb_Leaf", C(0.28f, 0.6f, 0.14f), 0.15f);
    static Material MLeaf2 => Mat("Climb_Leaf2", C(0.4f, 0.7f, 0.18f), 0.15f);
    static Material MCloud => Mat("Climb_Cloud", C(1f, 1f, 1f), 0.1f, C(0.18f, 0.18f, 0.2f));
    static Material MCloudGold => Mat("Climb_CloudBright", C(1f, 0.97f, 0.86f), 0.1f, C(0.35f, 0.3f, 0.18f));
    static Material MCloudGrey => Mat("Climb_CloudGrey", C(0.62f, 0.66f, 0.74f), 0.05f);
    static Material MCloudDark => Mat("Climb_CloudDark", C(0.3f, 0.33f, 0.42f), 0.05f);
    static Material MCloudStorm => Mat("Climb_CloudStorm", C(0.2f, 0.22f, 0.3f), 0.05f);
    static Material MMeteor => TexMat("Climb_Meteor", C(0.12f, 0.1f, 0.12f), C(0.3f, 0.24f, 0.22f), 4f, 0f, 0.4f, 0.2f, 2.4f);
    static Material MLava => Mat("Climb_Ember", C(1f, 0.45f, 0.1f), 0.6f, C(1f, 0.35f, 0.05f) * 3f);
    static Material MAsteroid => TexMat("Climb_Asteroid", C(0.22f, 0.18f, 0.3f), C(0.46f, 0.4f, 0.52f), 4f, 0f, 0.4f, 0.15f, 2.2f);
    static Material MRing => Mat("Climb_RingIce", C(0.78f, 0.74f, 1f), 0.85f, C(0.08f, 0.06f, 0.16f));
    static Material MMetal => Mat("Climb_SiloMetal", C(0.72f, 0.74f, 0.78f), 0.55f);
    static Material MRed => Mat("Climb_MushroomRed", C(0.9f, 0.16f, 0.12f), 0.35f);
    static Material MWhite => Mat("Climb_White", C(1f, 0.98f, 0.94f), 0.3f);
    static Material MTwine => Mat("Climb_Twine", C(0.62f, 0.46f, 0.22f), 0.1f);
    static Material MGlass => Mat("Climb_WindowGlow", C(1f, 0.92f, 0.6f), 0.5f, C(1f, 0.85f, 0.45f) * 1.6f);
    static Material MGold => Mat("Goal_GoldenEgg", C(1f, 0.84f, 0.3f), 0.9f, C(1f, 0.75f, 0.2f) * 1.8f);

    // ───────────── 기본 도구 ─────────────

    static GameObject Static(GameObject g)
    {
        GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        return g;
    }

    // 원기둥(윗면 평평한 볼록 메시 충돌). top = 윗면 가운데.
    static GameObject Disc(Transform parent, string name, Vector3 top, float radius, float thickness, Material mat, bool collide)
    {
        GameObject g = Prim(PrimitiveType.Cylinder, parent, name, mat, false);
        g.transform.position = top - Vector3.up * thickness / 2f;
        g.transform.localScale = new Vector3(radius * 2f, thickness / 2f, radius * 2f);
        if (collide)
        {
            MeshCollider mc = g.AddComponent<MeshCollider>();
            mc.sharedMesh = g.GetComponent<MeshFilter>().sharedMesh;
            mc.convex = true;
        }
        return g;
    }

    static GameObject Block(Transform parent, string name, Vector3 center, Vector3 size, Quaternion rot, Material mat, bool collide)
    {
        GameObject g = Box(parent, name, center, size, rot, mat);
        if (!collide) RemoveCollider(g);
        return g;
    }

    static GameObject Ball(Transform parent, string name, Vector3 center, Vector3 size, Material mat)
    {
        GameObject g = Prim(PrimitiveType.Sphere, parent, name, mat, false);
        g.transform.position = center;
        g.transform.localScale = size;
        return g;
    }

    static GameObject Rock(Transform parent, string name, Vector3 center, Vector3 size, Material mat, bool collide)
    {
        Mesh mesh = RockMesh(Random.Range(0, 6));
        GameObject g = MeshObject(parent, name, mesh, mat);
        g.transform.position = center;
        g.transform.rotation = Quaternion.Euler(Random.Range(-6f, 6f), Random.Range(0f, 360f), Random.Range(-6f, 6f));
        g.transform.localScale = size;
        if (collide)
        {
            MeshCollider mc = g.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
        }
        return g;
    }

    // 윗면 판정용 보이지 않는 평평한 판(둥근 바위 · 구름 · 나뭇가지 위를 '보이는 윗면'에 맞춰 서게)
    static GameObject TopPlate(Transform parent, Vector3 top, Vector2 size, Quaternion rot)
    {
        GameObject g = new GameObject("TopPlate");
        g.transform.SetParent(parent, false);
        g.transform.position = top - rot * Vector3.up * 0.3f;
        g.transform.rotation = rot;
        BoxCollider b = g.AddComponent<BoxCollider>();
        b.size = new Vector3(size.x, 0.6f, size.y);
        return g;
    }

    static GameObject Group(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            t = new GameObject(name).transform;
            t.SetParent(parent, false);
        }
        return t.gameObject;
    }

    // ───────────── 발판 ─────────────

    static PlatKind PickKind(Sec s, float d)
    {
        float v = Random.value;
        switch (s.theme)
        {
            case Theme.Coop: return PlatKind.Normal;
            case Theme.Meadow: return v < 0.12f ? PlatKind.Tilted : v < 0.3f ? PlatKind.Small : PlatKind.Normal;
            case Theme.Forest: return v < 0.22f ? PlatKind.Bouncy : v < 0.42f ? PlatKind.Small : PlatKind.Normal;
            case Theme.Cliffs: return v < 0.22f ? PlatKind.Tilted : v < 0.42f ? PlatKind.Small : PlatKind.Normal;
            case Theme.Snow: return v < 0.35f ? PlatKind.Slippery : v < 0.5f ? PlatKind.Tilted : v < 0.62f ? PlatKind.Small : PlatKind.Normal;
            case Theme.Sky: return v < 0.25f ? PlatKind.Moving : v < 0.5f ? PlatKind.Small : PlatKind.Normal;
            case Theme.Clouds: return v < 0.18f ? PlatKind.Bouncy : v < 0.3f ? PlatKind.Moving : PlatKind.Normal;
            case Theme.DarkClouds: return v < 0.22f ? PlatKind.Moving : v < 0.3f ? PlatKind.Bouncy : PlatKind.Normal;
            case Theme.Thunder: return v < 0.22f ? PlatKind.Moving : v < 0.48f ? PlatKind.Small : PlatKind.Normal;
            case Theme.Atmosphere: return v < 0.22f ? PlatKind.Moving : v < 0.5f ? PlatKind.Small : v < 0.6f ? PlatKind.Tilted : PlatKind.Normal;
            case Theme.Space: return v < 0.35f ? PlatKind.Spinning : v < 0.55f ? PlatKind.Small : v < 0.7f ? PlatKind.Moving : PlatKind.Normal;
            default: return v < 0.3f ? PlatKind.Slippery : v < 0.55f ? PlatKind.Small : v < 0.78f ? PlatKind.Moving : PlatKind.Spinning;
        }
    }

    static Plat MakePlat(Sec s, Vector3 top, float r, PlatKind kind, Vector3 outward)
    {
        GameObject root = new GameObject($"P{path.Count:000}_{kind}_{top.y:0}m");
        root.transform.SetParent(Group(s.root, "Path").transform, false);
        root.transform.position = top;
        Vector3 tangent = Vector3.Cross(Vector3.up, outward).normalized;
        if (kind == PlatKind.Small) r *= 0.85f;
        if (kind == PlatKind.Slippery) r = Mathf.Max(r, 1.8f);   // 미끄러운 판은 너무 작으면 착지 자체가 불가능

        switch (s.theme)
        {
            case Theme.Coop: CoopPlatform(s, root.transform, top, r, outward); break;
            case Theme.Meadow: MeadowPlatform(root.transform, top, r, kind, outward); break;
            case Theme.Forest: ForestPlatform(s, root.transform, top, r, kind, outward); break;
            case Theme.Cliffs: RockPlatform(root.transform, top, r, kind, outward, false); break;
            case Theme.Snow: RockPlatform(root.transform, top, r, kind, outward, true); break;
            case Theme.Sky: IslandPlatform(root.transform, top, r, kind); break;
            case Theme.Clouds: CloudPlatform(root.transform, top, r, kind, MCloudGold, MCloud); break;
            case Theme.DarkClouds: CloudPlatform(root.transform, top, r, kind, MCloudGold, MCloudGrey); break;
            case Theme.Thunder: CloudPlatform(root.transform, top, r, kind, MCloudGrey, MCloudDark); break;
            case Theme.Atmosphere: MeteorPlatform(root.transform, top, r, kind); break;
            case Theme.Space: AsteroidPlatform(root.transform, top, r, kind); break;
            default: RingPlatform(root.transform, top, r, kind, tangent); break;
        }

        Breadcrumbs(root.transform, top, r, kind == PlatKind.Rest);

        // 종류별 성질
        if (kind == PlatKind.Slippery)
        {
            root.AddComponent<ClimbSurface>().friction = 0.15f;
        }
        if (kind == PlatKind.Bouncy)
        {
            ClimbSurface sf = root.AddComponent<ClimbSurface>();
            sf.launchSpeed = 17f;
        }
        if (kind == PlatKind.Moving || kind == PlatKind.Spinning)
        {
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            ClimbMovingPlatform mp = root.AddComponent<ClimbMovingPlatform>();
            if (kind == PlatKind.Moving)
            {
                // 위아래로 천천히(간격은 그대로, 높이 차만 바뀜) 또는 나선 방향으로 조금.
                // 앞 발판보다 많이 높은 곳(높이 차 7m 이상)은 꼭대기에 있을 때 닿지 않을 수 있어 옆으로만 움직임
                float dhPrev = path.Count > 0 ? top.y - path[path.Count - 1].pos.y : 0f;
                bool vertical = dhPrev < 7f && Random.value < 0.5f;
                Vector3 move = vertical ? Vector3.up * Random.Range(0.8f, 1.4f) : tangent * Random.Range(1f, 1.8f);
                mp.Configure(move, Random.Range(3f, 4.6f), Random.value, Vector3.zero);
            }
            else
            {
                mp.Configure(Vector3.zero, 1f, 0f, new Vector3(0f, Random.Range(18f, 34f) * (Random.value < 0.5f ? 1f : -1f), 0f));
            }
        }
        else
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>()) Static(t.gameObject);
        }

        Plat p = new Plat { pos = top, r = r, t = root.transform, sec = s };
        path.Add(p);
        return p;
    }

    // 길 안내(자연스럽게): 길이 되는 발판에만 빛나는 옥수수 알갱이(모이) + 위로 피어오르는 금빛 반짝임.
    // 닭은 모이를 따라간다 → 멀리서도 반짝임이 '다음에 갈 곳'을 알려 줌. 장식(밟을 수 없는 구름 · 섬)에는 없음.
    static void Breadcrumbs(Transform root, Vector3 top, float r, bool rest)
    {
        Material corn = Mat("Climb_Corn", C(1f, 0.82f, 0.2f), 0.6f, C(1f, 0.7f, 0.1f) * 1.4f);
        int n = rest ? 7 : 4;
        for (int i = 0; i < n; i++)
        {
            Vector2 q = Random.insideUnitCircle * r * 0.45f;
            GameObject k = Prim(PrimitiveType.Capsule, root, "Corn", corn, false);
            k.transform.position = top + new Vector3(q.x, 0.08f, q.y);
            k.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            k.transform.localScale = new Vector3(0.16f, 0.12f, 0.16f);
            k.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        GameObject sp = new GameObject("GuideSparkle");
        sp.transform.SetParent(root, false);
        sp.transform.position = top + Vector3.up * 0.3f;
        ParticleSystem ps = sp.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = 2.2f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
        main.startColor = new Color(1f, 0.85f, 0.35f, 0.9f);
        main.maxParticles = 20;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = rest ? 6f : 4f;
        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = Mathf.Max(0.5f, r * 0.6f);
        sh.rotation = new Vector3(90f, 0f, 0f);
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = 0f; vel.y = 0f; vel.z = 0f;
        vel.y = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;
        sp.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat;
    }

    // 1구간: 낮은 곳은 바닥부터 쌓은 짚더미, 높은 곳은 천장에 밧줄로 매단 나무 선반, 지붕 위는 기둥 위 나무 상자
    static void CoopPlatform(Sec s, Transform root, Vector3 top, float r, Vector3 outward)
    {
        float w = r * 2f;
        Quaternion rot = Quaternion.LookRotation(outward);
        if (top.y > CoopTop + 0.5f)
        {
            // 지붕 위: 기둥 위 나무 상자 + 짚
            Block(root, "Crate", top - Vector3.up * 0.7f, new Vector3(w, 1.4f, w), rot, MWood, true);
            Block(root, "StrawTop", top + Vector3.up * 0.02f, new Vector3(w * 0.85f, 0.08f, w * 0.85f), rot, MStraw, false);
            float postBottom = CoopTop + 0.3f;
            GameObject post = Prim(PrimitiveType.Cylinder, root, "Post", MWood, true);
            float ph = top.y - 1.4f - postBottom;
            post.transform.position = new Vector3(top.x, postBottom + ph / 2f, top.z);
            post.transform.localScale = new Vector3(0.45f, ph / 2f, 0.45f);
            return;
        }
        if (top.y < 9.5f)
        {
            // 바닥부터 쌓은 짚더미(층마다 방향을 바꿔 엇갈려 쌓음)
            float bale = 1.3f;
            int layers = Mathf.Max(1, Mathf.RoundToInt(top.y / bale));
            float h = top.y / layers;
            for (int i = 0; i < layers; i++)
            {
                Quaternion lr = rot * Quaternion.Euler(0f, i % 2 == 0 ? 0f : 90f, 0f);
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector3 c = new Vector3(top.x, h * (i + 0.5f), top.z) + lr * new Vector3(k * w / 4f, 0f, 0f);
                    HayBale(root, c, new Vector3(w / 2f - 0.05f, h - 0.04f, w), lr);
                }
            }
            GameObject col = new GameObject("StackHit");
            col.transform.SetParent(root, false);
            col.transform.SetPositionAndRotation(new Vector3(top.x, top.y / 2f, top.z), rot);
            col.AddComponent<BoxCollider>().size = new Vector3(w, top.y, w);
            return;
        }
        // 천장에 매단 선반: 판자 + 짚 + 밧줄 4개
        Block(root, "Shelf", top - Vector3.up * 0.2f, new Vector3(w, 0.4f, w), rot, MWood, true);
        for (int i = 0; i < 4; i++)
        {
            Vector3 corner = top + rot * new Vector3((i < 2 ? -1 : 1) * (w / 2f - 0.15f), 0f, (i % 2 == 0 ? -1 : 1) * (w / 2f - 0.15f));
            float len = CoopTop - corner.y;
            GameObject rope = Prim(PrimitiveType.Cylinder, root, "Rope", MTwine, false);
            rope.transform.position = corner + Vector3.up * len / 2f;
            rope.transform.localScale = new Vector3(0.08f, len / 2f, 0.08f);
        }
    }

    // 짚더미 한 덩이(묶은 끈 2줄)
    static GameObject HayBale(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
    {
        GameObject b = Block(parent, "HayBale", center, size, rot, MStraw, false);
        for (int k = -1; k <= 1; k += 2)
        {
            Block(b.transform, "Twine", center + rot * new Vector3(0f, 0f, k * size.z * 0.25f), new Vector3(size.x + 0.03f, size.y + 0.03f, 0.05f), rot, MTwine, false)
                .transform.SetParent(parent, true);
        }
        return b;
    }

    static void MeadowPlatform(Transform root, Vector3 top, float r, PlatKind kind, Vector3 outward)
    {
        if (kind == PlatKind.Small)
        {
            // 짚더미 하나(언덕에 박힌 나무 받침 위)
            Quaternion rot = Quaternion.LookRotation(Vector3.Cross(Vector3.up, outward));
            HayBale(root, top - Vector3.up * 0.7f, new Vector3(r * 2f, 1.4f, r * 1.3f), rot);
            GameObject hit = new GameObject("Hit");
            hit.transform.SetParent(root, false);
            hit.transform.SetPositionAndRotation(top - Vector3.up * 0.7f, rot);
            hit.AddComponent<BoxCollider>().size = new Vector3(r * 2f, 1.4f, r * 1.3f);
            Block(root, "Beam", top - Vector3.up * 1.6f - outward * r * 0.6f, new Vector3(0.4f, 0.4f, r * 2.6f), Quaternion.LookRotation(outward), MWood, true);
            return;
        }
        Quaternion tilt = kind == PlatKind.Tilted ? Quaternion.AngleAxis(Random.Range(22f, 34f), Vector3.Cross(Vector3.up, outward)) : Quaternion.identity;
        // 풀 덮인 흙 턱: 아래 흙 바위 + 위 풀 원판(윗면 = 판정)
        Rock(root, "Earth", top - Vector3.up * 1.2f - outward * 0.4f, new Vector3(r * 2.3f, 2.4f, r * 2.3f), MEarth, false);
        GameObject grass = Disc(root, "Grass", top, r, 0.5f, MGrass, true);
        grass.transform.rotation = tilt;
        if (Random.value < 0.35f) Flowers(root, top, r * 0.8f, 5);
    }

    static void ForestPlatform(Sec s, Transform root, Vector3 top, float r, PlatKind kind, Vector3 outward)
    {
        Vector3 c = Axis(top.y);
        float core = CoreRadius(s, top.y);
        if (kind == PlatKind.Bouncy)
        {
            // 나무에 붙은 큰 버섯(빨간 갓 · 흰 점): 위에서 떨어지면 통통 튀어 오름
            GameObject cap = Disc(root, "MushroomCap", top, r, 0.7f, MRed, true);
            Ball(root, "CapDome", top - Vector3.up * 0.15f, new Vector3(r * 2f, 0.7f, r * 2f), MRed);
            for (int i = 0; i < 6; i++)
            {
                Vector2 q = Random.insideUnitCircle * r * 0.75f;
                Ball(root, "Spot", top + new Vector3(q.x, 0.12f, q.y), Vector3.one * Random.Range(0.25f, 0.45f), MWhite);
            }
            Vector3 stemFrom = new Vector3(c.x, top.y - 1.8f, c.z) + outward * core * 0.95f;
            GameObject stem = Prim(PrimitiveType.Cylinder, root, "Stem", MWhite, false);
            Vector3 mid = (stemFrom + (top - Vector3.up * 0.7f)) / 2f;
            stem.transform.position = mid;
            stem.transform.up = ((top - Vector3.up * 0.7f) - stemFrom).normalized;
            stem.transform.localScale = new Vector3(0.6f, Vector3.Distance(stemFrom, top) / 2f, 0.6f);
            return;
        }
        // 나뭇가지: 줄기 표면에서 바깥으로 뻗은 굵은 가지 + 그 위 이끼 낀 평평한 면(윗면 = 판정)
        float len = r * 2f;
        Vector3 inner = new Vector3(c.x, top.y - 0.6f, c.z) + outward * (core - 0.5f);
        Vector3 outer = top - Vector3.up * 0.6f + outward * r;
        GameObject branch = Prim(PrimitiveType.Cylinder, root, "Branch", MBark, false);
        branch.transform.position = (inner + outer) / 2f;
        branch.transform.up = (outer - inner).normalized;
        float thick = kind == PlatKind.Small ? 0.7f : 1.1f;
        branch.transform.localScale = new Vector3(thick, Vector3.Distance(inner, outer) / 2f, thick);
        Quaternion rot = Quaternion.LookRotation(outward);
        float width = kind == PlatKind.Small ? r * 1.1f : r * 1.7f;
        Block(root, "Moss", top - Vector3.up * 0.2f, new Vector3(width, 0.4f, len), rot, MMoss, true);
        // 가지 끝 잎 뭉치(옆 · 아래로만, 뛰어오를 때 머리에 안 걸리게)
        Vector3 tip = outer + outward * 0.8f;
        Ball(root, "Leaves", tip + Vector3.down * 0.9f, new Vector3(2.2f, 1.4f, 2.2f), MLeaf);
        Ball(root, "Leaves2", tip + Vector3.down * 0.8f + Vector3.Cross(Vector3.up, outward) * 0.9f, new Vector3(1.6f, 1.1f, 1.6f), MLeaf2);
    }

    static void RockPlatform(Transform root, Vector3 top, float r, PlatKind kind, Vector3 outward, bool snowy)
    {
        Quaternion rot = Quaternion.LookRotation(outward);
        if (kind == PlatKind.Slippery)
        {
            // 얼음판: 살짝 기울어 있고 매끈하게 빛남
            Quaternion t = Quaternion.AngleAxis(Random.Range(3f, 9f), Vector3.Cross(Vector3.up, outward)) * rot;
            Block(root, "Ice", top - t * Vector3.up * 0.45f, new Vector3(r * 2f, 0.9f, r * 2f), t, MIce, true);
            Rock(root, "IceBase", top - Vector3.up * 1.3f, new Vector3(r * 1.8f, 1.6f, r * 1.8f), MRock, false);
            return;
        }
        if (kind == PlatKind.Tilted)
        {
            Quaternion t = Quaternion.AngleAxis(Random.Range(24f, 38f) * (Random.value < 0.5f ? 1f : -1f), Vector3.Cross(Vector3.up, outward)) * rot;
            Block(root, "Slab", top - t * Vector3.up * 0.5f, new Vector3(r * 2f, 1f, r * 2.2f), t, MRock, true);
            if (snowy) Block(root, "SlabSnow", top + t * Vector3.up * 0.04f, new Vector3(r * 1.9f, 0.1f, r * 2.1f), t, MSnow, false);
            return;
        }
        // 바위 턱: 울퉁불퉁한 바위 + 평평한 윗면(윗면 판정은 보이지 않는 판, 바위 가장자리는 둥글게 처짐)
        Rock(root, "Ledge", top - Vector3.up * 0.9f - outward * 0.3f, new Vector3(r * 2.4f, 2.2f, r * 2.4f), MRock, false);
        GameObject cap = Disc(root, snowy ? "SnowCap" : "LedgeTop", top, r, 0.35f, snowy ? MSnow : MRock, true);
        if (snowy) Ball(root, "SnowLump", top + Vector3.up * 0.02f, new Vector3(r * 1.7f, 0.35f, r * 1.7f), MSnow);
    }

    static void IslandPlatform(Transform root, Vector3 top, float r, PlatKind kind)
    {
        // 떠 있는 섬: 아래로 뾰족한 바위 + 위 풀밭(윗면 = 판정) + 작은 꽃 · 풀
        Rock(root, "IslandRock", top - Vector3.up * (r * 0.9f + 0.2f), new Vector3(r * 2.2f, r * 2.2f, r * 2.2f), MRock, false);
        Rock(root, "IslandTip", top - Vector3.up * (r * 1.9f + 0.4f), new Vector3(r * 1.1f, r * 1.6f, r * 1.1f), MRock, false);
        Disc(root, "IslandGrass", top, r, 0.5f, MGrass, true);
        if (kind != PlatKind.Small && Random.value < 0.4f) Flowers(root, top, r * 0.7f, 6);
    }

    static void CloudPlatform(Transform root, Vector3 top, float r, PlatKind kind, Material topMat, Material bodyMat)
    {
        // 길이 되는 구름: 윗면이 밝고(bright) 납작한 뭉게구름. 판정은 윗면 판(구름 속으로 발이 살짝 묻힘)
        int n = Mathf.Clamp(Mathf.RoundToInt(r * 2.2f), 4, 9);
        for (int i = 0; i < n; i++)
        {
            Vector2 q = Random.insideUnitCircle * r * 0.65f;
            float sz = Random.Range(r * 0.9f, r * 1.4f);
            Ball(root, "Puff", top + new Vector3(q.x, -sz * 0.28f, q.y), new Vector3(sz, sz * 0.6f, sz), i < n / 2 ? topMat : bodyMat);
        }
        Ball(root, "PuffTop", top - Vector3.up * 0.25f, new Vector3(r * 2f, 0.7f, r * 2f), topMat);
        if (kind == PlatKind.Bouncy)
        {
            Ball(root, "Bouncy", top + Vector3.up * 0.05f, new Vector3(r * 1.4f, 0.5f, r * 1.4f), Mat("Climb_CloudPink", C(1f, 0.78f, 0.88f), 0.2f, C(0.4f, 0.2f, 0.3f)));
        }
        TopPlate(root, top - Vector3.up * 0.2f, new Vector2(r * 1.9f, r * 1.9f), Quaternion.identity);
    }

    static void MeteorPlatform(Transform root, Vector3 top, float r, PlatKind kind)
    {
        Rock(root, "Meteor", top - Vector3.up * (r * 0.55f), new Vector3(r * 2.2f, r * 1.2f, r * 2.2f), MMeteor, false);
        if (kind == PlatKind.Tilted)
        {
            Quaternion t = Quaternion.Euler(Random.Range(24f, 36f), Random.Range(0f, 360f), 0f);
            Block(root, "MeteorSlab", top - t * Vector3.up * 0.3f, new Vector3(r * 2f, 0.6f, r * 2f), t, MMeteor, true);
        }
        else
        {
            Disc(root, "MeteorTop", top, r * 0.95f, 0.4f, MMeteor, true);
        }
        // 빛나는 금(갈라진 틈의 불씨)
        for (int i = 0; i < 4; i++)
        {
            GameObject c = MeshObject(root, "Ember", CrystalMesh(), MLava);
            c.transform.position = top + new Vector3(Random.Range(-r, r) * 0.7f, -Random.Range(0.4f, r), Random.Range(-r, r) * 0.7f);
            c.transform.rotation = Random.rotation;
            c.transform.localScale = new Vector3(0.6f, 1.6f, 0.6f);
        }
    }

    static void AsteroidPlatform(Transform root, Vector3 top, float r, PlatKind kind)
    {
        Rock(root, "Asteroid", top - Vector3.up * (r * 0.7f), new Vector3(r * 2.3f, r * 1.6f, r * 2.3f), MAsteroid, false);
        Disc(root, "AsteroidTop", top, r * 0.95f, 0.4f, MAsteroid, true);
        if (Random.value < 0.4f)
        {
            Material[] neon = NeonSet();
            GameObject c = MeshObject(root, "Crystal", CrystalMesh(), neon[Random.Range(0, neon.Length)]);
            c.transform.position = top + new Vector3(Random.Range(-r, r) * 0.5f, -0.3f, Random.Range(-r, r) * 0.5f) + Vector3.down * r * 0.6f;
            c.transform.rotation = Random.rotation;
            c.transform.localScale = new Vector3(0.8f, 2.4f, 0.8f);
        }
    }

    static void RingPlatform(Transform root, Vector3 top, float r, PlatKind kind, Vector3 tangent)
    {
        // 행성 고리 조각: 얇고 넓은 얼음 판(긴 쪽이 나선 방향)
        Quaternion rot = Quaternion.LookRotation(tangent);
        if (kind == PlatKind.Slippery) rot = Quaternion.AngleAxis(Random.Range(5f, 12f), tangent) * rot;
        Block(root, "RingShard", top - rot * Vector3.up * 0.3f, new Vector3(r * 1.6f, 0.6f, r * 2.4f), rot, MRing, true);
        Block(root, "RingShardEdge", top - rot * Vector3.up * 0.75f, new Vector3(r * 1.3f, 0.4f, r * 2f), rot, MAsteroid, false);
    }

    static void Flowers(Transform root, Vector3 top, float radius, int count)
    {
        Color[] cols = { C(1f, 0.95f, 0.4f), C(1f, 0.55f, 0.75f), C(0.95f, 0.95f, 1f), C(0.6f, 0.7f, 1f) };
        for (int i = 0; i < count; i++)
        {
            Vector2 q = Random.insideUnitCircle * radius;
            int k = Random.Range(0, cols.Length);
            Ball(root, "Flower", top + new Vector3(q.x, 0.12f, q.y), Vector3.one * 0.3f, Mat($"Climb_Flower{k}", cols[k], 0.3f));
        }
    }

    // ───────────── 바닥 · 배경 ─────────────

    static void Ground(Transform world)
    {
        Transform g = new GameObject("Ground").transform;
        g.SetParent(world, false);

        Material grass = Mat("Climb_GroundGrass", Color.white, 0.08f);
        Material src = MGrass;
        grass.SetTexture("_BaseMap", src.GetTexture("_BaseMap"));
        grass.SetTexture("_BumpMap", src.GetTexture("_BumpMap"));
        grass.EnableKeyword("_NORMALMAP");
        grass.SetTextureScale("_BaseMap", new Vector2(220f, 220f));
        grass.SetTextureScale("_BumpMap", new Vector2(220f, 220f));
        GameObject plane = Prim(PrimitiveType.Plane, g, "GroundPlane", grass, true);
        plane.transform.position = new Vector3(150f, -0.06f, 60f);   // 닭장 바닥(높이 0)과 겹쳐 깜빡이지 않게 살짝 아래
        plane.transform.localScale = new Vector3(220f, 1f, 220f);
        Static(plane);

        // 멀리 둘러싼 산(안개 속 실루엣)
        Material mountain = Mat("Climb_FarMountain", C(0.42f, 0.62f, 0.78f), 0.05f);
        for (int i = 0; i < 26; i++)
        {
            float a = i / 26f * Mathf.PI * 2f + Random.Range(-0.08f, 0.08f);
            float dist = Random.Range(650f, 900f);
            float h = Random.Range(140f, 360f);
            GameObject m = Rock(g, "FarMountain", new Vector3(150f + Mathf.Cos(a) * dist, h * 0.3f, 60f + Mathf.Sin(a) * dist), new Vector3(h * 1.6f, h, h * 1.4f), mountain, false);
            Static(m);
        }

        // 닭장 둘레 나무 · 울타리 · 풀
        Material leafA = MLeaf, leafB = MLeaf2;
        for (int i = 0; i < 70; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(45f, 260f);
            Vector3 p = new Vector3(Mathf.Cos(a) * dist, 0f, Mathf.Sin(a) * dist);
            if (Vector3.Distance(p, HillCenter) < 32f) continue;
            DecorTree(g, p, Random.Range(0.8f, 1.5f), MBark, Random.value < 0.5f ? leafA : leafB);
        }
        for (int i = 0; i < 48; i++)
        {
            float a = i / 48f * Mathf.PI * 2f;
            Vector3 p = new Vector3(Mathf.Cos(a) * 30f, 0.8f, Mathf.Sin(a) * 30f);
            if (Vector3.Distance(p, HillCenter) < 28f) continue;
            GameObject post = Block(g, "FencePost", p, new Vector3(0.35f, 1.6f, 0.35f), Quaternion.identity, MWood, true);
            Static(post);
            Vector3 nxt = new Vector3(Mathf.Cos(a + Mathf.PI * 2f / 48f) * 30f, 1.1f, Mathf.Sin(a + Mathf.PI * 2f / 48f) * 30f);
            Static(Block(g, "FenceRail", (p + nxt) / 2f + Vector3.up * 0.3f, new Vector3(0.15f, 0.2f, Vector3.Distance(p, nxt)), Quaternion.LookRotation(nxt - p), MWood, false));
        }
        Material tuft = GrassTuftMat(LookFor("Meadow"));
        Mesh cross = CrossQuadMesh();
        for (int i = 0; i < 260; i++)
        {
            Vector2 q = Random.insideUnitCircle * 90f;
            Vector3 p = new Vector3(q.x, 0f, q.y);
            if (Mathf.Abs(p.x) < CoopHalf + 1f && Mathf.Abs(p.z) < CoopHalf + 1f) continue;
            GameObject t = MeshObject(g, "Tuft", cross, tuft);
            t.transform.position = p;
            t.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            t.transform.localScale = Vector3.one * Random.Range(0.8f, 1.6f);
            Static(t);
        }
        for (int i = 0; i < 80; i++)
        {
            Vector2 q = Random.insideUnitCircle * 70f;
            if (Mathf.Abs(q.x) < CoopHalf + 1f && Mathf.Abs(q.y) < CoopHalf + 1f) continue;
            Flowers(g, new Vector3(q.x, 0f, q.y), 1.2f, 3);
        }
    }

    // ───────────── 1구간: 짚 깔린 닭장 ─────────────

    static void Coop(Sec s)
    {
        Transform g = Group(s.root, "Coop").transform;

        // 바닥(짚)
        Static(Block(g, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(CoopHalf * 2f + 1f, 0.5f, CoopHalf * 2f + 1f), Quaternion.identity, MStraw, true));
        for (int i = 0; i < 60; i++)
        {
            Vector2 q = new Vector2(Random.Range(-CoopHalf + 1f, CoopHalf - 1f), Random.Range(-CoopHalf + 1f, CoopHalf - 1f));
            Ball(g, "StrawTuft", new Vector3(q.x, 0.02f, q.y), new Vector3(Random.Range(1f, 2.4f), 0.25f, Random.Range(1f, 2.4f)), MStraw);
        }

        // 벽(빨간 판자) 4면 + 창문 빛
        for (int side = 0; side < 4; side++)
        {
            Quaternion rot = Quaternion.Euler(0f, side * 90f, 0f);
            Vector3 center = rot * new Vector3(0f, CoopTop / 2f, CoopHalf + 0.5f);
            Static(Block(g, $"Wall_{side}", center, new Vector3(CoopHalf * 2f + 2f, CoopTop, 1f), rot, MBarn, true));
            for (int w = -1; w <= 1; w++)
            {
                for (int row = 0; row < 3; row++)
                {
                    Vector3 wp = rot * new Vector3(w * 8f, 6f + row * 9f, CoopHalf - 0.02f);
                    GameObject win = Block(g, "Window", wp, new Vector3(2.6f, 2.2f, 0.1f), rot, MGlass, false);
                    // 창틀
                    Block(win.transform, "Frame", wp + rot * new Vector3(0f, 0f, -0.05f), new Vector3(0.2f, 2.4f, 0.12f), rot, MWood, false);
                    Block(win.transform, "Frame2", wp + rot * new Vector3(0f, 0f, -0.05f), new Vector3(2.8f, 0.2f, 0.12f), rot, MWood, false);
                }
            }
            // 기둥 · 들보
            for (int k = -1; k <= 1; k += 2)
            {
                Vector3 bp = rot * new Vector3(k * (CoopHalf - 0.4f), CoopTop / 2f, CoopHalf - 0.4f);
                Static(Block(g, "Beam", bp, new Vector3(0.8f, CoopTop, 0.8f), rot, MWood, true));
            }
            for (int lv = 1; lv <= 2; lv++)
            {
                Vector3 bp = rot * new Vector3(0f, lv * 10f, CoopHalf - 0.3f);
                Static(Block(g, "CrossBeam", bp, new Vector3(CoopHalf * 2f, 0.6f, 0.5f), rot, MWood, false));
            }
        }

        // 따뜻한 실내 빛
        for (int i = 0; i < 4; i++)
        {
            GameObject lg = new GameObject("CoopLight");
            lg.transform.SetParent(g, false);
            lg.transform.position = new Vector3((i % 2 == 0 ? -1 : 1) * 7f, 10f + (i / 2) * 11f, (i < 2 ? -1 : 1) * 7f);
            Light l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = C(1f, 0.82f, 0.55f);
            l.range = 22f;
            l.intensity = 2.2f;
        }

        // 출발 둥지 + 알 · 조작 안내 표지판
        Vector3 nest = new Vector3(-10f, 0.4f, -10.5f);
        NestRing(g, nest, 1.6f, 0.8f);
        Ball(g, "Egg", nest + Vector3.up * 0.4f, new Vector3(0.7f, 0.9f, 0.7f), MWhite);
        Ball(g, "Egg2", nest + new Vector3(0.6f, 0.35f, 0.3f), new Vector3(0.6f, 0.8f, 0.6f), MWhite);
        Sign3D(g, new Vector3(-3f, 5.5f, -CoopHalf + 0.2f), Quaternion.Euler(0f, 180f, 0f), "HOLD SPACE : CHARGE\nRELEASE : FLY", 1.3f);
        Sign3D(g, new Vector3(-CoopHalf + 0.2f, 5.5f, -3f), Quaternion.Euler(0f, -90f, 0f), "WASD : MOVE   MOUSE : LOOK", 1.1f);
        Sign3D(g, new Vector3(CoopHalf - 0.2f, 14f, 0f), Quaternion.Euler(0f, 90f, 0f), "IN THE AIR\nHOLD SPACE : GLIDE", 1.3f);
        Sign3D(g, new Vector3(0f, 22f, CoopHalf - 0.2f), Quaternion.Euler(0f, 0f, 0f), "THE WAY OUT IS UP", 1.4f);

        // 공중 먼지(햇빛에 떠다니는 짚 가루)
        Particles(g, new Vector3(0f, 15f, 0f), new Vector3(26f, 28f, 26f), 14f, 9f, new Vector3(0f, 0.2f, 0f), 0.12f, C(1f, 0.9f, 0.6f), 0f);
    }

    // 닭장 천장(지붕 바닥) + 마지막 실내 발판 바로 위의 구멍 + 지붕 위 낮은 난간 · 짚더미
    static void CoopCeiling(Sec s, Plat below)
    {
        Transform g = Group(s.root, "Coop").transform;
        const float hole = 5.5f;
        float hx = Mathf.Clamp(below.pos.x, -CoopHalf + hole / 2f + 0.5f, CoopHalf - hole / 2f - 0.5f);
        float hz = Mathf.Clamp(below.pos.z, -CoopHalf + hole / 2f + 0.5f, CoopHalf - hole / 2f - 0.5f);
        float y = CoopTop;
        float t = 0.6f;
        float x0 = -CoopHalf - 1f, x1 = CoopHalf + 1f, z0 = -CoopHalf - 1f, z1 = CoopHalf + 1f;
        float hx0 = hx - hole / 2f, hx1 = hx + hole / 2f, hz0 = hz - hole / 2f, hz1 = hz + hole / 2f;
        void Slab(string n, float ax, float bx, float az, float bz)
        {
            if (bx - ax < 0.05f || bz - az < 0.05f) return;
            Static(Block(g, n, new Vector3((ax + bx) / 2f, y, (az + bz) / 2f), new Vector3(bx - ax, t, bz - az), Quaternion.identity, MWood, true));
        }
        Slab("Roof_W", x0, hx0, z0, z1);
        Slab("Roof_E", hx1, x1, z0, z1);
        Slab("Roof_S", hx0, hx1, z0, hz0);
        Slab("Roof_N", hx0, hx1, hz1, z1);
        // 구멍 테두리(밝은 빛 + 나무 틀)
        GameObject lg = new GameObject("HoleLight");
        lg.transform.SetParent(g, false);
        lg.transform.position = new Vector3(hx, y - 2f, hz);
        Light l = lg.AddComponent<Light>();
        l.type = LightType.Spot;
        l.spotAngle = 70f;
        l.range = 30f;
        l.intensity = 6f;
        l.color = C(1f, 0.95f, 0.8f);
        lg.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        for (int k = 0; k < 4; k++)
        {
            Quaternion r = Quaternion.Euler(0f, k * 90f, 0f);
            Block(g, "HoleFrame", new Vector3(hx, y + 0.35f, hz) + r * new Vector3(0f, 0f, hole / 2f), new Vector3(hole + 0.4f, 0.2f, 0.3f), r, MGlass, false);
        }
        // 지붕 위: 짚 · 난간 · 풍향계
        for (int i = 0; i < 6; i++)
        {
            Vector3 p = new Vector3(Random.Range(-11f, 11f), y + 0.95f, Random.Range(-11f, 11f));
            if (Mathf.Abs(p.x - hx) < hole && Mathf.Abs(p.z - hz) < hole) continue;
            if (new Vector2(p.x, p.z).magnitude < SiloR + 2f) continue;
            HayBale(g, p, new Vector3(2.2f, 1.3f, 1.4f), Quaternion.Euler(0f, Random.Range(0f, 180f), 0f));
        }
        for (int side = 0; side < 4; side++)
        {
            Quaternion rot = Quaternion.Euler(0f, side * 90f, 0f);
            Static(Block(g, "Parapet", rot * new Vector3(0f, y + 0.8f, CoopHalf + 0.6f), new Vector3(CoopHalf * 2f + 2.4f, 1f, 0.6f), rot, MBarn, true));
        }
    }

    // ───────────── 기둥(구간의 중심 지형) ─────────────

    static void Core(Sec s)
    {
        Transform g = Group(s.root, "Core").transform;
        switch (s.theme)
        {
            case Theme.Coop:
            {
                // 닭장 가운데를 뚫고 올라가는 사일로(위는 둥근 지붕)
                // 사일로 꼭대기는 52m(평원으로 건너가는 길보다 아래)
                const float siloTop = 52f;
                GameObject silo = Prim(PrimitiveType.Cylinder, g, "Silo", MMetal, false);
                silo.transform.position = new Vector3(0f, siloTop / 2f, 0f);
                silo.transform.localScale = new Vector3(SiloR * 2f, siloTop / 2f, SiloR * 2f);
                MeshCollider mc = silo.AddComponent<MeshCollider>();
                mc.sharedMesh = silo.GetComponent<MeshFilter>().sharedMesh;
                mc.convex = true;
                Static(silo);
                for (int k = 1; k < 11; k++) Static(Block(g, "SiloRing", new Vector3(0f, k * 5f, 0f), new Vector3(SiloR * 2f + 0.15f, 0.25f, SiloR * 2f + 0.15f), Quaternion.identity, MBarn, false));
                GameObject dome = Ball(g, "SiloDome", new Vector3(0f, siloTop, 0f), new Vector3(SiloR * 2.1f, SiloR * 1.6f, SiloR * 2.1f), MBarn);
                dome.AddComponent<SphereCollider>();
                Static(dome);
                break;
            }
            case Theme.Meadow:
            {
                // 닭장 옆 가파른 풀 언덕(아래는 넓고 위는 좁음)
                // 울퉁불퉁함(±1.1m)은 기둥 반지름 안쪽으로만: 점프 길(반지름 + 1.2m 밖)로 튀어나오지 않게
                Mesh hill = FrustumMesh("Climb_Hill", y => Axis(y), 0f, 180f, y => CoreRadius(s, y) - 0.6f, 40, 60, 0.4f, 18f);
                GameObject h = MeshObject(g, "Hill", hill, MGrass);
                h.AddComponent<MeshCollider>().sharedMesh = hill;
                Static(h);
                // 언덕 꼭대기 = 숲 구간의 시작 쉼터(떨어지면 받아 주는 넓은 풀밭)
                Static(Disc(g, "HillTop", Axis(180f), CoreRadius(s, 180f) + 0.3f, 1f, MGrass, true));   // 처마처럼 튀어나오지 않게
                Flowers(g, Axis(180f), 9f, 30);
                // 언덕에 박힌 바위 · 흙 드러난 곳(판정 없음, 표면 안쪽)
                for (int i = 0; i < 40; i++)
                {
                    float y = Random.Range(4f, 175f);
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    Vector3 c = Axis(y);
                    float r = CoreRadius(s, y) - 1.8f;
                    Static(Rock(g, "HillRock", c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r, Vector3.one * Random.Range(2f, 3.5f), MEarth, false));
                }
                break;
            }
            case Theme.Forest:
            {
                // 언덕 꼭대기에서 자란 거대한 나무: 줄기 + 뿌리 + 꼭대기 잎(길보다 바깥 · 위라서 길을 막지 않음)
                Mesh trunk = FrustumMesh("Climb_Trunk", y => Axis(y), 179f, 366f, y => CoreRadius(s, y) - 0.5f, 28, 40, 0.45f, 10f);
                GameObject t = MeshObject(g, "Trunk", trunk, MBark);
                t.AddComponent<MeshCollider>().sharedMesh = trunk;
                Static(t);
                for (int i = 0; i < 7; i++)
                {
                    float a = i / 7f * Mathf.PI * 2f;
                    Vector3 c = Axis(181f);
                    Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    GameObject root = Prim(PrimitiveType.Capsule, g, "Root", MBark, false);
                    root.transform.position = c + dir * 9f + Vector3.down * 0.5f;
                    root.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(80f, 0f, 0f);
                    root.transform.localScale = new Vector3(2.2f, 5f, 2.2f);
                    Static(root);
                }
                // 꼭대기 잎: 길(반지름 약 9.5m)보다 바깥, 절벽이 시작되는 높이(362m)보다 아래 → 길 · 시야를 가리지 않음
                for (int i = 0; i < 26; i++)
                {
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    float y = Random.Range(330f, 352f);
                    float d = Random.Range(19f, 27f);
                    Vector3 c = Axis(y) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d;
                    Static(Ball(g, "Canopy", c, new Vector3(Random.Range(10f, 16f), Random.Range(7f, 11f), Random.Range(10f, 16f)), Random.value < 0.5f ? MLeaf : MLeaf2));
                }
                Particles(g, Axis(270f), new Vector3(40f, 180f, 40f), 25f, 12f, new Vector3(0.4f, -1.2f, 0.2f), 0.35f, C(0.5f, 0.75f, 0.2f), 0f);
                break;
            }
            case Theme.Cliffs:
            case Theme.Snow:
            {
                bool snow = s.theme == Theme.Snow;
                float a = snow ? 538f : 365f, b = snow ? 726f : 545f;   // 절벽 기둥은 숲 길이 끝난 뒤부터(겹치지 않게)
                // 울퉁불퉁함은 기둥 반지름 안쪽으로만(점프 길 침범 없음)
                Mesh col = FrustumMesh(snow ? "Climb_SnowPeak" : "Climb_Cliff", y => Axis(y), a, b, y => CoreRadius(s, y) - 1.6f, 30, 46, 1.5f, 14f);
                GameObject c = MeshObject(g, snow ? "SnowColumn" : "CliffColumn", col, snow ? MSnow : MRock);
                c.AddComponent<MeshCollider>().sharedMesh = col;
                Static(c);
                // 큰 바위 덩어리를 표면에 붙여 울퉁불퉁하게(바깥 끝이 기둥 반지름을 넘지 않는 크기 · 위치)
                for (int i = 0; i < 70; i++)
                {
                    float y = Random.Range(a + 4f, b - 2f);
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    Vector3 cc = Axis(y);
                    float size = Random.Range(2.6f, 4f);
                    float rr = CoreRadius(s, y) - size * 0.6f;
                    GameObject rock = Rock(g, "CliffRock", cc + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * rr, new Vector3(size, Random.Range(4f, 8f), size), snow && Random.value < 0.5f ? MSnow : MRock, true);
                    Static(rock);
                }
                if (snow)
                {
                    // 꼭대기 뾰족한 눈 봉우리
                    Mesh peak = FrustumMesh("Climb_SnowTip", y => Axis(y), 724f, 742f, y => Mathf.Lerp(7f, 0.3f, Mathf.InverseLerp(724f, 742f, y)), 20, 8, 0.6f, 8f);
                    GameObject p = MeshObject(g, "Peak", peak, MSnow);
                    p.AddComponent<MeshCollider>().sharedMesh = peak;
                    Static(p);
                    Particles(g, Axis(630f), new Vector3(70f, 200f, 70f), 120f, 8f, new Vector3(1.5f, -2.2f, 0.4f), 0.22f, Color.white, 0f);
                }
                else
                {
                    // 나무 꼭대기에서 바위가 솟은 자리: 아래쪽 뿌리 같은 바위
                    for (int i = 0; i < 10; i++)
                    {
                        float ang = i / 10f * Mathf.PI * 2f;
                        Static(Rock(g, "CliffBase", Axis(356f) + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 8f, new Vector3(5f, 7f, 5f), MRock, false));
                    }
                }
                break;
            }
            case Theme.Sky:
            {
                // 아래 구름 바다 + 멀리 떠 있는 섬들
                CloudSea(g, Axis(745f), 60f, 320f, 90);
                for (int i = 0; i < 16; i++)
                {
                    float y = Random.Range(730f, 880f);
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    Vector3 c = Axis(y) + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * Random.Range(60f, 140f);
                    float r = Random.Range(6f, 14f);
                    Static(Rock(g, "FarIsland", c - Vector3.up * r, new Vector3(r * 2f, r * 2f, r * 2f), MRock, false));
                    Static(Disc(g, "FarIslandGrass", c, r, 1f, MGrass, false));
                    if (Random.value < 0.6f) DecorTree(g, c, Random.Range(0.5f, 0.9f), MBark, MLeaf);
                }
                break;
            }
            case Theme.Clouds:
            case Theme.DarkClouds:
            case Theme.Thunder:
            {
                // 멀리 큰 구름 덩어리(밟을 수 없음, 흐린 색) - 7 · 8구간은 길 가까이에도 겁주는 구름이 많음
                Material m = s.theme == Theme.Clouds ? MCloudGrey : s.theme == Theme.DarkClouds ? MCloudDark : MCloudStorm;
                for (int i = 0; i < 40; i++)
                {
                    float y = Random.Range(s.y0, s.y1);
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    Vector3 c = Axis(y) + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * Random.Range(40f, 120f);
                    float sz = Random.Range(18f, 40f);
                    Static(Ball(g, "BigCloud", c, new Vector3(sz * 1.6f, sz * 0.7f, sz * 1.3f), m));
                }
                if (s.theme == Theme.Clouds) CloudSea(g, Axis(s.y0 - 6f), 30f, 260f, 70);
                if (s.theme != Theme.Clouds)
                {
                    // 비
                    Particles(g, Axis((s.y0 + s.y1) / 2f), new Vector3(60f, s.y1 - s.y0 + 30f, 60f), 450f, 2f, new Vector3(0.6f, -26f, 0.2f), 0.06f, C(0.7f, 0.75f, 0.9f), 0f, 0.6f);
                }
                break;
            }
            case Theme.Atmosphere:
            {
                Aurora(g, s);
                Particles(g, Axis(1500f), new Vector3(120f, 220f, 120f), 160f, 6f, Vector3.zero, 0.14f, C(0.85f, 0.9f, 1f), 0f);
                break;
            }
            case Theme.Space:
            {
                Stars(g, Axis(1700f), 700f, 2500);
                for (int i = 0; i < 5; i++)
                {
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    Vector3 c = Axis(Random.Range(1620f, 1900f)) + new Vector3(Mathf.Cos(ang), Random.Range(-0.2f, 0.3f), Mathf.Sin(ang)) * Random.Range(320f, 520f);
                    Material[] neon = NeonSet();
                    float r = Random.Range(18f, 60f);
                    Static(Ball(g, "FarPlanet", c, Vector3.one * r * 2f, Mat($"Climb_Planet{i}", Color.Lerp(neon[i % neon.Length].GetColor("_BaseColor"), Color.white, 0.25f), 0.2f, neon[i % neon.Length].GetColor("_BaseColor") * 0.25f)));
                }
                // 낮은 중력 구역(구간 전체)
                GameObject z = new GameObject("LowGravity");
                z.transform.SetParent(g, false);
                z.transform.position = Axis((s.y0 + s.y1) / 2f);
                BoxCollider bc = z.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.size = new Vector3(180f, s.y1 - s.y0 + 10f, 180f);
                z.AddComponent<ClimbZone>().Configure(Vector3.zero, SpaceGravity, 0f);
                break;
            }
            case Theme.Planet:
            {
                Stars(g, Axis(1900f), 700f, 1500);
                // 고리를 두른 거대한 행성(배경)
                Vector3 c = Axis(1880f) + new Vector3(-260f, -60f, 280f);
                Static(Ball(g, "GiantPlanet", c, Vector3.one * 300f, Mat("Climb_GiantPlanet", C(0.85f, 0.6f, 0.95f), 0.2f, C(0.12f, 0.06f, 0.18f))));
                GameObject ring = Prim(PrimitiveType.Cylinder, g, "PlanetRing", Mat("Climb_PlanetRing", C(0.95f, 0.85f, 1f), 0.6f, C(0.3f, 0.25f, 0.4f)), false);
                ring.transform.position = c;
                ring.transform.rotation = Quaternion.Euler(18f, 0f, 12f);
                ring.transform.localScale = new Vector3(560f, 0.5f, 560f);
                Static(ring);
                break;
            }
        }
    }

    // 기울어진 축을 따라 가는 원뿔대 메시(울퉁불퉁한 표면, 위 · 아래 뚜껑 없음)
    static Mesh FrustumMesh(string name, System.Func<float, Vector3> center, float y0, float y1, System.Func<float, float> radius, int segments, int rings, float noise, float uvScale)
    {
        Vector3[] v = new Vector3[(segments + 1) * (rings + 1)];
        Vector2[] uv = new Vector2[v.Length];
        for (int j = 0; j <= rings; j++)
        {
            float y = Mathf.Lerp(y0, y1, j / (float)rings);
            Vector3 c = center(y);
            float r = radius(y);
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                float n = (Fbm(Mathf.Cos(a) * 1.7f + y * 0.05f, Mathf.Sin(a) * 1.7f + y * 0.03f, 3) - 0.5f) * 2f * noise;
                float rr = Mathf.Max(0.2f, r + n);
                v[j * (segments + 1) + i] = new Vector3(c.x + Mathf.Cos(a) * rr, y, c.z + Mathf.Sin(a) * rr);
                uv[j * (segments + 1) + i] = new Vector2(i / (float)segments * Mathf.Max(1f, r * 2f * Mathf.PI / uvScale), (y - y0) / uvScale);
            }
        }
        int[] t = new int[segments * rings * 6];
        int k = 0;
        for (int j = 0; j < rings; j++)
        {
            for (int i = 0; i < segments; i++)
            {
                int a = j * (segments + 1) + i, b = a + 1, c = a + segments + 1, d = c + 1;
                t[k++] = a; t[k++] = c; t[k++] = b;
                t[k++] = b; t[k++] = c; t[k++] = d;
            }
        }
        Mesh m = new Mesh { name = name };
        if (v.Length > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.vertices = v;
        m.uv = uv;
        m.triangles = t;
        m.RecalculateNormals();
        // 바깥을 향하게
        Vector3 n0 = m.normals[segments / 4];
        Vector3 out0 = v[segments / 4] - center(y0);
        out0.y = 0f;
        if (Vector3.Dot(n0, out0) < 0f)
        {
            for (int q = 0; q < t.Length; q += 3) (t[q + 1], t[q + 2]) = (t[q + 2], t[q + 1]);
            m.triangles = t;
            m.RecalculateNormals();
        }
        m.RecalculateTangents();
        m.RecalculateBounds();
        return SaveMesh(m);
    }

    // ───────────── 장식 · 기믹 ─────────────

    static void Dress(Sec s)
    {
        List<Plat> mine = path.FindAll(p => p.sec == s);
        Transform g = Group(s.root, "Gimmicks").transform;
        float d = s.D01;

        for (int i = 1; i < mine.Count; i++)
        {
            Plat a = mine[i - 1], b = mine[i];
            if (b.r >= 3.2f) continue;   // 쉼터 직전에는 기믹 없음
            Vector3 mid = (a.pos + b.pos) / 2f;
            Vector3 dir = b.pos - a.pos; dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) continue;
            dir.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            float roll = Random.value;

            switch (s.theme)
            {
                case Theme.Cliffs:
                    if (roll < 0.14f) Updraft(g, a.pos, 14f);
                    else if (roll < 0.26f) Gust(g, mid + Vector3.up * 3f, side * Random.Range(7f, 10f), new Vector3(10f, 12f, 10f));
                    else if (roll < 0.36f) Flock(g, mid + Vector3.up * Random.Range(2.5f, 4f), side, 3, false);
                    break;
                case Theme.Snow:
                    if (roll < 0.22f) Icicles(g, Mathf.Max(a.pos.y, b.pos.y) + Random.Range(3.6f, 5f), mid);
                    else if (roll < 0.34f) Gust(g, mid + Vector3.up * 3f, side * Random.Range(7f, 11f), new Vector3(10f, 12f, 10f));
                    break;
                case Theme.Sky:
                    if (roll < 0.3f) Flock(g, mid + Vector3.up * Random.Range(2f, 4f), side, 4, false);
                    break;
                case Theme.Clouds:
                case Theme.DarkClouds:
                    // 어려워 보이는 장식: 길 옆 흐린 구름(밟으면 빠짐) · 멀리 번쩍이는 번개(닿지 않음)
                    if (roll < 0.55f) Decoy(g, mid + side * Random.Range(3.5f, 6f) * (Random.value < 0.5f ? 1f : -1f) + Vector3.up * Random.Range(-2f, 2f), s.theme == Theme.Clouds ? MCloudGrey : MCloudDark);
                    if (s.theme == Theme.DarkClouds && roll > 0.8f) Lightning(g, mid + side * Random.Range(14f, 22f), Random.Range(14f, 22f), true, false, Random.value * 3f);
                    break;
                case Theme.Thunder:
                    if (roll < 0.35f) Lightning(g, mid - Vector3.up * 3f, Mathf.Max(a.pos.y, b.pos.y) - mid.y + 10f, true, true, Random.value * 3f);   // 길 위: 박자 맞춰 지나가기
                    else if (roll < 0.7f) Lightning(g, mid + side * Random.Range(3f, 4f) * (Random.value < 0.5f ? 1f : -1f) - Vector3.up * 4f, 14f, false, true, 0f);   // 길 옆: 늘 켜져 있음(곧게 뛰면 안 닿는 거리)
                    break;
                case Theme.Atmosphere:
                    if (roll < 0.22f) Gust(g, mid + Vector3.up * 3f, side * Random.Range(9f, 13f), new Vector3(10f, 12f, 10f), 2.5f);
                    else if (roll < 0.36f) Comet(g, mid + Vector3.up * Random.Range(2f, 4f), side);
                    break;
                case Theme.Space:
                    if (roll < 0.25f) Comet(g, mid + Vector3.up * Random.Range(2f, 5f), side);
                    break;
                case Theme.Planet:
                    if (roll < 0.3f) Comet(g, mid + Vector3.up * Random.Range(2f, 5f), side);
                    else if (roll < 0.42f) Gust(g, mid + Vector3.up * 3f, side * Random.Range(8f, 12f), new Vector3(10f, 12f, 10f), 2f);
                    break;
            }
        }
    }

    // 상승풍: 발판 위로 솟는 바람(가속 + 위로 날리는 먼지)
    static void Updraft(Transform parent, Vector3 at, float height)
    {
        GameObject z = new GameObject("Updraft");
        z.transform.SetParent(parent, false);
        z.transform.position = at + Vector3.up * (height / 2f + 1f);
        BoxCollider bc = z.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(5f, height, 5f);
        z.AddComponent<ClimbZone>().Configure(new Vector3(0f, 16f, 0f), 1f, 0f);
        Particles(z.transform, z.transform.position, new Vector3(4f, 0.5f, 4f), 26f, 1.6f, new Vector3(0f, 9f, 0f), 0.18f, C(0.9f, 0.85f, 0.7f), 0f);
    }

    // 돌풍: period초마다 세졌다 약해지는 옆바람(바람 줄)
    static void Gust(Transform parent, Vector3 center, Vector3 accel, Vector3 size, float period = 3f)
    {
        GameObject z = new GameObject("Gust");
        z.transform.SetParent(parent, false);
        z.transform.position = center;
        BoxCollider bc = z.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = size;
        z.AddComponent<ClimbZone>().Configure(accel, 1f, period);
        Particles(z.transform, center - accel.normalized * size.x * 0.5f, new Vector3(1f, size.y, size.z), 30f, 1.2f, accel.normalized * 10f, 0.08f, new Color(1f, 1f, 1f, 0.6f), 0f, 3f);
    }

    // 새 떼: 길을 가로질러 왕복(닿으면 밀려남)
    static void Flock(Transform parent, Vector3 center, Vector3 across, int count, bool space)
    {
        GameObject f = new GameObject("BirdFlock");
        f.transform.SetParent(parent, false);
        f.transform.position = center;
        f.transform.rotation = Quaternion.LookRotation(across);
        Material body = Mat("Climb_Bird", C(0.32f, 0.24f, 0.18f), 0.2f);
        Material wing = Mat("Climb_BirdWing", C(0.22f, 0.16f, 0.12f), 0.2f);
        for (int i = 0; i < count; i++)
        {
            Vector3 off = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-0.8f, 0.8f), -i * 1.6f);
            GameObject bird = Ball(f.transform, "Bird", center + f.transform.rotation * off, new Vector3(0.7f, 0.6f, 1.3f), body);
            bird.transform.rotation = f.transform.rotation;
            GameObject w = Block(bird.transform, "Wings", bird.transform.position + Vector3.up * 0.1f, new Vector3(2.6f, 0.08f, 0.6f), f.transform.rotation, wing, false);
            w.transform.SetParent(bird.transform, true);
            SphereCollider sc = bird.AddComponent<SphereCollider>();
            sc.radius = 0.6f;
        }
        Rigidbody rb = f.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        f.AddComponent<ClimbMovingPlatform>().Configure(across.normalized * Random.Range(10f, 14f), Random.Range(5f, 7f), Random.value, Vector3.zero);
        f.AddComponent<ClimbHazard>().Configure(ClimbHazard.Kind.Push, 9f, 1f, 0.35f, Vector3.zero);
    }

    // 고드름: 길 위에 떠 있는 얼음 덩어리 아래로 매달림(너무 높이 뛰면 맞고 떨어짐)
    static void Icicles(Transform parent, float bottomY, Vector3 at)
    {
        GameObject g = new GameObject("Icicles");
        g.transform.SetParent(parent, false);
        g.transform.position = new Vector3(at.x, bottomY, at.z);
        Rock(g.transform, "IceChunk", new Vector3(at.x, bottomY + 3.2f, at.z), new Vector3(4f, 1.6f, 4f), MSnow, false);
        for (int i = 0; i < 6; i++)
        {
            Vector2 q = Random.insideUnitCircle * 1.6f;
            float len = Random.Range(1.6f, 2.6f);
            GameObject ic = MeshObject(g.transform, "Icicle", CrystalMesh(), MIce);
            ic.transform.position = new Vector3(at.x + q.x, bottomY + len / 2f + (2.6f - len), at.z + q.y);
            ic.transform.localScale = new Vector3(0.9f, len, 0.9f);
            CapsuleCollider cc = ic.AddComponent<CapsuleCollider>();
            cc.radius = 0.2f;
            cc.height = 1f;
        }
        g.AddComponent<ClimbHazard>().Configure(ClimbHazard.Kind.Push, 5f, -6f, 0.3f, Vector3.zero);
    }

    // 밟을 수 없는 흐린 구름(장식)
    static void Decoy(Transform parent, Vector3 center, Material m)
    {
        GameObject g = new GameObject("DecoyCloud");
        g.transform.SetParent(parent, false);
        g.transform.position = center;
        for (int i = 0; i < 4; i++)
        {
            Vector2 q = Random.insideUnitCircle * 1.6f;
            float sz = Random.Range(2.2f, 3.4f);
            Static(Ball(g.transform, "Wisp", center + new Vector3(q.x, Random.Range(-0.3f, 0.3f), q.y), new Vector3(sz, sz * 0.55f, sz), m));
        }
    }

    // 번개: 지그재그 노란 줄기. flicker면 박자 맞춰 꺼졌다 켜짐(꺼진 동안 지나갈 수 있음), 아니면 늘 켜짐.
    // hazard가 아니면 장식(먼 곳에서 번쩍이기만, 닿지 않음)
    static void Lightning(Transform parent, Vector3 bottom, float height, bool flicker, bool hazard, float delay)
    {
        GameObject g = new GameObject(hazard ? "Lightning" : "FarLightning");
        g.transform.SetParent(parent, false);
        g.transform.position = bottom;
        Material bolt = Mat("Climb_Bolt", C(1f, 0.86f, 0.1f), 0.5f, C(1f, 0.86f, 0.1f) * 3f);
        int n = Mathf.Max(3, Mathf.RoundToInt(height / 2.5f));
        Vector3 p = bottom;
        for (int i = 0; i < n; i++)
        {
            Vector3 q = bottom + Vector3.up * height * (i + 1) / n + new Vector3(Random.Range(-0.9f, 0.9f), 0f, Random.Range(-0.9f, 0.9f));
            if (i == n - 1) q = bottom + Vector3.up * height;
            GameObject seg = Prim(PrimitiveType.Cube, g.transform, "Bolt", bolt, hazard);
            seg.transform.position = (p + q) / 2f;
            seg.transform.rotation = Quaternion.LookRotation(q - p);
            seg.transform.localScale = new Vector3(0.45f, 0.45f, Vector3.Distance(p, q) + 0.2f);
            p = q;
        }
        FlickerHazard fh = g.AddComponent<FlickerHazard>();
        SetField(fh, "flicker", flicker);
        SetField(fh, "offDuration", Random.value < 0.5f ? 1f : 1.6f);
        SetField(fh, "onDuration", 1.3f);
        SetField(fh, "startDelay", delay);
        if (hazard) g.AddComponent<ClimbHazard>().Configure(ClimbHazard.Kind.Shock, 8f, -3f, 0.5f, Vector3.zero);
    }

    // 혜성: 빛나는 머리 + 꼬리, 길을 가로질러 왕복
    static void Comet(Transform parent, Vector3 center, Vector3 across)
    {
        GameObject g = new GameObject("Comet");
        g.transform.SetParent(parent, false);
        g.transform.position = center;
        Material head = Mat("Climb_Comet", C(0.75f, 0.95f, 1f), 0.6f, C(0.5f, 0.85f, 1f) * 3f);
        GameObject h = Ball(g.transform, "Head", center, Vector3.one * 1.6f, head);
        h.AddComponent<SphereCollider>().radius = 0.45f;
        Particles(g.transform, center, Vector3.one * 0.6f, 60f, 0.8f, Vector3.zero, 0.5f, C(0.6f, 0.9f, 1f), 0f, 1f, true);
        Rigidbody rb = g.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        g.AddComponent<ClimbMovingPlatform>().Configure(across.normalized * Random.Range(16f, 22f), Random.Range(6f, 8f), Random.value, Vector3.zero);
        g.AddComponent<ClimbHazard>().Configure(ClimbHazard.Kind.Push, 12f, 2f, 0.4f, Vector3.zero);
    }

    static void CloudSea(Transform parent, Vector3 center, float inner, float outer, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float d = Mathf.Sqrt(Random.Range(inner * inner, outer * outer));
            float sz = Random.Range(20f, 46f);
            Static(Ball(parent, "CloudSea", center + new Vector3(Mathf.Cos(a) * d, Random.Range(-4f, 3f), Mathf.Sin(a) * d), new Vector3(sz * 1.5f, sz * 0.45f, sz * 1.3f), MCloud));
        }
    }

    static void Aurora(Transform parent, Sec s)
    {
        Material[] neon = { NeonMat("Climb_AuroraGreen", C(0.3f, 1f, 0.6f), 2.5f), NeonMat("Climb_AuroraPurple", C(0.7f, 0.4f, 1f), 2.5f) };
        for (int i = 0; i < 9; i++)
        {
            float y = Random.Range(s.y0 + 20f, s.y1 + 40f);
            float ang = Random.Range(0f, Mathf.PI * 2f);
            Vector3 c = Axis(y) + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * Random.Range(190f, 260f);
            GameObject band = Block(parent, "Aurora", c, new Vector3(Random.Range(90f, 160f), Random.Range(40f, 70f), 0.3f), Quaternion.LookRotation(c - Axis(y)) * Quaternion.Euler(0f, 0f, Random.Range(-10f, 10f)), neon[i % 2], false);
            band.AddComponent<GlowPulse>();   // 오로라가 숨 쉬듯 일렁임(움직이므로 정적 배칭 안 함)
        }
    }

    static void Stars(Transform parent, Vector3 center, float radius, int count)
    {
        GameObject go = new GameObject("Stars");
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.startLifetime = 100000f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 3.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, C(0.75f, 0.85f, 1f));
        main.maxParticles = count;
        main.prewarm = false;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = radius;
        sh.radiusThickness = 0.05f;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat;
    }

    static void Particles(Transform parent, Vector3 center, Vector3 box, float rate, float lifetime, Vector3 velocity, float size, Color color, float gravity, float stretch = 0f, bool local = false)
    {
        GameObject go = new GameObject("Particles");
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.4f);
        main.startColor = color;
        main.gravityModifier = gravity;
        main.maxParticles = Mathf.CeilToInt(rate * lifetime * 1.3f) + 10;
        main.prewarm = true;
        main.simulationSpace = local ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = rate;
        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = box;
        if (velocity.sqrMagnitude > 0f)
        {
            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = velocity.x; vel.y = velocity.y; vel.z = velocity.z;
        }
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;
        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = ParticleMat;
        if (stretch > 0f)
        {
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = stretch * 0.05f;
            r.lengthScale = 1f;
        }
    }

    // ───────────── 표지판 · 체크포인트 · 도착 ─────────────

    static GameObject Sign3D(Transform parent, Vector3 pos, Quaternion rot, string text, float size)
    {
        GameObject go = new GameObject("Sign");
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, rot);
        TextMeshPro t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.fontSize = size * 10f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = C(1f, 0.97f, 0.86f);
        t.fontStyle = FontStyles.Bold;
        t.outlineWidth = 0.25f;
        t.outlineColor = new Color32(40, 24, 10, 255);
        t.rectTransform.sizeDelta = new Vector2(30f, 8f);
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return go;
    }

    // 구간 시작 발판의 바깥 가장자리에 나무 표지판(구간 번호 · 이름)
    static void SectionSign(Sec s, Plat p)
    {
        Vector3 outward = OutwardAt(s, p.pos);
        Vector3 at = p.pos + outward * (p.r - 0.5f);
        Transform g = Group(s.root, "Signs").transform;
        GameObject post = Block(g, "SignPost", at + Vector3.up * 1.2f, new Vector3(0.25f, 2.4f, 0.25f), Quaternion.identity, MWood, false);   // 장식: 판정 없음(점프가 걸리지 않게)
        Static(post);
        // 발판 위(안쪽)에서 읽히게 글자는 안쪽을 봄. 시야를 덜 가리게 작고 낮게
        Quaternion face = Quaternion.LookRotation(-outward);
        Static(Block(g, "SignBoard", at + Vector3.up * 2.5f, new Vector3(4.6f, 1.1f, 0.16f), face, MWood, false));
        Sign3D(g, at + Vector3.up * 2.5f - outward * 0.12f, Quaternion.LookRotation(outward), $"{s.number}. {s.title.ToUpperInvariant()}", 0.55f);
    }

    static void CheckpointNest(Sec s, Plat p)
    {
        Transform g = Group(s.root, "Checkpoint").transform;
        Vector3 at = p.pos - OutwardAt(s, p.pos) * (p.r * 0.35f);
        NestRing(g, at + Vector3.up * 0.3f, 1.3f, 0.7f);
        GameObject egg = Ball(g, "CheckpointEgg", at + Vector3.up * 0.75f, new Vector3(0.9f, 1.15f, 0.9f), Mat("Checkpoint_Egg", C(1f, 0.97f, 0.9f), 0.6f, C(0.25f, 0.25f, 0.2f)));
        GameObject trig = new GameObject("CheckpointTrigger");
        trig.transform.SetParent(g, false);
        trig.transform.position = p.pos + Vector3.up * 1.5f;
        BoxCollider bc = trig.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(p.r * 2f, 3f, p.r * 2f);
        ClimbCheckpoint cp = trig.AddComponent<ClimbCheckpoint>();
        GameObject spawn = new GameObject("RespawnPoint");
        spawn.transform.SetParent(trig.transform, false);
        spawn.transform.position = p.pos + Vector3.up * 0.3f;
        spawn.transform.rotation = Quaternion.LookRotation(-OutwardAt(s, p.pos));
        SetField(cp, "index", s.checkpoint);
        SetField(cp, "respawnPoint", spawn.transform);
        SetField(cp, "eggRenderer", egg.GetComponent<Renderer>());
        GameObject lg = new GameObject("CheckpointLight");
        lg.transform.SetParent(g, false);
        lg.transform.position = at + Vector3.up * 2.5f;
        Light l = lg.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = C(1f, 0.95f, 0.7f);
        l.range = 10f;
        l.intensity = 3f;
    }

    // 꼭대기: 작은 행성 위 금빛 알 둥지(도착)
    static void Goal(Sec s, Plat last)
    {
        Transform g = Group(s.root, "Goal").transform;
        float topY = s.y1;
        float ang = AngleToward(s, topY, last.pos);
        Vector3 top = RingPoint(s, topY, ang + s.turn * 0.4f);
        float theta = 0f;
        Plat before = Bridge(s, last, top, 5f, ref theta);

        const float R = 9f;
        Vector3 center = top - Vector3.up * R;
        GameObject planetoid = Ball(g, "Planetoid", center, Vector3.one * R * 2f, Mat("Climb_Planetoid", C(0.55f, 0.85f, 0.6f), 0.3f, C(0.05f, 0.12f, 0.06f)));
        planetoid.AddComponent<SphereCollider>();
        Static(planetoid);
        Disc(g, "GoalPad", top + Vector3.up * 0.05f, 4.5f, 0.6f, MGrass, true);
        Flowers(g, top, 4f, 14);

        Vector3 nest = top + Vector3.up * 0.6f;
        NestRing(g, nest, 2.2f, 1.1f);
        GameObject egg = Ball(g, "GoldenEgg", nest + Vector3.up * 1.1f, new Vector3(1.6f, 2.1f, 1.6f), MGold);
        GameObject trig = new GameObject("GoalTrigger");
        trig.transform.SetParent(g, false);
        trig.transform.position = top + Vector3.up * 1.5f;
        BoxCollider bc = trig.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(8f, 3f, 8f);
        trig.AddComponent<ClimbGoal>();

        GameObject beam = new GameObject("GoalBeam");
        beam.transform.SetParent(g, false);
        beam.transform.position = nest;
        Particles(beam.transform, nest, new Vector3(2f, 0.2f, 2f), 70f, 3f, new Vector3(0f, 7f, 0f), 0.7f, C(1f, 0.9f, 0.5f), 0f);
        Light l = beam.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = C(1f, 0.85f, 0.4f);
        l.range = 30f;
        l.intensity = 5f;
        Sign3D(g, top + Vector3.up * 6f, Quaternion.LookRotation(top - before.pos), "THE GOLDEN EGG", 1.4f);
        path.Add(new Plat { pos = top, r = 4.5f, t = g, sec = s });
    }
}
