using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Chicken Fly!2 월드 생성기 (핵심).
// 메뉴: Chicken Fly ▸ Build World (10 Stages)
//  - 시즌1처럼 '처음부터 끝까지 이어진 하나의 맵'. 10개 스테이지는 그 안의 구간입니다.
//  - 길은 위로 계속 올라가면서(높이 0 → 약 570m) 구간마다 왼쪽 · 오른쪽으로 꺾입니다(시즌1 구조).
//  - 구간마다 폭 · 높이가 다릅니다: 넓은 곳은 자유 비행, 구름 구간처럼 좁은 곳은 좁음 자체가 난이도.
//  - 하늘 · 안개 · 빛 · 후처리는 ZoneAtmosphere가 구간 사이에서 서서히 섞습니다.
//  - 숫자는 StageBuildSettings 에셋에서 조절하고, 다시 누르면 월드 씬을 덮어씁니다.
// 파일: StageBuilder.cs(핵심 · 길) · StageBuilder.Environment.cs(지형 · 벽 · 배경 · 분위기) · StageBuilder.Nature.cs(스테이지 내용 · 자연물)
public static partial class StageBuilder
{
    const string TemplateScene = "Assets/Scenes/Scenes_Level1.unity";
    const string WorldScene = "Assets/Scenes/ChickenFly_World.unity";
    const string OldStageFolder = "Assets/Scenes/Stages";
    const string MatFolder = "Assets/Art/Materials/Stages";
    const string FxFolder = "Assets/Resources/FX";
    const string CloudPrefabPath = "Assets/Prefabs/Cloud.prefab";
    const string SettingsPath = "Assets/Settings/ChickenFly/StageBuildSettings.asset";
    const string FeedbackSettingsPath = "Assets/Resources/FX/FeedbackSettings.asset";

    // 스테이지 기본값을 바꾸면 올립니다(설정 에셋의 스테이지 목록을 새 기본값으로 다시 채움).
    const int StagesVersion = 7;

    static StageBuildSettings S;

    static readonly string[] KeepRoots = { "Player", "Main Camera", "CinemachineCamera", "EventSystem", "Directional Light" };
    static readonly string[] RequiredTags = { "Text", "Trigger", "Ending", "Obstacle", "Hit", "Drop", "Roof", "SpearTrigger", "Boundary" };

    // ───────────────────────── 메뉴 ─────────────────────────

    [MenuItem("Chicken Fly/Project Setup (tags · gravity · input)")]
    public static void ProjectSetup()
    {
        EnsureTags();

        SerializedObject dynamics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset")[0]);
        dynamics.FindProperty("m_Gravity").vector3Value = new Vector3(0f, -20f, 0f);
        dynamics.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject player = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        SerializedProperty input = player.FindProperty("activeInputHandler");
        bool needsRestart = false;
        if (input != null && input.intValue != 2)
        {
            input.intValue = 2;   // Both: 옛 Input.GetKey와 새 Input System을 함께 씀
            needsRestart = true;
        }
        player.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        Debug.Log($"[StageBuilder] 프로젝트 설정 완료: 태그, 중력 -20, 입력 Both{(needsRestart ? " (입력 방식 변경은 에디터 재시작 뒤 적용)" : "")}");
    }

    [MenuItem("Chicken Fly/Select Stage Build Settings")]
    public static void SelectSettings()
    {
        Selection.activeObject = LoadOrCreateSettings();
    }

    [MenuItem("Chicken Fly/Select Feedback (Effect) Settings")]
    public static void SelectFeedbackSettings()
    {
        EnsureFolder(FxFolder);
        Selection.activeObject = LoadOrCreateFeedbackSettings();
    }

    [MenuItem("Chicken Fly/Build World (10 Stages)")]
    public static void BuildWorld()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureTags();
        EnsureFolder(MatFolder);
        EnsureFolder(FxFolder);
        BuildFxAssets();
        LoadOrCreateFeedbackSettings();
        S = LoadOrCreateSettings();

        if (AssetDatabase.IsValidFolder(OldStageFolder)) AssetDatabase.DeleteAsset(OldStageFolder);
        CleanGeneratedMeshes();

        Scene scene = EditorSceneManager.OpenScene(TemplateScene, OpenSceneMode.Single);
        foreach (GameObject r in scene.GetRootGameObjects())
        {
            if (System.Array.IndexOf(KeepRoots, r.name) < 0) Object.DestroyImmediate(r);
        }
        Random.InitState(20261111);

