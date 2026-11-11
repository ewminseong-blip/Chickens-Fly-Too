using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 세이브 포인트를 지나면 화면 위쪽 가운데에 "SAVED!"가 톡 튀어나왔다가 사라집니다.
// 처음 부를 때 화면 UI를 코드로 만들고, 씬이 다시 시작되면 함께 사라집니다.
public class SaveNotice : MonoBehaviour
{
    [SerializeField] string message = "SAVED!";
    [SerializeField] Color color = new Color(0.45f, 1f, 0.5f, 1f);
    [Tooltip("글이 떠 있는 시간(초)")]
    [SerializeField] float showTime = 1.8f;

    static SaveNotice instance;
    TextMeshProUGUI label;
    float timer;

    public static void Show(string text = null)
    {
        if (instance == null)
        {
            instance = new GameObject("SaveNotice").AddComponent<SaveNotice>();
            instance.Build();
        }
        instance.label.text = string.IsNullOrEmpty(text) ? instance.message : text;
        instance.timer = instance.showTime;
    }

    void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject go = new GameObject("Label");
        go.transform.SetParent(transform, false);
        label = go.AddComponent<TextMeshProUGUI>();
        label.fontSize = 96f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        // 번개 · 구름처럼 밝은 배경 위에서도 읽히게 진한 테두리
        label.outlineWidth = 0.3f;
        label.outlineColor = new Color32(10, 40, 15, 255);
        RectTransform rt = label.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.68f);
        rt.sizeDelta = new Vector2(1000f, 160f);
        label.alpha = 0f;
    }

    void Update()
    {
        if (label == null)
        {
            return;
        }
        if (timer <= 0f)
        {
            label.alpha = 0f;
            return;
        }
        timer -= Time.unscaledDeltaTime;
        float age = showTime - timer;
        // 처음 0.15초에 크게 튀어나왔다가 제자리, 마지막 0.4초에 사라짐
        float pop = age < 0.15f ? Mathf.Lerp(1.6f, 1f, age / 0.15f) : 1f;
        label.rectTransform.localScale = Vector3.one * pop;
        label.alpha = Mathf.Clamp01(timer / 0.4f);
    }
}
