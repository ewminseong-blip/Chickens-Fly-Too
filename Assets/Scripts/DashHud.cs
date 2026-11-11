using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 대시 안내 HUD: 화면 왼쪽 아래에 "HOLD SHIFT : DASH"와 게이지 막대를 보여 줍니다.
// Shift를 누르고 있는 동안 게이지가 닳고, 떼면 다시 차오릅니다.
// 다시 쓸 수 있게 되는 순간 살짝 커졌다 돌아와 알려 줍니다.
public class DashHud : MonoBehaviour
{
    [SerializeField] string label = "HOLD SHIFT : DASH";
    [SerializeField] Color readyColor = new Color(1f, 0.85f, 0.25f, 1f);
    [SerializeField] Color chargingColor = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField] Vector2 position = new Vector2(36f, 36f);

    Mover mover;
    TextMeshProUGUI text;
    Image fill;
    RectTransform root;
    bool wasReady = true;
    float pop;

    void Start()
    {
        mover = FindAnyObjectByType<Mover>();
        Build();
    }

    void Update()
    {
        if (mover == null || root == null)
        {
            return;
        }

        float charge = mover.DashCharge;
        bool ready = mover.DashReady;
        if (ready && !wasReady)
        {
            pop = 1f;
        }
        wasReady = ready;
        pop = Mathf.MoveTowards(pop, 0f, Time.unscaledDeltaTime * 4f);

        fill.fillAmount = charge;
        Color c = ready ? readyColor : chargingColor;
        fill.color = c;
        text.color = ready ? readyColor : new Color(1f, 1f, 1f, 0.6f);
        root.localScale = Vector3.one * (1f + 0.18f * pop);
    }

    void Build()
    {
        GameObject canvasGo = new GameObject("DashHudOverlay");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject rootGo = new GameObject("Dash");
        rootGo.transform.SetParent(canvasGo.transform, false);
        root = rootGo.AddComponent<RectTransform>();
        root.anchorMin = root.anchorMax = Vector2.zero;
        root.pivot = Vector2.zero;
        root.anchoredPosition = position;
        root.sizeDelta = new Vector2(420f, 70f);

        // 막대 배경
        Image back = NewImage(root, "Back", new Color(0f, 0f, 0f, 0.35f));
        RectTransform brt = back.rectTransform;
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(0f, 0f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(0f, 14f);

        // 차오르는 막대
        fill = NewImage(brt, "Fill", readyColor);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        RectTransform frt = fill.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(2f, 2f);
        frt.offsetMax = new Vector2(-2f, -2f);

        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(root, false);
        text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 34f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.BottomLeft;
        text.raycastTarget = false;
        RectTransform trt = text.rectTransform;
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = new Vector2(0f, 20f);
        trt.offsetMax = Vector2.zero;
    }

    static Image NewImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        // Filled 타입은 스프라이트가 있어야 동작하므로 흰 1px 스프라이트를 씀
        Texture2D white = Texture2D.whiteTexture;
        img.sprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(0.5f, 0.5f));
        img.color = color;
        img.raycastTarget = false;
        return img;
    }
}
