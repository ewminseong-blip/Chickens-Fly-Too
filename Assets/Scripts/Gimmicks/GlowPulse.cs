using UnityEngine;

// 빠져나갈 구멍 표시(HoleMarker)가 숨 쉬듯 밝아졌다 어두워지며 살짝 커졌다 작아집니다. 움직이는 빛이 눈에 먼저 들어옴.
public class GlowPulse : MonoBehaviour
{
    [SerializeField] Light pulseLight;
    [Tooltip("한 번 숨 쉬는 시간(초)")]
    [SerializeField] float period = 1.1f;
    [Tooltip("빛 세기 변화(0.5면 기본의 50% ~ 150%)")]
    [SerializeField] float lightSwing = 0.5f;
    [Tooltip("크기 변화(0.06이면 ±6%)")]
    [SerializeField] float scaleSwing = 0.06f;

    float baseIntensity;
    Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
        if (pulseLight != null) baseIntensity = pulseLight.intensity;
    }

    void Update()
    {
        float k = Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.1f, period));
        transform.localScale = baseScale * (1f + scaleSwing * k);
        if (pulseLight != null) pulseLight.intensity = baseIntensity * (1f + lightSwing * k);
    }
}
