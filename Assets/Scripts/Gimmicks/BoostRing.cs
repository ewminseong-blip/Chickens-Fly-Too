using UnityEngine;

// 부스트 링: 통과하면 잠깐 빨라집니다. 링 모양은 자식(충돌 없음)으로 두고, 이 오브젝트는 트리거입니다.
public class BoostRing : PlayerTrigger
{
    [SerializeField] float boostSpeed = 18f;     // m/s
    [SerializeField] float duration = 1.2f;      // 초
    [SerializeField] Transform visual;           // 통과하면 살짝 커졌다 돌아옴
    [SerializeField] float spinSpeed = 90f;      // 링이 도는 속도(°/s)

    float punch;

    protected override void OnPlayerEnter(Mover mover)
    {
        mover.Boost(boostSpeed, duration);
        FeedbackFX.Boost(transform.position, transform.right);
        punch = 1f;
    }

    void Update()
    {
        if (visual == null)
        {
            return;
        }

        visual.Rotate(Vector3.right, spinSpeed * Time.deltaTime, Space.Self);

        if (punch > 0f)
        {
            punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 4f);
            float s = 1f + 0.35f * punch;
            visual.localScale = Vector3.one * s;
        }
    }
}
