using UnityEngine;

// 짧은 파티클 터짐(깃털 · 불꽃 · 색종이)을 코드로 만들어 재생하는 도우미입니다.
// 숫자 · 색은 Resources/FX/FeedbackSettings.asset에서 조절합니다(없으면 기본값).
// 머티리얼은 Resources/FX/FX_Particle.mat (StageBuilder가 만듦)을 씁니다.
public static class FeedbackFX
{
    static Material particleMaterial;
    static FeedbackSettings settings;

    public static FeedbackSettings Settings
    {
        get
        {
            if (settings == null)
            {
                settings = Resources.Load<FeedbackSettings>("FX/FeedbackSettings");
            }

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<FeedbackSettings>();
            }

            return settings;
        }
    }

    // 히트스톱 · 슬로모션 · 번쩍임을 쓸지(접근성)
    public static bool ScreenEffectsEnabled => Settings.screenEffects;

    static Material ParticleMaterial
    {
        get
        {
            if (particleMaterial == null)
            {
                particleMaterial = Resources.Load<Material>("FX/FX_Particle");
            }

            return particleMaterial;
        }
    }

    public struct BurstSettings
    {
        public Color colorA;
        public Color colorB;
        public int count;
        public float speed;
        public float size;
        public float lifetime;
        public float gravity;
        public float radius;
        public Vector3 direction;   // 0이면 사방으로
        public float coneAngle;
    }

    static BurstSettings FromPreset(BurstPreset p, Vector3 direction)
    {
        return new BurstSettings
        {
            colorA = p.colorA,
            colorB = p.colorB,
            count = p.count,
            speed = p.speed,
            size = p.size,
            lifetime = p.lifetime,
            gravity = p.gravity,
            radius = p.radius,
            direction = p.directional ? direction : Vector3.zero,
            coneAngle = p.coneAngle
        };
    }

    public static ParticleSystem Burst(Vector3 position, BurstSettings s)
    {
        if (s.count <= 0)
        {
            return null;
        }

        GameObject go = new GameObject("FX_Burst");
        go.transform.position = position;
        if (s.direction.sqrMagnitude > 0.0001f)
        {
            go.transform.rotation = Quaternion.LookRotation(s.direction.normalized);
        }

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.2f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(s.lifetime * 0.6f, s.lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(s.speed * 0.4f, s.speed);
        main.startSize = new ParticleSystem.MinMaxCurve(s.size * 0.5f, s.size);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(s.colorA, s.colorB);
        main.gravityModifier = s.gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.maxParticles = Mathf.Max(8, s.count);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)s.count) });

        ParticleSystem.ShapeModule shape = ps.shape;
        if (s.direction.sqrMagnitude > 0.0001f)
        {
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = s.coneAngle;
        }
        else
        {
            shape.shapeType = ParticleSystemShapeType.Sphere;
        }
        shape.radius = Mathf.Max(0.05f, s.radius);

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = fade;

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f));

        ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        if (ParticleMaterial != null)
        {
            r.sharedMaterial = ParticleMaterial;
        }

        ps.Play();
        return ps;
    }

    public static ParticleSystem Burst(Vector3 position, BurstPreset preset, Vector3 direction)
    {
        return preset == null ? null : Burst(position, FromPreset(preset, direction));
    }

    // ===== 상황별 (강도는 '이룬 일의 크기'에 맞춤) =====

    public static void Flap(Vector3 position, bool reversed)
    {
        Burst(position, Settings.flap, reversed ? Vector3.up : Vector3.down);
    }

    public static void Death(Vector3 position)
    {
        Burst(position, Settings.deathFeathers, Vector3.up);
        Burst(position, Settings.deathSparks, Vector3.up);
    }

    public static void Checkpoint(Vector3 position)
    {
        Burst(position, Settings.checkpoint, Vector3.up);
    }

    public static void Boost(Vector3 position, Vector3 forward)
    {
        Burst(position, Settings.boost, -forward);
    }

    public static void Bounce(Vector3 position, bool reversed)
    {
        Burst(position, Settings.bounce, reversed ? Vector3.down : Vector3.up);
    }

    // 클리어: 색을 바꿔 가며 여러 겹으로 크게
    public static void Goal(Vector3 position)
    {
        Color[] palette =
        {
            new Color(1f, 0.35f, 0.35f), new Color(1f, 0.85f, 0.25f),
            new Color(0.35f, 0.85f, 1f), new Color(0.5f, 1f, 0.45f), new Color(1f, 0.5f, 0.95f)
        };

        BurstSettings s = FromPreset(Settings.goal, Vector3.up);
        for (int i = 0; i < Settings.goalLayers; i++)
        {
            s.colorA = palette[i % palette.Length];
            s.colorB = palette[(i + 1) % palette.Length];
            Burst(position + Vector3.up * 2f, s);
        }
    }
}
