using UnityEngine;

// 진자: 피벗(이 오브젝트)을 중심으로 좌우로 흔들립니다. 추는 자식 오브젝트로 둡니다.
public class Pendulum : MonoBehaviour
{
    [SerializeField] Vector3 axis = Vector3.right;   // 흔들리는 축(로컬)
    [SerializeField] float amplitude = 60f;          // 최대 각도
    [SerializeField] float period = 2.5f;            // 한 번 왕복 시간(초)
    [Range(0f, 1f)]
    [SerializeField] float phase = 0f;

    Quaternion startRotation;

    void Awake()
    {
        startRotation = transform.localRotation;
    }

    void Update()
    {
        float t = Time.timeSinceLevelLoad / Mathf.Max(0.1f, period) + phase;
        float angle = amplitude * Mathf.Sin(t * Mathf.PI * 2f);
        transform.localRotation = startRotation * Quaternion.AngleAxis(angle, axis);
    }
}
