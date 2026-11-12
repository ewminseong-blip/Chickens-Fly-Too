using UnityEngine;

// 등반용 닭 애니메이션(코드로). 기존 ChickenAnimator와 같은 부품 이름을 씁니다.
//  - 충전: 몸이 점점 납작하게 웅크리고 날개를 모으며 부들부들(꽉 차면 더 떨림)
//  - 날아오름: 파닥임마다 날개를 크게 내려침 + 몸이 늘어남, 다리는 뒤로
//  - 활공: 날개를 쫙 펴고 살짝 흔들림, 몸은 앞으로 기울어짐
//  - 낙하: 날개를 위로 들고 허둥지둥, 빠를수록 심함
//  - 서 있음: 다리 가만히, 가끔 날개 들썩 / 걸음: 다리 번갈아 + 몸 뒤뚱
//  - 미끄러짐: 몸이 뒤로 젖혀지고 다리를 앞으로 버팀 / 구르기: 몸 전체가 데굴데굴
public class ClimbChickenAnimator : MonoBehaviour
{
    [SerializeField] float flapAngle = 70f;
    [SerializeField] float flapDownTime = 0.06f;
    [SerializeField] float flapUpTime = 0.14f;
    [SerializeField] float squash = 0.18f;
    [SerializeField] float chargeSquash = 0.32f;

    ClimbController ctrl;
    Transform model, bodyPivot, headPivot, wingL, wingR, legL, legR, tail, eyeL, eyeR;
    Vector3 bodyScale, headPos, eyeScale, modelPos;
    Quaternion bodyRest, wingLRest, wingRRest, legLRest, legRRest, tailRest, modelRest;

    float flapTimer = -1f;
    float walkPhase;
    float tilt, roll, panic, glide, crouch, slide;
    float tumbleAngle;
    float blinkTimer = 3f, blinkPhase = -1f;
    float landSquash;
    float fold = 1f;

    void Awake()
    {
        ctrl = GetComponent<ClimbController>();
        model = transform.Find("Model");
        bodyPivot = Find("BodyPivot");
        headPivot = Find("HeadPivot");
        wingL = Find("WingPivot_L");
        wingR = Find("WingPivot_R");
        legL = Find("LegPivot_L");
        legR = Find("LegPivot_R");
        tail = Find("TailPivot");
        eyeL = Find("Eye_L");
        eyeR = Find("Eye_R");

        if (model != null) { modelRest = model.localRotation; modelPos = model.localPosition; }
        if (bodyPivot != null) { bodyScale = bodyPivot.localScale; bodyRest = bodyPivot.localRotation; }
        if (headPivot != null) headPos = headPivot.localPosition;
        if (wingL != null) wingLRest = wingL.localRotation;
        if (wingR != null) wingRRest = wingR.localRotation;
        if (legL != null) legLRest = legL.localRotation;
        if (legR != null) legRRest = legR.localRotation;
        if (tail != null) tailRest = tail.localRotation;
        if (eyeL != null) eyeScale = eyeL.localScale;
    }

    void OnEnable()
    {
        if (ctrl == null) return;
        ctrl.Flapped += OnFlap;
        ctrl.Landed += OnLanded;
        ctrl.Launched += OnLaunched;
    }

    void OnDisable()
    {
        if (ctrl == null) return;
        ctrl.Flapped -= OnFlap;
        ctrl.Landed -= OnLanded;
        ctrl.Launched -= OnLaunched;
    }

    void OnFlap()
    {
        flapTimer = 0f;
    }

    void OnLaunched(float charge)
    {
        FeedbackFX.Flap(transform.position + Vector3.up * 0.4f, false);
        landSquash = 0f;
    }

