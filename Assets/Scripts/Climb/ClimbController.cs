using UnityEngine;

// Can Chicken Fly? 닭 조작.
//  WASD  = 카메라 기준 앞뒤좌우(닭은 움직이는 쪽으로 몸을 돌림), 마우스 = 카메라만(ClimbCamera)
//  Space 꾹 = 제자리에서 웅크리며 충전(최대 chargeTime초)
//  Space 뗌 = 위로 + 누르고 있던 WASD 쪽으로 날아오름 → 파닥이며 오름(받은 힘이 점점 줄어듦) → 정점 → 낙하
//  공중 Space 꾹 = 누르는 동안만 활공(천천히 떨어짐, 힘이 점점 빠짐), 떼면 바로 떨어짐. 착지하면 다시 참.
// 충돌: 모든 물체에 설 수 있음. 경사가 가파를수록 미끄러지고, 모서리에 반쯤 걸리면 바깥으로 미끄러짐.
//       세게 부딪힐수록(많이 떨어질수록) 크게 튕기고 멀리 미끄러지며, 아주 세게 부딪히면 데굴데굴 구름.
// 숫자는 ClimbSettings 에셋(Resources/Climb/ClimbSettings)에서 조절합니다.
[RequireComponent(typeof(Rigidbody))]
public class ClimbController : MonoBehaviour
{
    public enum State { Grounded, Charging, Rising, Airborne, Gliding, Tumbling }

    [SerializeField] ClimbSettings settings;
    [Tooltip("WASD 기준이 되는 카메라(비우면 Main Camera)")]
    [SerializeField] Transform cameraTransform;
    [Tooltip("몸통 캡슐 반지름(m). 발 기준 피벗")]
    [SerializeField] float radius = 0.62f;
    [SerializeField] float height = 2.2f;
    [SerializeField] LayerMask groundMask = ~0;

    Rigidbody rb;
    ClimbSettings S;

    public State Current { get; private set; } = State.Grounded;
    public float Charge01 { get; private set; }
    public float Glide01 => glideEnergy;
    public bool IsGrounded => grounded;
    public bool IsSliding { get; private set; }
    public float GroundAngle => groundAngle;
    public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
    public float FallStartHeight => apexY;
    public bool InputLocked { get; set; }
    public ClimbSettings Settings => S;
    public float BodyHeight => height;

    public event System.Action Flapped;                 // 날갯짓 한 번(애니메이션 · 깃털)
    public event System.Action<float> Launched;         // 충전량
    public event System.Action<float, float> Landed;    // 부딪힌 속도, 떨어진 높이
    public event System.Action<float> Bounced;          // 부딪힌 속도(벽 · 바닥 튕김)
    public event System.Action Tumbled;
    public event System.Action KnockedBack;

    // 입력
    Vector2 moveInput;
    bool spaceHeld;

    // 바닥
    bool grounded;
    Vector3 groundNormal = Vector3.up;
    float groundAngle;
    float support = 1f;
    Vector3 edgeOut;
    ClimbSurface surface;
    ClimbMovingPlatform groundPlatform;
    Vector3 groundPoint;
    float lastGroundedTime = -10f;

    // 비행
    float chargeStart;
    float launchCharge;
    float flightTime;
    float flapClock;
    bool glideArmed;
    float glideEnergy = 1f;
    float apexY;

    // 충돌 뒤 상태
    float slipTimer;
    float tumbleTimer;
    float stunTimer;
    float glideLockTimer;
    float launchGraceTimer;   // 뛰어오른 직후 바닥 판정을 잠깐 끔

    Vector3 lastVelocity;
    Vector3 externalAccel;
    float gravityScale = 1f;
    float yaw;

