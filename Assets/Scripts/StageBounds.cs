using UnityEngine;

// 맵 밖으로 나가는 경우를 막는 마지막 안전장치입니다.
// 보이는 바리케이드 · 보이지 않는 경계를 어떤 이유로든(아주 빠른 속도, 모서리 틈) 빠져나가면
// 사망 처리해 체크포인트(또는 출발점)에서 다시 시작합니다.
// 바닥 높이는 StageBuilder가 2m마다 미리 계산해 넣어 둡니다.
public class StageBounds : MonoBehaviour
{
    [SerializeField] float[] floorHeights = new float[0];   // x = 0, step, 2·step … 의 바닥 높이
    [SerializeField] float step = 2f;
    [SerializeField] float length = 200f;
    [SerializeField] float width = 24f;
    [SerializeField] float height = 26f;
    [Tooltip("경계 밖으로 이만큼(m) 더 나가면 사망 처리")]
    [SerializeField] float margin = 6f;

    Mover mover;
    GameOverTrigger gameOver;

    public void Configure(float[] heights, float sampleStep, float courseLength, float corridorWidth, float corridorHeight)
    {
        floorHeights = heights;
        step = sampleStep;
        length = courseLength;
        width = corridorWidth;
        height = corridorHeight;
    }

    void Start()
    {
        // 씬이 바뀌는 중에 슬로모션 · 히트스톱이 남아 있지 않도록
        Time.timeScale = 1f;

        mover = FindAnyObjectByType<Mover>();
        if (mover != null)
        {
            gameOver = mover.GetComponent<GameOverTrigger>();
        }
    }

    public float FloorAt(float x)
    {
        if (floorHeights == null || floorHeights.Length == 0)
        {
            return 0f;
        }

        float f = Mathf.Clamp(x, 0f, length) / step;
        int i = Mathf.Clamp(Mathf.FloorToInt(f), 0, floorHeights.Length - 1);
        int j = Mathf.Min(i + 1, floorHeights.Length - 1);
        return Mathf.Lerp(floorHeights[i], floorHeights[j], f - i);
    }

    void LateUpdate()
    {
        if (mover == null || gameOver == null || !mover.enabled)
        {
            return;
        }

        Vector3 p = mover.transform.position;
        float floor = FloorAt(p.x);
        bool outside =
            p.x < -10f - margin ||
            p.x > length + margin ||
            Mathf.Abs(p.z) > width / 2f + margin ||
            p.y < floor - margin ||
            p.y > floor + height / Mathf.Cos(15f * Mathf.Deg2Rad) + margin * 2f;

        if (outside)
        {
            Debug.LogWarning($"[StageBounds] 맵 밖으로 나감 → 다시 시작 (위치 {p})");
            gameOver.Kill(gameObject);
        }
    }
}
