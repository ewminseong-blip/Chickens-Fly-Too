using System.Collections;
using TMPro;
using UnityEngine;

// 하나로 이어진 맵의 스테이지(구간) 안내입니다.
// 닭이 다음 구간의 시작선(x)을 넘으면 화면 위쪽에 "STAGE N · 이름 · 힌트"를 잠깐 띄우고,
// 왼쪽 위 HUD의 스테이지 글자를 바꿉니다. 체크포인트에서 다시 시작하면 이미 본 배너는 다시 띄우지 않습니다.
public class StageInfo : MonoBehaviour
{
    [SerializeField] float[] stageStartX = new float[0];
    [SerializeField] string[] stageTitles = new string[0];
    [SerializeField] string[] stageHints = new string[0];
    [SerializeField] TextMeshProUGUI bannerText;
    [SerializeField] TextMeshProUGUI hudStageText;

    Transform player;
    int current = -1;
    int lastBanner = -1;
    Coroutine showing;

    public int StageNumber => current + 1;
    public int StageCount => stageStartX.Length;
    public string StageTitle => current >= 0 && current < stageTitles.Length ? stageTitles[current] : "";

    void Awake()
    {
        HazardSync.Clear();
    }

    void Start()
    {
        Mover mover = FindAnyObjectByType<Mover>();
        player = mover != null ? mover.transform : null;

        if (bannerText != null)
        {
            bannerText.gameObject.SetActive(false);
        }

        if (player != null)
        {
            current = IndexAt(player.position.x);
            // 처음 시작이면 1스테이지 배너를 보여 주고, 체크포인트 부활이면 지금 구간 배너는 생략
            lastBanner = GameSession.HasCheckpoint ? current : -1;
            UpdateHud();
            if (!GameSession.HasCheckpoint)
            {
                ShowBanner(current);
            }
        }
    }

    void Update()
    {
        if (player == null)
        {
            return;
        }

        int idx = IndexAt(player.position.x);
        if (idx == current)
        {
            return;
        }

        current = idx;
        UpdateHud();
        if (idx > lastBanner)
        {
            ShowBanner(idx);
        }
    }

    int IndexAt(float x)
    {
        int idx = 0;
        for (int i = 0; i < stageStartX.Length; i++)
        {
            if (x >= stageStartX[i]) idx = i;
        }
        return idx;
    }

    void UpdateHud()
    {
        if (hudStageText != null)
        {
            hudStageText.text = $"STAGE {current + 1} / {Mathf.Max(1, stageStartX.Length)}";
        }
    }

    void ShowBanner(int idx)
    {
        if (bannerText == null || idx < 0)
        {
            return;
        }

        lastBanner = idx;
        string title = idx < stageTitles.Length ? stageTitles[idx] : "";
        string hint = idx < stageHints.Length ? stageHints[idx] : "";
        bannerText.text = string.IsNullOrEmpty(hint)
            ? $"STAGE {idx + 1}\n<size=60%>{title}</size>"
            : $"STAGE {idx + 1}\n<size=60%>{title}</size>\n<size=32%><alpha=#CC>{hint}</size>";

        if (showing != null) StopCoroutine(showing);
        showing = StartCoroutine(Show());
    }

    IEnumerator Show()
    {
        RectTransform rt = bannerText.rectTransform;
        bannerText.gameObject.SetActive(true);

        // 나타남 0.25초(살짝 크게 → 제자리), 유지 2초, 사라짐 0.5초
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.25f);
            rt.localScale = Vector3.one * (Mathf.Lerp(0.6f, 1f, k) * (1f + 0.15f * Mathf.Sin(k * Mathf.PI)));
            bannerText.alpha = k;
            yield return null;
        }

        rt.localScale = Vector3.one;
        bannerText.alpha = 1f;
        yield return new WaitForSecondsRealtime(2f);

        t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            bannerText.alpha = 1f - Mathf.Clamp01(t / 0.5f);
            yield return null;
        }

        bannerText.gameObject.SetActive(false);
        showing = null;
    }
}
