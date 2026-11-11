using UnityEngine;

public class Mover : MonoBehaviour
{
    // W를 누를 때 수평으로 빨라지는 정도(m/s²)
    [SerializeField] float moveForce = 15f;
    // 날갯짓 한 번의 힘(질량 1이면 세로 속도 +jumpForce m/s)
    [SerializeField] float jumpForce = 10f;

    [Header("수평 이동 (미끄러짐 방지)")]
    [Tooltip("기본 속도(m/s): W를 누르면 여기까지는 빠르게(moveForce) 도달")]
    [SerializeField] float maxForwardSpeed = 10f;
    [Tooltip("최고 속도(m/s): W를 계속 누르고 있으면 기본 속도를 넘어 여기까지 점점 빨라짐(기본 속도의 3배)")]
    [SerializeField] float topSpeed = 30f;
    [Tooltip("기본 속도를 넘은 뒤의 가속(m/s²). 2.8이면 10 → 30m/s까지 약 7초")]
    [SerializeField] float overdriveAcceleration = 2.8f;
    [SerializeField] float maxBackwardSpeed = 4f;
    // W를 뗐을 때 감속(m/s²). 10m/s에서 약 1.4초 만에 멈춥니다.
    [SerializeField] float coastDeceleration = 7f;
    // S 브레이크 감속(m/s²). 10m/s에서 약 0.5초 만에 멈춥니다.
    [SerializeField] float brakeDeceleration = 20f;

    [Header("바람에 밀리는 속도")]
    // 바람이 수평으로 밀어 줄 수 있는 최대 속도(m/s)
    [SerializeField] float maxWindDrift = 8f;
    // 바람 밖에서 밀린 속도가 사라지는 정도(m/s²)
    [SerializeField] float windDriftDecay = 10f;

    [Header("마우스 방향")]
    [SerializeField] float minYAngle = -45f;
    [SerializeField] float maxYAngle = 45f;
    [SerializeField] float rotationSpeed = 180f;

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
            currentYAngle = targetYAngle = path.Yaw(s);
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
        Vector3 facing = FacingDirection();
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
        return Quaternion.LookRotation(FacingDirection(), Vector3.up) *
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

        cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, CameraTargetPosition(), ref cameraVelocity, cameraSmoothTime);
        cameraTransform.rotation = CameraTargetRotation();

        // 속도감: 시야가 넓어지고 바람 줄이 많아짐
        if (cameraComponent != null)
        {
            cameraComponent.fieldOfView = baseFov + speedFovBoost * speedFeel;
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

    // 바람 고리(부스트): duration초 동안 수평 속도를 speed 또는 '지금 속도 + 6' 중 큰 값까지(최고 속도의 1.25배 이내)
    public void Boost(float speed, float duration)
    {
        boostSpeed = Mathf.Min(Mathf.Max(speed, horizontalSpeed + 6f), topSpeed * 1.25f);
        boostTimer = Mathf.Max(boostTimer, duration);
        horizontalSpeed = Mathf.Max(horizontalSpeed, boostSpeed);
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

    void SetTargetAngle()
    {
        float mouseX = Mathf.Clamp01(Input.mousePosition.x / Screen.width);

        // 화면이 180° 돌아 있으면 화면 왼쪽 = 월드 오른쪽. 화면 기준을 지키도록 뒤집어 읽습니다.
        if (isViewFlipped)
        {
            mouseX = 1f - mouseX;
        }

        // 마우스 좌우는 '지금 위치의 길 방향' 기준 ±45°(길이 꺾여도 따라갈 수 있음)
        float baseYaw = path != null ? path.Yaw(path.Progress(transform.position, ref pathHint)) : 0f;
        targetYAngle = baseYaw + Mathf.Lerp(minYAngle, maxYAngle, mouseX);
    }

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

    Vector3 FacingDirection()
    {
        // 모델 축이 기울어져 있어 앞쪽은 transform.right입니다. 좌우 각도만으로 수평 앞쪽을 구합니다.
        Vector3 tiltedForward = Quaternion.Euler(initialXAngle, 0f, initialZAngle) * Vector3.right;
        Vector3 flatForward = new Vector3(tiltedForward.x, 0f, tiltedForward.z);

        if (flatForward.sqrMagnitude < 0.0001f)
        {
            flatForward = Vector3.right;
        }

        return Quaternion.Euler(0f, currentYAngle, 0f) * flatForward.normalized;
    }

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
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z) - windDrift;
        float speed = horizontal.magnitude;
        horizontalSpeed = Vector3.Dot(horizontal, facing) < 0f ? -speed : speed;

        if (boostTimer > 0f)
        {
            boostTimer -= dt;
        }

        // W를 계속 누르면: 기본 속도까지는 빠르게(moveForce), 그 뒤로는 천천히(overdrive) 최고 속도까지 계속 빨라짐
        float forwardLimit = boostTimer > 0f ? Mathf.Max(boostSpeed, topSpeed) : topSpeed;
        bool forwardKey = Input.GetKey(KeyCode.W);
        bool backKey = Input.GetKey(KeyCode.S);

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
        else if (boostTimer <= 0f)
        {
            horizontalSpeed = Mathf.MoveTowards(horizontalSpeed, 0f, coastDeceleration * dt);
        }

        if (horizontalSpeed > forwardLimit)
        {
            horizontalSpeed = Mathf.MoveTowards(horizontalSpeed, forwardLimit, brakeDeceleration * dt);
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

        Vector3 newHorizontal = facing * horizontalSpeed + windDrift;
        playerRigidbody.linearVelocity = new Vector3(newHorizontal.x, velocity.y, newHorizontal.z);
    }
}
