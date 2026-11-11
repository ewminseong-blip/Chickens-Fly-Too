using UnityEngine;

// 점멸 위험물(번개 · 레이저): 꺼짐 → 경고 → 켜짐을 반복합니다.
//  - 꺼짐: 희미하게만 보이고 닿아도 안전
//  - 경고: 노랗게 깜빡임(곧 켜짐), 아직 안전
//  - 켜짐: 밝은 빨강/하양, 닿으면 죽음
// syncSpeed를 쓰면 가장 최근에 지난 HazardSyncGate 기준으로 박자를 맞춥니다.
// → 그 속도(최고 속도)로 직진하는 플레이어는 지나가는 순간마다 꺼져 있는 '초록 신호' 길이 생깁니다.
//    화면에서는 정신없이 깜빡이지만, W를 꾹 누르고 똑바로 가면 다 피해집니다.
public class TimedHazard : MonoBehaviour
{
    [SerializeField] float offTime = 1.2f;
    [SerializeField] float warnTime = 0.5f;
    [SerializeField] float onTime = 0.8f;
    [SerializeField] float phaseOffset = 0f;      // 초
    [SerializeField] float syncSpeed = 0f;        // m/s, 0이면 사용 안 함

    [SerializeField] Color offColor = new Color(0.4f, 0.4f, 0.6f, 1f);
    [SerializeField] Color warnColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] Color onColor = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] float offScale = 0.25f;      // 꺼졌을 때 굵기 비율(가늘게 보임)

    Collider[] colliders;
    Renderer[] renderers;
    MaterialPropertyBlock block;
    Vector3 fullScale;

    enum State { Off, Warn, On }
    State current = (State)(-1);

    void Awake()
    {
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        fullScale = transform.localScale;
    }

    float CurrentPhaseOffset()
    {
        if (syncSpeed <= 0f || !HazardSync.HasGate || transform.position.x < HazardSync.OriginX)
        {
            return phaseOffset;
        }

        // 게이트를 지난 시각 + (내 위치 - 게이트 위치) / 속도 = 플레이어가 나에게 도착하는 시각.
        // 그 순간이 '꺼짐 구간 한가운데'가 되도록 박자를 맞춥니다.
        float arrival = HazardSync.StartTime + (transform.position.x - HazardSync.OriginX) / syncSpeed;
        return arrival - offTime * 0.5f;
    }

    void Update()
    {
        float cycle = offTime + warnTime + onTime;
        float t = Mathf.Repeat(Time.timeSinceLevelLoad - CurrentPhaseOffset(), cycle);

        State next = t < offTime ? State.Off : (t < offTime + warnTime ? State.Warn : State.On);

        if (next == State.Warn)
        {
            // 경고 중에는 빠르게 깜빡여 '곧 켜진다'를 알립니다.
            bool blink = Mathf.Repeat(t * 10f, 1f) < 0.5f;
            SetColor(blink ? warnColor : offColor);
        }

        if (next == current)
        {
            return;
        }

        current = next;

        bool deadly = current == State.On;
        foreach (Collider c in colliders)
        {
            c.enabled = deadly;
        }

        Vector3 scale = fullScale;
        if (!deadly)
        {
            // 굵기(가로 두 축)만 가늘게 해서 '꺼짐'을 보여 줍니다. 가장 긴 축은 유지.
            scale = ThinScale(fullScale, offScale);
        }
        transform.localScale = scale;

        if (current == State.Off)
        {
            SetColor(offColor);
        }
        else if (current == State.On)
        {
            SetColor(onColor);
        }
    }

    static Vector3 ThinScale(Vector3 s, float k)
    {
        float max = Mathf.Max(s.x, Mathf.Max(s.y, s.z));
        return new Vector3(
            Mathf.Approximately(s.x, max) ? s.x : s.x * k,
            Mathf.Approximately(s.y, max) ? s.y : s.y * k,
            Mathf.Approximately(s.z, max) ? s.z : s.z * k);
    }

    void SetColor(Color c)
    {
        block.SetColor("_BaseColor", c);
        block.SetColor("_EmissionColor", c * 2.5f);
        foreach (Renderer r in renderers)
        {
            r.SetPropertyBlock(block);
        }
    }
}
