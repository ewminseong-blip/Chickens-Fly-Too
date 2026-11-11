using UnityEngine;

// 맵 밖으로 나가는 경우를 막는 마지막 안전장치입니다.
// 바리케이드 · 보이지 않는 경계를 어떤 이유로든(아주 빠른 속도, 모서리 틈) 빠져나가면
// 사망 처리해 체크포인트(또는 출발점)에서 다시 시작합니다. 기준은 CoursePath(꺾이는 길)입니다.
public class StageBounds : MonoBehaviour
{
    [Tooltip("경계 밖으로 이만큼(m) 더 나가면 사망 처리")]
    [SerializeField] float margin = 6f;
    [Tooltip("출발점 뒤로 이만큼(m)까지는 괜찮음")]
    [SerializeField] float backLimit = 12f;

    Mover mover;
    GameOverTrigger gameOver;
    CoursePath path;
    int hint = -1;

    void Start()
    {
        // 씬이 바뀌는 중에 슬로모션 · 히트스톱이 남아 있지 않도록
        Time.timeScale = 1f;

        path = CoursePath.Get();
        mover = FindAnyObjectByType<Mover>();
        if (mover != null) gameOver = mover.GetComponent<GameOverTrigger>();
    }

    void LateUpdate()
    {
        if (mover == null || gameOver == null || path == null || !mover.enabled)
        {
            return;
        }

        Vector3 p = mover.transform.position;
        float s = path.Progress(p, ref hint);
        path.Local(p, Mathf.Clamp(s, 0f, path.Length), out float lateral, out float up);
        float half = path.Width(s) / 2f;
        float top = path.Height(s) / Mathf.Cos(35f * Mathf.Deg2Rad);

        bool outside =
            s < -backLimit - margin ||
            s > path.Length + margin ||
            Mathf.Abs(lateral) > half + margin ||
            up < -margin ||
            up > top + margin * 2f;

        if (outside)
        {
            Debug.LogWarning($"[StageBounds] 맵 밖으로 나감 → 다시 시작 (위치 {p}, 길 {s:0}m)");
            gameOver.Kill(gameObject);
        }
    }
}
