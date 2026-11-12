using UnityEngine;

// Can Chicken Fly? 조작 · 물리 숫자 모음입니다(Assets/Resources/Climb/ClimbSettings.asset).
// 닭 조작(ClimbController)과 맵 생성기(발판 간격 계산)가 같은 값을 씁니다.
// → 여기서 점프 높이를 바꾸고 맵을 다시 만들면 발판 간격도 그 높이에 맞춰집니다.
[CreateAssetMenu(menuName = "Chicken Fly/Climb Settings", fileName = "ClimbSettings")]
public class ClimbSettings : ScriptableObject
{
    [Header("중력 · 낙하")]
    [Tooltip("중력(m/s²). 프로젝트 중력과 따로 씁니다")]
    public float gravity = 24f;
    [Tooltip("최고 낙하 속도(m/s). 많이 떨어질수록 여기까지 빨라짐")]
    public float terminalFallSpeed = 40f;

    [Header("땅 이동 (WASD = 카메라 기준)")]
    public float walkSpeed = 6f;
    [Tooltip("WASD를 누를 때 가속(m/s²)")]
    public float groundAcceleration = 40f;
    [Tooltip("WASD를 뗐을 때 멈추는 감속(m/s²)")]
    public float groundDeceleration = 35f;
    [Tooltip("몸을 돌리는 속도(°/초)")]
    public float turnSpeed = 720f;

    [Header("충전 점프 (Space 꾹 → 뗌)")]
    [Tooltip("충전이 꽉 차는 시간(초)")]
    public float chargeTime = 1.5f;
    [Tooltip("톡 눌렀을 때 오르는 높이(m)")]
    public float minJumpHeight = 1.5f;
    [Tooltip("꽉 충전했을 때 오르는 높이(m)")]
    public float maxJumpHeight = 12f;
    [Tooltip("떼는 순간 WASD 쪽으로 기울어 나가는 속도(m/s): x = 톡, y = 꽉 충전")]
    public Vector2 launchHorizontalSpeed = new Vector2(2f, 4.5f);
    [Tooltip("파닥이며 오르는 시간(초): x = 톡, y = 꽉 충전. 이 동안 받은 힘이 점점 줄어듦")]
    public Vector2 flapRiseTime = new Vector2(0.2f, 0.9f);
    [Tooltip("파닥임이 시작될 때 위로 미는 힘(m/s², 꽉 충전 기준). 시간이 지나며 0으로 줄어듦")]
    public float flapThrust = 30f;
    [Tooltip("파닥이는 날갯짓 횟수(초당): x = 끝무렵, y = 처음")]
    public Vector2 flapRate = new Vector2(4f, 9f);
    [Tooltip("땅을 떠난 직후에도 충전 점프가 되는 시간(초)")]
    public float coyoteTime = 0.12f;
    [Tooltip("충전할 수 있는 최대 경사(°)")]
    public float maxChargeSlope = 55f;

    [Header("공중")]
    [Tooltip("공중 WASD 힘(땅 대비 비율)")]
    [Range(0f, 1f)] public float airControl = 0.7f;
    [Tooltip("공중에서 WASD로 낼 수 있는 수평 속도(m/s)")]
    public float airMaxSpeed = 6f;
    [Tooltip("공중에서 WASD를 안 누를 때 수평 감속(m/s²)")]
    public float airDrag = 1.5f;

    [Header("활공 (공중에서 Space를 누르는 동안만)")]
    [Tooltip("활공을 시작할 때 떨어지는 속도(m/s)")]
    public float glideFallSpeed = 2.5f;
    [Tooltip("활공 힘이 다 빠질 무렵 떨어지는 속도(m/s)")]
    public float glideEndFallSpeed = 7f;
    [Tooltip("활공할 수 있는 최대 시간(초). 착지하면 다시 참")]
    public float glideMaxTime = 2f;

    [Header("경사 · 모서리")]
    [Tooltip("이 각도(°)까지는 미끄러지지 않고 섬")]
    public float standSlope = 35f;
    [Tooltip("이 각도(°)까지는 천천히 미끄러지며 버틸 수 있음. 넘으면 벽(빠르게 미끄러져 떨어짐)")]
    public float holdSlope = 55f;
    [Tooltip("경사에서 미끄러지는 정도(중력 대비)")]
    public float slopeSlide = 0.55f;
    [Tooltip("발이 모서리에 반쯤 걸렸을 때 바깥으로 미끄러지는 가속(m/s²)")]
    public float edgeSlide = 7f;

