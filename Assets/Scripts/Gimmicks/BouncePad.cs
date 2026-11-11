using UnityEngine;

// 점프대: 위쪽 트리거에 들어오면 세로 속도를 launchSpeed로 맞춥니다(월드 위쪽).
// 발판 모양은 자식(충돌 없음)으로 두고, 바닥에 닿기 전에 트리거에 들어오게 높이를 잡습니다.
public class BouncePad : PlayerTrigger
{
    [SerializeField] float launchSpeed = 18f;
    [SerializeField] Transform visual;

    float squash;
    Vector3 visualScale = Vector3.one;

    void Awake()
    {
        if (visual != null)
        {
            visualScale = visual.localScale;
        }
    }

    protected override void OnPlayerEnter(Mover mover)
    {
        mover.Launch(launchSpeed);
        FeedbackFX.Bounce(transform.position, false);
        squash = 1f;
    }

    void Update()
    {
        if (visual == null || squash <= 0f)
        {
            return;
        }

        squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 5f);
        float k = Mathf.Sin(squash * Mathf.PI);
        visual.localScale = new Vector3(
            visualScale.x * (1f + 0.25f * k),
            visualScale.y * (1f - 0.4f * k),
            visualScale.z * (1f + 0.25f * k));
    }
}
