using UnityEngine;

// 닭을 귀엽게 움직이는 절차 애니메이션(키프레임 없이 코드로).
//  - 날갯짓(Space): 날개를 크게 내려쳤다 올림 + 몸이 순간 납작 → 쭉 늘어남 + 다리를 뒤로 참 + 깃털
//  - 날 때: 다리를 번갈아 동동 젓고 날개는 작게 파르르
//  - 빨리 떨어질 때: 날개를 위로 들고 허둥지둥, 다리는 아래로 쭉
//  - 앞으로 갈 때: 속도만큼 몸이 앞으로 기울고 머리가 까딱
//  - 가끔 눈 깜빡
// 모델 부품 이름(ChickenModelBuilder가 만듦): Model/BodyPivot, HeadPivot, WingPivot_L/R, LegPivot_L/R, TailPivot, Eye_L/R
public class ChickenAnimator : MonoBehaviour
{
    [Header("날갯짓")]
    [SerializeField] float flapAngle = 60f;          // 내려칠 때 각도(°)
    [SerializeField] float flapDownTime = 0.07f;     // 내려치는 시간(초)
    [SerializeField] float flapUpTime = 0.22f;       // 돌아오는 시간(초)
    [SerializeField] float squash = 0.16f;           // 날갯짓 순간 몸이 납작해지는 정도

    [Header("날고 있을 때")]
    [SerializeField] float flutterAngle = 9f;        // 날개 파르르(°)
    [SerializeField] float flutterSpeed = 9f;        // 파르르 속도(Hz 비슷)
    [SerializeField] float paddleAngle = 28f;        // 다리 동동(°)
    [SerializeField] float paddleSpeed = 2.2f;       // 다리 젓는 속도(회/초)

    [Header("떨어질 때")]
    [SerializeField] float fallSpeedForPanic = 7f;   // 이보다 빨리 떨어지면 허둥지둥(m/s)
    [SerializeField] float panicWingLift = 45f;

    [Header("기울기 · 머리")]
    [SerializeField] float tiltPerSpeed = 1.3f;      // 앞으로 1m/s당 몸 기울기(°)
    [SerializeField] float maxTilt = 16f;
    [SerializeField] float headBob = 0.05f;

    [Header("눈 깜빡")]
    [SerializeField] Vector2 blinkInterval = new Vector2(2.2f, 5f);

    Mover mover;
    Rigidbody body;
    Transform bodyPivot, headPivot, wingL, wingR, legL, legR, tail, eyeL, eyeR;
    Vector3 bodyScale, headPos, eyeScale;
    Quaternion bodyRest, wingLRest, wingRRest, legLRest, legRRest, tailRest;

    float flapTimer = -1f;
    float blinkTimer;
    float blinkPhase = -1f;
    float tilt;
    float panic;
    float standing;

    void Awake()
    {
        mover = GetComponent<Mover>();
        body = GetComponent<Rigidbody>();
        bodyPivot = Find("BodyPivot");
        headPivot = Find("HeadPivot");
        wingL = Find("WingPivot_L");
        wingR = Find("WingPivot_R");
        legL = Find("LegPivot_L");
        legR = Find("LegPivot_R");
        tail = Find("TailPivot");
        eyeL = Find("Eye_L");
        eyeR = Find("Eye_R");

        if (bodyPivot != null) { bodyScale = bodyPivot.localScale; bodyRest = bodyPivot.localRotation; }
        if (headPivot != null) headPos = headPivot.localPosition;
        if (wingL != null) wingLRest = wingL.localRotation;
        if (wingR != null) wingRRest = wingR.localRotation;
        if (legL != null) legLRest = legL.localRotation;
        if (legR != null) legRRest = legR.localRotation;
        if (tail != null) tailRest = tail.localRotation;
        if (eyeL != null) eyeScale = eyeL.localScale;
        blinkTimer = Random.Range(blinkInterval.x, blinkInterval.y);
    }

    void OnEnable()
    {
        if (mover != null) mover.Flapped += OnFlapped;
    }

    void OnDisable()
    {
        if (mover != null) mover.Flapped -= OnFlapped;
    }

    void OnFlapped()
    {
        flapTimer = 0f;
        FeedbackFX.Flap(transform.position, false);
    }

