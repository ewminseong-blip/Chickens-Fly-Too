using System;
using UnityEngine;

// 이펙트(파티클) · 화면 효과 숫자 모음입니다. 위치: Assets/Resources/FX/FeedbackSettings.asset
// 플레이 중에 바꿔도 바로 반영됩니다. 강도는 '이룬 일의 크기'에 맞춥니다(날갯짓 < 체크포인트 < 클리어).
[CreateAssetMenu(menuName = "Chicken Fly/Feedback Settings", fileName = "FeedbackSettings")]
public class FeedbackSettings : ScriptableObject
{
    [Header("화면 효과 (접근성: 끄면 히트스톱 · 슬로모션 · 번쩍임이 꺼짐)")]
    public bool screenEffects = true;

    [Header("파티클 프리셋")]
    public BurstPreset flap = new BurstPreset(Color.white, new Color(1f, 0.93f, 0.75f), 5, 3f, 0.35f, 0.7f, 0.3f, 0.3f, 35f, true);
    public BurstPreset deathFeathers = new BurstPreset(Color.white, new Color(1f, 0.95f, 0.85f), 30, 9f, 0.5f, 1.2f, 0.6f, 0.4f, 0f, false);
    public BurstPreset deathSparks = new BurstPreset(new Color(1f, 0.25f, 0.15f), new Color(1f, 0.65f, 0.1f), 18, 6f, 0.6f, 0.5f, 0f, 0.2f, 0f, false);
    public BurstPreset checkpoint = new BurstPreset(new Color(0.4f, 1f, 0.5f), new Color(1f, 0.95f, 0.4f), 24, 6f, 0.45f, 0.9f, -0.2f, 1f, 40f, true);
    public BurstPreset boost = new BurstPreset(new Color(1f, 0.85f, 0.2f), new Color(1f, 1f, 0.7f), 20, 12f, 0.35f, 0.45f, 0f, 1.5f, 25f, true);
    public BurstPreset bounce = new BurstPreset(new Color(0.4f, 0.9f, 1f), Color.white, 16, 8f, 0.4f, 0.6f, 0.5f, 1f, 20f, true);
    public BurstPreset goal = new BurstPreset(new Color(1f, 0.35f, 0.35f), new Color(1f, 0.85f, 0.25f), 25, 14f, 0.5f, 2f, 0.8f, 2f, 55f, true);
    [Tooltip("클리어 색종이를 몇 번 겹쳐 터뜨릴지(색이 바뀜)")]
    [Range(1, 8)] public int goalLayers = 5;
}

[Serializable]
public class BurstPreset
{
    public Color colorA = Color.white;
    public Color colorB = Color.white;
    [Range(0, 200)] public int count = 10;
    public float speed = 5f;
    public float size = 0.4f;
    public float lifetime = 0.8f;
    public float gravity = 0f;
    public float radius = 0.3f;
    [Range(0f, 90f)] public float coneAngle = 30f;
    [Tooltip("켜면 한 방향(원뿔)으로, 끄면 사방으로 터짐")]
    public bool directional = false;

    public BurstPreset() { }

    public BurstPreset(Color a, Color b, int count, float speed, float size, float lifetime, float gravity, float radius, float cone, bool directional)
    {
        colorA = a; colorB = b; this.count = count; this.speed = speed; this.size = size;
        this.lifetime = lifetime; this.gravity = gravity; this.radius = radius; coneAngle = cone; this.directional = directional;
    }
}
