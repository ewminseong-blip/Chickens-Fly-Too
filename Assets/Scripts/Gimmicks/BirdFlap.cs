using UnityEngine;

// 다른 새(독수리 · 갈매기 · 우주 닭)의 날갯짓: 자식 "WingPivot_L" / "WingPivot_R"을 위아래로 흔듭니다.
public class BirdFlap : MonoBehaviour
{
    [SerializeField] float flapsPerSecond = 3f;
    [SerializeField] float angle = 40f;

    Transform left;
    Transform right;
    Quaternion leftRest;
    Quaternion rightRest;
    float offset;

    void Awake()
    {
        left = transform.Find("WingPivot_L");
        right = transform.Find("WingPivot_R");
        if (left != null) leftRest = left.localRotation;
        if (right != null) rightRest = right.localRotation;
        offset = Random.Range(0f, 10f);   // 새마다 박자를 다르게
    }

    void Update()
    {
        float a = Mathf.Sin((Time.time + offset) * flapsPerSecond * Mathf.PI * 2f) * angle;
        if (left != null) left.localRotation = leftRest * Quaternion.Euler(0f, 0f, a);
        if (right != null) right.localRotation = rightRest * Quaternion.Euler(0f, 0f, -a);
    }
}
