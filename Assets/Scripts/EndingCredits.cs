using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

// 도착 오브젝트(태그 Ending)에 붙여 사용합니다.
// 플레이어가 닿으면 색종이가 터지고 잠시 뒤 결과 화면(높이 · 시간 · 최고 기록 · 사망 · 다시하기 · 다음 스테이지)을 보여 줍니다.
public class EndingCredits : MonoBehaviour
{
    const string BestTimeKeyPrefix = "ChickenFly2_BestTime_";

    [SerializeField] GameObject resultPanel;        // 처음엔 꺼 두기
    [SerializeField] TextMeshProUGUI titleText;     // "Congratulations!" 등(선택)
    [SerializeField] TextMeshProUGUI distanceText;  // 높이(선택)
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI bestText;      // 비우면 TimeText를 복사해 만듦
    [SerializeField] TextMeshProUGUI deathsText;    // 비우면 TimeText를 복사해 만듦
    [SerializeField] float autoLineSpacing = 40f;
    [SerializeField] GameObject nextStageButton;    // 마지막 스테이지면 숨김
    [SerializeField] ScoreMeter scoreMeter;
    [SerializeField] float stopDelay = 0.1f;
    [SerializeField] float panelDelay = 0.9f;       // 색종이를 볼 시간

    bool isFinished;
    Mover playerMover;
    Rigidbody playerRigidbody;

    void OnCollisionEnter(Collision other)
    {
        TryFinish(other.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        TryFinish(other);
    }

    void TryFinish(Collider otherCollider)
    {
        if (isFinished || otherCollider == null)
        {
            return;
        }

        // 몸통 · 날개 같은 자식 Collider가 닿아도 플레이어 본체를 찾습니다.
        Mover mover = otherCollider.GetComponentInParent<Mover>();
        if (mover == null || !mover.CompareTag("Player") || !mover.enabled)
        {
            return;
        }

        // 죽는 중(히트스톱 · 깃털)에 도착선에 닿은 경우는 클리어로 치지 않습니다.
        GameOverTrigger death = mover.GetComponent<GameOverTrigger>();
        if (death != null)
        {
            if (death.IsDying)
            {
                return;
            }

            death.MarkCleared();   // 도착한 뒤에는 장애물에 닿아도 죽지 않음
        }

        isFinished = true;
        playerMover = mover;
        playerRigidbody = mover.GetComponent<Rigidbody>();
        StartCoroutine(FinishSequence());
    }

    IEnumerator FinishSequence()
    {
        yield return new WaitForSeconds(stopDelay);

        if (playerMover != null)
        {
            playerMover.enabled = false;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }

        float clearTime = scoreMeter != null ? scoreMeter.time : Time.timeSinceLevelLoad;
        if (scoreMeter != null)
        {
            scoreMeter.enabled = false;
        }

        // 클리어는 이 게임에서 가장 큰 성취 → 가장 큰 연출(색종이)
        FeedbackFX.Goal(playerMover != null ? playerMover.transform.position : transform.position);

        yield return new WaitForSeconds(panelDelay);

        ShowResult(clearTime);
    }

    void ShowResult(float clearTime)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        StageInfo stage = FindAnyObjectByType<StageInfo>();

        if (titleText != null)
        {
            titleText.text = stage != null ? $"STAGE {stage.StageNumber} CLEAR!" : "Congratulations!";
        }

        if (distanceText != null && scoreMeter != null)
        {
            distanceText.text = $"Altitude: {scoreMeter.score:0} m";
        }

        if (timeText != null)
        {
            timeText.text = "Time: " + clearTime.ToString("F2") + "s";
        }

        // 최고 기록(스테이지마다 따로 저장)
        string key = BestTimeKeyPrefix + sceneName;
        bool hasBest = PlayerPrefs.HasKey(key);
        float best = PlayerPrefs.GetFloat(key, float.MaxValue);
        bool isNewBest = !hasBest || clearTime < best;
        if (isNewBest)
        {
            best = clearTime;
            PlayerPrefs.SetFloat(key, best);
            PlayerPrefs.Save();
        }

        if (bestText == null)
        {
            bestText = CreateLineBelowTime(1, "BestText");
        }

        if (deathsText == null)
        {
            deathsText = CreateLineBelowTime(2, "DeathsText");
        }

        if (bestText != null)
        {
            bestText.text = "Best: " + best.ToString("F2") + "s" + (isNewBest ? "  <color=#FFD84A>NEW!</color>" : "");
        }

        if (deathsText != null)
        {
            deathsText.text = "Deaths: " + GameSession.Deaths;
        }

        if (nextStageButton != null)
        {
            nextStageButton.SetActive(HasNextStage());
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }
    }

    TextMeshProUGUI CreateLineBelowTime(int lineIndex, string objectName)
    {
        if (timeText == null)
        {
            return null;
        }

        TextMeshProUGUI copy = Instantiate(timeText, timeText.transform.parent);
        copy.name = objectName;
        copy.rectTransform.anchoredPosition =
            timeText.rectTransform.anchoredPosition - new Vector2(0f, autoLineSpacing * lineIndex);
        return copy;
    }

    static bool HasNextStage()
    {
        return SceneManager.GetActiveScene().buildIndex + 1 < SceneManager.sceneCountInBuildSettings;
    }

    // 다시하기 버튼: 체크포인트 · 누적 시간 · 사망 횟수를 지우고 이 스테이지를 처음부터.
    public void Restart()
    {
        GameSession.ResetRun();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 다음 스테이지 버튼
    public void NextStage()
    {
        GameSession.ResetRun();
        Time.timeScale = 1f;
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(next < SceneManager.sceneCountInBuildSettings ? next : 0);
    }
}