    void Awake()
    {
        S = settings != null ? settings : ClimbSettings.Load();
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        yaw = transform.eulerAngles.y;
        apexY = transform.position.y;
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    // ───────────── 입력(Update) ─────────────

    void Update()
    {
        if (Time.timeScale <= 0f) return;

        bool locked = InputLocked;
        moveInput = Vector2.zero;
        if (!locked)
        {
            if (Input.GetKey(KeyCode.W)) moveInput.y += 1f;
            if (Input.GetKey(KeyCode.S)) moveInput.y -= 1f;
            if (Input.GetKey(KeyCode.D)) moveInput.x += 1f;
            if (Input.GetKey(KeyCode.A)) moveInput.x -= 1f;
            if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();
        }

        bool down = !locked && Input.GetKeyDown(KeyCode.Space);
        spaceHeld = !locked && Input.GetKey(KeyCode.Space);

        // 충전 시작: 땅에서(또는 막 떠난 직후) 새로 눌렀을 때만.
        // 활공하며 누른 채 내려앉은 경우는 다시 눌러야 충전(작은 발판에서 원치 않는 점프 방지)
        if (down && CanCharge())
        {
            Current = State.Charging;
            chargeStart = Time.time;
            Charge01 = 0f;
        }
        else if (down && IsAirState(Current))
        {
            glideArmed = true;
        }

        if (Current == State.Charging)
        {
            Charge01 = Mathf.Clamp01((Time.time - chargeStart) / Mathf.Max(0.05f, S.chargeTime));
            if (!spaceHeld)
            {
                Launch(Charge01);
            }
        }
    }

    bool CanCharge()
    {
        if (Current == State.Tumbling || stunTimer > 0f) return false;
        bool recentlyGrounded = grounded || Time.time - lastGroundedTime <= S.coyoteTime;
        return recentlyGrounded && Current != State.Rising && (!grounded || groundAngle <= S.maxChargeSlope);
    }

    static bool IsAirState(State s) => s == State.Rising || s == State.Airborne || s == State.Gliding;

    // 자동 점검용(봇): 정해진 방향을 정해진 시간 동안 '누르고 있는' 것처럼
    Vector3 scriptedInput;
    float scriptedTimer;
    bool scriptedGlide;

    // 자동 점검: charge만큼 충전해 dir 쪽으로 뛰고, 공중에서도 holdSeconds 동안 그쪽을 누름
    public void BotJump(float charge, Vector3 dir, float holdSeconds, bool glide = false)
    {
        dir.y = 0f;
        scriptedInput = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
        scriptedTimer = holdSeconds;
        scriptedGlide = glide;
        Launch(Mathf.Clamp01(charge));
        if (glide) glideArmed = true;
    }

    // 자동 점검: 이번 물리 프레임 동안 이 방향을 누름
    public void BotSteer(Vector3 dir)
    {
        dir.y = 0f;
        scriptedInput = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
        scriptedTimer = Time.fixedDeltaTime * 1.5f;
        scriptedGlide = false;
    }

    // WASD를 카메라 기준 수평 방향으로
    Vector3 WorldInput()
    {
        if (scriptedTimer > 0f) return scriptedInput;
        if (moveInput.sqrMagnitude < 0.0001f) return Vector3.zero;
        float camYaw = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;
        Quaternion q = Quaternion.Euler(0f, camYaw, 0f);
        return q * new Vector3(moveInput.x, 0f, moveInput.y);
    }

    // ───────────── 날아오르기 ─────────────

    void Launch(float charge)
    {
        launchCharge = charge;
        flightTime = 0f;
        flapClock = 0f;
        glideArmed = false;
        launchGraceTimer = 0.12f;

        Vector3 dir = WorldInput();
        Vector3 v = rb.linearVelocity;
        Vector3 h = new Vector3(v.x, 0f, v.z);
        if (dir.sqrMagnitude > 0.0001f)
        {
            h = dir.normalized * Mathf.Max(S.LaunchHorizontal(charge), Vector3.Dot(h, dir.normalized));
        }
        else
        {
            h *= 0.3f;   // 제자리 수직 점프(미끄러지던 속도는 조금만 남김)
        }

        float vy = S.LaunchSpeed(charge);
        if (groundPlatform != null) h += Flat(groundPlatform.PointVelocity(groundPoint));
        rb.linearVelocity = new Vector3(h.x, vy, h.z);
        lastVelocity = rb.linearVelocity;
        grounded = false;
        Current = State.Rising;
        Charge01 = 0f;
        apexY = rb.position.y;

        Flapped?.Invoke();
        Launched?.Invoke(charge);
    }

    // ───────────── 물리(FixedUpdate) ─────────────

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        ProbeGround();

        if (slipTimer > 0f) slipTimer -= dt;
        if (stunTimer > 0f) stunTimer -= dt;
        if (glideLockTimer > 0f) glideLockTimer -= dt;
        if (launchGraceTimer > 0f) launchGraceTimer -= dt;
        if (scriptedTimer > 0f)
        {
            scriptedTimer -= dt;
            if (scriptedGlide) spaceHeld = true;
        }

        Vector3 v = rb.linearVelocity;
        Vector3 input = stunTimer > 0f ? Vector3.zero : WorldInput();

        // 착지(바닥 판정 + 내려가는 중)
        if (grounded && IsAirState(Current) && v.y <= 1f)
        {
            Land(Mathf.Max(0f, -v.y));
        }
        // 발밑이 사라짐(걸어서 떨어짐 · 미끄러져 떨어짐)
        if (!grounded && (Current == State.Grounded || (Current == State.Charging && Time.time - lastGroundedTime > S.coyoteTime)))
        {
            Current = State.Airborne;
            Charge01 = 0f;
        }

        switch (Current)
        {
            case State.Grounded:
            case State.Charging:
                v = GroundMove(v, Current == State.Charging ? Vector3.zero : input, dt);
                break;
            case State.Tumbling:
                tumbleTimer -= dt;
                v = grounded ? GroundMove(v, Vector3.zero, dt, S.slipFriction) : AirMove(v, Vector3.zero, dt);
                if (tumbleTimer <= 0f) Current = grounded ? State.Grounded : State.Airborne;
                break;
            default:
                v = AirMove(v, input, dt);
                break;
        }

        // 바람: 땅에 서 있을 때는 발로 버텨서 약하게(공중에서는 그대로)
        v += externalAccel * (grounded ? 0.25f : 1f) * dt;
        externalAccel = Vector3.zero;

        if (!grounded) apexY = Mathf.Max(apexY, rb.position.y);
        else apexY = rb.position.y;

        rb.linearVelocity = v;
        lastVelocity = v;

        Face(input, dt);
    }

