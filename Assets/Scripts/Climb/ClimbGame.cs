using UnityEngine;
using UnityEngine.SceneManagement;

// Can Chicken Fly? 한 판 관리: 시간 · 떨어진 횟수 · 최고 높이 · 체크포인트 · 이어하기 저장 · 일시정지 · 도착.
//  - 체크포인트는 드물게 3개. 켜진 뒤 그 높이보다 20m 넘게 떨어지면 깃털이 흩날리며 체크포인트로 돌아옴.
//  - 게임을 끄고 켜면 마지막에 서 있던 자리에서 이어 함(PlayerPrefs).
//  - Esc: 일시정지(계속 / 처음부터 / 종료)
public class ClimbGame : MonoBehaviour
{
    public static ClimbGame Instance { get; private set; }

    [SerializeField] ClimbController player;
    [SerializeField] ClimbCamera cameraRig;
    [SerializeField] ClimbCheckpoint[] checkpoints = new ClimbCheckpoint[0];
    [Header("구간 (생성기가 채움)")]
    [SerializeField] string[] sectionTitles = new string[0];
    [SerializeField] string[] sectionHints = new string[0];
    [SerializeField] float[] sectionStartHeights = new float[0];
    [Tooltip("구간 시작 발판 위 위치(개발용 순간이동 F1 ~ F12)")]
    [SerializeField] Vector3[] sectionSpawns = new Vector3[0];
    [SerializeField] Vector3 startPosition;
    [SerializeField] float startYaw;
    [SerializeField] float goalHeight = 2000f;
    [Tooltip("이만큼(m) 넘게 떨어지면 '떨어짐' 1회로 셈")]
    [SerializeField] float fallCountHeight = 10f;

    const string Key = "CCF.";

    public float ElapsedTime { get; private set; }
    public int Falls { get; private set; }
    public float BestHeight { get; private set; }
    public float Height => player != null ? player.transform.position.y : 0f;
    public int SectionIndex { get; private set; }
    public int HighestSection { get; private set; }
    public bool Finished { get; private set; }
    public bool Paused { get; private set; }
    public float GoalHeight => goalHeight;
    public ClimbController Player => player;
    public string SectionTitle(int i) => i >= 0 && i < sectionTitles.Length ? sectionTitles[i] : "";
    public string SectionHint(int i) => i >= 0 && i < sectionHints.Length ? sectionHints[i] : "";
    public int SectionCount => sectionTitles.Length;

    public event System.Action<int> SectionEntered;          // 처음 들어간 구간
    public event System.Action<int> CheckpointReached;
    public event System.Action<float> FellFar;              // 떨어진 높이
    public event System.Action Respawned;
    public event System.Action Goal;
    public event System.Action<bool> PauseChanged;

    ClimbCheckpoint active;
    float saveTimer;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (player == null) player = FindAnyObjectByType<ClimbController>();
        if (cameraRig == null) cameraRig = FindAnyObjectByType<ClimbCamera>();

