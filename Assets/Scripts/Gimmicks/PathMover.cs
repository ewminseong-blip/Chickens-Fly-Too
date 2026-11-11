using UnityEngine;

// 한 방향으로 가로질러 가는 자연물: 새 떼 · 굴러오는 바위 · 흘러가는 구름 · 혜성 · 소행성.
// 시작 위치에서 travel만큼 speed로 이동 → 끝에서 사라짐 → interval초 쉬고 → 다시 처음부터.
// 나타날 때 · 사라질 때 크기가 부드럽게 커졌다 작아지고, 작을 때는 충돌이 꺼집니다.
public class PathMover : MonoBehaviour
{
    [SerializeField] Vector3 travel = new Vector3(0f, 0f, 30f);   // 이동 거리 · 방향(월드)
    [SerializeField] float speed = 8f;                           // m/s
    [SerializeField] float interval = 1.5f;                      // 끝에 닿은 뒤 다시 나오기까지(초)
    [SerializeField] float startDelay = 0f;                      // 처음 나오기까지(초)
    [Tooltip("0보다 크면 이 반지름으로 굴러가듯 회전(바위)")]
    [SerializeField] float rollRadius = 0f;
    [Tooltip("이동 방향을 바라봄(새 · 혜성)")]
    [SerializeField] bool faceDirection = false;
    [Tooltip("양 끝에서 커지고 작아지는 구간 비율")]
    [Range(0.01f, 0.4f)] [SerializeField] float fade = 0.08f;

    Vector3 startPosition;
    Vector3 baseScale;
    Quaternion baseRotation;
    Collider[] colliders;
    bool collidersOn = true;

    void Awake()
    {
        startPosition = transform.position;
        baseScale = transform.localScale;
        baseRotation = transform.rotation;
        colliders = GetComponentsInChildren<Collider>();

        if (faceDirection && travel.sqrMagnitude > 0.001f)
        {
            baseRotation = Quaternion.LookRotation(travel.normalized, Vector3.up);
            transform.rotation = baseRotation;
        }
    }

    void Update()
    {
        float distance = travel.magnitude;
        float travelTime = distance / Mathf.Max(0.1f, speed);
        float cycle = travelTime + Mathf.Max(0f, interval);
        float local = Time.timeSinceLevelLoad - startDelay;

        float k;
        bool moving;
        if (local < 0f)
        {
            k = 0f;
            moving = false;
        }
        else
        {
            float c = Mathf.Repeat(local, cycle);
            moving = c < travelTime;
            k = moving ? c / travelTime : 1f;
        }

        transform.position = startPosition + travel * k;

        float s = moving ? Mathf.Clamp01(Mathf.Min(k, 1f - k) / fade) : 0f;
        transform.localScale = baseScale * Mathf.SmoothStep(0f, 1f, s);
        SetColliders(s > 0.6f);

        if (rollRadius > 0f && distance > 0.001f)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, travel.normalized);
            float angle = distance * k / rollRadius * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, axis) * baseRotation;
        }
    }

    void SetColliders(bool on)
    {
        if (on == collidersOn)
        {
            return;
        }

        collidersOn = on;
        foreach (Collider c in colliders)
        {
            if (c != null) c.enabled = on;
        }
    }
}