    Vector3 GroundMove(Vector3 v, Vector3 input, float dt, float frictionScale = 1f)
    {
        float friction = (surface != null ? surface.friction : 1f) * frictionScale * (slipTimer > 0f ? S.slipFriction : 1f);
        Vector3 platformVel = groundPlatform != null ? groundPlatform.PointVelocity(groundPoint) : Vector3.zero;

        // 표면을 따라가는 속도(발판 속도를 빼고 계산)
        Vector3 rel = Vector3.ProjectOnPlane(v - platformVel, groundNormal);
        Vector3 target = Vector3.zero;
        if (input.sqrMagnitude > 0.0001f)
        {
            target = Vector3.ProjectOnPlane(input, groundNormal).normalized * S.walkSpeed * input.magnitude;
        }
        float accel = input.sqrMagnitude > 0.0001f
            ? S.groundAcceleration * Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(friction))
            : S.groundDeceleration * friction;
        if (Current == State.Charging) accel = S.groundDeceleration * 1.5f * friction;   // 충전 중엔 멈춰 섬(미끄러운 곳에선 덜 멈춤)
        rel = Vector3.MoveTowards(rel, target, accel * dt);

        // 경사: standSlope까지는 그대로 섬, 그 위로는 가파를수록 빨리 미끄러짐. 미끄러운 표면은 더 완만해도 미끄러짐.
        float standLimit = S.standSlope * Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(friction));
        IsSliding = false;
        if (groundAngle > standLimit)
        {
            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
            float k = Mathf.InverseLerp(standLimit, 90f, groundAngle);
            rel += downhill * S.gravity * gravityScale * S.slopeSlide * (0.25f + k) * dt;
            IsSliding = true;
        }

        // 모서리: 발이 반쯤만 걸려 있으면 바깥으로 미끄러짐
        if (support < 0.55f && edgeOut.sqrMagnitude > 0.0001f)
        {
            rel += edgeOut * S.edgeSlide * (1f - support) * dt;
            IsSliding = true;
        }

        return rel + platformVel;
    }

    Vector3 AirMove(Vector3 v, Vector3 input, float dt)
    {
        // 위아래
        float g = S.gravity * gravityScale;
        if (Current == State.Rising)
        {
            flightTime += dt;
            v.y += (S.Thrust(launchCharge, flightTime) - g) * dt;

            // 파닥파닥: 처음엔 빠르게, 힘이 줄수록 느리게
            float rise = S.RiseTime(launchCharge);
            float k = Mathf.Clamp01(flightTime / Mathf.Max(0.01f, rise));
            flapClock += dt * Mathf.Lerp(S.flapRate.y, S.flapRate.x, k);
            if (flapClock >= 1f)
            {
                flapClock -= 1f;
                Flapped?.Invoke();
            }
            if (flightTime >= rise) Current = State.Airborne;
        }
        else
        {
            v.y -= g * dt;
        }

        // 활공: 정점을 지나 내려갈 때, 공중에서 새로 누른 Space를 누르고 있는 동안만
        bool canGlide = glideArmed && spaceHeld && glideEnergy > 0f && glideLockTimer <= 0f && Current != State.Tumbling;
        if (Current == State.Airborne && canGlide && v.y < 0f)
        {
            Current = State.Gliding;
            Flapped?.Invoke();
        }
        if (Current == State.Gliding)
        {
            if (!canGlide)
            {
                Current = State.Airborne;
            }
            else
            {
                float cap = Mathf.Lerp(S.glideEndFallSpeed, S.glideFallSpeed, glideEnergy);
                if (v.y < -cap) v.y = Mathf.MoveTowards(v.y, -cap, 70f * dt);
                glideEnergy = Mathf.Max(0f, glideEnergy - dt / Mathf.Max(0.1f, S.glideMaxTime));
            }
        }

        v.y = Mathf.Max(v.y, -S.terminalFallSpeed);

        // 수평: 땅의 airControl(70%) 힘으로
        Vector3 h = new Vector3(v.x, 0f, v.z);
        if (input.sqrMagnitude > 0.0001f && Current != State.Tumbling)
        {
            float accel = S.groundAcceleration * S.airControl;
            Vector3 target = input * S.airMaxSpeed;
            // 튕겨서 빨라진 속도는 WASD로 바로 꺾이지 않게: 최고 속도보다 빠르면 그 속도를 유지한 채 방향만 조금씩
            if (h.magnitude > S.airMaxSpeed) target = input * h.magnitude;
            h = Vector3.MoveTowards(h, target, accel * dt);
        }
        else
        {
            h = Vector3.MoveTowards(h, Vector3.zero, S.airDrag * dt);
        }

        return new Vector3(h.x, v.y, h.z);
    }

    void Land(float fallSpeed)
    {
        Current = State.Grounded;
        Charge01 = 0f;
        ReportLanding(Mathf.Max(fallSpeed, pendingImpact));
    }

    // 착지 알림(떨어진 높이 · 부딪힌 속도). 활공 힘은 다시 참.
    void ReportLanding(float impact)
    {
        float dropped = Mathf.Max(0f, apexY - rb.position.y);
        glideEnergy = 1f;
        glideArmed = false;
        pendingImpact = 0f;
        Landed?.Invoke(impact, dropped);
        apexY = rb.position.y;
    }

    float pendingImpact;

    // FPS처럼: 마우스를 돌린 만큼 닭이 그쪽을 봄(몸 방향 = 카메라 방향). WASD는 닭 기준 앞 · 뒤 · 옆걸음.
    void Face(Vector3 input, float dt)
    {
        if (Current != State.Tumbling && cameraTransform != null && scriptedTimer <= 0f)
        {
            yaw = cameraTransform.eulerAngles.y;
        }
        else if (scriptedTimer > 0f && input.sqrMagnitude > 0.0001f)
        {
            yaw = Mathf.MoveTowardsAngle(yaw, Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg, S.turnSpeed * dt);   // 자동 점검 봇
        }
        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // ───────────── 바닥 판정 ─────────────

    void ProbeGround()
    {
        bool wasGrounded = grounded;
        grounded = false;
        surface = null;
        groundPlatform = null;
        support = 0f;
        edgeOut = Vector3.zero;

        if (launchGraceTimer > 0f) return;

        Vector3 pos = rb.position;
        float probe = 0.25f;
        Vector3 origin = pos + Vector3.up * (radius + 0.05f);
        if (!Physics.SphereCast(origin, radius * 0.92f, Vector3.down, out RaycastHit hit, 0.05f + probe + radius * 0.08f, groundMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }
        if (hit.collider.attachedRigidbody == rb) return;

        // 발밑 5곳에 광선: 몇 곳이 받쳐 주는지(모서리 판정)
        int hits = 0;
        Vector3 sum = Vector3.zero;
        float rayLen = 0.35f + probe;
        Vector3 rayTop = pos + Vector3.up * 0.3f;
        Vector3 centerNormal = Vector3.zero;
        for (int i = 0; i < 5; i++)
        {
            Vector3 off = i == 0 ? Vector3.zero : Quaternion.Euler(0f, i * 90f + 45f, 0f) * Vector3.forward * radius * 0.75f;
            if (Physics.Raycast(rayTop + off, Vector3.down, out RaycastHit r, rayLen + 0.3f, groundMask, QueryTriggerInteraction.Ignore) && r.collider.attachedRigidbody != rb)
            {
                hits += i == 0 ? 2 : 1;
                sum += r.point;
                if (i == 0) centerNormal = r.normal;
            }
        }
        support = hits / 6f;

        // 가운데 광선이 윗면을 맞히면 그 면의 기울기(모서리 근처라도 평평한 윗면이면 평평), 아니면 구가 닿은 기울기
        Vector3 n = centerNormal.sqrMagnitude > 0f ? centerNormal : hit.normal;
        float angle = Vector3.Angle(n, Vector3.up);
        if (angle > S.holdSlope) return;   // 벽: 서 있을 수 없음(공중 취급 → 빠르게 미끄러져 떨어짐)

        if (centerNormal.sqrMagnitude <= 0f)
        {
            Vector3 o = Flat(pos - hit.point);
            edgeOut = o.sqrMagnitude > 0.0001f ? o.normalized : Flat(hit.normal).normalized;
        }

        // 올라가는 중(튕김 · 점프)에는 땅에 붙지 않음
        if (rb.linearVelocity.y > 2f && !wasGrounded) return;

        grounded = true;
        groundNormal = n;
        groundAngle = angle;
        groundPoint = hit.point;
        surface = hit.collider.GetComponentInParent<ClimbSurface>();
        groundPlatform = hit.collider.GetComponentInParent<ClimbMovingPlatform>();
        lastGroundedTime = Time.time;
    }

    // ───────────── 충돌: 튕김 · 미끄러짐 · 구르기 ─────────────

    void OnCollisionEnter(Collision c)
    {
        HandleImpact(c);
    }

    // 마지막으로 부딪힌 물체(점검 · 디버그용)
    public Collider LastHit { get; private set; }

    void HandleImpact(Collision c)
    {
        if (c.contactCount == 0) return;
        LastHit = c.collider;

        ClimbHazard hazard = c.collider.GetComponentInParent<ClimbHazard>();
        if (hazard != null)
        {
            hazard.Hit(this, c.GetContact(0).point);
            return;
        }

        Vector3 n = Vector3.zero;
        for (int i = 0; i < c.contactCount; i++) n += c.GetContact(i).normal;
        n.Normalize();

        Vector3 v = lastVelocity;
        if (c.rigidbody != null && c.rigidbody != rb)
        {
            ClimbMovingPlatform mp = c.collider.GetComponentInParent<ClimbMovingPlatform>();
            if (mp != null) v -= mp.PointVelocity(c.GetContact(0).point);
        }
        float impact = -Vector3.Dot(v, n);
        if (impact < 1f) return;

        ClimbSurface surf = c.collider.GetComponentInParent<ClimbSurface>();
        float floorCos = Mathf.Cos(S.holdSlope * Mathf.Deg2Rad);

        // 통통 튀는 표면(버섯 등): 높은 곳에서 떨어져 세게 내려오면 튀어 오름(살짝 내려앉으면 그냥 섬)
        if (surf != null && surf.launchSpeed > 0f && n.y > 0.5f && impact >= surf.launchMinImpact)
        {
            Vector3 b = Vector3.ProjectOnPlane(v, n) + Vector3.up * surf.launchSpeed;
            SetAirVelocity(b);
            Bounced?.Invoke(impact);
            return;
        }

        float extraBounce = surf != null ? surf.bounce : 0f;

        if (n.y >= floorCos)
        {
            // 바닥 · 경사면
            if (impact >= S.bounceSpeed || extraBounce > 0f)
            {
                float k = Mathf.InverseLerp(S.bounceSpeed, S.terminalFallSpeed, impact);
                float e = Mathf.Lerp(S.floorRestitution.x, S.floorRestitution.y, k) + extraBounce;
                Vector3 tangent = Vector3.ProjectOnPlane(v, n);
                Vector3 flat = Flat(tangent);
                Vector3 slideDir = flat.sqrMagnitude > 0.09f ? flat.normalized : Flat(transform.forward).normalized;
                // 많이 떨어질수록(세게 부딪힐수록) 앞으로 더 크게 미끄러짐
                Vector3 slip = slideDir * Mathf.Max(0f, impact - S.bounceSpeed) * S.landingSlipBoost;
                Vector3 nv = tangent + slip + n * impact * e;
                slipTimer = Mathf.Lerp(S.landingSlipTime.x, S.landingSlipTime.y, k);

                ReportLanding(impact);
                if (impact >= S.tumbleSpeed)
                {
                    Current = State.Tumbling;
                    tumbleTimer = S.tumbleTime;
                    Tumbled?.Invoke();
                }
                else
                {
                    Current = nv.y > 1.5f ? State.Airborne : State.Grounded;
                }
                rb.linearVelocity = nv;
                lastVelocity = nv;
                launchGraceTimer = nv.y > 1.5f ? 0.08f : 0f;
                Bounced?.Invoke(impact);
            }
            else
            {
                // 사뿐히 착지: 표면 쪽 속도만 버리고 미끄러지는 속도는 남김
                pendingImpact = Mathf.Max(pendingImpact, impact);
                rb.linearVelocity = Vector3.ProjectOnPlane(v, n);
                lastVelocity = rb.linearVelocity;
            }
            return;
        }

        // 벽 · 천장: 튕겨 나감(걷다가 살짝 닿은 건 무시)
        if (Current == State.Grounded && impact < 4f) return;
        float kw = Mathf.InverseLerp(2f, S.terminalFallSpeed * 0.6f, impact);
        float ew = Mathf.Lerp(S.wallRestitution.x, S.wallRestitution.y, kw) + extraBounce;
        Vector3 r = v - (1f + ew) * Vector3.Dot(v, n) * n;
        if (n.y < -0.5f) r.y = Mathf.Min(r.y, 0f);   // 천장에 머리를 박으면 바로 떨어지기 시작
        if (Current == State.Rising) Current = State.Airborne;   // 날갯짓이 끊김
        stunTimer = Mathf.Max(stunTimer, Mathf.Lerp(0.08f, 0.35f, kw));
        SetAirVelocity(r, keepState: true);
        Bounced?.Invoke(impact);
    }

    void SetAirVelocity(Vector3 v, bool keepState = false)
    {
        if (!keepState && v.y > 1f) Current = State.Airborne;
        rb.linearVelocity = v;
        lastVelocity = v;
    }

    // ───────────── 외부에서 부르는 것 ─────────────

    // 장애물(새 · 번개 · 우박 등): 죽지 않고 밀려남. 날갯짓 · 활공이 끊김.
    public void Knockback(Vector3 velocity, float stun)
    {
        Current = State.Airborne;
        Charge01 = 0f;
        glideArmed = false;
        glideLockTimer = 0.8f;
        stunTimer = Mathf.Max(stunTimer, stun);
        launchGraceTimer = 0.1f;
        rb.linearVelocity = velocity;
        lastVelocity = velocity;
        KnockedBack?.Invoke();
    }

    public void AddAcceleration(Vector3 a) => externalAccel += a;

    public void SetGravityScale(float scale) => gravityScale = Mathf.Max(0.05f, scale);

    public void Teleport(Vector3 position, float faceYaw)
    {
        rb.position = position;
        transform.position = position;
        rb.linearVelocity = Vector3.zero;
        lastVelocity = Vector3.zero;
        yaw = faceYaw;
        rb.rotation = Quaternion.Euler(0f, yaw, 0f);
        transform.rotation = rb.rotation;   // 카메라가 바로 뒤로 붙을 수 있게 즉시 반영
        Current = State.Airborne;
        Charge01 = 0f;
        apexY = position.y;
        slipTimer = tumbleTimer = stunTimer = 0f;
        glideEnergy = 1f;
    }
}
