using UnityEngine;

public class Mover : MonoBehaviour
{
    // W를 누를 때 수평으로 빨라지는 정도(m/s²)
    // (2026-11-11 속도 1.5배: 기본 · 최고 속도와 기본 가속은 ×1.5, '빨라지는 기믹'(W 계속 가속 · 대시 · 바람 고리)은 ×1.5에서 15% 낮춤)
    [SerializeField] float moveForce = 22.5f;
    // 날갯짓 한 번의 힘(질량 1이면 세로 속도 +jumpForce m/s)
    [SerializeField] float jumpForce = 10f;

    [Header("수평 이동")]
    [Tooltip("기본 속도(m/s): W를 누르면 여기까지는 빠르게(moveForce) 도달")]
    [SerializeField] float maxForwardSpeed = 15f;
    [Tooltip("최고 속도(m/s): W를 계속 누르고 있으면 기본 속도를 넘어 여기까지 점점 빨라짐(기본 속도의 3배)")]
    [SerializeField] float topSpeed = 45f;
    [Tooltip("기본 속도를 넘은 뒤의 가속(m/s²). 3.57이면 15 → 45m/s까지 약 8.4초")]
    [SerializeField] float overdriveAcceleration = 3.57f;

    [Header("대시 (Shift)")]
    [Tooltip("Shift를 꾹 눌렀을 때 1초에 걸쳐 더해지는 속도(m/s)")]
    [SerializeField] float dashBoost = 12.75f;
    [Tooltip("Shift를 꾹 누르면 dashBoost까지 차오르는 데 걸리는 시간(초). 짧게 누르면 그만큼만 빨라짐")]
    [SerializeField] float dashRampTime = 1f;
    [Tooltip("한 번에 누르고 있을 수 있는 최대 시간(초). 게이지가 다 닳으면 대시가 끝남")]
    [SerializeField] float dashMaxHold = 2f;
    [Tooltip("게이지가 빈 상태에서 다 차기까지(초)")]
    [SerializeField] float dashRechargeTime = 2f;
    [Tooltip("대시를 놓은 뒤 최고 속도 위로 넘친 속도가 줄어드는 감속(m/s²). 작을수록 부드럽게 풀림")]
    [SerializeField] float overSpeedDeceleration = 8f;
    [Tooltip("대시가 다 찼을 때 넓어지는 시야(°)")]
    [SerializeField] float dashFovKick = 10f;
    float dashEnergy = 1f;     // 1 = 가득(2초 누를 수 있음), 0 = 빈
    bool dashing;
    bool dashHeld;
    float dashBaseSpeed;       // 누르기 시작한 순간의 속도
    float dashRamp;            // 0 ~ 1(1초에 걸쳐 차오름)
    float dashKick;
    public bool IsDashing => dashing;
    public bool DashReady => !dashing && dashEnergy >= 0.25f;
    // 게이지(HUD 막대): 1 = 가득
    public float DashCharge => dashEnergy;
    public event System.Action Dashed;
    [SerializeField] float maxBackwardSpeed = 6f;
    [Tooltip("바람 고리를 지날 때 지금 속도에 최소 이만큼 더함(m/s)")]
    [SerializeField] float ringExtraSpeed = 7.65f;
    // W를 뗐을 때 감속(m/s²). 4.5면 15m/s에서 약 3.3초 동안 미끄러지듯 날아감(시즌1은 공기 저항 0이라 계속 날아감).
    [Tooltip("W를 뗐을 때 감속(m/s²). 작을수록 오래 미끄러지듯 날아감")]
    [SerializeField] float coastDeceleration = 4.5f;
    [Tooltip("방향을 튼 뒤 옆으로 미끄러지는 속도가 줄어드는 정도(m/s²). 작을수록 더 미끄러짐(얼음 · 관성), 크면 바로 꺾임")]
    [SerializeField] float slideGrip = 9f;
    // S 브레이크 감속(m/s²). 15m/s에서 약 0.5초 만에 멈춥니다.
    [SerializeField] float brakeDeceleration = 30f;

