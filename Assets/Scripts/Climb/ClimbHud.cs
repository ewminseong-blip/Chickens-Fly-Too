using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 화면 표시(실행할 때 코드로 만듦):
//  왼쪽 위: 지금 높이 · 최고 높이 · 구간 이름 / 오른쪽 위: 시간 · 떨어진 횟수
//  아래 가운데: 충전 게이지(충전할 때) · 활공 게이지(활공할 때)
//  가운데 위: 구간 배너, 짧은 알림(체크포인트 · 떨어짐) / 왼쪽 아래: 조작 안내(처음 구간)
//  일시정지 창(Esc) · 도착 창
public class ClimbHud : MonoBehaviour
{
    ClimbGame game;
    ClimbController player;

    TextMeshProUGUI heightText, bestText, sectionText, timeText, fallsText, bannerText, bannerSub, toastText, helpText;
    RectTransform chargeRoot, glideRoot;
    Image chargeFill, glideFill;
    CanvasGroup chargeGroup, glideGroup, bannerGroup, toastGroup;
    GameObject pausePanel, resultPanel;
    TextMeshProUGUI resultText;
    float bannerTimer, toastTimer;

    static readonly Color Ink = new Color(0.12f, 0.08f, 0.04f, 1f);
    static readonly Color Cream = new Color(1f, 0.97f, 0.88f, 1f);
    static readonly Color Yolk = new Color(1f, 0.8f, 0.15f, 1f);

    void Start()
    {
        game = ClimbGame.Instance != null ? ClimbGame.Instance : FindAnyObjectByType<ClimbGame>();
        player = game != null ? game.Player : FindAnyObjectByType<ClimbController>();
        Build();

        if (game != null)
        {
            game.SectionEntered += i => ShowBanner(game.SectionTitle(i), game.SectionHint(i));
            game.CheckpointReached += i => Toast($"CHECKPOINT {i} / 3", new Color(0.5f, 1f, 0.45f));
            game.FellFar += d => Toast($"FELL {d:0} m", new Color(1f, 0.55f, 0.4f));
            game.Respawned += () => Toast("BACK TO THE NEST", Cream);
            game.Goal += ShowResult;
            game.PauseChanged += p => pausePanel.SetActive(p);
            ShowBanner(game.SectionTitle(game.SectionIndex), game.SectionHint(game.SectionIndex));
        }
    }

    // ───────────── 만들기 ─────────────

    void Build()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        GameObject cg = new GameObject("ClimbHUD");
        Canvas canvas = cg.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = cg.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        cg.AddComponent<GraphicRaycaster>();
        RectTransform root = cg.GetComponent<RectTransform>();

        heightText = Label(root, "Height", new Vector2(0f, 1f), new Vector2(40f, -26f), new Vector2(600f, 90f), 72f, TextAlignmentOptions.TopLeft);
        bestText = Label(root, "Best", new Vector2(0f, 1f), new Vector2(44f, -106f), new Vector2(600f, 40f), 28f, TextAlignmentOptions.TopLeft);
        sectionText = Label(root, "Section", new Vector2(0f, 1f), new Vector2(44f, -144f), new Vector2(700f, 40f), 28f, TextAlignmentOptions.TopLeft);
        sectionText.color = Yolk;
        timeText = Label(root, "Time", new Vector2(1f, 1f), new Vector2(-40f, -26f), new Vector2(500f, 70f), 52f, TextAlignmentOptions.TopRight);
        fallsText = Label(root, "Falls", new Vector2(1f, 1f), new Vector2(-44f, -92f), new Vector2(500f, 40f), 28f, TextAlignmentOptions.TopRight);

