using System;
using UnityEngine;
using UnityEngine.Rendering;

// 하나로 이어진 맵에서 '점점 높아지는 느낌'을 만드는 분위기 전환기입니다.
// 닭의 앞쪽 위치(x)에 따라 구간(zone)마다 정해 둔 하늘색 · 안개 · 주변광 · 햇빛 · 후처리를
// 구간 경계 앞 blend(m) 동안 서서히 섞습니다. (시즌1 하늘1 → 하늘5처럼 위로 갈수록 짙어짐)
public class ZoneAtmosphere : MonoBehaviour
{
    [Serializable]
    public class Zone
    {
        public string name;
        public float startX;
        [Header("하늘")]
        public Color skyTint = Color.white;
        public Color skyGround = Color.gray;
        public float exposure = 1.2f;
        public float atmosphereThickness = 1f;
        [Header("안개 · 주변광")]
        public Color fog = Color.white;
        public float fogStart = 60f;
        public float fogEnd = 260f;
        public Color ambientSky = Color.white;
        public Color ambientEquator = Color.gray;
        public Color ambientGround = Color.gray;
        [Header("햇빛")]
        public Color light = Color.white;
        public float lightIntensity = 1.2f;
        [Header("후처리")]
        public Volume volume;
    }

    [SerializeField] Zone[] zones = new Zone[0];
    [Tooltip("다음 구간이 시작되기 전 몇 m 동안 섞을지")]
    [SerializeField] float blend = 60f;
    [SerializeField] Light sun;
    [SerializeField] Material skyboxTemplate;
    [Tooltip("켜면 길 거리 대신 닭의 높이(y)로 구간을 판단(Can Chicken Fly? 수직 등반)")]
    [SerializeField] bool useHeight = false;

    Material sky;
    Transform player;
    CoursePath path;
    int hint = -1;

    void Start()
    {
        Mover mover = FindAnyObjectByType<Mover>();
        player = mover != null ? mover.transform : null;
        if (player == null)
        {
            ClimbController climber = FindAnyObjectByType<ClimbController>();
            if (climber != null) player = climber.transform;
        }
        path = useHeight ? null : CoursePath.Get();

        if (skyboxTemplate != null)
        {
            sky = new Material(skyboxTemplate);
            RenderSettings.skybox = sky;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        Apply();
    }

    void Update()
    {
        Apply();
    }

    void Apply()
    {
        if (zones == null || zones.Length == 0 || player == null)
        {
            return;
        }

        // 꺾이는 길: 앞쪽 X 대신 '길을 따라 온 거리'로 구간을 판단
        float x = useHeight ? player.position.y : (path != null ? path.Progress(player.position, ref hint) : player.position.x);
        int i = 0;
        while (i + 1 < zones.Length && x >= zones[i + 1].startX)
        {
            i++;
        }

        Zone a = zones[i];
        Zone b = i + 1 < zones.Length ? zones[i + 1] : a;
        float t = b == a ? 0f : Mathf.Clamp01((x - (b.startX - blend)) / blend);
        t = Mathf.SmoothStep(0f, 1f, t);

        RenderSettings.fogColor = Color.Lerp(a.fog, b.fog, t);
        RenderSettings.fogStartDistance = Mathf.Lerp(a.fogStart, b.fogStart, t);
        RenderSettings.fogEndDistance = Mathf.Lerp(a.fogEnd, b.fogEnd, t);
        RenderSettings.ambientSkyColor = Color.Lerp(a.ambientSky, b.ambientSky, t);
        RenderSettings.ambientEquatorColor = Color.Lerp(a.ambientEquator, b.ambientEquator, t);
        RenderSettings.ambientGroundColor = Color.Lerp(a.ambientGround, b.ambientGround, t);

        if (sun != null)
        {
            sun.color = Color.Lerp(a.light, b.light, t);
            sun.intensity = Mathf.Lerp(a.lightIntensity, b.lightIntensity, t);
        }

        if (sky != null)
        {
            sky.SetColor("_SkyTint", Color.Lerp(a.skyTint, b.skyTint, t));
            sky.SetColor("_GroundColor", Color.Lerp(a.skyGround, b.skyGround, t));
            sky.SetFloat("_Exposure", Mathf.Lerp(a.exposure, b.exposure, t));
            sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(a.atmosphereThickness, b.atmosphereThickness, t));
        }

        for (int k = 0; k < zones.Length; k++)
        {
            if (zones[k].volume == null) continue;
            zones[k].volume.weight = k == i ? 1f - t : (k == i + 1 ? t : 0f);
        }
    }
}