    [Header("바람에 밀리는 속도")]
    // 바람이 수평으로 밀어 줄 수 있는 최대 속도(m/s)
    [SerializeField] float maxWindDrift = 8f;
    // 바람 밖에서 밀린 속도가 사라지는 정도(m/s²)
    [SerializeField] float windDriftDecay = 10f;

    [Header("마우스 방향 (화면에서 닭 → 포인터 각도 = 닭 회전각)")]
    [Tooltip("길 방향에서 왼쪽으로 돌 수 있는 최대 각도(°)")]
    [SerializeField] float minYAngle = -45f;
    [Tooltip("길 방향에서 오른쪽으로 돌 수 있는 최대 각도(°)")]
    [SerializeField] float maxYAngle = 45f;
    [Tooltip("닭이 마우스 각도를 따라 도는 속도(°/초). 클수록 즉시 따라감")]
    [SerializeField] float rotationSpeed = 900f;
    [Tooltip("포인터가 닭에서 이 픽셀 안이면 방향을 바꾸지 않음")]
    [SerializeField] float mouseDeadZone = 12f;
    [Tooltip("켜면 카메라가 닭이 아니라 길 방향을 따라감(닭이 마우스 쪽으로 도는 게 화면에 그대로 보임). 끄면 예전처럼 닭 뒤를 따라 돎")]
    [SerializeField] bool cameraFollowsPath = true;
    [Tooltip("길이 꺾일 때 카메라가 따라 도는 부드러움(초)")]
    [SerializeField] float cameraYawSmoothTime = 0.35f;

    [Header("카메라 (Mover 혼자 위치 · 방향을 정함)")]
    [SerializeField] Transform cameraTransform;
    [Tooltip("진행 방향 기준: x = 뒤(-) / 앞(+), y = 위, z = 옆")]
    [SerializeField] Vector3 cameraOffset = new Vector3(-10f, 4.5f, 0f);
    [Tooltip("따라오는 부드러움(초). 작을수록 바로 따라옴")]
    [SerializeField] float cameraSmoothTime = 0.12f;
    [Tooltip("내려다보는 각도(°)")]
    [SerializeField] float cameraPitch = 10f;
    [Header("속도감 (빨라질수록)")]
    [SerializeField] float baseFov = 60f;
    [Tooltip("최고 속도일 때 넓어지는 시야(°)")]
    [SerializeField] float speedFovBoost = 16f;
    [Tooltip("최고 속도일 때 카메라가 더 뒤로 물러나는 거리(m)")]
    [SerializeField] float speedPullBack = 3f;
    [Tooltip("화면을 스쳐 가는 바람 줄(최고 속도일 때 초당 개수, 0이면 끔)")]
    [SerializeField] float speedLinesPerSecond = 140f;
    Vector3 cameraVelocity;
    Camera cameraComponent;
    ParticleSystem speedLines;

    // 0 = 기본 속도 이하, 1 = 최고 속도
    public float SpeedRatio => Mathf.Clamp01(Mathf.InverseLerp(maxForwardSpeed, topSpeed, horizontalSpeed));
    public Vector3 Velocity => playerRigidbody != null ? playerRigidbody.linearVelocity : Vector3.zero;

    // 체크포인트에서 부활한 직후 공중에 멈춰 있는 시간(초)
    [SerializeField] float respawnHoldTime = 0.5f;
    float respawnHoldTimer;

    // 막힌 동안 누른 날갯짓을 기억하는 시간(초)
    [SerializeField] float flapBufferTime = 0.15f;
    float flapBufferTimer;

    Rigidbody playerRigidbody;

    // 중력 반전 구간: 카메라가 180° 돌아 화면이 뒤집힌 상태입니다.
    // 물리(중력 · 날갯짓)는 그대로라서 화면 기준으로는 조작이 거꾸로 느껴집니다.
    // 마우스는 화면 기준이 유지되도록 좌우를 뒤집어 읽습니다.
    bool isViewFlipped;
    public bool IsViewFlipped => isViewFlipped;

    // 카메라 롤(°). GravityFlipDirector가 애니메이션합니다.
    public float CameraRoll { get; set; }