        // 1) 스테이지 정의 → 하나의 이어진 길(오르막 · 좌우 꺾임 · 구간별 폭과 높이)
        List<StageDef> stages = DefineStages();
        ApplyOverrides(stages);

        Course course = new Course { baseY = 0f };
        List<float> starts = new List<float>();
        float acc = 0f;
        foreach (StageDef d in stages)
        {
            starts.Add(acc);
            foreach (Vector3 s in d.segs)
            {
                course.segs.Add(s);
                course.sizes.Add(new Vector2(d.width, d.height));
                acc += s.x;
            }
        }
        course.Bake(S.sizeBlend);

        Transform world = new GameObject("World").transform;
        Material triggerMat = Mat("Trigger_Invisible", new Color(1f, 1f, 1f, 0f), 0f);
        List<Ctx> contexts = new List<Ctx>();

        // 2) 구간마다: 지형 · 벽 · 배경 → 구간 시작 체크포인트 → 자연물 기믹
        for (int i = 0; i < stages.Count; i++)
        {
            StageDef def = stages[i];
            float len = 0f;
            foreach (Vector3 s in def.segs) len += s.x;

            Ctx ctx = new Ctx
            {
                def = def,
                index = i,
                isFirst = i == 0,
                isLast = i == stages.Count - 1,
                course = course,
                c = new View { full = course, x0 = starts[i], length = len },
                look = LookFor(def.zone),
                triggerMat = triggerMat,
                cloudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CloudPrefabPath)
            };
            ctx.root = new GameObject($"Stage{def.number:00}_{def.title.Replace(' ', '_')}").transform;
            ctx.root.SetParent(world, false);
            contexts.Add(ctx);

