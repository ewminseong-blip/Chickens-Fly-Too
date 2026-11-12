using UnityEngine;

// 온리업식 3인칭 카메라: 마우스로 닭 주위를 돌고(바라보는 방향만), WASD는 이 카메라 기준.
//  - 마우스 휠: 거리, 벽 · 발판에 가리면 앞으로 당겨짐
//  - 빨리 떨어질수록 시야가 넓어지고, 세게 부딪히면 화면이 흔들림
public class ClimbCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float distance = 8f;
    [SerializeField] Vector2 distanceRange = new Vector2(3.5f, 16f);
    [Tooltip("닭 발에서 카메라가 바라보는 높이(m)")]
    [SerializeField] float pivotHeight = 1.4f;
    [SerializeField] float mouseSensitivity = 2.2f;
    [SerializeField] Vector2 pitchRange = new Vector2(-35f, 80f);
    [SerializeField] float pitch = 18f;
    [SerializeField] float followSmooth = 0.06f;
    [SerializeField] LayerMask obstructionMask = ~0;
    [Header("속도감")]
    [SerializeField] float baseFov = 62f;
    [SerializeField] float fallFovBoost = 14f;
    [SerializeField] float fallSpeedForFullFov = 35f;

    float yaw;
    float currentDistance;
    Vector3 pivotVelocity;
    Vector3 pivot;
    float shake;
    Camera cam;
    ClimbController player;

    public float Yaw => yaw;

    public void SetTarget(Transform t)
    {
        target = t;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        if (target == null)
        {
            ClimbController c = FindAnyObjectByType<ClimbController>();
            if (c != null) target = c.transform;
        }
        if (target != null)
        {
            player = target.GetComponent<ClimbController>();
            if (!snapped)
            {
                yaw = target.eulerAngles.y;
                pivot = target.position + Vector3.up * pivotHeight;
            }
            if (player != null)
            {
                player.Bounced += s => Shake(Mathf.InverseLerp(10f, 40f, s));
                player.Landed += (s, d) => Shake(Mathf.InverseLerp(17f, 40f, s) * 0.7f);
                player.KnockedBack += () => Shake(0.35f);
            }
        }
        currentDistance = distance;
        LockCursor(true);
    }

    public void Shake(float amount)
    {
        shake = Mathf.Max(shake, Mathf.Clamp01(amount));
    }

    public static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    bool snapped;

    public void SnapBehind()
    {
        if (target == null) return;
        yaw = target.eulerAngles.y;
        pivot = target.position + Vector3.up * pivotHeight;
        snapped = true;
    }

    // 자동 점검 · 스크린샷용: 카메라 각도를 직접 정함
    public void SetView(float newYaw, float newPitch, float newDistance)
    {
        yaw = newYaw;
        pitch = newPitch;
        distance = newDistance;
        snapped = true;
    }

    void LateUpdate()
    {
        if (target == null) return;
        float dt = Time.unscaledDeltaTime;

        if (Time.timeScale > 0f && Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, pitchRange.x, pitchRange.y);
            distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * 1.2f, distanceRange.x, distanceRange.y);
        }

        Vector3 want = target.position + Vector3.up * pivotHeight;
        pivot = Vector3.SmoothDamp(pivot, want, ref pivotVelocity, followSmooth, Mathf.Infinity, dt);
        // 아주 빨리 떨어질 때 카메라가 뒤처지지 않게
        if ((pivot - want).sqrMagnitude > 9f) pivot = want + (pivot - want).normalized * 3f;

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 back = rot * Vector3.back;

        // 가리는 물체가 있으면 앞으로 당김(닭은 Ignore Raycast 층이라 안 걸림, 트리거 무시)
        float d = distance;
        int mask = obstructionMask & ~(1 << 2);
        if (Physics.SphereCast(pivot, 0.3f, back, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
        {
            d = Mathf.Max(0.8f, hit.distance - 0.1f);
        }
        currentDistance = d < currentDistance ? d : Mathf.MoveTowards(currentDistance, d, dt * 10f);

        Vector3 pos = pivot + back * currentDistance;
        if (shake > 0f)
        {
            float s = shake * shake * 0.35f;
            pos += new Vector3(Mathf.PerlinNoise(Time.unscaledTime * 31f, 0f) - 0.5f, Mathf.PerlinNoise(0f, Time.unscaledTime * 29f) - 0.5f, 0f) * s * 2f;
            shake = Mathf.MoveTowards(shake, 0f, dt * 2.5f);
        }
        transform.SetPositionAndRotation(pos, rot);

        if (cam != null && player != null)
        {
            float fall = Mathf.Max(0f, -player.Velocity.y);
            float k = Mathf.Clamp01(Mathf.InverseLerp(12f, fallSpeedForFullFov, fall));
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, baseFov + fallFovBoost * k, 1f - Mathf.Exp(-dt * 5f));
        }
    }
}