    // 날갯짓할 때 알립니다(날개 모션 · 깃털 이펙트).
    public event System.Action Flapped;

    float targetYAngle;
    float currentYAngle;
    float initialXAngle;
    float initialZAngle;

    float horizontalSpeed;

    // 부스트 링
    float boostTimer;
    float boostSpeed;
    public bool IsBoosting => boostTimer > 0f;

    // 바람: 이번 물리 프레임에 받은 수평 가속, 그리고 누적된 밀림 속도
    Vector3 windAcceleration;
    Vector3 windDrift;

    void Awake()
    {
        GameSession.EnsureStage(gameObject.scene.name);

        playerRigidbody = GetComponent<Rigidbody>();
        // 카메라가 따라갈 때 떨리지 않게 보간합니다.
        playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        // 부스트 · 점프대로 빨라져도 얇은 벽 · 경계를 뚫고 나가지 않게 연속 충돌 검사
        playerRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Vector3 initialAngles = transform.eulerAngles;

        targetYAngle = initialAngles.y;
        currentYAngle = initialAngles.y;
        initialXAngle = initialAngles.x;
        initialZAngle = initialAngles.z;

        path = CoursePath.Get();

        if (GameSession.HasCheckpoint)
        {
            transform.position = GameSession.CheckpointPosition;
            playerRigidbody.position = GameSession.CheckpointPosition;
            respawnHoldTimer = respawnHoldTime;

            // 반전 구간 안의 체크포인트면 처음부터 뒤집힌 화면으로 시작합니다.
            if (GameSession.CheckpointReversed)
            {
                SetViewFlipped(true);
                CameraRoll = 180f;
            }
        }
    }

    void Start()
    {
        // 시작 · 부활 위치의 길 방향을 바라보고 시작(꺾이는 길)
        if (path != null)
        {
            float s = path.Progress(transform.position, ref pathHint);
            currentYAngle = targetYAngle = pathYaw = cameraYaw = path.Yaw(s);
            transform.rotation = Quaternion.Euler(initialXAngle, currentYAngle, initialZAngle);
            playerRigidbody.rotation = transform.rotation;
        }

        if (cameraTransform != null)
        {
            cameraComponent = cameraTransform.GetComponent<Camera>();
            if (cameraComponent != null) baseFov = cameraComponent.fieldOfView;
            CreateSpeedLines();

            // 첫 프레임에 카메라를 바로 제자리로(부활 · 시작 때 멀리서 날아오지 않게)
            cameraTransform.position = CameraTargetPosition();
            cameraTransform.rotation = CameraTargetRotation();
        }
    }

    Vector3 CameraTargetPosition()
    {
        Vector3 facing = CameraHeading();
        Vector3 side = Vector3.Cross(Vector3.up, facing);
        float back = cameraOffset.x - speedPullBack * speedFeel;   // 빨라질수록 살짝 뒤로
        return transform.position + facing * back + Vector3.up * cameraOffset.y + side * cameraOffset.z;
    }

