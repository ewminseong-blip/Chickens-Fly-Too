using UnityEngine;

// 움직이는 · 도는 발판. 키네마틱 Rigidbody를 물리 프레임마다 옮기고, 위에 선 닭이 함께 움직이도록 속도를 알려 줍니다.
//  - move: 처음 위치에서 이 거리만큼 왕복(사인 곡선), period초에 한 번
//  - spin: 초당 회전(°, 월드 위 축 기준이면 y)
// 박자는 씬 시작 시각 기준이라 다시 해도 같은 타이밍입니다.
[RequireComponent(typeof(Rigidbody))]
public class ClimbMovingPlatform : MonoBehaviour
{
    [SerializeField] Vector3 move = Vector3.zero;
    [SerializeField] float period = 4f;
    [SerializeField] float phase = 0f;
    [SerializeField] Vector3 spin = Vector3.zero;

    Rigidbody body;
    Vector3 origin;
    Quaternion startRot;
    Vector3 velocity;
    Vector3 angularVelocity;   // rad/s

    public void Configure(Vector3 moveBy, float periodSeconds, float phase01, Vector3 spinDegPerSec)
    {
        move = moveBy;
        period = periodSeconds;
        phase = phase01;
        spin = spinDegPerSec;
    }

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        origin = transform.position;
        startRot = transform.rotation;
    }

    // 자동 점검(물리를 직접 돌릴 때)용 시간. 비어 있으면 실제 시간.
    public static float? TimeOverride;

    public void Tick()
    {
        FixedUpdate();
    }

    void FixedUpdate()
    {
        float t = TimeOverride ?? Time.timeSinceLevelLoad;
        float dt = Time.fixedDeltaTime;
        Vector3 next = origin;
        if (move.sqrMagnitude > 0f && period > 0.01f)
        {
            next = origin + move * Mathf.Sin((t / period + phase) * Mathf.PI * 2f);
        }
        velocity = (next - body.position) / dt;
        body.MovePosition(next);

        if (spin.sqrMagnitude > 0f)
        {
            Quaternion rot = startRot * Quaternion.Euler(spin * (TimeOverride ?? Time.timeSinceLevelLoad));
            angularVelocity = spin * Mathf.Deg2Rad;
            body.MoveRotation(rot);
        }
    }

    // 발판 위 한 점이 움직이는 속도(이동 + 회전)
    public Vector3 PointVelocity(Vector3 worldPoint)
    {
        Vector3 w = startRot * angularVelocity;
        return velocity + Vector3.Cross(w, worldPoint - body.position);
    }
}
