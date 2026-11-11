using UnityEngine;

public class Mover : MonoBehaviour
{
    // W를 누를 때 수평으로 빨라지는 정도(m/s²)
    [SerializeField] float moveForce = 15f;
    // 날갯짓 한 번의 힘(질량 1이면 세로 속도 +jumpForce m/s)
    [SerializeField] float jumpForce = 10f;

    [Header("수평 이동 (미끄러짐 방지)")]
    [SerializeField] float maxForwardSpeed = 10f;
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

    [SerializeField] Transform cameraTransform;

    // 체크포인트에서 부활한 직후 공중에 멈춰 있는 시간(초)
    [SerializeField] float respawnHoldTime = 0.5f;
    float respawnHoldTimer;

    // 막힌 동안 누른 날갯짓을 기억하는 시간(초)
    [SerializeField] float flapBufferTime = 0.15f;
    float flapBufferTimer;

    float initialCameraXAngle;
    float initialCameraZAngle;

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

        if (cameraTransform != null)
        {
            Vector3 cameraAngles = cameraTransform.eulerAngles;
            initialCameraXAngle = cameraAngles.x;
            initialCameraZAngle = cameraAngles.z;
        }

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

    void LateUpdate()
    {
        if (cameraTransform == null)
        {
            return;
        }

        cameraTransform.rotation =
            Quaternion.Euler(initialCameraXAngle, currentYAngle, initialCameraZAngle) *
            Quaternion.AngleAxis(CameraRoll, Vector3.forward);
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

    // 부스트 링: duration초 동안 수평 속도를 speed까지 끌어올립니다.
    public void Boost(float speed, float duration)
    {
        boostSpeed = Mathf.Max(speed, maxForwardSpeed);
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

        targetYAngle = Mathf.Lerp(minYAngle, maxYAngle, mouseX);
    }

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

        float forwardLimit = boostTimer > 0f ? boostSpeed : maxForwardSpeed;
        bool forwardKey = Input.GetKey(KeyCode.W);
        bool backKey = Input.GetKey(KeyCode.S);

        if (forwardKey && !backKey)
        {
            horizontalSpeed = horizontalSpeed < 0f
                ? Mathf.MoveTowards(horizontalSpeed, 0f, brakeDeceleration * dt)
                : Mathf.MoveTowards(horizontalSpeed, forwardLimit, moveForce * dt);
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