    // 화면을 스쳐 지나가는 바람 줄(카메라 앞에서 생겨 뒤로 흘러감). 빠를수록 많이.
    void CreateSpeedLines()
    {
        if (speedLinesPerSecond <= 0f || speedLines != null) return;

        GameObject go = new GameObject("SpeedLines");
        go.transform.SetParent(cameraTransform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 14f);
        speedLines = go.AddComponent<ParticleSystem>();
        speedLines.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = speedLines.main;
        main.loop = true;
        main.startLifetime = 0.45f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = new Color(1f, 1f, 1f, 0.5f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 300;

        ParticleSystem.EmissionModule em = speedLines.emission;
        em.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = speedLines.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 7f;
        shape.radiusThickness = 0.35f;   // 가운데(닭)는 비우고 테두리 쪽에서만

        ParticleSystem.VelocityOverLifetimeModule vel = speedLines.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = 0f; vel.y = 0f; vel.z = -45f;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.06f;
        r.lengthScale = 1f;
        Material mat = Resources.Load<Material>("FX/FX_Particle");
        if (mat != null) r.sharedMaterial = mat;

        speedLines.Play();
    }

    // 0~1: 기본 속도를 넘은 만큼(부드럽게)
    float speedFeel;

    Quaternion CameraTargetRotation()
    {
        return Quaternion.LookRotation(CameraHeading(), Vector3.up) *
               Quaternion.Euler(cameraPitch, 0f, 0f) *
               Quaternion.AngleAxis(CameraRoll, Vector3.forward);
    }

    void LateUpdate()
    {
        if (cameraTransform == null)
        {
            return;
        }

        speedFeel = Mathf.Lerp(speedFeel, SpeedRatio, 1f - Mathf.Exp(-Time.deltaTime * 4f));
        cameraYaw = Mathf.SmoothDampAngle(cameraYaw, pathYaw, ref cameraYawVelocity, cameraYawSmoothTime);

        cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, CameraTargetPosition(), ref cameraVelocity, cameraSmoothTime);
        cameraTransform.rotation = CameraTargetRotation();

        // 속도감: 시야가 넓어지고 바람 줄이 많아짐
        if (cameraComponent != null)
        {
            cameraComponent.fieldOfView = baseFov + speedFovBoost * speedFeel + dashFovKick * dashKick;
        }
        if (speedLines != null)
        {
            ParticleSystem.EmissionModule em = speedLines.emission;
            em.rateOverTime = speedLinesPerSecond * speedFeel * speedFeel;
        }
    }

    void Update()
    {
        SetTargetAngle();

        // 대시: Shift를 꾹 누르면 누르기 시작한 속도 + dashBoost까지 dashRampTime(1초)에 걸쳐 차오름.
        // 누르는 동안 게이지가 닳고(dashMaxHold초), 떼면 다시 참.
        dashHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool canDash = respawnHoldTimer <= 0f && !InputLocked && !playerRigidbody.isKinematic;
        if (!dashing && dashHeld && canDash && DashReady &&
            (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)))
        {
            StartDash();
        }
        if (dashing)
        {
            dashEnergy -= Time.deltaTime / Mathf.Max(0.1f, dashMaxHold);
            if (!dashHeld || !canDash || dashEnergy <= 0f)
            {
                dashEnergy = Mathf.Max(dashEnergy, 0f);
                dashing = false;
            }
        }
        else
        {
            dashEnergy = Mathf.MoveTowards(dashEnergy, 1f, Time.deltaTime / Mathf.Max(0.1f, dashRechargeTime));
            dashRamp = 0f;
        }
        dashKick = Mathf.MoveTowards(dashKick, dashing ? dashRamp : 0f, Time.deltaTime * 2.5f);

