using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 천장(위 경계) 경고: 천장은 닿으면 죽지만 하늘이 열려 있어 눈에 안 보이므로,
// warnDistance(m) 안으로 가까워지면 화면의 '천장 쪽' 가장자리가 붉게 물들고 "TOO HIGH!"가 뜹니다.
// 가까울수록 진해지고, 중력 반전으로 화면이 뒤집혀 있으면 경고는 화면 아래쪽에 나옵니다.
public class CeilingWarning : MonoBehaviour
{
    [SerializeField] float warnDistance = 6f;
    [SerializeField] Color warnColor = new Color(1f, 0.2f, 0.15f, 0.55f);
    [SerializeField] string message = "TOO HIGH!";
    [Tooltip("천장에 닿아 죽었을 때 뜨는 글")]
    [SerializeField] string deathMessage = "HIT THE CEILING!";
    [Tooltip("앞으로 몇 초 동안 갈 거리의 천장까지 미리 보고 경고할지")]
    [SerializeField] float lookAheadTime = 0.6f;
    bool dead;

    Mover mover;
    CoursePath path;
    int hint = -1;
    RectTransform band;
    Image bandImage;
    TextMeshProUGUI label;
    float shown;

    void Start()
    {
        mover = FindAnyObjectByType<Mover>();
        path = CoursePath.Get();
        BuildOverlay();
        GameOverTrigger.Died += OnDied;
    }

    void OnDestroy()
    {
        GameOverTrigger.Died -= OnDied;
    }

    // 무엇에 죽었는지 모르는 일이 없게: 천장(Boundary_Top)에 닿아 죽으면 경고를 꽉 채우고 이유를 띄움
    void OnDied(GameObject cause)
    {
        if (cause != null && cause.name == "Boundary_Top" && label != null)
        {
            dead = true;
            label.text = deathMessage;
        }
    }

    void Update()
    {
        if (mover == null || path == null || band == null)
        {
            return;
        }

        Vector3 pos = mover.transform.position;
        float s = path.Progress(pos, ref hint);

        // 지금 자리만 보면 천장이 내려오는 곳(구간이 바뀌며 낮아짐)에서 경고가 늦으므로,
        // 앞으로 lookAheadTime초 동안 갈 거리까지 살펴 가장 가까운 천장으로 경고합니다.
        float ahead = Mathf.Max(0f, Vector3.Dot(mover.Velocity, path.Forward(s))) * lookAheadTime + 2f;
        float gap = float.MaxValue;
        for (float d = 0f; d <= ahead; d += 2f)
        {
            float si = Mathf.Clamp(s + d, 0f, path.Length);
            float floorY = path.Center(si).y;
            gap = Mathf.Min(gap, floorY + path.Height(si) - pos.y);
        }
        if (dead)
        {
            shown = 1f;
        }
        else
        {
            float k = mover.enabled ? Mathf.Clamp01(1f - gap / Mathf.Max(0.5f, warnDistance)) : 0f;
            shown = Mathf.MoveTowards(shown, k, Time.unscaledDeltaTime * 4f);
        }

        // 화면이 뒤집혀 있으면 천장은 화면 아래
        bool flipped = mover.IsViewFlipped;
        band.anchorMin = new Vector2(0f, flipped ? 0f : 0.62f);
        band.anchorMax = new Vector2(1f, flipped ? 0.38f : 1f);
        band.localScale = new Vector3(1f, flipped ? -1f : 1f, 1f);

        // 아주 가까우면 살짝 깜빡여 '위험'을 강조
        float pulse = shown > 0.75f ? 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 18f) : 1f;
        Color c = warnColor;
        c.a *= shown * pulse;
        bandImage.color = c;
        label.alpha = Mathf.Clamp01(shown * 1.6f - 0.4f);
        label.rectTransform.anchoredPosition = new Vector2(0f, flipped ? 90f : -90f);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, flipped ? 0f : 1f);
    }

    void BuildOverlay()
    {
        GameObject canvasGo = new GameObject("CeilingWarningOverlay");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // 위쪽이 진하고 아래로 갈수록 투명해지는 띠(텍스처를 코드로 만듦)
        Texture2D grad = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 64; y++)
        {
            float a = Mathf.Pow(y / 63f, 2f);
            grad.SetPixel(0, y, new Color(1f, 1f, 1f, a));
        }
        grad.Apply();

        GameObject bandGo = new GameObject("Band");
        bandGo.transform.SetParent(canvasGo.transform, false);
        bandImage = bandGo.AddComponent<Image>();
        bandImage.sprite = Sprite.Create(grad, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f));
        bandImage.raycastTarget = false;
        bandImage.color = Color.clear;
        band = bandImage.rectTransform;
        band.offsetMin = Vector2.zero;
        band.offsetMax = Vector2.zero;

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(canvasGo.transform, false);
        label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = message;
        label.fontSize = 64f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.alpha = 0f;
        label.raycastTarget = false;
        label.rectTransform.sizeDelta = new Vector2(800f, 120f);
    }
}
