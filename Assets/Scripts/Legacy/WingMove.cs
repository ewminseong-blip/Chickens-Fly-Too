using UnityEngine;

// 날갯짓 모션: Space를 누를 때마다 날개를 한 번 '내려쳤다가' 돌아옵니다(시즌1의 올림/내림 토글을 바꿈).
// 발도 함께 움츠렸다 폅니다. 깃털 이펙트도 여기서 냅니다.
public class WingMove : MonoBehaviour
{
    [SerializeField] float flapAngle = 35f;      // 내려칠 때 날개 각도
    [SerializeField] float downTime = 0.06f;     // 내려치는 시간(초)
    [SerializeField] float upTime = 0.18f;       // 돌아오는 시간(초)

    Transform wing1;
    Transform wing2;
    Transform leftFoot;
    Transform rightFoot;

    Quaternion wing1Rest;
    Quaternion wing2Rest;
    Quaternion leftFootRest;
    Quaternion rightFootRest;

    Mover mover;
    float flapTimer = -1f;

    void Awake()
    {
        wing1 = FindDeepChild(transform, "wing_1");
        wing2 = FindDeepChild(transform, "wing_2");
        leftFoot = FindDeepChild(transform, "LeftFoot");
        rightFoot = FindDeepChild(transform, "RightFoot");

        if (wing1 != null) wing1Rest = wing1.localRotation;
        if (wing2 != null) wing2Rest = wing2.localRotation;
        if (leftFoot != null) leftFootRest = leftFoot.localRotation;
        if (rightFoot != null) rightFootRest = rightFoot.localRotation;

        mover = GetComponent<Mover>();
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

    void Update()
    {
        if (flapTimer < 0f)
        {
            return;
        }

        flapTimer += Time.deltaTime;

        // 0 → 1(내려침) → 0(복귀). 내려칠 때는 빠르게, 돌아올 때는 부드럽게(ease-out).
        float t;
        if (flapTimer < downTime)
        {
            t = flapTimer / downTime;
        }
        else
        {
            float u = Mathf.Clamp01((flapTimer - downTime) / upTime);
            t = 1f - (1f - (1f - u) * (1f - u));
        }

        Apply(t);

        if (flapTimer >= downTime + upTime)
        {
            flapTimer = -1f;
            Apply(0f);
        }
    }

    void Apply(float t)
    {
        if (wing1 != null) wing1.localRotation = wing1Rest * Quaternion.Euler(0f, -flapAngle * t, 0f);
        if (wing2 != null) wing2.localRotation = wing2Rest * Quaternion.Euler(0f, flapAngle * t, 0f);
        if (leftFoot != null) leftFoot.localRotation = leftFootRest * Quaternion.Euler(-30f * t, 0f, 0f);
        if (rightFoot != null) rightFoot.localRotation = rightFootRest * Quaternion.Euler(30f * t, 0f, 0f);
    }

    static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }

            Transform found = FindDeepChild(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