        // 입력 버퍼: 잠깐 막힌 동안(부활 직후 · 반전 연출) 누른 날갯짓은 flapBufferTime 안에 풀리면 바로 나갑니다.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            flapBufferTimer = flapBufferTime;
        }

        if (flapBufferTimer > 0f)
        {
            flapBufferTimer -= Time.unscaledDeltaTime;

            if (respawnHoldTimer <= 0f && !InputLocked)
            {
                flapBufferTimer = 0f;
                // 날갯짓은 늘 월드 위쪽. 화면이 뒤집혀 있으면 화면에서는 아래로 보입니다.
                playerRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                Flapped?.Invoke();
            }
        }
    }

    void FixedUpdate()
    {
        if (respawnHoldTimer > 0f)
        {
            respawnHoldTimer -= Time.fixedDeltaTime;
            playerRigidbody.linearVelocity = Vector3.zero;
            windAcceleration = Vector3.zero;
            RotateByMouse();
            return;
        }

        RotateByMouse();
        MovePlayer();
    }

    // 반전 연출 중에는 날갯짓 입력을 잠깐 막습니다(GravityFlipDirector가 켜고 끔).
    public bool InputLocked { get; set; }

    public void SetViewFlipped(bool flipped)
    {
        isViewFlipped = flipped;
    }

    void StartDash()
    {
        dashing = true;
        dashRamp = 0f;
        dashBaseSpeed = Mathf.Max(horizontalSpeed, 0f);
        FeedbackFX.Boost(transform.position, FacingDirection());
        Dashed?.Invoke();
    }

    // 바람 고리(부스트): duration초 동안 수평 속도를 speed 또는 '지금 속도 + 6' 중 큰 값까지(최고 속도의 1.25배 이내)
    public void Boost(float speed, float duration)
    {
        boostSpeed = Mathf.Min(Mathf.Max(speed, horizontalSpeed + ringExtraSpeed), topSpeed * 1.25f);
        boostTimer = Mathf.Max(boostTimer, duration);
        ApplyForwardSpeed(Mathf.Max(horizontalSpeed, boostSpeed));
    }

    // 앞으로 가는 수평 속도를 바로 바꿉니다. FixedUpdate가 매번 실제 속도에서 horizontalSpeed를 다시 읽으므로
    // 변수만 바꾸면 효과가 사라집니다(부스트 · 대시 모두 이 함수로 실제 속도에 넣음). 세로 속도와 바람 밀림은 유지.
    void ApplyForwardSpeed(float speed)
    {
        horizontalSpeed = speed;
        if (playerRigidbody == null || playerRigidbody.isKinematic)
        {
            return;
        }
        Vector3 v = playerRigidbody.linearVelocity;
        Vector3 facing = FacingDirection();
        Vector3 current = new Vector3(v.x, 0f, v.z) - windDrift;
        Vector3 slide = current - facing * Vector3.Dot(current, facing);   // 미끄러지던 옆 속도는 유지
        Vector3 h = facing * speed + slide + windDrift;
        playerRigidbody.linearVelocity = new Vector3(h.x, v.y, h.z);
    }

    // 바람 구역이 물리 프레임마다 부릅니다. 세로 성분은 바로 힘으로, 수평 성분은 밀림 속도로 처리합니다.
    public void AddWind(Vector3 acceleration)
    {
        playerRigidbody.AddForce(new Vector3(0f, acceleration.y, 0f), ForceMode.Acceleration);
        windAcceleration += new Vector3(acceleration.x, 0f, acceleration.z);
    }

    // 점프대: 세로 속도를 정해진 값으로 맞춥니다(월드 위쪽).
    public void Launch(float verticalSpeed)
    {
        Vector3 v = playerRigidbody.linearVelocity;
        v.y = verticalSpeed;
        playerRigidbody.linearVelocity = v;
    }

    // 마우스 각도 = 닭 회전각: 화면에서 '닭 → 마우스 포인터' 선이 길 앞쪽에서 기운 각도만큼 닭이 돕니다.
    // 화면에 그려지는 각도로 맞추므로(원근 · 카메라 기울기 · 중력 반전 화면 회전 포함) 닭 머리가 늘 포인터 쪽을 향합니다.
    void SetTargetAngle()
    {
        pathYaw = path != null ? path.Yaw(path.Progress(transform.position, ref pathHint)) : currentYAngle;

        if (cameraComponent == null)
        {
            // 카메라가 없으면 예전 방식(화면 가로 위치 → 각도)
            float mouseX = Mathf.Clamp01(Input.mousePosition.x / Screen.width);
            if (isViewFlipped) mouseX = 1f - mouseX;
            targetYAngle = pathYaw + Mathf.Lerp(minYAngle, maxYAngle, mouseX);
            return;
        }

        if (TryYawOffsetForScreenPoint(Input.mousePosition, out float offset))
        {
            targetYAngle = pathYaw + offset;
        }
    }

    // 화면의 한 점(포인터)을 '닭이 그쪽을 보려면 길 방향에서 몇 ° 돌아야 하나'로 바꿉니다(최소 ~ 최대 각도로 자름).
    public bool TryYawOffsetForScreenPoint(Vector2 screenPoint, out float offset)
    {
        offset = 0f;
        if (cameraComponent == null) return false;
        Vector3 origin = transform.position;
        Vector3 o3 = cameraComponent.WorldToScreenPoint(origin);
        if (o3.z <= 0f) return false;
        Vector2 o = o3;
        Vector2 m = screenPoint - o;
        if (m.sqrMagnitude < mouseDeadZone * mouseDeadZone)
        {
            return false;   // 포인터가 닭 바로 위면 방향을 정할 수 없으니 그대로
        }

        Vector2 fwd = ScreenDir(origin, pathYaw, o);
        float want = -Vector2.SignedAngle(fwd, m);   // +면 화면에서 오른쪽으로 기움

        // 월드 각도 θ를 화면에 그렸을 때의 각도가 want가 되는 θ를 이분 탐색(원근 때문에 1:1이 아님)
        float lo = -89f, hi = 89f;
        for (int i = 0; i < 14; i++)
        {
            float mid = (lo + hi) * 0.5f;
            float shown = -Vector2.SignedAngle(fwd, ScreenDir(origin, pathYaw + mid, o));
            if (shown < want) lo = mid; else hi = mid;
        }
        offset = Mathf.Clamp((lo + hi) * 0.5f, minYAngle, maxYAngle);
        return true;
    }

    // 확인용: 길 방향에서 offset만큼 돈 방향이 화면에서 닭 기준 몇 °로 그려지는지(오른쪽 +)
    public float ScreenAngleOfYawOffset(float offset)
    {
        if (cameraComponent == null) return float.NaN;
        Vector2 o = cameraComponent.WorldToScreenPoint(transform.position);
        Vector2 fwd = ScreenDir(transform.position, pathYaw, o);
        return -Vector2.SignedAngle(fwd, ScreenDir(transform.position, pathYaw + offset, o));
    }

    // 닭에서 yaw 방향으로 조금 나간 점이 화면에서 닭으로부터 어느 쪽인지
    Vector2 ScreenDir(Vector3 origin, float yaw, Vector2 originOnScreen)
    {
        Vector3 p = cameraComponent.WorldToScreenPoint(origin + YawDirection(yaw) * 2f);
        return (Vector2)p - originOnScreen;
    }

    Vector3 YawDirection(float yaw)
    {
        Vector3 tiltedForward = Quaternion.Euler(initialXAngle, 0f, initialZAngle) * Vector3.right;
        Vector3 flatForward = new Vector3(tiltedForward.x, 0f, tiltedForward.z);
        if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.right;
        return Quaternion.Euler(0f, yaw, 0f) * flatForward.normalized;
    }

    float pathYaw;
    float cameraYaw;
    float cameraYawVelocity;

    CoursePath path;
    int pathHint = -1;

    void RotateByMouse()
    {
        currentYAngle = Mathf.MoveTowardsAngle(
            currentYAngle,
            targetYAngle,
            rotationSpeed * Time.fixedDeltaTime);

        playerRigidbody.MoveRotation(Quaternion.Euler(initialXAngle, currentYAngle, initialZAngle));
    }

    // 모델 축이 기울어져 있어 앞쪽은 transform.right입니다. 좌우 각도만으로 수평 앞쪽을 구합니다.
    Vector3 FacingDirection() => YawDirection(currentYAngle);

    // 카메라가 바라보는 수평 방향: 닭이 아니라 '길 방향'을 부드럽게 따라감 → 닭이 마우스 쪽으로 도는 게 화면에 그대로 보임
    Vector3 CameraHeading() => cameraFollowsPath ? YawDirection(cameraYaw) : FacingDirection();

    // W 전진 · S 브레이크 겸 후진 · 좌우는 마우스. 위아래 속도는 건드리지 않습니다.
    void MovePlayer()
    {
        // 도착 · 사망 연출 중(Kinematic)에는 속도를 건드리지 않습니다.
        if (playerRigidbody.isKinematic)
        {
            return;
        }

        float dt = Time.fixedDeltaTime;
        Vector3 facing = FacingDirection();
        Vector3 velocity = playerRigidbody.linearVelocity;

        // 지난 프레임에 더한 바람 밀림을 빼고 조작 속도만 읽습니다.
        // 앞(바라보는 방향) 성분과 옆 성분으로 나눕니다. 방향을 틀면 원래 가던 속도가 옆 성분으로 남아
        // 바로 사라지지 않고 slideGrip만큼씩 줄어듭니다 = 시즌1처럼 미끄러지는 손맛(관성).
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z) - windDrift;
        horizontalSpeed = Vector3.Dot(horizontal, facing);
        Vector3 slide = horizontal - facing * horizontalSpeed;
        slide = Vector3.MoveTowards(slide, Vector3.zero, slideGrip * dt);

        if (boostTimer > 0f)
        {
            boostTimer -= dt;
        }

        // W를 계속 누르면: 기본 속도까지는 빠르게(moveForce), 그 뒤로는 천천히(overdrive) 최고 속도까지 계속 빨라짐
        float forwardLimit = boostTimer > 0f ? Mathf.Max(boostSpeed, topSpeed) : topSpeed;
        bool forwardKey = Input.GetKey(KeyCode.W);
        bool backKey = Input.GetKey(KeyCode.S);

        // 대시 중: 목표 속도(시작 속도 + dashBoost × 차오름)로 부드럽게 끌어올림. 1초면 +dashBoost 전부.
        float dashTarget = 0f;
        if (dashing)
        {
            dashRamp = Mathf.MoveTowards(dashRamp, 1f, dt / Mathf.Max(0.05f, dashRampTime));
            dashTarget = dashBaseSpeed + dashBoost * dashRamp;
            forwardLimit = Mathf.Max(forwardLimit, dashTarget);
            if (forwardKey)
            {
                // W도 누르고 있으면 기본 가속으로 오른 만큼 기준 속도도 따라 올라감
                dashBaseSpeed = Mathf.Max(dashBaseSpeed, horizontalSpeed - dashBoost * dashRamp);
            }
        }

        if (forwardKey && !backKey)
        {
            float accel = horizontalSpeed < maxForwardSpeed ? moveForce : overdriveAcceleration;
            horizontalSpeed = horizontalSpeed < 0f
                ? Mathf.MoveTowards(horizontalSpeed, 0f, brakeDeceleration * dt)
                : Mathf.MoveTowards(horizontalSpeed, forwardLimit, accel * dt);
        }
        else if (backKey && !forwardKey)
        {
            horizontalSpeed = horizontalSpeed > 0f
                ? Mathf.MoveTowards(horizontalSpeed, 0f, brakeDeceleration * dt)
                : Mathf.MoveTowards(horizontalSpeed, -maxBackwardSpeed, moveForce * dt);
        }
        else if (boostTimer <= 0f && !dashing)
        {
            horizontalSpeed = Mathf.MoveTowards(horizontalSpeed, 0f, coastDeceleration * dt);
        }

        if (dashing && !backKey && horizontalSpeed < dashTarget)
        {
            // 차오르는 목표를 따라감(목표 자체가 1초에 걸쳐 오르므로 갑자기 튀지 않음)
            horizontalSpeed = Mathf.MoveTowards(horizontalSpeed, dashTarget, (dashBoost / Mathf.Max(0.05f, dashRampTime) + moveForce) * dt);
        }

        if (horizontalSpeed > forwardLimit)
        {
            // 대시 · 바람 고리가 끝나 최고 속도를 넘친 만큼은 부드럽게 풀고, S를 누르면 바로 브레이크
            float decel = backKey ? brakeDeceleration : overSpeedDeceleration;
            horizontalSpeed = Mathf.MoveTowards(horizontalSpeed, forwardLimit, decel * dt);
        }

        // 바람 밀림: 바람 안에서는 쌓이고, 밖에서는 서서히 사라집니다.
        if (windAcceleration.sqrMagnitude > 0.0001f)
        {
            windDrift += windAcceleration * dt;
            windDrift = Vector3.ClampMagnitude(windDrift, maxWindDrift);
        }
        else
        {
            windDrift = Vector3.MoveTowards(windDrift, Vector3.zero, windDriftDecay * dt);
        }
        windAcceleration = Vector3.zero;

        Vector3 newHorizontal = facing * horizontalSpeed + slide + windDrift;
        playerRigidbody.linearVelocity = new Vector3(newHorizontal.x, velocity.y, newHorizontal.z);
    }
}
