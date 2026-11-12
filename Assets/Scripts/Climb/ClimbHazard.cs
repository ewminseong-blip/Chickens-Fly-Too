using UnityEngine;

// 장애물(새 떼 · 번개 · 우박 · 혜성 등): 닿아도 죽지 않고 밀려남. 날갯짓 · 활공이 끊겨 떨어지기 시작합니다.
// 충돌(일반 콜라이더) 또는 트리거 둘 다 됩니다. 번개처럼 켜졌다 꺼지는 것은 FlickerHazard가 콜라이더를 켜고 끕니다.
public class ClimbHazard : MonoBehaviour
{
    public enum Kind { Push, Shock }

    [SerializeField] Kind kind = Kind.Push;
    [Tooltip("밀려나는 수평 속도(m/s)")]
    [SerializeField] float push = 9f;
    [Tooltip("밀려날 때 위아래 속도(m/s). 음수면 아래로 내리꽂음")]
    [SerializeField] float lift = 2f;
    [Tooltip("조작이 안 되는 시간(초)")]
    [SerializeField] float stun = 0.35f;
    [Tooltip("이 방향으로 밀어냄(비우면 장애물 중심에서 닭 쪽으로)")]
    [SerializeField] Vector3 fixedDirection = Vector3.zero;
    [Tooltip("같은 닭을 다시 밀기까지 기다리는 시간(초)")]
    [SerializeField] float cooldown = 0.5f;

    float lastHit = -10f;

    public void Configure(Kind k, float pushSpeed, float liftSpeed, float stunTime, Vector3 dir)
    {
        kind = k;
        push = pushSpeed;
        lift = liftSpeed;
        stun = stunTime;
        fixedDirection = dir;
    }

    public void Hit(ClimbController player, Vector3 point)
    {
        if (Time.time - lastHit < cooldown) return;
        lastHit = Time.time;

        Vector3 dir = fixedDirection.sqrMagnitude > 0.0001f ? fixedDirection : player.transform.position - point;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = -player.transform.forward;
        dir.Normalize();

        player.Knockback(dir * push + Vector3.up * lift, stun);

        Vector3 at = player.transform.position + Vector3.up * 0.9f;
        if (kind == Kind.Shock)
        {
            FeedbackFX.Death(at);   // 깃털 + 불꽃(감전)
        }
        else
        {
            FeedbackFX.Flap(at, false);
        }
        HazardFlash.Flash(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        ClimbController p = other.GetComponentInParent<ClimbController>();
        if (p != null) Hit(p, other.ClosestPoint(transform.position));
    }
}
