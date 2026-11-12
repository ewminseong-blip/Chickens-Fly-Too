using UnityEngine;

// 바람 · 낮은 중력 구역(트리거).
//  - acceleration: 안에 있는 동안 받는 가속(m/s²). 상승풍은 +y, 옆바람은 수평.
//  - gravityScale: 1이 아니면 안에 있는 동안 중력 배율(우주 낮은 중력 0.55 등)
public class ClimbZone : MonoBehaviour
{
    [SerializeField] Vector3 acceleration = Vector3.zero;
    [SerializeField] float gravityScale = 1f;
    [Tooltip("바람 세기가 period초마다 0 ~ 1로 오르내림(0이면 늘 같은 세기)")]
    [SerializeField] float gustPeriod = 0f;

    public void Configure(Vector3 accel, float gravity, float gust)
    {
        acceleration = accel;
        gravityScale = gravity;
        gustPeriod = gust;
    }

    void OnTriggerStay(Collider other)
    {
        ClimbController p = other.GetComponentInParent<ClimbController>();
        if (p == null) return;
        float k = gustPeriod > 0.01f ? 0.5f + 0.5f * Mathf.Sin(Time.timeSinceLevelLoad / gustPeriod * Mathf.PI * 2f) : 1f;
        if (acceleration.sqrMagnitude > 0f) p.AddAcceleration(acceleration * k);
        if (!Mathf.Approximately(gravityScale, 1f)) p.SetGravityScale(gravityScale);
    }

    void OnTriggerExit(Collider other)
    {
        ClimbController p = other.GetComponentInParent<ClimbController>();
        if (p != null && !Mathf.Approximately(gravityScale, 1f)) p.SetGravityScale(1f);
    }
}
