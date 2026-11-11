using UnityEngine;
using TMPro;

// HUD 높이계: 닭의 현재 높이(월드 Y, m)를 보여 줍니다. 이 게임의 핵심은 '계속 위로 올라간다'입니다.
// 50m를 새로 넘을 때마다 숫자가 살짝 튀어 올라 '올라왔다'를 알려 줍니다.
public class ScoreMeter : MonoBehaviour
{
    // 다른 스크립트(EndingCredits 등)가 읽습니다.
    public float score;      // 이번 도전에서 도달한 최고 높이(m)
    public float time;       // 플레이 시간(초): 죽기 전까지 쌓인 시간 + 이번 목숨

    [SerializeField] Transform playerTransform;
    // 비워 두면 시간은 계산만 하고 화면에는 표시하지 않습니다.
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] float milestoneStep = 50f;

    TextMeshProUGUI scoreText;
    float nextMilestone;
    float pop;
    Vector3 baseScale;

    void Start()
    {
        scoreText = GetComponent<TextMeshProUGUI>();
        baseScale = scoreText.rectTransform.localScale;

        float y = playerTransform.position.y;
        score = y;
        nextMilestone = (Mathf.Floor(y / milestoneStep) + 1f) * milestoneStep;
    }

    void Update()
    {
        float y = playerTransform.position.y;
        score = Mathf.Max(score, y);
        time = GameSession.AccumulatedTime + Time.timeSinceLevelLoad;

        if (y >= nextMilestone)
        {
            nextMilestone += milestoneStep;
            pop = 1f;
        }

        if (pop > 0f)
        {
            pop = Mathf.MoveTowards(pop, 0f, Time.unscaledDeltaTime * 3f);
            scoreText.rectTransform.localScale = baseScale * (1f + 0.25f * Mathf.Sin(pop * Mathf.PI));
            scoreText.color = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.35f), pop);
        }

        scoreText.text = $"ALT {y:0} m";

        if (timeText != null)
        {
            timeText.text = time.ToString("F2") + "s";
        }
    }
}