        player.Landed += OnLanded;
        Load();
        SectionIndex = SectionAt(Height);
        HighestSection = Mathf.Max(HighestSection, SectionIndex);
        SectionEntered?.Invoke(SectionIndex);
    }

    // ───────────── 매 프레임 ─────────────

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !Finished) SetPaused(!Paused);
        if (Paused || Finished) return;

        ElapsedTime += Time.deltaTime;
        float h = Height;
        BestHeight = Mathf.Max(BestHeight, h);

        int s = SectionAt(h);
        if (s != SectionIndex)
        {
            SectionIndex = s;
            if (s > HighestSection)
            {
                HighestSection = s;
                SectionEntered?.Invoke(s);
            }
        }

        // 체크포인트 아래로 많이 떨어짐 → 체크포인트로
        ClimbSettings set = player.Settings;
        if (active != null && h < active.RespawnPosition.y - set.checkpointCatchDepth)
        {
            RespawnAt(active.RespawnPosition, active.RespawnYaw);
        }
        // 맵 밖(아주 아래)
        if (h < -40f)
        {
            if (active != null) RespawnAt(active.RespawnPosition, active.RespawnYaw);
            else RespawnAt(startPosition, startYaw);
        }

        // 서 있을 때 2초마다 위치 저장(이어하기)
        saveTimer += Time.deltaTime;
        if (saveTimer >= 2f && player.IsGrounded && !player.IsSliding)
        {
            saveTimer = 0f;
            Save();
        }

        // 개발용(에디터 · 개발 빌드에서만): F1 ~ F12 = 구간 시작으로 순간이동
        if (Application.isEditor || Debug.isDebugBuild)
        {
            for (int i = 0; i < Mathf.Min(12, sectionSpawns.Length); i++)
            {
                if (Input.GetKeyDown(KeyCode.F1 + i)) TeleportToSection(i);
            }
        }
    }

    public void TeleportToSection(int i)
    {
        if (i < 0 || i >= sectionSpawns.Length) return;
        player.Teleport(sectionSpawns[i], 0f);
        cameraRig?.SnapBehind();
    }

    public Vector3 SectionSpawn(int i) => i >= 0 && i < sectionSpawns.Length ? sectionSpawns[i] : startPosition;

    int SectionAt(float h)
    {
        int s = 0;
        for (int i = 1; i < sectionStartHeights.Length; i++)
        {
            if (h >= sectionStartHeights[i] - 0.5f) s = i;
        }
        return s;
    }

    void OnLanded(float impact, float dropped)
    {
        if (dropped >= fallCountHeight)
        {
            Falls++;
            FellFar?.Invoke(dropped);
        }
    }

    // ───────────── 체크포인트 · 부활 ─────────────

    public void ReachCheckpoint(ClimbCheckpoint cp)
    {
        if (active != null && active.Index >= cp.Index) return;
        if (active != null) active.SetLit(false);
        active = cp;
        cp.SetLit(true);
        FeedbackFX.Checkpoint(cp.transform.position + Vector3.up);
        CheckpointReached?.Invoke(cp.Index);
        Save();
    }

    void RespawnAt(Vector3 pos, float yaw)
    {
        FeedbackFX.Death(player.transform.position + Vector3.up);
        player.Teleport(pos, yaw);
        cameraRig?.SnapBehind();
        FeedbackFX.Checkpoint(pos);
        Respawned?.Invoke();
    }

    // ───────────── 도착 · 일시정지 ─────────────

    public void ReachGoal(Vector3 at)
    {
        if (Finished) return;
        Finished = true;
        player.InputLocked = true;
        FeedbackFX.Goal(at);
        float best = PlayerPrefs.GetFloat(Key + "bestTime", 0f);
        if (best <= 0f || ElapsedTime < best) PlayerPrefs.SetFloat(Key + "bestTime", ElapsedTime);
        ClearSave();
        ClimbCamera.LockCursor(false);
        Goal?.Invoke();
    }

    public float BestClearTime => PlayerPrefs.GetFloat(Key + "bestTime", 0f);

    public void SetPaused(bool pause)
    {
        Paused = pause;
        Time.timeScale = pause ? 0f : 1f;
        player.InputLocked = pause || Finished;
        ClimbCamera.LockCursor(!pause && !Finished);
        if (pause) Save();
        PauseChanged?.Invoke(pause);
    }

    public void RestartFromBeginning()
    {
        ClearSave();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().name : gameObject.scene.name);
    }

    public void Quit()
    {
        Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ───────────── 저장(이어하기) ─────────────

    void OnApplicationQuit()
    {
        if (!Finished) Save();
    }

    void Save()
    {
        if (player == null || Finished) return;
        Vector3 p = player.transform.position;
        PlayerPrefs.SetInt(Key + "saved", 1);
        PlayerPrefs.SetFloat(Key + "x", p.x);
        PlayerPrefs.SetFloat(Key + "y", p.y);
        PlayerPrefs.SetFloat(Key + "z", p.z);
        PlayerPrefs.SetFloat(Key + "yaw", player.transform.eulerAngles.y);
        PlayerPrefs.SetFloat(Key + "time", ElapsedTime);
        PlayerPrefs.SetInt(Key + "falls", Falls);
        PlayerPrefs.SetFloat(Key + "best", BestHeight);
        PlayerPrefs.SetInt(Key + "cp", active != null ? active.Index : 0);
        PlayerPrefs.SetInt(Key + "section", HighestSection);
        PlayerPrefs.Save();
    }

    void Load()
    {
        if (PlayerPrefs.GetInt(Key + "saved", 0) != 1)
        {
            player.Teleport(startPosition, startYaw);
            cameraRig?.SnapBehind();
            return;
        }
        Vector3 p = new Vector3(PlayerPrefs.GetFloat(Key + "x"), PlayerPrefs.GetFloat(Key + "y"), PlayerPrefs.GetFloat(Key + "z"));
        ElapsedTime = PlayerPrefs.GetFloat(Key + "time", 0f);
        Falls = PlayerPrefs.GetInt(Key + "falls", 0);
        BestHeight = PlayerPrefs.GetFloat(Key + "best", 0f);
        HighestSection = PlayerPrefs.GetInt(Key + "section", 0);
        int cp = PlayerPrefs.GetInt(Key + "cp", 0);
        foreach (ClimbCheckpoint c in checkpoints)
        {
            if (c != null && c.Index == cp)
            {
                active = c;
                c.SetLit(true);
            }
        }
        player.Teleport(p + Vector3.up * 0.2f, PlayerPrefs.GetFloat(Key + "yaw", 0f));
        cameraRig?.SnapBehind();
    }

    public static void ClearSave()
    {
        foreach (string k in new[] { "saved", "x", "y", "z", "yaw", "time", "falls", "best", "cp", "section" })
        {
            PlayerPrefs.DeleteKey(Key + k);
        }
        PlayerPrefs.Save();
    }
}
