using UnityEngine;

// 번개(전기): 노란색 한 가지. 규칙은 하나 — "밝게 켜져 있으면 닿음(죽음), 꺼져 있으면 안 닿음".
//  - flicker 끔: 늘 켜져 있고 밝기만 지글지글 흔들림(항상 위험)
//  - flicker 켬: 고정 박자로 반복 — 꺼짐(offDuration초, 어두운 가는 흔적, 충돌 없음)
//                → 번쩍번쩍(flashCount번, 켜진 순간은 위험) → 켜짐(onDuration초, 위험)
//    꺼지는 시간은 번개마다 1초 또는 2초로 고정(랜덤 아님)이라, 박자를 보고 '지나가는 순간'을 노릴 수 있습니다.
// 박자는 씬 시작 시각 기준으로 계산하므로 몇 번을 다시 해도 같은 타이밍입니다.
public class FlickerHazard : MonoBehaviour
{
    [SerializeField] bool flicker = false;
    [Tooltip("꺼져 있는 시간(초, 고정). 이 동안 지나갈 수 있음")]
    [SerializeField] float offDuration = 1f;
    [Tooltip("켜져 있는 시간(초, 고정)")]
    [SerializeField] float onDuration = 1.2f;
    [Tooltip("박자 시작을 늦추는 시간(초, 고정). 번개마다 다르게 줘서 한꺼번에 꺼지지 않게")]
    [SerializeField] float startDelay = 0f;
    [Tooltip("켜지기 직전 번쩍이는 횟수")]
    [SerializeField] int flashCount = 2;
    [Tooltip("번쩍임 한 번의 길이(초)")]
    [SerializeField] float flashTime = 0.06f;
    [SerializeField] Color litColor = new Color(1f, 0.86f, 0.1f, 1f);
    [SerializeField] float litGlow = 3f;
    [Tooltip("꺼졌을 때 흔적 색(충돌 없음)")]
    [SerializeField] Color offColor = new Color(0.28f, 0.25f, 0.12f, 1f);
    [Tooltip("꺼졌을 때 굵기 비율")]
    [SerializeField] float offThickness = 0.25f;
    [Tooltip("켜져 있을 때 밝기 흔들림(0 ~ 1)")]
    [SerializeField] float shimmer = 0.35f;

    Collider[] colliders;
    Renderer[] renderers;
    Transform[] segments;
    Vector3[] fullScales;
    MaterialPropertyBlock block;
    bool lit;
    bool initialized;

    public bool IsLit => lit;
    public bool Flickers => flicker;
    public float OffDuration => offDuration;

    void Awake()
    {
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        segments = new Transform[renderers.Length];
        fullScales = new Vector3[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            segments[i] = renderers[i].transform;
            fullScales[i] = segments[i].localScale;
        }
        block = new MaterialPropertyBlock();
    }

    void Start()
    {
        SetLit(LitAt(Time.timeSinceLevelLoad), true);
    }

    // 한 주기: [꺼짐 offDuration] [번쩍임 flashCount × (켜짐 flashTime + 꺼짐 flashTime)] [켜짐 onDuration]
    bool LitAt(float time)
    {
        if (!flicker)
        {
            return true;
        }
        float flashes = flashCount * flashTime * 2f;
        float cycle = offDuration + flashes + onDuration;
        float t = Mathf.Repeat(time - startDelay, cycle);
        if (t < offDuration)
        {
            return false;
        }
        t -= offDuration;
        if (t < flashes)
        {
            return Mathf.Repeat(t, flashTime * 2f) < flashTime;   // 번쩍: 켜짐 → 꺼짐
        }
        return true;
    }

    void Update()
    {
        bool next = LitAt(Time.timeSinceLevelLoad);
        if (next != lit)
        {
            SetLit(next, false);
        }
        if (lit && shimmer > 0f)
        {
            // 지글지글: 켜져 있는 동안 밝기가 빠르게 흔들림
            Apply(litColor, litGlow * (1f - shimmer * Random.value));
        }
    }

    // 켜짐 = 보이고 닿음, 꺼짐 = 가는 흔적만 남고 안 닿음(보이는 상태와 판정이 언제나 같음)
    void SetLit(bool on, bool force)
    {
        if (!force && initialized && on == lit)
        {
            return;
        }
        initialized = true;
        lit = on;
        foreach (Collider c in colliders)
        {
            c.enabled = on;
        }
        for (int i = 0; i < segments.Length; i++)
        {
            Vector3 s = fullScales[i];
            // 조각의 z가 길이, x · y가 굵기
            segments[i].localScale = on ? s : new Vector3(s.x * offThickness, s.y * offThickness, s.z);
        }
        if (on) Apply(litColor, litGlow);
        else Apply(offColor, 0f);
    }

    void Apply(Color c, float glow)
    {
        block.SetColor("_BaseColor", c);
        block.SetColor("_EmissionColor", c * glow);
        foreach (Renderer r in renderers)
        {
            r.SetPropertyBlock(block);
        }
    }
}