        bannerGroup = Group(root, "BannerGroup");
        RectTransform bannerRoot = (RectTransform)bannerGroup.transform;
        bannerText = Label(bannerRoot, "Banner", new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1400f, 110f), 86f, TextAlignmentOptions.Top);
        bannerSub = Label(bannerRoot, "BannerSub", new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(1400f, 50f), 32f, TextAlignmentOptions.Top);
        bannerGroup.alpha = 0f;

        toastText = Label(root, "Toast", new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(1200f, 70f), 48f, TextAlignmentOptions.Center);
        toastGroup = toastText.gameObject.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f;

        chargeRoot = Bar(root, "Charge", new Vector2(0f, 150f), new Vector2(460f, 26f), "CHARGE", out chargeFill, out chargeGroup);
        glideRoot = Bar(root, "Glide", new Vector2(0f, 104f), new Vector2(320f, 16f), "GLIDE", out glideFill, out glideGroup);
        glideFill.color = new Color(0.55f, 0.85f, 1f);

        helpText = Label(root, "Help", new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(760f, 220f), 26f, TextAlignmentOptions.BottomLeft);
        helpText.text = "WASD  move\nMOUSE  look\nHOLD SPACE  charge   RELEASE  fly\nIN THE AIR HOLD SPACE  glide\nESC  pause";
        helpText.rectTransform.pivot = new Vector2(0f, 0f);

        pausePanel = Panel(root, "Pause", "PAUSED", out _, new (string, UnityEngine.Events.UnityAction)[]
        {
            ("RESUME", () => game.SetPaused(false)),
            ("RESTART FROM THE COOP", () => game.RestartFromBeginning()),
            ("SAVE & QUIT", () => game.Quit()),
        });
        pausePanel.SetActive(false);

        resultPanel = Panel(root, "Result", "YES, CHICKENS CAN FLY!", out resultText, new (string, UnityEngine.Events.UnityAction)[]
        {
            ("CLIMB AGAIN", () => game.RestartFromBeginning()),
            ("QUIT", () => game.Quit()),
        });
        resultPanel.SetActive(false);
    }

    static TextMeshProUGUI Label(RectTransform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = Cream;
        t.fontStyle = FontStyles.Bold;
        t.outlineWidth = 0.22f;
        t.outlineColor = Ink;
        t.raycastTarget = false;
        return t;
    }

    static CanvasGroup Group(RectTransform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go.AddComponent<CanvasGroup>();
    }

    static RectTransform Bar(RectTransform parent, string name, Vector2 pos, Vector2 size, string caption, out Image fill, out CanvasGroup group)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        Image back = new GameObject("Back", typeof(RectTransform)).AddComponent<Image>();
        back.transform.SetParent(go.transform, false);
        Stretch(back.rectTransform, -4f);
        back.color = new Color(Ink.r, Ink.g, Ink.b, 0.75f);

        fill = new GameObject("Fill", typeof(RectTransform)).AddComponent<Image>();
        fill.transform.SetParent(go.transform, false);
        Stretch(fill.rectTransform, 0f);
        fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        fill.color = Yolk;

        TextMeshProUGUI cap = Label(rt, "Caption", new Vector2(0.5f, 1f), new Vector2(0f, 34f), new Vector2(400f, 34f), 24f, TextAlignmentOptions.Bottom);
        cap.rectTransform.pivot = new Vector2(0.5f, 1f);
        cap.text = caption;
        return rt;
    }

    static void Stretch(RectTransform rt, float pad)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
    }

    GameObject Panel(RectTransform parent, string name, string title, out TextMeshProUGUI body, (string label, UnityEngine.Events.UnityAction action)[] buttons)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        Stretch(rt, 0f);
        Image dim = go.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject box = new GameObject("Box", typeof(RectTransform));
        box.transform.SetParent(go.transform, false);
        RectTransform brt = (RectTransform)box.transform;
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(820f, 220f + buttons.Length * 96f);
        Image bimg = box.AddComponent<Image>();
        bimg.color = new Color(0.22f, 0.14f, 0.07f, 0.95f);

        TextMeshProUGUI t = Label(brt, "Title", new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(780f, 80f), 54f, TextAlignmentOptions.Top);
        t.text = title;
        t.color = Yolk;
        body = Label(brt, "Body", new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(760f, 90f), 30f, TextAlignmentOptions.Top);
        body.text = "";

        for (int i = 0; i < buttons.Length; i++)
        {
            GameObject b = new GameObject(buttons[i].label, typeof(RectTransform));
            b.transform.SetParent(box.transform, false);
            RectTransform r = (RectTransform)b.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0f);
            r.anchoredPosition = new Vector2(0f, 30f + (buttons.Length - 1 - i) * 92f);
            r.sizeDelta = new Vector2(560f, 76f);
            Image img = b.AddComponent<Image>();
            img.color = new Color(1f, 0.55f, 0.1f, 1f);
            Button btn = b.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(1f, 0.85f, 0.6f);
            cb.pressedColor = new Color(0.85f, 0.4f, 0.05f);
            btn.colors = cb;
            btn.onClick.AddListener(buttons[i].action);
            TextMeshProUGUI lt = Label(r, "Text", new Vector2(0.5f, 0.5f), Vector2.zero, r.sizeDelta, 34f, TextAlignmentOptions.Center);
            lt.text = buttons[i].label;
        }
        return go;
    }

    // ───────────── 갱신 ─────────────

    void Update()
    {
        if (game == null || player == null) return;
        float dt = Time.unscaledDeltaTime;

        heightText.text = $"{Mathf.Max(0f, game.Height):0} m";
        bestText.text = $"BEST {game.BestHeight:0} m  /  {game.GoalHeight:0} m";
        sectionText.text = $"{game.SectionIndex + 1}. {game.SectionTitle(game.SectionIndex)}";
        int t = Mathf.FloorToInt(game.ElapsedTime);
        timeText.text = t >= 3600 ? $"{t / 3600}:{t / 60 % 60:00}:{t % 60:00}" : $"{t / 60:00}:{t % 60:00}";
        fallsText.text = $"FALLS {game.Falls}";

        // 충전 게이지: 차오를수록 노랑 → 주황 → 빨강, 꽉 차면 깜빡
        bool charging = player.Current == ClimbController.State.Charging;
        chargeGroup.alpha = Mathf.MoveTowards(chargeGroup.alpha, charging ? 1f : 0f, dt * (charging ? 12f : 4f));
        if (charging)
        {
            float c = player.Charge01;
            chargeFill.rectTransform.anchorMax = new Vector2(c, 1f);
            Color col = c < 0.6f ? Color.Lerp(Yolk, new Color(1f, 0.5f, 0.1f), c / 0.6f) : Color.Lerp(new Color(1f, 0.5f, 0.1f), new Color(1f, 0.2f, 0.15f), (c - 0.6f) / 0.4f);
            if (c >= 1f && Mathf.Repeat(Time.unscaledTime * 6f, 1f) < 0.5f) col = Color.white;
            chargeFill.color = col;
        }

        bool showGlide = player.Current == ClimbController.State.Gliding || (!player.IsGrounded && player.Glide01 < 0.999f);
        glideGroup.alpha = Mathf.MoveTowards(glideGroup.alpha, showGlide ? 1f : 0f, dt * 5f);
        glideFill.rectTransform.anchorMax = new Vector2(player.Glide01, 1f);

        if (bannerTimer > 0f)
        {
            bannerTimer -= dt;
            bannerGroup.alpha = Mathf.Clamp01(Mathf.Min(bannerTimer, 3.6f - bannerTimer) * 3f);
        }
        if (toastTimer > 0f)
        {
            toastTimer -= dt;
            toastGroup.alpha = Mathf.Clamp01(toastTimer * 2f);
            toastText.rectTransform.localScale = Vector3.one * (1f + Mathf.Max(0f, toastTimer - 1.6f) * 1.2f);
        }

        // 조작 안내: 처음 두 구간에서만
        helpText.alpha = Mathf.MoveTowards(helpText.alpha, game.HighestSection <= 1 && !game.Finished ? 1f : 0f, dt);
    }

    void ShowBanner(string title, string hint)
    {
        if (string.IsNullOrEmpty(title)) return;
        bannerText.text = title.ToUpperInvariant();
        bannerSub.text = hint;
        bannerTimer = 3.6f;
    }

    void Toast(string text, Color color)
    {
        toastText.text = text;
        toastText.color = color;
        toastTimer = 2f;
    }

    void ShowResult()
    {
        int t = Mathf.FloorToInt(game.ElapsedTime);
        string time = t >= 3600 ? $"{t / 3600}:{t / 60 % 60:00}:{t % 60:00}" : $"{t / 60:00}:{t % 60:00}";
        int b = Mathf.FloorToInt(game.BestClearTime);
        resultText.text = $"TIME {time}    FALLS {game.Falls}\nBEST CLEAR {b / 60:00}:{b % 60:00}";
        resultPanel.SetActive(true);
    }
}