    void OnLanded(float impact, float dropped)
    {
        landSquash = Mathf.Clamp01(impact / 30f) * 0.6f + 0.15f;
        if (impact > 12f) FeedbackFX.Bounce(transform.position, false);
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
        if (ctrl == null) return;
        float dt = Time.deltaTime;
        float time = Time.time;
        Vector3 v = ctrl.Velocity;
        float speed = new Vector2(v.x, v.z).magnitude;
        ClimbController.State st = ctrl.Current;
        bool grounded = st == ClimbController.State.Grounded || st == ClimbController.State.Charging;

        // 날갯짓 진행도(0 → 1 내려침 → 0)
        float flap = 0f;
        if (flapTimer >= 0f)
        {
            flapTimer += dt;
            flap = flapTimer < flapDownTime ? flapTimer / flapDownTime : Mathf.Pow(1f - Mathf.Clamp01((flapTimer - flapDownTime) / flapUpTime), 2f);
            if (flapTimer >= flapDownTime + flapUpTime) flapTimer = -1f;
        }

        crouch = Mathf.MoveTowards(crouch, st == ClimbController.State.Charging ? ctrl.Charge01 : 0f, dt * (st == ClimbController.State.Charging ? 4f : 14f));
        glide = Mathf.MoveTowards(glide, st == ClimbController.State.Gliding ? 1f : 0f, dt * 6f);
        panic = Mathf.MoveTowards(panic, !grounded && st != ClimbController.State.Gliding && v.y < -9f ? Mathf.Clamp01(-v.y / 25f) : 0f, dt * 4f);
        slide = Mathf.MoveTowards(slide, grounded && ctrl.IsSliding ? 1f : 0f, dt * 6f);
        landSquash = Mathf.MoveTowards(landSquash, 0f, dt * 3f);
        float walk = grounded && st != ClimbController.State.Charging ? Mathf.Clamp01(speed / 6f) : 0f;
        walkPhase += dt * Mathf.Lerp(0f, 9f, walk);

        // ── 구르기: 모델 전체를 진행 방향으로 데굴데굴 ──
        if (model != null)
        {
            if (st == ClimbController.State.Tumbling)
            {
                tumbleAngle += dt * Mathf.Max(360f, speed * 120f);
            }
            else
            {
                tumbleAngle = Mathf.MoveTowards(tumbleAngle % 360f, tumbleAngle % 360f > 180f ? 360f : 0f, dt * 900f);
            }
            float shiver = st == ClimbController.State.Charging ? Mathf.Sin(time * 70f) * 0.02f * (0.3f + crouch) : 0f;
            model.localRotation = Quaternion.AngleAxis(tumbleAngle, Vector3.right) * modelRest;
            model.localPosition = modelPos + new Vector3(shiver, 0f, 0f);
        }

        // ── 날개 ──
        float flutter = Mathf.Sin(time * 9f * Mathf.PI * 2f) * 8f;
        float fidget = grounded && walk < 0.1f && Mathf.Sin(time * Mathf.PI * 0.8f) > 0.985f ? -14f : 0f;
        float rest = grounded ? fidget : flutter;
        float flail = Mathf.Sin(time * 15f * Mathf.PI) * 22f;
        float wing = Mathf.Lerp(rest, -50f + flail, panic);
        wing = Mathf.Lerp(wing, -78f + Mathf.Sin(time * 5f) * 4f, glide);   // 활공: 쫙 폄
        wing = Mathf.Lerp(wing, 22f, crouch);                                  // 충전: 몸에 붙임
        wing = Mathf.Lerp(wing, flapAngle, flap);
        // 땅에서는 날개를 몸 옆으로 접어 붙임(뒤로 접힘), 날기 시작하면 펼침
        fold = Mathf.MoveTowards(fold, grounded && flapTimer < 0f ? 1f : 0f, dt * (grounded ? 5f : 12f));
        if (wingL != null) wingL.localRotation = wingLRest * Quaternion.AngleAxis(-78f * fold, Vector3.up) * Quaternion.AngleAxis(wing * (1f - fold * 0.7f), Vector3.right);
        if (wingR != null) wingR.localRotation = wingRRest * Quaternion.AngleAxis(78f * fold, Vector3.up) * Quaternion.AngleAxis(-wing * (1f - fold * 0.7f), Vector3.right);

        // ── 다리 ──
        float step = Mathf.Sin(walkPhase) * 32f * walk;
        float kick = -flap * 40f;
        float brace = slide * 35f;   // 미끄러질 때 앞으로 버팀
        float l = step + kick + brace + Mathf.Lerp(0f, 5f, panic) - crouch * 25f;
        float r = -step + kick + brace - Mathf.Lerp(0f, 5f, panic) - crouch * 25f;
        if (!grounded && glide < 0.5f) { l += Mathf.Sin(time * 14f) * 18f; r -= Mathf.Sin(time * 14f) * 18f; }
        if (legL != null) legL.localRotation = legLRest * Quaternion.AngleAxis(l, Vector3.forward);
        if (legR != null) legR.localRotation = legRRest * Quaternion.AngleAxis(r, Vector3.forward);

        // ── 몸: 웅크림 · 착지 납작 · 파닥임 늘어남 · 기울기 ──
        if (bodyPivot != null)
        {
            float flat = crouch * chargeSquash + landSquash * 0.35f + (flapTimer >= 0f && flapTimer < flapDownTime ? flap * squash : 0f);
            float stretch = flapTimer >= flapDownTime ? flap * squash * 0.6f : 0f;
            stretch += !grounded && v.y > 6f ? 0.08f : 0f;
            bodyPivot.localScale = new Vector3(
                bodyScale.x * (1f + flat * 0.55f - stretch * 0.3f),
                bodyScale.y * (1f - flat + stretch),
                bodyScale.z * (1f + flat * 0.55f - stretch * 0.3f));
            bodyPivot.localPosition = new Vector3(0f, -flat * 0.35f, 0f);

            float targetTilt = grounded ? -walk * 8f + slide * 18f : Mathf.Clamp(v.y * 0.9f, -25f, 20f) - glide * 12f;
            tilt = Mathf.Lerp(tilt, targetTilt, 1f - Mathf.Exp(-dt * 8f));
            float waddle = Mathf.Sin(walkPhase) * 7f * walk;
            roll = Mathf.Lerp(roll, waddle, 1f - Mathf.Exp(-dt * 12f));
            bodyPivot.localRotation = bodyRest * Quaternion.AngleAxis(tilt, Vector3.forward) * Quaternion.AngleAxis(roll, Vector3.right);
        }

        if (headPivot != null)
        {
            float bob = Mathf.Abs(Mathf.Sin(walkPhase)) * 0.08f * walk;
            headPivot.localPosition = headPos + new Vector3(0f, bob - crouch * 0.12f - flap * 0.05f, 0f);
        }
        if (tail != null)
        {
            tail.localRotation = tailRest * Quaternion.AngleAxis(Mathf.Sin(time * 5f) * 6f + flap * 14f + crouch * 20f, Vector3.forward);
        }

        Blink(dt, st == ClimbController.State.Tumbling || panic > 0.6f);
    }

    void Blink(float dt, bool squeeze)
    {
        if (eyeL == null || eyeR == null) return;
        if (squeeze)
        {
            // 구르거나 크게 떨어질 때: 눈을 질끈
            Vector3 s = new Vector3(eyeScale.x, eyeScale.y * 0.25f, eyeScale.z);
            eyeL.localScale = eyeR.localScale = s;
            return;
        }
        if (blinkPhase < 0f)
        {
            eyeL.localScale = eyeR.localScale = eyeScale;
            blinkTimer -= dt;
            if (blinkTimer <= 0f) blinkPhase = 0f;
            return;
        }
        blinkPhase += dt;
        float k = Mathf.Abs(Mathf.Cos(Mathf.Clamp01(blinkPhase / 0.14f) * Mathf.PI));
        Vector3 sc = new Vector3(eyeScale.x, eyeScale.y * Mathf.Max(0.1f, k), eyeScale.z);
        eyeL.localScale = eyeR.localScale = sc;
        if (blinkPhase >= 0.14f)
        {
            blinkPhase = -1f;
            blinkTimer = Random.Range(2.2f, 5f);
        }
    }
}