            BuildSection(ctx);
            // 세이브 포인트: N스테이지마다(기본 3 → 4 · 7 · 10스테이지 시작)
            if (!ctx.isFirst && i % Mathf.Max(1, S.checkpointEveryStages) == 0) Checkpoint(ctx, 4f, i * 100);
            def.content?.Invoke(ctx);
        }

        // 3) 맵 전체: 길 정보 · 출발 · 도착 · 바리케이드 · 경계 · 분위기 · 카메라 · UI
        BuildWorldWide(world, course, contexts);
        SetupPlayerCameraUi(course, contexts);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, WorldScene);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(WorldScene, true) };
        AssetDatabase.SaveAssets();
        Debug.Log($"[StageBuilder] 월드 생성 완료: 스테이지 {stages.Count}개, 길이 {course.Length:0}m, 마지막 높이 {course.FloorY(course.Length):0}m");
    }

    static void CleanGeneratedMeshes()
    {
        if (!AssetDatabase.IsValidFolder(MeshFolder)) return;
        foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { MeshFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string file = Path.GetFileName(path);
            if (file.StartsWith("Stage") || file.StartsWith("World_")) AssetDatabase.DeleteAsset(path);
        }
    }

    // ───────────────────────── 설정 ─────────────────────────

    static StageBuildSettings LoadOrCreateSettings()
    {
        StageBuildSettings s = AssetDatabase.LoadAssetAtPath<StageBuildSettings>(SettingsPath);
        if (s == null)
        {
            EnsureFolder("Assets/Settings/ChickenFly");
            s = ScriptableObject.CreateInstance<StageBuildSettings>();
            AssetDatabase.CreateAsset(s, SettingsPath);
        }

        if (s.stagesVersion < 4) s.cloudScale = 3.6f;   // v4: 구름이 시야를 가려 크기를 줄임

        List<StageDef> defaults = DefineStages();
        if (s.stages == null || s.stages.Count != defaults.Count || s.stagesVersion != StagesVersion)
        {
            s.stages = new List<StageEntry>();
            foreach (StageDef d in defaults)
            {
                StageEntry e = new StageEntry { number = d.number, title = d.title, hint = d.hint, difficulty = d.difficulty, width = d.width, height = d.height };
                e.path.AddRange(d.segs);
                s.stages.Add(e);
            }
            s.stagesVersion = StagesVersion;
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssets();
        }

        return s;
    }

    static FeedbackSettings LoadOrCreateFeedbackSettings()
    {
        FeedbackSettings f = AssetDatabase.LoadAssetAtPath<FeedbackSettings>(FeedbackSettingsPath);
        if (f == null)
        {
            f = ScriptableObject.CreateInstance<FeedbackSettings>();
            AssetDatabase.CreateAsset(f, FeedbackSettingsPath);
            AssetDatabase.SaveAssets();
        }
        return f;
    }

    // 설정 에셋의 스테이지 값(이름 · 힌트 · 난이도 · 폭 · 높이 · 길)을 코드 정의에 덮어씁니다.
    static void ApplyOverrides(List<StageDef> defs)
    {
        foreach (StageDef d in defs)
        {
            StageEntry e = S.stages.Find(x => x.number == d.number);
            if (e == null) continue;
            if (!string.IsNullOrEmpty(e.title)) d.title = e.title;
            d.hint = e.hint;
            d.difficulty = e.difficulty;
            d.width = Mathf.Max(10f, e.width);
            d.height = Mathf.Max(12f, e.height);
            if (e.path != null && e.path.Count > 0)
            {
                d.segs.Clear();
                d.segs.AddRange(e.path);
            }
        }
    }

    static float R(Vector2 range, float d01) => Mathf.Lerp(range.x, range.y, d01);

    // ───────────────────────── 길 ─────────────────────────

    // 맵 전체 길: 0.5m마다 중심점(바닥) · 방향 · 오르막 · 폭 · 높이를 미리 계산
    class Course
    {
        public float baseY;
        public readonly List<Vector3> segs = new List<Vector3>();   // (길이, 오르막°, 좌우 꺾임°)
        public readonly List<Vector2> sizes = new List<Vector2>();  // 그 구간 소속 스테이지의 (폭, 높이)

        const float Res = 0.5f;
        Vector3[] pts;
        float[] yaws, slopes, widths, heights;
        public float Length { get; private set; }

        public void Bake(float sizeBlend)
        {
            Length = 0f;
            foreach (Vector3 s in segs) Length += s.x;
            int n = Mathf.CeilToInt(Length / Res) + 1;
            pts = new Vector3[n];
            yaws = new float[n];
            slopes = new float[n];
            widths = new float[n];
            heights = new float[n];

            Vector3 pos = new Vector3(0f, baseY, 0f);
            float yaw = 0f;
            int k = 0;
            float segStart = 0f;
            for (int i = 0; i < n; i++)
            {
                float s = i * Res;
                while (k < segs.Count - 1 && s >= segStart + segs[k].x)
                {
                    segStart += segs[k].x;
                    k++;
                }
                Vector3 seg = segs[k];
                pts[i] = pos;
                yaws[i] = yaw;
                slopes[i] = seg.y;
                widths[i] = sizes[k].x;
                heights[i] = sizes[k].y;

                float turnRate = seg.x > 0.01f ? seg.z / seg.x : 0f;
                yaw += turnRate * Res;
                Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
                pos += dir * Res;
                pos.y += Mathf.Tan(seg.y * Mathf.Deg2Rad) * Res;
            }

            // 구간이 바뀔 때 폭 · 높이가 서서히 바뀌게(이동 평균)
            int r = Mathf.Max(1, Mathf.RoundToInt(sizeBlend / Res));
            widths = Smooth(widths, r);
            heights = Smooth(heights, r);
        }

        static float[] Smooth(float[] a, int r)
        {
            float[] o = new float[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                float sum = 0f;
                int cnt = 0;
                for (int j = Mathf.Max(0, i - r); j <= Mathf.Min(a.Length - 1, i + r); j++) { sum += a[j]; cnt++; }
                o[i] = sum / cnt;
            }
            return o;
        }

        void Index(float s, out int i, out float t)
        {
            float f = Mathf.Clamp(s, 0f, Length) / Res;
            i = Mathf.Clamp(Mathf.FloorToInt(f), 0, pts.Length - 2);
            t = f - i;
        }

        public float Yaw(float s) { Index(s, out int i, out float t); return Mathf.LerpAngle(yaws[i], yaws[i + 1], t); }
        public float Slope(float s) { Index(s, out int i, out float t); return Mathf.Lerp(slopes[i], slopes[i + 1], t); }
        public float Width(float s) { Index(s, out int i, out float t); return Mathf.Lerp(widths[i], widths[i + 1], t); }
        public float Height(float s) { Index(s, out int i, out float t); return Mathf.Lerp(heights[i], heights[i + 1], t); }
        public Quaternion Rot(float s) => Quaternion.Euler(0f, Yaw(s), 0f);
        public Vector3 Fwd(float s) => Rot(s) * Vector3.right;
        public Vector3 Lat(float s) => Rot(s) * Vector3.forward;

        public Vector3 Center(float s)
        {
            if (s < 0f) return pts[0] + Fwd(0f) * s;
            if (s > Length) return pts[pts.Length - 1] + Fwd(Length) * (s - Length);
            Index(s, out int i, out float t);
            return Vector3.Lerp(pts[i], pts[i + 1], t);
        }

        public float FloorY(float s) => Center(s).y;
        public float VerticalHeight(float s) => Height(s) / Mathf.Cos(Slope(s) * Mathf.Deg2Rad);
        public Vector3 P(float s, float h, float z) => Center(s) + Vector3.up * h + Lat(s) * z;
    }

    // 한 스테이지 구간을 '구간 안 거리 x'로 다루는 창. 기믹 위치는 모두 구간 안 거리 · 옆(z) · 높이(h)로 씁니다.
    class View
    {
        public Course full;
        public float x0;
        public float length;

        public float Length => length;
        public float X(float x) => x0 + x;
        public float FloorY(float x) => full.FloorY(x0 + x);
        public float Slope(float x) => full.Slope(Mathf.Clamp(x0 + x, 0f, full.Length));
        public float Width(float x) => full.Width(Mathf.Clamp(x0 + x, 0f, full.Length));
        public float Half(float x) => Width(x) / 2f;
        public float Height(float x) => full.Height(Mathf.Clamp(x0 + x, 0f, full.Length));
        public float VerticalHeight(float x) => full.VerticalHeight(Mathf.Clamp(x0 + x, 0f, full.Length));
        public Quaternion Rot(float x) => full.Rot(Mathf.Clamp(x0 + x, 0f, full.Length));
        public Quaternion RotSloped(float x) => Rot(x) * Quaternion.Euler(0f, 0f, Slope(x));
        public Vector3 Fwd(float x) => full.Fwd(Mathf.Clamp(x0 + x, 0f, full.Length));
        public Vector3 Lat(float x) => full.Lat(Mathf.Clamp(x0 + x, 0f, full.Length));
        public Vector3 P(float x, float h, float z) => full.P(x0 + x, h, z);
        public float ClampZ(float x, float z, float margin) => Mathf.Clamp(z, -Half(x) + margin, Half(x) - margin);
    }

    class StageDef
    {
        public int number;
        public string title;
        public string hint;
        public float difficulty;
        public string zone;
        public float width = 30f;
        public float height = 45f;
        public List<Vector3> segs = new List<Vector3>();
        public System.Action<Ctx> content;

        public StageDef Seg(float length, float slope, float turn = 0f)
        {
            segs.Add(new Vector3(length, slope, turn));
            return this;
        }
    }

    class Ctx
    {
        public StageDef def;
        public int index;
        public bool isFirst, isLast;
        public Course course;
        public View c;
        public Look look;
        public Transform root;
        public Material obstacleMat, triggerMat, floorMat;
        public GameObject cloudPrefab;
        public int checkpointCount;

        public float D01 => Mathf.Clamp01((def.difficulty - 1f) / 7f);
        public float Hole => R(S.holeSize, D01);
        public float CrossSpeed => R(S.crossSpeed, D01);
        public float CrossWait => R(S.crossInterval, D01);
        public float Drift => R(S.driftSpeed, D01);
        public float Lift => R(S.windLift, D01);
        public float CrossWind => R(S.crossWind, D01);
    }

    // ───────────────────────── 카메라 · UI ─────────────────────────

    static void SetupPlayerCameraUi(Course course, List<Ctx> contexts)
    {
        GameObject player = GameObject.Find("Player");
        GameObject cam = GameObject.Find("Main Camera");
        Vector3 offset = S.cameraOffset;

        if (player != null)
        {
            player.transform.position = course.Center(0f) + Vector3.up * 2.4f;
            GameOverTrigger got = player.GetComponent<GameOverTrigger>();
            GameObject pad = GameObject.Find("FirstGroundStart");
            if (got != null && pad != null) SetField(got, "firstGroundStart", pad);
            Mover mover = player.GetComponent<Mover>();
            if (mover != null) SetField(mover, "jumpForce", S.playerJumpForce);
        }

        // 시네머신은 쓰지 않음: 카메라 위치 · 방향은 Mover 혼자 정함(두 주인 문제 해결)
        CinemachineCamera vcam = Object.FindAnyObjectByType<CinemachineCamera>();
        if (vcam != null) vcam.gameObject.SetActive(false);

        if (cam != null && player != null)
        {
            cam.transform.position = player.transform.position + offset;
            cam.transform.rotation = Quaternion.Euler(S.cameraPitch, 90f, 0f);
            Camera camera = cam.GetComponent<Camera>();
            if (camera != null)
            {
                camera.farClipPlane = 1500f;
                camera.fieldOfView = S.cameraFov;
            }
            CinemachineBrain brain = cam.GetComponent<CinemachineBrain>();
            if (brain != null) brain.enabled = false;

            Mover mover = player.GetComponent<Mover>();
            if (mover != null)
            {
                SetField(mover, "cameraTransform", cam.transform);
                SetField(mover, "cameraOffset", offset);
                SetField(mover, "cameraPitch", S.cameraPitch);
                SetField(mover, "baseFov", S.cameraFov);
                SetField(mover, "cameraSmoothTime", Mathf.Clamp(S.cameraDamping.x * 0.15f, 0.02f, 0.5f));
            }
        }

        EndingCredits ending = Object.FindAnyObjectByType<EndingCredits>();
        Transform canvas = cam != null ? cam.transform.Find("Canvas") : null;
        if (ending == null || canvas == null)
        {
            Debug.LogWarning("[StageBuilder] Canvas 또는 EndingCredits를 찾지 못했습니다.");
            return;
        }

        Transform panel = canvas.Find("Panel");
        TextMeshProUGUI timeText = panel != null && panel.Find("TimeText") != null ? panel.Find("TimeText").GetComponent<TextMeshProUGUI>() : null;
        TextMeshProUGUI title = panel != null && panel.Find("Congratulations!") != null ? panel.Find("Congratulations!").GetComponent<TextMeshProUGUI>() : null;
        Transform scoreT = canvas.Find("ScoreText");
        ScoreMeter meter = scoreT != null ? scoreT.GetComponent<ScoreMeter>() : null;

        SetField(ending, "resultPanel", panel != null ? panel.gameObject : null);
        SetField(ending, "timeText", timeText);
        SetField(ending, "titleText", title);
        SetField(ending, "scoreMeter", meter);

        if (timeText != null)
        {
            TextMeshProUGUI alt = Object.Instantiate(timeText, timeText.transform.parent);
            alt.name = "AltitudeText";
            alt.rectTransform.anchoredPosition = timeText.rectTransform.anchoredPosition + new Vector2(0f, 40f);
            SetField(ending, "distanceText", alt);
        }

        Transform restart = panel != null ? panel.Find("RestartButton") : null;
        if (restart != null)
        {
            Button rb = restart.GetComponent<Button>();
            ClearListeners(rb.onClick);
            UnityEventTools.AddVoidPersistentListener(rb.onClick, ending.Restart);
        }

        GameObject infoGo = new GameObject("StageInfo");
        StageInfo info = infoGo.AddComponent<StageInfo>();
        float[] startX = new float[contexts.Count];
        string[] titles = new string[contexts.Count];
        string[] hints = new string[contexts.Count];
        for (int i = 0; i < contexts.Count; i++)
        {
            startX[i] = contexts[i].c.x0;
            titles[i] = contexts[i].def.title;
            hints[i] = contexts[i].def.hint;
        }
        SetField(info, "stageStartX", startX);
        SetField(info, "stageTitles", titles);
        SetField(info, "stageHints", hints);
        new GameObject("GravityFlipDirector").AddComponent<GravityFlipDirector>();
        new GameObject("DashHud").AddComponent<DashHud>();

        if (scoreT != null)
        {
            TextMeshProUGUI score = scoreT.GetComponent<TextMeshProUGUI>();
            float baseSize = score.fontSize;
            PlaceTopLeft(score.rectTransform, new Vector2(36f, -28f), new Vector2(420f, 60f));
            score.alignment = TextAlignmentOptions.TopLeft;

            TextMeshProUGUI hud = Object.Instantiate(score, canvas);
            Object.DestroyImmediate(hud.GetComponent<ScoreMeter>());
            hud.name = "StageLabel";
            PlaceTopLeft(hud.rectTransform, new Vector2(36f, -28f - baseSize * 1.25f), new Vector2(420f, 50f));
            hud.fontSize = baseSize * 0.65f;
            hud.color = new Color(1f, 1f, 1f, 0.8f);
            SetField(info, "hudStageText", hud);

            TextMeshProUGUI banner = Object.Instantiate(score, canvas);
            Object.DestroyImmediate(banner.GetComponent<ScoreMeter>());
            banner.name = "StageBanner";
            RectTransform brt = banner.rectTransform;
            brt.anchorMin = new Vector2(0.5f, 1f);
            brt.anchorMax = new Vector2(0.5f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0f, -70f);
            brt.sizeDelta = new Vector2(1200f, 320f);
            banner.fontSize = baseSize * 1.8f;
            banner.alignment = TextAlignmentOptions.Top;
            banner.gameObject.SetActive(false);
            SetField(info, "bannerText", banner);
        }
    }

    static void PlaceTopLeft(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    // ───────────────────────── 이펙트 에셋 ─────────────────────────

    static void BuildFxAssets()
    {
        string texPath = $"{FxFolder}/FX_Soft.png";
        if (!File.Exists(texPath))
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            File.WriteAllBytes(texPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        string matPath = $"{FxFolder}/FX_Particle.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(m, matPath);
        }
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
    }

    static Material ParticleMat => AssetDatabase.LoadAssetAtPath<Material>($"{FxFolder}/FX_Particle.mat");

    // ───────────────────────── 작은 도구 ─────────────────────────

    static Transform Group(Ctx ctx, string name)
    {
        Transform t = ctx.root.Find(name);
        if (t == null)
        {
            t = new GameObject(name).transform;
            t.SetParent(ctx.root, false);
        }
        return t;
    }

    // 복도 단면 전체를 덮는 트리거(태그 Trigger, 안 보임). 길 방향에 맞춰 놓임.
    static GameObject FullTrigger(Ctx ctx, string name, float x, float thickness)
    {
        float top = ctx.c.VerticalHeight(x);
        GameObject g = Box(Group(ctx, "Triggers"), name, ctx.c.P(x, top / 2f, 0f), new Vector3(thickness, top, ctx.c.Width(x)), ctx.c.Rot(x), ctx.triggerMat);
        g.GetComponent<Collider>().isTrigger = true;
        g.GetComponent<Renderer>().enabled = false;
        g.tag = "Trigger";
        return g;
    }

    static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale, Quaternion rot, Material mat)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.position = pos;
        g.transform.rotation = rot;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g;
    }

    static GameObject Prim(PrimitiveType type, Transform parent, string name, Material mat, bool keepCollider)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!keepCollider) RemoveCollider(g);
        return g;
    }

    static GameObject RemoveCollider(GameObject g)
    {
        Collider c = g.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
        return g;
    }

    static Material Mat(string name, Color color, float smoothness, Color? emission = null)
    {
        string path = $"{MatFolder}/{name}.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smoothness);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
        }
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    static void SetField(Object target, string field, object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogWarning($"[StageBuilder] {target.GetType().Name}.{field} 필드가 없습니다.");
            return;
        }

        switch (value)
        {
            case float f: p.floatValue = f; break;
            case int i: p.intValue = i; break;
            case bool b: p.boolValue = b; break;
            case string s: p.stringValue = s; break;
            case Vector3 v: p.vector3Value = v; break;
            case Color c: p.colorValue = c; break;
            case float[] fa:
                p.arraySize = fa.Length;
                for (int k = 0; k < fa.Length; k++) p.GetArrayElementAtIndex(k).floatValue = fa[k];
                break;
            case string[] sa:
                p.arraySize = sa.Length;
                for (int k = 0; k < sa.Length; k++) p.GetArrayElementAtIndex(k).stringValue = sa[k];
                break;
            case Transform[] arr:
                p.arraySize = arr.Length;
                for (int k = 0; k < arr.Length; k++) p.GetArrayElementAtIndex(k).objectReferenceValue = arr[k];
                break;
            case Object o: p.objectReferenceValue = o; break;
            case null: p.objectReferenceValue = null; break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ClearListeners(UnityEventBase evt)
    {
        for (int i = evt.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(evt, i);
        }
    }

    static void EnsureTags()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        foreach (string tag in RequiredTags)
        {
            bool found = false;
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) { found = true; break; }
            }
            if (!found)
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            }
        }
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