    [Header("충돌 · 튕김 · 구르기")]
    [Tooltip("이 속도(m/s)보다 세게 부딪히면 튕김(아래면 사뿐히 착지). 중력 24에서 17m/s ≈ 6m 낙하")]
    public float bounceSpeed = 17f;
    [Tooltip("튕기는 비율: x = 약하게 부딪힘, y = 최고 속도로 부딪힘")]
    public Vector2 floorRestitution = new Vector2(0.12f, 0.4f);
    [Tooltip("벽에 부딪혔을 때 튕기는 비율: x = 약하게, y = 세게")]
    public Vector2 wallRestitution = new Vector2(0.35f, 0.6f);
    [Tooltip("세게 착지한 뒤 미끄러지는 시간(초): x = 살짝 세게, y = 최고 속도")]
    public Vector2 landingSlipTime = new Vector2(0.2f, 1.2f);
    [Tooltip("세게 착지했을 때 '튕김 기준을 넘은 속도' 중 앞으로 미끄러지는 속도로 바뀌는 비율")]
    public float landingSlipBoost = 0.3f;
    [Tooltip("미끄러지는 동안 마찰(평소 대비 비율)")]
    public float slipFriction = 0.12f;
    [Tooltip("이 속도(m/s) 이상으로 부딪히면 데굴데굴 구름. 중력 24에서 34m/s ≈ 25m 낙하")]
    public float tumbleSpeed = 34f;
    [Tooltip("구르는 동안 조작이 안 되는 시간(초)")]
    public float tumbleTime = 1f;

    [Header("체크포인트")]
    [Tooltip("체크포인트를 지난 뒤 그 높이보다 이만큼(m) 넘게 떨어지면 체크포인트로 돌아옴")]
    public float checkpointCatchDepth = 20f;

    // ───────────── 점프 계산(조작과 맵 생성기가 함께 씀) ─────────────
    public const float SimStep = 0.02f;   // FixedUpdate 간격과 같게

    public float TargetHeight(float charge) => Mathf.Lerp(minJumpHeight, maxJumpHeight, Mathf.Clamp01(charge));
    public float RiseTime(float charge) => Mathf.Lerp(flapRiseTime.x, flapRiseTime.y, Mathf.Clamp01(charge));
    public float LaunchHorizontal(float charge) => Mathf.Lerp(launchHorizontalSpeed.x, launchHorizontalSpeed.y, Mathf.Clamp01(charge));

    // 파닥임 힘: 처음이 가장 세고 rise 시간 동안 0으로 줄어듦(받은 힘이 점점 줄어드는 느낌)
    public float Thrust(float charge, float t)
    {
        float T = RiseTime(charge);
        if (t >= T) return 0f;
        return flapThrust * Mathf.Clamp01(charge) * (1f - t / T);
    }

    // 처음 세로 속도 v0로 뛰었을 때 가장 높이 오르는 높이(m)
    public float ApexFor(float v0, float charge)
    {
        float y = 0f, v = v0, t = 0f, best = 0f;
        for (int i = 0; i < 2000; i++)
        {
            v += (Thrust(charge, t) - gravity) * SimStep;
            y += v * SimStep;
            t += SimStep;
            if (y > best) best = y;
            if (v < 0f && t > RiseTime(charge)) break;
        }
        return best;
    }

    // 목표 높이(TargetHeight)에 딱 닿는 처음 세로 속도(이분 탐색)
    public float LaunchSpeed(float charge)
    {
        float target = TargetHeight(charge);
        float lo = 0f, hi = 60f;
        for (int i = 0; i < 32; i++)
        {
            float mid = (lo + hi) * 0.5f;
            if (ApexFor(mid, charge) < target) lo = mid; else hi = mid;
        }
        return (lo + hi) * 0.5f;
    }

    // 높이 차 dh(m)인 곳에 내려앉을 때까지 수평으로 갈 수 있는 거리(m). 공중 WASD를 끝까지 누른 경우, 활공 없음.
    // 정점이 dh보다 낮으면 -1.
    public float HorizontalReach(float dh, float charge)
    {
        float v0 = LaunchSpeed(charge);
        float y = 0f, v = v0, t = 0f;
        float h = LaunchHorizontal(charge), x = 0f;
        float airAccel = groundAcceleration * airControl;
        float top = 0f;
        for (int i = 0; i < 3000; i++)
        {
            v += (Thrust(charge, t) - gravity) * SimStep;
            v = Mathf.Max(v, -terminalFallSpeed);
            h = Mathf.MoveTowards(h, airMaxSpeed, airAccel * SimStep);
            y += v * SimStep;
            x += h * SimStep;
            t += SimStep;
            top = Mathf.Max(top, y);
            if (v < 0f)
            {
                if (top < dh) return -1f;
                if (y <= dh) return x;
            }
        }
        return -1f;
    }

    // 높이 차 dh에서 가장 멀리 갈 수 있는 거리(충전량을 바꿔 가며)
    public float MaxReach(float dh)
    {
        float best = -1f;
        for (int k = 0; k <= 20; k++)
        {
            best = Mathf.Max(best, HorizontalReach(dh, k / 20f));
        }
        return best;
    }

    static ClimbSettings cached;
    public static ClimbSettings Load()
    {
        if (cached == null) cached = Resources.Load<ClimbSettings>("Climb/ClimbSettings");
        if (cached == null) cached = CreateInstance<ClimbSettings>();
        return cached;
    }
}