    Transform Find(string n)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == n) return t;
        }
        return null;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float time = Time.time;
        Vector3 v = body != null ? body.linearVelocity : Vector3.zero;
        float forward = new Vector2(v.x, v.z).magnitude;
        bool alive = mover == null || mover.enabled;

        // 날갯짓 진행도: 0 → 1(내려침) → 0(복귀)
        float flap = 0f;
        if (flapTimer >= 0f)
        {
            flapTimer += dt;
            if (flapTimer < flapDownTime)
            {
                flap = flapTimer / flapDownTime;
            }
            else
            {
                float u = Mathf.Clamp01((flapTimer - flapDownTime) / flapUpTime);
                flap = (1f - u) * (1f - u);   // ease-out 복귀
            }
            if (flapTimer >= flapDownTime + flapUpTime) flapTimer = -1f;
        }

        // 빨리 떨어지면 허둥지둥(부드럽게 들어가고 나옴)
        float targetPanic = alive && v.y < -fallSpeedForPanic ? 1f : 0f;
        panic = Mathf.MoveTowards(panic, targetPanic, dt * 4f);

        // 발밑 1.3m 안에 무언가 있고 거의 안 움직이면 '서 있음' → 다리는 가만히, 날개는 접고 가끔 꼼지락
        bool grounded = Mathf.Abs(v.y) < 0.3f && forward < 0.5f &&
                        Physics.Raycast(transform.position, Vector3.down, 1.3f, ~0, QueryTriggerInteraction.Ignore);
        standing = Mathf.MoveTowards(standing, grounded ? 1f : 0f, dt * 5f);

        // ── 날개: 내려치기 + 파르르 + 허둥지둥(위로 들고 빠르게 퍼덕) ──
        float flutter = Mathf.Sin(time * flutterSpeed * Mathf.PI * 2f) * flutterAngle;
        // 서 있을 때: 2초마다 한 번 날개를 살짝 들썩
        float fidget = Mathf.Max(0f, Mathf.Sin(time * Mathf.PI)) > 0.97f ? -12f : 0f;
        flutter = Mathf.Lerp(flutter, fidget, standing);
        float flail = Mathf.Sin(time * 16f * Mathf.PI) * 18f;
        float wing = flap * flapAngle + (1f - flap) * Mathf.Lerp(flutter, -panicWingLift + flail, panic);
        if (wingL != null) wingL.localRotation = wingLRest * Quaternion.AngleAxis(wing, Vector3.right);
        if (wingR != null) wingR.localRotation = wingRRest * Quaternion.AngleAxis(-wing, Vector3.right);

        // ── 다리: 동동 젓기 / 날갯짓 때 뒤로 차기 / 떨어질 때 아래로 쭉 ──
        float paddle = Mathf.Sin(time * paddleSpeed * Mathf.PI * 2f) * paddleAngle * (1f - standing);
        float kick = -flap * 40f;   // 날갯짓 때 다리를 뒤로 참
        float legL_ = Mathf.Lerp(paddle, 5f, panic) + kick;
        float legR_ = Mathf.Lerp(-paddle, -5f, panic) + kick;
        if (legL != null) legL.localRotation = legLRest * Quaternion.AngleAxis(legL_, Vector3.forward);
        if (legR != null) legR.localRotation = legRRest * Quaternion.AngleAxis(legR_, Vector3.forward);

        // ── 몸: 날갯짓 순간 납작(squash) → 늘어남(stretch), 속도만큼 앞으로 기울기 ──
        if (bodyPivot != null)
        {
            float s = flapTimer >= 0f && flapTimer < flapDownTime ? flap : 0f;           // 내려칠 때 납작
            float st = flapTimer >= flapDownTime ? flap : 0f;                             // 올라갈 때 쭉
            bodyPivot.localScale = new Vector3(
                bodyScale.x * (1f + squash * 0.6f * s - squash * 0.3f * st),
                bodyScale.y * (1f - squash * s + squash * 0.6f * st),
                bodyScale.z * (1f + squash * 0.6f * s - squash * 0.3f * st));

            float targetTilt = Mathf.Clamp(-forward * tiltPerSpeed + Mathf.Clamp(v.y, -10f, 10f) * 0.8f, -maxTilt, maxTilt);
            tilt = Mathf.Lerp(tilt, targetTilt, 1f - Mathf.Exp(-dt * 8f));
            bodyPivot.localRotation = bodyRest * Quaternion.AngleAxis(tilt, Vector3.forward);
        }

        // ── 머리 까딱 ──
        if (headPivot != null)
        {
            float bob = Mathf.Sin(time * 7f) * headBob * Mathf.Clamp01(forward / 6f + 0.3f);
            headPivot.localPosition = headPos + new Vector3(0f, bob - flap * 0.06f, 0f);
        }

        // ── 꽁지깃 살랑 ──
        if (tail != null)
        {
            tail.localRotation = tailRest * Quaternion.AngleAxis(Mathf.Sin(time * 5f) * 6f + flap * 12f, Vector3.forward);
        }

        // ── 눈 깜빡 ──
        Blink(dt);
    }

    void Blink(float dt)
    {
        if (eyeL == null || eyeR == null) return;

        if (blinkPhase < 0f)
        {
            blinkTimer -= dt;
            if (blinkTimer <= 0f) blinkPhase = 0f;
            return;
        }

        blinkPhase += dt;
        const float dur = 0.14f;
        float k = Mathf.Abs(Mathf.Cos(Mathf.Clamp01(blinkPhase / dur) * Mathf.PI));   // 1 → 0 → 1
        Vector3 s = new Vector3(eyeScale.x, eyeScale.y * Mathf.Max(0.1f, k), eyeScale.z);
        eyeL.localScale = s;
        eyeR.localScale = s;
        if (blinkPhase >= dur)
        {
            blinkPhase = -1f;
            eyeL.localScale = eyeScale;
            eyeR.localScale = eyeScale;
            blinkTimer = Random.Range(blinkInterval.x, blinkInterval.y);
        }
    }
}
