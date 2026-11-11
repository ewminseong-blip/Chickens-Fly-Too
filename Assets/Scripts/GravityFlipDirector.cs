using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

// 중력 반전 연출: 맵이 통째로 180° 도는 것처럼 보이게 합니다.
//  1) 슬로모션 + 화면 번쩍임 + "GRAVITY FLIP!" 배너 + 파티클
//  2) 카메라가 진행 방향 축으로 1초 동안 180° 회전(시네머신 Dutch + Mover 카메라 롤)
//  3) 회전이 끝나면 화면 기준 조작이 반대가 됨(Space = 화면 아래, 놓으면 화면 위로 떨어짐)
//     물리는 그대로이고 화면만 뒤집혔기 때문입니다. 마우스 좌우는 화면 기준을 유지합니다.
// 씬에 하나만 있으면 되고, 없으면 ReverseGravity가 자동으로 만듭니다.
public class GravityFlipDirector : MonoBehaviour
{
    [SerializeField] float rollDuration = 1.0f;       // 회전 시간(실제 초)
    [Tooltip("연출 시작 후 날갯짓을 막는 시간(실제 초). 0.5초를 넘기면 '조작을 뺏겼다'로 느껴짐")]
    [SerializeField] float inputLockTime = 0.3f;
    [SerializeField] float slowMotionScale = 0.3f;    // 회전 중 게임 속도
    [SerializeField] float flashAlpha = 0.55f;        // 번쩍임 세기(0이면 끔)
    [SerializeField] float flashDuration = 0.35f;
    [SerializeField] Color flashColor = new Color(0.85f, 0.75f, 1f);
    [SerializeField] string bannerTitle = "GRAVITY FLIP!";
    [SerializeField] string bannerHintFlipped = "SPACE = DOWN";
    [SerializeField] string bannerHintNormal = "SPACE = UP";

    static GravityFlipDirector instance;

    CinemachineCamera cinemachineCamera;
    Image flashImage;
    TextMeshProUGUI banner;
    Coroutine running;

    public static GravityFlipDirector Get()
    {
        if (instance == null)
        {
            instance = FindAnyObjectByType<GravityFlipDirector>();
        }

        if (instance == null)
        {
            instance = new GameObject("GravityFlipDirector").AddComponent<GravityFlipDirector>();
        }

        return instance;
    }

    void Awake()
    {
        instance = this;
        cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
        BuildOverlay();
    }

    void Start()
    {
        // 반전 구간 체크포인트에서 부활했다면 이미 뒤집힌 화면으로 시작합니다.
        if (GameSession.HasCheckpoint && GameSession.CheckpointReversed)
        {
            SetDutch(180f);
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void Flip(Mover mover, bool toFlipped)
    {
        if (mover == null || mover.IsViewFlipped == toFlipped)
        {
            return;
        }

        if (running != null)
        {
            StopCoroutine(running);
        }

        running = StartCoroutine(FlipSequence(mover, toFlipped));
    }

    IEnumerator FlipSequence(Mover mover, bool toFlipped)
    {
        bool effects = FeedbackFX.ScreenEffectsEnabled;
        float startRoll = mover.CameraRoll;
        float endRoll = toFlipped ? 180f : 0f;

        mover.InputLocked = true;

        if (effects)
        {
            Time.timeScale = slowMotionScale;
        }

        FeedbackFX.Burst(mover.transform.position, new FeedbackFX.BurstSettings
        {
            colorA = new Color(0.75f, 0.55f, 1f),
            colorB = Color.white,
            count = 40,
            speed = 10f,
            size = 0.5f,
            lifetime = 1.2f,
            gravity = 0f,
            radius = 1.5f
        });

        StartCoroutine(Flash(effects));
        StartCoroutine(Banner(toFlipped));

        float t = 0f;
        while (t < rollDuration)
        {
            t += Time.unscaledDeltaTime;
            if (t >= inputLockTime)
            {
                mover.InputLocked = false;
            }

            float k = Mathf.Clamp01(t / rollDuration);
            // ease-in-out (cubic): 천천히 시작해 가운데서 빠르고 끝에서 부드럽게 멈춤
            float e = k < 0.5f ? 4f * k * k * k : 1f - Mathf.Pow(-2f * k + 2f, 3f) * 0.5f;
            float roll = Mathf.Lerp(startRoll, endRoll, e);
            mover.CameraRoll = roll;
            SetDutch(roll);
            yield return null;
        }

        mover.CameraRoll = endRoll;
        SetDutch(endRoll);

        // 회전이 끝난 뒤부터 화면 기준 조작이 반대가 됩니다.
        mover.SetViewFlipped(toFlipped);
        mover.InputLocked = false;
        Time.timeScale = 1f;
        running = null;
    }

    void SetDutch(float roll)
    {
        if (cinemachineCamera == null)
        {
            return;
        }

        LensSettings lens = cinemachineCamera.Lens;
        // Dutch는 -180~180 범위입니다.
        lens.Dutch = Mathf.Clamp(roll > 180f ? roll - 360f : roll, -180f, 180f);
        cinemachineCamera.Lens = lens;
    }

    IEnumerator Flash(bool effects)
    {
        if (!effects || flashAlpha <= 0f || flashImage == null)
        {
            yield break;
        }

        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.unscaledDeltaTime;
            Color c = flashColor;
            c.a = flashAlpha * (1f - Mathf.Clamp01(t / flashDuration));
            flashImage.color = c;
            yield return null;
        }

        flashImage.color = Color.clear;
    }

    IEnumerator Banner(bool toFlipped)
    {
        if (banner == null)
        {
            yield break;
        }

        banner.text = $"{bannerTitle}\n<size=55%>{(toFlipped ? bannerHintFlipped : bannerHintNormal)}</size>";
        banner.gameObject.SetActive(true);
        RectTransform rt = banner.rectTransform;

        // 펀치: 1.4배로 튀어나왔다가 0.2초에 제자리, 1.2초 유지, 0.4초 사라짐
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.2f);
            rt.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, 1f - (1f - k) * (1f - k));
            banner.alpha = k;
            yield return null;
        }

        rt.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(1.2f);

        t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            banner.alpha = 1f - Mathf.Clamp01(t / 0.4f);
            yield return null;
        }

        banner.gameObject.SetActive(false);
    }

    void BuildOverlay()
    {
        GameObject canvasGo = new GameObject("FlipOverlay");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject flashGo = new GameObject("Flash");
        flashGo.transform.SetParent(canvasGo.transform, false);
        flashImage = flashGo.AddComponent<Image>();
        flashImage.color = Color.clear;
        flashImage.raycastTarget = false;
        RectTransform frt = flashImage.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        GameObject bannerGo = new GameObject("Banner");
        bannerGo.transform.SetParent(canvasGo.transform, false);
        banner = bannerGo.AddComponent<TextMeshProUGUI>();
        banner.alignment = TextAlignmentOptions.Center;
        banner.fontSize = 110f;
        banner.fontStyle = FontStyles.Bold;
        banner.color = Color.white;
        banner.outlineWidth = 0.25f;
        banner.outlineColor = new Color32(60, 20, 110, 255);
        banner.raycastTarget = false;
        RectTransform brt = banner.rectTransform;
        brt.anchorMin = new Vector2(0f, 0.55f);
        brt.anchorMax = new Vector2(1f, 0.85f);
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;
        bannerGo.SetActive(false);
    }
}
