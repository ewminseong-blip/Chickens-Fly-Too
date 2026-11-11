using System.Collections;
using UnityEngine;

// 번개(전기): 노란색 한 가지. 규칙은 하나 — "밝게 켜져 있으면 닿음(죽음), 꺼져 있으면 안 닿음".
//  - flicker 끔: 늘 켜져 있고 밝기만 지글지글 흔들림(항상 위험)
//  - flicker 켬: 꺼짐(어두운 가는 흔적, 충돌 없음) → 번쩍번쩍(짧게 켜졌다 꺼짐, 켜진 순간은 위험) → 켜짐(위험) 반복
// 꺼졌을 때도 가는 흔적은 남겨 '여기서 전기가 친다'는 자리는 보여 줍니다.
public class FlickerHazard : MonoBehaviour
{
    [SerializeField] bool flicker = false;
    [Tooltip("켜져 있는 시간(초, 최소 ~ 최대)")]
    [SerializeField] Vector2 onTime = new Vector2(0.7f, 1.3f);
    [Tooltip("꺼져 있는 시간(초, 최소 ~ 최대)")]
    [SerializeField] Vector2 offTime = new Vector2(0.6f, 1.1f);
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

    public bool IsLit => lit;

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
        SetLit(true);
        if (flicker)
        {
            StartCoroutine(Loop());
        }
    }

    IEnumerator Loop()
    {
        // 줄마다 박자가 다르게 처음 한 번 랜덤하게 쉼
        yield return new WaitForSeconds(Random.Range(0f, onTime.y));
        while (true)
        {
            SetLit(false);
            yield return new WaitForSeconds(Random.Range(offTime.x, offTime.y));
            for (int i = 0; i < flashCount; i++)
            {
                SetLit(true);
                yield return new WaitForSeconds(flashTime);
                SetLit(false);
                yield return new WaitForSeconds(flashTime);
            }
            SetLit(true);
            yield return new WaitForSeconds(Random.Range(onTime.x, onTime.y));
        }
    }

    void Update()
    {
        if (!lit || shimmer <= 0f)
        {
            return;
        }
        // 지글지글: 켜져 있는 동안 밝기가 빠르게 흔들림
        float k = 1f - shimmer * Random.value;
        Apply(litColor, litGlow * k);
    }

    // 켜짐 = 보이고 닿음, 꺼짐 = 가는 흔적만 남고 안 닿음(보이는 상태와 판정이 언제나 같음)
    void SetLit(bool on)
    {
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
