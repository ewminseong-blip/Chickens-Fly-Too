using UnityEngine;

// 시작 위치에서 offset만큼 왕복합니다(사인 곡선이라 끝에서 느려짐).
// 움직이는 기둥 · 오르내리는 막대 · 닫혔다 열리는 벽에 씁니다.
public class Oscillator : MonoBehaviour
{
    [SerializeField] Vector3 offset = new Vector3(0f, 0f, 8f);
    [SerializeField] float period = 3f;          // 한 번 왕복하는 시간(초)
    [Range(0f, 1f)]
    [SerializeField] float phase = 0f;           // 0~1, 같은 줄의 기둥끼리 엇갈리게

    Vector3 startPosition;

    void Awake()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        float t = Time.timeSinceLevelLoad / Mathf.Max(0.1f, period) + phase;
        // 0 → 1 → 0 으로 부드럽게 왕복
        float k = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f);
        transform.localPosition = startPosition + offset * k;
    }
}
