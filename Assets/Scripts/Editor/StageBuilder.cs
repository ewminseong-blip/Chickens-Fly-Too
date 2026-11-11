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

// Chicken Fly!2 스테이지 생성기.
// 메뉴: Chicken Fly ▸ Build 10 Stages
//  - Scenes_Level1을 틀로 열어 Player · 카메라 · UI만 남기고 코스를 새로 만들어 Stage01~10으로 저장합니다.
//  - 10개 스테이지는 하나로 이어지는 '상승'입니다. 각 스테이지는 앞 스테이지가 끝난 높이에서 시작합니다.
//  - 다시 누르면 10개 씬을 덮어씁니다(손으로 고친 씬은 이름을 바꿔 보관하세요).
public static partial class StageBuilder
{
    const string TemplateScene = "Assets/Scenes/Scenes_Level1.unity";
    const string StageFolder = "Assets/Scenes/Stages";
    const string MatFolder = "Assets/Art/Materials/Stages";
    const string FxFolder = "Assets/Resources/FX";
    const string CloudPrefabPath = "Assets/Prefabs/Cloud.prefab";

    const string SettingsPath = "Assets/Settings/ChickenFly/StageBuildSettings.asset";
    const string FeedbackSettingsPath = "Assets/Resources/FX/FeedbackSettings.asset";

    // 숫자는 StageBuildSettings 에셋에서 읽습니다(인스펙터에서 조절).
    static StageBuildSettings S;
    // 복도 크기: 폭(Z), 바닥에서 천장까지 높이(바닥에 수직)
    static float W = 24f;
    static float H = 26f;

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

    static StageBuildSettings LoadOrCreateSettings()
    {
        StageBuildSettings s = AssetDatabase.LoadAssetAtPath<StageBuildSettings>(SettingsPath);
        if (s == null)
        {
            EnsureFolder("Assets/Settings/ChickenFly");
            s = ScriptableObject.CreateInstance<StageBuildSettings>();
            AssetDatabase.CreateAsset(s, SettingsPath);
        }

        // 스테이지 목록이 비어 있으면 코드의 기본값으로 채워 둡니다.
        if (s.stages == null || s.stages.Count == 0)
        {
            S = s;
            s.stages = new List<StageEntry>();
            foreach (StageDef d in DefineStages())
            {
                StageEntry e = new StageEntry { number = d.number, title = d.title, hint = d.hint, difficulty = d.difficulty };
                e.segments.AddRange(d.course.segs);
                s.stages.Add(e);
            }
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

    // 설정 에셋의 스테이지 값(이름 · 힌트 · 난이도 · 비탈 구간)을 코드 정의에 덮어씁니다.
    static void ApplyOverrides(List<StageDef> defs)
    {
        foreach (StageDef d in defs)
        {
            StageEntry e = S.stages.Find(x => x.number == d.number);
            if (e == null) continue;
            if (!string.IsNullOrEmpty(e.title)) d.title = e.title;
            d.hint = e.hint;
            d.difficulty = e.difficulty;
            if (e.segments != null && e.segments.Count > 0)
            {
                d.course.segs.Clear();
                d.course.segs.AddRange(e.segments);
            }
        }
    }

    // 난이도(0~1)에 맞춰 범위(x=쉬움, y=어려움) 사이 값을 고릅니다.
    static float R(Vector2 range, float d01) => Mathf.Lerp(range.x, range.y, d01);

    static readonly string[] KeepRoots = { "Player", "Main Camera", "CinemachineCamera", "EventSystem", "Directional Light", "Global Volume" };
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

    [MenuItem("Chicken Fly/Build 10 Stages")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureTags();
        EnsureFolder(StageFolder);
        EnsureFolder(MatFolder);
        EnsureFolder(FxFolder);
        BuildFxAssets();
        LoadOrCreateFeedbackSettings();

        S = LoadOrCreateSettings();
        W = S.corridorWidth;
        H = S.corridorHeight;

        List<StageDef> stages = DefineStages();
        ApplyOverrides(stages);
        float altitude = 0f;
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();

        for (int i = 0; i < stages.Count; i++)
        {
            StageDef def = stages[i];
            def.course.baseY = altitude;
            string path = $"{StageFolder}/Stage{def.number:00}.unity";
            BuildStage(def, path);
            altitude = def.course.EndY;
            buildScenes.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(buildScenes[0].path, OpenSceneMode.Single);
        Debug.Log($"[StageBuilder] 스테이지 {stages.Count}개 생성 완료. 마지막 높이 {altitude:0}m");
    }

    // ───────────────────────── 코스(비탈 복도) ─────────────────────────

    class Course
    {
        public float baseY;
        public readonly List<Vector2> segs = new List<Vector2>();   // (가로 길이, 오르막 각도°)

        public Course Seg(float length, float angle)
        {
            segs.Add(new Vector2(length, angle));
            return this;
        }

        public float Length
        {
            get
            {
                float l = 0f;
                foreach (Vector2 s in segs) l += s.x;
                return l;
            }
        }

        public float EndY => FloorY(Length);

        public float FloorY(float x)
        {
            float y = baseY;
            float x0 = 0f;
            foreach (Vector2 s in segs)
            {
                float dx = Mathf.Clamp(x - x0, 0f, s.x);
                y += dx * Mathf.Tan(s.y * Mathf.Deg2Rad);
                x0 += s.x;
                if (x <= x0) break;
            }
            return y;
        }

        public float AngleAt(float x)
        {
            float x0 = 0f;
            foreach (Vector2 s in segs)
            {
                if (x <= x0 + s.x) return s.y;
                x0 += s.x;
            }
            return segs.Count > 0 ? segs[segs.Count - 1].y : 0f;
        }

        // 그 위치에서 바닥~천장 세로 높이(비탈이면 H/cos)
        public float VerticalHeight(float x) => H / Mathf.Cos(AngleAt(x) * Mathf.Deg2Rad);

        public Vector3 P(float x, float heightAboveFloor, float z) => new Vector3(x, FloorY(x) + heightAboveFloor, z);
    }

    class Theme
    {
        public string name;
        public Color floor, wall, ceiling, obstacle, edge, light, fog, skyTint, ground;
        public float lightIntensity = 1.2f, exposure = 1.2f, fogStart = 60f, fogEnd = 260f;
        public bool stars, cloudFloor, neonEdges;
    }

    class StageDef
    {
        public int number;
        public string title;
        public string hint;
        public float difficulty;      // 1~10: 구멍 크기 · 속도 · 밀도에 씀
        public Theme theme;
        public Course course;
        public System.Action<Ctx> content;
    }

    // 스테이지를 만드는 동안 쓰는 도구 모음
    class Ctx
    {
        public StageDef def;
        public Course c;
        public Theme t;
        public Transform root;
        public Material floorMat, wallMat, ceilMat, obstacleMat, hazardMat, edgeMat, triggerMat;
        public GameObject cloudPrefab;
        public int checkpointCount;

        public float D01 => Mathf.Clamp01((def.difficulty - 1f) / 7f);
        public float Hole => R(S.holeSize, D01);           // 구멍 크기(m)
        public float Period => R(S.movePeriod, D01);       // 움직이는 장애물 왕복 시간
        public float Spin => R(S.windmillSpeed, D01);      // 풍차 회전 속도
        public float Swing => R(S.pendulumPeriod, D01);    // 진자 왕복 시간
        public float Lift => R(S.windLift, D01);           // 상승 · 하강풍
        public float Cross => R(S.crossWind, D01);         // 옆바람
    }

    // ───────────────────────── 스테이지 정의 ─────────────────────────
    // 난이도: 1→6 상승, 7·8은 '어려워 보이지만 한 길만 따라가면 다 피해지는' 숨 고르기, 9·10은 6보다 높게.

    static List<StageDef> DefineStages()
    {
        List<StageDef> list = new List<StageDef>();

        list.Add(new StageDef
        {
            number = 1, title = "First Flap", hint = "SPACE = FLAP   W = GO   S = STOP", difficulty = 1f,
            theme = Themes.Meadow,
            course = new Course().Seg(30, 0).Seg(60, 12).Seg(40, 0).Seg(50, 18).Seg(35, 0),
            content = ctx =>
            {
                Sign(ctx, 8f, 14f, 0f, "SPACE : FLAP", 3f);
                Sign(ctx, 20f, 9f, 0f, "W : GO     S : STOP", 2.2f);
                Gate(ctx, 45f, 2f, 2f + ctx.Hole, -10f, 10f);          // 아래로 지나가기
                Gate(ctx, 110f, 11f, 11f + ctx.Hole, -10f, 10f);       // 위로 지나가기
                Sign(ctx, 125f, 16f, 0f, "MOUSE : TURN", 3f);
                Gate(ctx, 150f, 4f, 20f, -11f, -11f + ctx.Hole);       // 왼쪽
                Gate(ctx, 178f, 4f, 20f, 11f - ctx.Hole, 11f);         // 오른쪽
            }
        });

        list.Add(new StageDef
        {
            number = 2, title = "Growing Forest", hint = "Trees grow when you pass by", difficulty = 2f,
            theme = Themes.Forest,
            course = new Course().Seg(20, 0).Seg(80, 15).Seg(30, 0).Seg(80, 20).Seg(30, 0),
            content = ctx =>
            {
                float[] zs = { -6f, 5f, -3f, 7f, -7f, 2f };
                for (int i = 0; i < zs.Length; i++) Tree(ctx, 32f + i * 11f, zs[i], 1f);
                Sign(ctx, 112f, 15f, 0f, "WHAT A NICE TREE!", 2.6f);
                List<Transform> grow = new List<Transform>
                {
                    Tree(ctx, 128f, -5f, 0.9f), Tree(ctx, 136f, 6f, 0.9f), Tree(ctx, 144f, -1f, 0.9f)
                };
                foreach (Transform g in grow) GrowTriggerFor(ctx, 118f, g, new Vector3(1.5f, 2f, 1.5f));
                float[] zs2 = { 6f, -6f, 0f, -8f, 8f, -2f, 4f };
                for (int i = 0; i < zs2.Length; i++) Tree(ctx, 158f + i * 9f, zs2[i], 1.1f);
                // 끝 무렵 천장 쪽에 구름 몇 조각: 곧 구름 높이에 닿는다는 예고
                CloudCluster(ctx, 215f, H - 4f, -6f, 0.7f);
                CloudCluster(ctx, 228f, H - 3f, 5f, 0.6f);
            }
        });

        list.Add(new StageDef
        {
            number = 3, title = "Windy Cliffs", hint = "Ride the wind", difficulty = 3f,
            theme = Themes.Cliffs,
            course = new Course().Seg(20, 0).Seg(60, 28).Seg(25, 0).Seg(60, 30).Seg(25, 0).Seg(40, 15).Seg(25, 0),
            content = ctx =>
            {
                float lift = ctx.Lift;
                Wind(ctx, 22f, 78f, new Vector3(0f, lift, 0f), "UPDRAFT");            // 비탈을 도와주는 상승풍
                Wind(ctx, 108f, 165f, new Vector3(0f, lift * 0.9f, 0f), "UPDRAFT");
                Pillar(ctx, 125f, -6f, 3f); Pillar(ctx, 140f, 6f, 3f); Pillar(ctx, 155f, -2f, 3f);
                Wind(ctx, 172f, 205f, new Vector3(0f, 0f, ctx.Cross), "CROSSWIND");   // 옆바람
                Pillar(ctx, 185f, 7f, 3f); Pillar(ctx, 198f, 2f, 3f);
                Wind(ctx, 212f, 235f, new Vector3(0f, -lift, 0f), "DOWNDRAFT");         // 하강풍 + 아래 구멍
                Gate(ctx, 228f, 2f, 2f + ctx.Hole, -10f, 10f);
            }
        });

        list.Add(new StageDef
        {
            number = 4, title = "Moving Rocks", hint = "Watch the rhythm", difficulty = 4f,
            theme = Themes.Mountain,
            course = new Course().Seg(20, 0).Seg(70, 20).Seg(40, 0).Seg(70, 22).Seg(40, 0).Seg(30, 0),
            content = ctx =>
            {
                float period = ctx.Period;
                SlidingPillar(ctx, 40f, period, 0f); SlidingPillar(ctx, 55f, period, 0.33f); SlidingPillar(ctx, 70f, period, 0.66f);
                LiftBar(ctx, 100f, period, 0f); LiftBar(ctx, 115f, period, 0.5f);
                ClosingWalls(ctx, 150f, period * 1.2f, 0f); ClosingWalls(ctx, 175f, period * 1.2f, 0.5f);
                SlidingPillar(ctx, 205f, period * 0.9f, 0.2f); SlidingPillar(ctx, 215f, period * 0.9f, 0.7f);
                // 끝 무렵: 구름이 바닥까지 내려와 있음 = 구름 높이에 도착
                Sign(ctx, 228f, 16f, 0f, "CLOUD LEVEL", 3f);
                for (float x = 232f; x < ctx.c.Length - 8f; x += 9f) CloudFloorRow(ctx, x);
            }
        });

        list.Add(new StageDef
        {
            number = 5, title = "Windmill Clouds", hint = "Find the gap", difficulty = 5f,
            theme = Themes.CloudLayer,
            course = new Course().Seg(20, 0).Seg(60, 18).Seg(40, 0).Seg(60, 20).Seg(40, 0).Seg(50, 15).Seg(30, 0),
            content = ctx =>
            {
                CloudSea(ctx);
                float spin = ctx.Spin;
                Windmill(ctx, 45f, spin); Windmill(ctx, 105f, -spin);
                Pendulum(ctx, 75f, ctx.Swing, 0f);
                Checkpoint(ctx, 128f);
                Pendulum(ctx, 150f, ctx.Swing, 0.25f);
                Pendulum(ctx, 162f, ctx.Swing, 0.75f);
                Windmill(ctx, 195f, spin * 1.1f);
                // 위에서 구름이 내려와 위쪽 길을 막음 → 아래로 지나가기
                DescendingCloud(ctx, 215f, 238f, 0f, -11f);
                DescendingCloud(ctx, 225f, 250f, 6f, -11f);
            }
        });

        list.Add(new StageDef
        {
            number = 6, title = "Spear Rain", hint = "Something falls from above", difficulty = 6f,
            theme = Themes.InsideClouds,
            course = new Course().Seg(20, 0).Seg(60, 22).Seg(30, 0).Seg(60, 24).Seg(30, 0).Seg(60, 20).Seg(25, 0),
            content = ctx =>
            {
                CloudSea(ctx);
                SpearDrop(ctx, 40f, 58f, 86f, 6);
                Gate(ctx, 95f, 6f, 6f + ctx.Hole, -4f, 4f + ctx.Hole * 0.3f);
                Checkpoint(ctx, 112f);
                SpearDrop(ctx, 120f, 136f, 166f, 8);
                DescendingCloud(ctx, 172f, 195f, -5f, -10f);
                SlidingPillar(ctx, 205f, ctx.Period, 0f);
                SpearDrop(ctx, 210f, 222f, 250f, 7);
                Gate(ctx, 262f, 3f, 3f + ctx.Hole, 2f - ctx.Hole, 2f);
            }
        });

        list.Add(new StageDef
        {
            // 숨 고르기: 사방에서 구름이 솟고 내려오지만 가운데 높이의 '링 길'은 끝까지 비어 있음
            number = 7, title = "Above the Clouds", hint = "Follow the golden rings", difficulty = 4.5f,
            theme = Themes.AboveClouds,
            course = new Course().Seg(20, 0).Seg(80, 12).Seg(40, 0).Seg(80, 12).Seg(40, 0).Seg(40, 0),
            content = ctx =>
            {
                CloudSea(ctx);
                const float laneH = 13f;   // 안전한 길: 바닥에서 13m, Z 0 (±5m)
                for (float x = 25f; x < ctx.c.Length - 25f; x += 22f)
                {
                    BoostRing(ctx, x, laneH, 0f, S.boostRingSpeed);
                    // 아래에서 솟는 구름(가운데는 바닥+7m까지만), 위에서 내려오는 구름(바닥+19m까지만), 양옆 구름
                    RisingCloud(ctx, x - 14f, x + 6f, 0f, -5f, 9f);
                    DescendingCloud(ctx, x - 12f, x + 9f, 0f, -4f);
                    RisingCloud(ctx, x - 10f, x + 4f, -10.5f, 0f, 16f);
                    RisingCloud(ctx, x - 10f, x + 11f, 10.5f, 0f, 16f);
                }
                for (float x = 60f; x < ctx.c.Length - 40f; x += 70f) BouncePad(ctx, x, 0f, S.bounceLaunchSpeed);
            }
        });

        list.Add(new StageDef
        {
            // 숨 고르기: 번개가 정신없이 깜빡이지만 W를 꾹 누르고 직진하면 지나가는 순간마다 꺼져 있음
            number = 8, title = "Thunder Storm", hint = "Don't stop. Never stop.", difficulty = 5f,
            theme = Themes.Storm,
            course = new Course().Seg(20, 0).Seg(120, 8).Seg(40, 0).Seg(100, 8).Seg(30, 0),
            content = ctx =>
            {
                for (float x = 32f; x < ctx.c.Length - 22f; x += 12f)
                {
                    SyncGate(ctx, x - 6f);
                    LightningRow(ctx, x, syncSpeed: S.syncedLightningSpeed);
                }
            }
        });

        list.Add(new StageDef
        {
            number = 9, title = "Upside-Down Space", hint = "The world turns over", difficulty = 7f,
            theme = Themes.Space,
            course = new Course().Seg(20, 0).Seg(40, 0).Seg(60, 20).Seg(40, 0).Seg(60, 25).Seg(40, 0).Seg(50, 20).Seg(30, 0),
            content = ctx =>
            {
                FlipZone(ctx, 35f, true);
                Checkpoint(ctx, 80f);
                Gate(ctx, 95f, 12f, 12f + ctx.Hole, -9f, 9f);
                LiftBar(ctx, 118f, ctx.Period, 0f);
                Windmill(ctx, 140f, ctx.Spin);
                Wind(ctx, 160f, 180f, new Vector3(0f, -16f, 0f), "GRAVITY WELL");
                Gate(ctx, 175f, 2f, 2f + ctx.Hole, -2f, 2f + ctx.Hole * 0.5f);
                Checkpoint(ctx, 205f);
                Pendulum(ctx, 230f, ctx.Swing, 0f);
                SlidingPillar(ctx, 250f, ctx.Period, 0.3f); SlidingPillar(ctx, 262f, ctx.Period, 0.8f);
                Gate(ctx, 285f, 10f, 10f + ctx.Hole, 3f, 3f + ctx.Hole);
            }
        });

        list.Add(new StageDef
        {
            number = 10, title = "To the Stars", hint = "Everything you learned", difficulty = 8f,
            theme = Themes.DeepSpace,
            course = new Course().Seg(20, 0).Seg(30, 0).Seg(60, 22).Seg(40, 0).Seg(60, 25).Seg(40, 0).Seg(60, 28).Seg(40, 0).Seg(50, 15).Seg(30, 0).Seg(40, 0),
            content = ctx =>
            {
                FlipZone(ctx, 38f, true);
                Checkpoint(ctx, 85f);
                Wind(ctx, 95f, 125f, new Vector3(0f, 0f, -14f), "SOLAR WIND");
                Pillar(ctx, 108f, -7f, 3f); Pillar(ctx, 120f, 3f, 3f);
                Windmill(ctx, 140f, ctx.Spin);
                Pendulum(ctx, 160f, ctx.Swing, 0.2f);
                for (int i = 0; i < 4; i++) LightningRow(ctx, 180f + i * 10f, syncSpeed: 0f, phase: i * 0.45f);
                Checkpoint(ctx, 228f);
                ClosingWalls(ctx, 250f, ctx.Period, 0f);
                BoostRing(ctx, 270f, 13f, 0f, 17f);
                SlidingPillar(ctx, 290f, ctx.Period * 0.85f, 0f); SlidingPillar(ctx, 300f, ctx.Period * 0.85f, 0.5f);
                Gate(ctx, 320f, 14f, 14f + ctx.Hole, -10f, -10f + ctx.Hole);
                Checkpoint(ctx, 345f);
                FlipZone(ctx, 365f, false);   // 마지막: 세상이 다시 돌아오고 최종 상승
                LiftBar(ctx, 395f, ctx.Period, 0.25f);
                Gate(ctx, 415f, 4f, 4f + ctx.Hole, -5f, 5f);
                NeonCorridor(ctx, 425f, ctx.c.Length);
            }
        });

        return list;
    }

    static class Themes
    {
        static Color C(float r, float g, float b) => new Color(r, g, b);

        public static readonly Theme Meadow = new Theme
        {
            name = "Meadow", floor = C(0.36f, 0.72f, 0.28f), wall = C(0.62f, 0.85f, 1f), ceiling = C(0.55f, 0.8f, 1f),
            obstacle = C(0.86f, 0.6f, 0.34f), edge = C(1f, 1f, 1f), light = C(1f, 0.96f, 0.88f), fog = C(0.78f, 0.9f, 1f),
            skyTint = C(0.62f, 0.91f, 1f), ground = C(0.4f, 0.6f, 0.3f)
        };
        public static readonly Theme Forest = new Theme
        {
            name = "Forest", floor = C(0.26f, 0.52f, 0.22f), wall = C(0.56f, 0.8f, 0.98f), ceiling = C(0.5f, 0.76f, 0.98f),
            obstacle = C(0.55f, 0.36f, 0.2f), edge = C(0.95f, 1f, 0.9f), light = C(1f, 0.95f, 0.85f), fog = C(0.72f, 0.88f, 0.98f),
            skyTint = C(0.62f, 0.91f, 1f), ground = C(0.3f, 0.5f, 0.25f)
        };
        public static readonly Theme Cliffs = new Theme
        {
            name = "Cliffs", floor = C(0.56f, 0.5f, 0.45f), wall = C(0.47f, 0.72f, 0.96f), ceiling = C(0.44f, 0.68f, 0.95f),
            obstacle = C(0.78f, 0.55f, 0.38f), edge = C(1f, 1f, 1f), light = C(1f, 0.95f, 0.85f), fog = C(0.7f, 0.84f, 0.98f),
            skyTint = C(0.45f, 0.82f, 1f), ground = C(0.5f, 0.45f, 0.4f)
        };
        public static readonly Theme Mountain = new Theme
        {
            name = "Mountain", floor = C(0.78f, 0.79f, 0.82f), wall = C(0.4f, 0.63f, 0.93f), ceiling = C(0.37f, 0.6f, 0.92f),
            obstacle = C(0.55f, 0.52f, 0.6f), edge = C(1f, 1f, 1f), light = C(1f, 0.97f, 0.92f), fog = C(0.75f, 0.85f, 0.98f),
            skyTint = C(0.2f, 0.72f, 0.99f), ground = C(0.7f, 0.7f, 0.75f)
        };
        public static readonly Theme CloudLayer = new Theme
        {
            name = "CloudLayer", floor = C(0.93f, 0.95f, 1f), wall = C(0.66f, 0.78f, 0.95f), ceiling = C(0.6f, 0.75f, 0.95f),
            obstacle = C(0.95f, 0.62f, 0.35f), edge = C(1f, 1f, 1f), light = C(1f, 0.98f, 0.93f), fog = C(0.9f, 0.93f, 1f),
            skyTint = C(0.2f, 0.72f, 0.99f), ground = C(0.9f, 0.92f, 1f), cloudFloor = true, fogStart = 40f, fogEnd = 200f
        };
        public static readonly Theme InsideClouds = new Theme
        {
            name = "InsideClouds", floor = C(0.8f, 0.82f, 0.88f), wall = C(0.52f, 0.57f, 0.7f), ceiling = C(0.48f, 0.52f, 0.66f),
            obstacle = C(0.9f, 0.55f, 0.3f), edge = C(0.95f, 0.97f, 1f), light = C(0.9f, 0.92f, 1f), fog = C(0.7f, 0.73f, 0.8f),
            skyTint = C(0.1f, 0.5f, 0.8f), ground = C(0.7f, 0.72f, 0.8f), cloudFloor = true, lightIntensity = 0.9f, fogStart = 30f, fogEnd = 170f
        };
        public static readonly Theme AboveClouds = new Theme
        {
            name = "AboveClouds", floor = C(1f, 0.93f, 0.84f), wall = C(0.96f, 0.66f, 0.56f), ceiling = C(0.62f, 0.52f, 0.86f),
            obstacle = C(0.98f, 0.5f, 0.35f), edge = C(1f, 0.9f, 0.6f), light = C(1f, 0.97f, 0.9f), fog = C(0.45f, 0.75f, 0.95f),
            skyTint = C(0.01f, 0.33f, 0.51f), ground = C(1f, 0.9f, 0.8f), cloudFloor = true, fogStart = 50f, fogEnd = 240f
        };
        public static readonly Theme Storm = new Theme
        {
            name = "Storm", floor = C(0.33f, 0.35f, 0.44f), wall = C(0.2f, 0.22f, 0.34f), ceiling = C(0.16f, 0.17f, 0.28f),
            obstacle = C(0.6f, 0.6f, 0.7f), edge = C(0.6f, 0.75f, 1f), light = C(0.7f, 0.75f, 1f), fog = C(0.14f, 0.15f, 0.24f),
            skyTint = C(0.01f, 0.2f, 0.33f), ground = C(0.2f, 0.2f, 0.25f), cloudFloor = true, lightIntensity = 0.7f, exposure = 0.6f, fogStart = 20f, fogEnd = 150f
        };
        public static readonly Theme Space = new Theme
        {
            name = "Space", floor = C(0.13f, 0.1f, 0.22f), wall = C(0.05f, 0.04f, 0.12f), ceiling = C(0.06f, 0.04f, 0.14f),
            obstacle = C(0.55f, 0.45f, 0.95f), edge = C(0.75f, 0.45f, 1f), light = C(0.85f, 0.8f, 1f), fog = C(0.03f, 0.02f, 0.08f),
            skyTint = C(0.05f, 0.03f, 0.12f), ground = C(0.02f, 0.02f, 0.05f), stars = true, neonEdges = true,
            lightIntensity = 0.9f, exposure = 0.25f, fogStart = 80f, fogEnd = 320f
        };
        public static readonly Theme DeepSpace = new Theme
        {
            name = "DeepSpace", floor = C(0.08f, 0.06f, 0.16f), wall = C(0.03f, 0.02f, 0.08f), ceiling = C(0.04f, 0.02f, 0.1f),
            obstacle = C(0.95f, 0.4f, 0.75f), edge = C(1f, 0.4f, 0.8f), light = C(0.85f, 0.8f, 1f), fog = C(0.02f, 0.01f, 0.06f),
            skyTint = C(0.03f, 0.02f, 0.08f), ground = C(0.01f, 0.01f, 0.03f), stars = true, neonEdges = true,
            lightIntensity = 0.9f, exposure = 0.2f, fogStart = 80f, fogEnd = 340f
        };
    }

    // ───────────────────────── 스테이지 만들기 ─────────────────────────

    static void BuildStage(StageDef def, string path)
    {
        Scene scene = EditorSceneManager.OpenScene(TemplateScene, OpenSceneMode.Single);
        Random.InitState(def.number * 7919);   // 다시 만들어도 같은 배치

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (System.Array.IndexOf(KeepRoots, root.name) < 0)
            {
                Object.DestroyImmediate(root);
            }
        }

        Ctx ctx = new Ctx { def = def, c = def.course, t = def.theme };
        ctx.root = new GameObject($"Stage{def.number:00}_{def.title.Replace(' ', '_')}").transform;
        ctx.cloudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CloudPrefabPath);

        Theme t = def.theme;
        ctx.triggerMat = Mat("Trigger_Invisible", new Color(1f, 1f, 1f, 0f), 0f);

        SetupLighting(ctx);
        BuildEnvironment(ctx);       // 절벽 · 지형 · 배경 · 후처리 (StageBuilder.Environment.cs)
        BuildStartAndGoal(ctx);
        def.content?.Invoke(ctx);
        if (t.stars) StarField(ctx);
        SetupPlayerCameraUi(ctx);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, path);
    }

    static void BuildStartAndGoal(Ctx ctx)
    {
        Course c = ctx.c;
        // 출발 발판(닿아도 안전: 이름이 FirstGroundStart)
        // 테마별 출발 발판: 땅 = 나무 둥지, 구름 = 구름 발판, 우주 = 네온 발판
        Look look = LookFor(ctx.t.name);
        Material padMat = look.wall == WallStyle.Rock
            ? TexMat("Bark_Generated", C(0.39f, 0.24f, 0f), C(0.68f, 0.43f, 0.02f), 8f, 12f, 0.15f, 0.1f, 1.5f)
            : look.wall == WallStyle.Cloud
                ? Mat("StartPad_Cloud", C(1f, 1f, 1f), 0.2f, C(0.15f, 0.2f, 0.3f))
                : NeonMat("StartPad_Neon", C(0.48f, 0.99f, 0.2f), 1.4f);
        GameObject pad = RemoveCollider(GameObject.CreatePrimitive(PrimitiveType.Cylinder));
        pad.name = "FirstGroundStart";
        pad.transform.position = new Vector3(0f, c.baseY + 0.25f, 0f);
        pad.transform.localScale = new Vector3(6f, 0.75f, 6f);
        pad.GetComponent<Renderer>().sharedMaterial = padMat;
        BoxCollider padCol = pad.AddComponent<BoxCollider>();   // 위가 평평한 상자 충돌(원기둥 메시 충돌보다 안정적)
        padCol.size = new Vector3(1f, 2f, 1f);
        if (look.wall == WallStyle.Rock)
        {
            // 둥지 테두리: 짚 색 고리
            Material straw = Mat("Nest_Straw", C(0.94f, 0.8f, 0.35f), 0.1f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                GameObject twig = RemoveCollider(GameObject.CreatePrimitive(PrimitiveType.Capsule));
                twig.name = "NestTwig";
                twig.transform.SetParent(pad.transform, true);
                twig.transform.position = new Vector3(Mathf.Cos(a) * 2.9f, c.baseY + 0.75f, Mathf.Sin(a) * 2.9f);
                twig.transform.rotation = Quaternion.Euler(90f, -a * Mathf.Rad2Deg, 0f);
                twig.transform.localScale = new Vector3(0.12f, 0.25f, 0.12f) * 1f;
                twig.GetComponent<Renderer>().sharedMaterial = straw;
            }
        }

        // 도착 게이트
        float gx = c.Length - 12f;
        float gy = c.FloorY(gx);
        Material goldMat = Mat("Goal_Gold", new Color(1f, 0.84f, 0.3f), 0.8f, new Color(1f, 0.75f, 0.2f) * 1.6f);
        Transform goal = Group(ctx, "Goal");
        GameObject trigger = Box(goal, "Ending_Trigger", new Vector3(gx, gy + H / 2f, 0f), new Vector3(2f, H, W), 0f, ctx.triggerMat);
        trigger.GetComponent<Collider>().isTrigger = true;
        trigger.GetComponent<Renderer>().enabled = false;
        trigger.tag = "Ending";
        trigger.AddComponent<EndingCredits>();

        foreach (float z in new[] { -W / 2f + 1.2f, W / 2f - 1.2f })
        {
            RemoveCollider(Box(goal, "Goal_Pillar", new Vector3(gx, gy + H / 2f, z), new Vector3(1.6f, H, 1.6f), 0f, goldMat)).tag = "Ending";
        }
        RemoveCollider(Box(goal, "Goal_Top", new Vector3(gx, gy + H - 1.5f, 0f), new Vector3(1.6f, 1.6f, W - 2f), 0f, goldMat)).tag = "Ending";
        Sign(ctx, gx - 0.5f, H - 5f, 0f, "GOAL", 4f, goal).tag = "Ending";
    }

    // ───────────────────────── 기믹 조립 ─────────────────────────

    // 구멍 뚫린 벽: (y0~y1: 바닥에서 높이, z0~z1) 자리가 비어 있음
    static void Gate(Ctx ctx, float x, float y0, float y1, float z0, float z1)
    {
        Transform g = Group(ctx, $"Gate_{x:0}");
        float floor = ctx.c.FloorY(x);
        float top = ctx.c.VerticalHeight(x);
        float th = 1.6f;
        y0 = Mathf.Clamp(y0, 0f, top); y1 = Mathf.Clamp(y1, y0, top);
        z0 = Mathf.Clamp(z0, -W / 2f, W / 2f); z1 = Mathf.Clamp(z1, z0, W / 2f);

        if (y0 > 0.01f) Box(g, "Gate_Bottom", new Vector3(x, floor + y0 / 2f, 0f), new Vector3(th, y0, W), 0f, ctx.obstacleMat);
        if (y1 < top - 0.01f) Box(g, "Gate_Top", new Vector3(x, floor + (y1 + top) / 2f, 0f), new Vector3(th, top - y1, W), 0f, ctx.obstacleMat);
        float hy = y1 - y0;
        if (z0 > -W / 2f + 0.01f) Box(g, "Gate_Left", new Vector3(x, floor + (y0 + y1) / 2f, (-W / 2f + z0) / 2f), new Vector3(th, hy, z0 + W / 2f), 0f, ctx.obstacleMat);
        if (z1 < W / 2f - 0.01f) Box(g, "Gate_Right", new Vector3(x, floor + (y0 + y1) / 2f, (z1 + W / 2f) / 2f), new Vector3(th, hy, W / 2f - z1), 0f, ctx.obstacleMat);

        // 구멍 테두리 빛: 어디로 지나가야 하는지 한눈에
        Material rim = Mat("Gate_Rim", new Color(1f, 0.95f, 0.6f), 0.5f, new Color(1f, 0.9f, 0.4f) * 1.2f);
        float cy = floor + (y0 + y1) / 2f, cz = (z0 + z1) / 2f;
        RemoveCollider(Box(g, "Rim_Top", new Vector3(x - th / 2f - 0.05f, floor + y1, cz), new Vector3(0.1f, 0.25f, z1 - z0), 0f, rim));
        RemoveCollider(Box(g, "Rim_Bottom", new Vector3(x - th / 2f - 0.05f, floor + y0, cz), new Vector3(0.1f, 0.25f, z1 - z0), 0f, rim));
        RemoveCollider(Box(g, "Rim_Left", new Vector3(x - th / 2f - 0.05f, cy, z0), new Vector3(0.1f, hy, 0.25f), 0f, rim));
        RemoveCollider(Box(g, "Rim_Right", new Vector3(x - th / 2f - 0.05f, cy, z1), new Vector3(0.1f, hy, 0.25f), 0f, rim));
    }

    static void Pillar(Ctx ctx, float x, float z, float size)
    {
        float top = ctx.c.VerticalHeight(x);
        Box(Group(ctx, $"Pillar_{x:0}"), "Pillar", ctx.c.P(x, top / 2f, z), new Vector3(size, top, size), 0f, ctx.obstacleMat);
    }

    static Transform Tree(Ctx ctx, float x, float z, float scale)
    {
        Material bark = Mat("Tree_Bark", new Color(0.45f, 0.29f, 0.14f), 0.1f);
        Material leaf = Mat("Tree_Leaf", new Color(0.3f, 0.62f, 0.22f), 0.15f);
        Material leaf2 = Mat("Tree_Leaf_Light", new Color(0.45f, 0.75f, 0.3f), 0.15f);
        GameObject tree = new GameObject($"Tree_{x:0}");
        tree.transform.SetParent(Group(ctx, "Trees"), false);
        tree.transform.position = ctx.c.P(x, 0f, z);
        tree.transform.localScale = Vector3.one * scale;

        GameObject trunk = Prim(PrimitiveType.Cylinder, tree.transform, "Trunk", bark);
        trunk.transform.localPosition = new Vector3(0f, 4f, 0f);
        trunk.transform.localScale = new Vector3(1.4f, 4f, 1.4f);
        GameObject crown = Prim(PrimitiveType.Sphere, tree.transform, "Crown", leaf);
        crown.transform.localPosition = new Vector3(0f, 9.5f, 0f);
        crown.transform.localScale = new Vector3(6.5f, 6f, 6.5f);
        GameObject crown2 = Prim(PrimitiveType.Sphere, tree.transform, "Crown_Top", leaf2);
        crown2.transform.localPosition = new Vector3(0.6f, 12.3f, -0.4f);
        crown2.transform.localScale = new Vector3(4f, 3.6f, 4f);
        return tree.transform;
    }

    static void GrowTriggerFor(Ctx ctx, float x, Transform target, Vector3 multiplier)
    {
        GameObject trig = FullTrigger(ctx, $"GrowTrigger_{target.name}", x, 2f);
        GrowTrigger grow = trig.AddComponent<GrowTrigger>();
        SetField(grow, "targetObject", target.gameObject);
        SetField(grow, "scaleMultiplier", multiplier);
    }

    static void Wind(Ctx ctx, float xa, float xb, Vector3 accel, string label)
    {
        float len = xb - xa;
        float xm = (xa + xb) / 2f;
        GameObject trig = FullTrigger(ctx, $"Wind_{label}_{xa:0}", xm, len);
        WindArea wind = trig.AddComponent<WindArea>();
        SetField(wind, "acceleration", accel);

        // 바람 줄무늬 파티클: 바람 방향으로 흐름
        GameObject fx = new GameObject("WindStreaks");
        fx.transform.SetParent(trig.transform.parent, false);
        fx.transform.position = ctx.c.P(xm, H / 2f, 0f);
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.startLifetime = 1.6f;
        main.startSpeed = 0f;
        main.startSize = 0.25f;
        main.startColor = new Color(1f, 1f, 1f, 0.55f);
        main.maxParticles = 400;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.prewarm = true;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = Mathf.Clamp(len * 3f, 40f, 220f);
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(len, H - 2f, W - 2f);
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        Vector3 dir = accel.normalized * 9f;
        vel.x = dir.x; vel.y = dir.y; vel.z = dir.z;
        ParticleSystemRenderer r = fx.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.35f;
        r.lengthScale = 2f;
        r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{FxFolder}/FX_Particle.mat");

        Sign(ctx, xa + 3f, H * 0.75f, 0f, label, 2.2f);
    }

    static void SlidingPillar(Ctx ctx, float x, float period, float phase)
    {
        float top = ctx.c.VerticalHeight(x);
        GameObject p = Box(Group(ctx, $"SlidingPillar_{x:0}"), "Pillar", ctx.c.P(x, top / 2f, -8f), new Vector3(3f, top, 3f), 0f, ctx.hazardMat);
        Oscillator o = p.AddComponent<Oscillator>();
        SetField(o, "offset", new Vector3(0f, 0f, 16f));
        SetField(o, "period", period);
        SetField(o, "phase", phase);
    }

    static void LiftBar(Ctx ctx, float x, float period, float phase)
    {
        float top = ctx.c.VerticalHeight(x);
        GameObject p = Box(Group(ctx, $"LiftBar_{x:0}"), "Bar", ctx.c.P(x, 4f, 0f), new Vector3(2.5f, 3f, W), 0f, ctx.hazardMat);
        Oscillator o = p.AddComponent<Oscillator>();
        SetField(o, "offset", new Vector3(0f, top - 8f, 0f));
        SetField(o, "period", period);
        SetField(o, "phase", phase);
    }

    static void ClosingWalls(Ctx ctx, float x, float period, float phase)
    {
        Transform g = Group(ctx, $"ClosingWalls_{x:0}");
        float top = ctx.c.VerticalHeight(x);
        foreach (float side in new[] { -1f, 1f })
        {
            GameObject w = Box(g, side < 0 ? "Wall_Left" : "Wall_Right", ctx.c.P(x, top / 2f, side * (W / 2f + 3f)), new Vector3(2.5f, top, W / 2f), 0f, ctx.hazardMat);
            Oscillator o = w.AddComponent<Oscillator>();
            SetField(o, "offset", new Vector3(0f, 0f, -side * (W / 2f - 0.5f)));
            SetField(o, "period", period);
            SetField(o, "phase", phase);
        }
    }

    static void Windmill(Ctx ctx, float x, float speed)
    {
        Transform g = Group(ctx, $"Windmill_{x:0}");
        GameObject hub = new GameObject("Hub");
        hub.transform.SetParent(g, false);
        hub.transform.position = ctx.c.P(x, H / 2f, 0f);
        Spinner sp = hub.AddComponent<Spinner>();
        SetField(sp, "xAngle", speed);
        SetField(sp, "yAngle", 0f);
        SetField(sp, "zAngle", 0f);
        float blade = W - 3f;
        Box(hub.transform, "Blade_A", hub.transform.position, new Vector3(1f, blade, 2.2f), 0f, ctx.hazardMat);
        Box(hub.transform, "Blade_B", hub.transform.position, new Vector3(1f, 2.2f, blade), 0f, ctx.hazardMat);
        Material hubMat = Mat("Windmill_Hub", new Color(0.95f, 0.95f, 1f), 0.6f);
        GameObject cap = Prim(PrimitiveType.Sphere, hub.transform, "Cap", hubMat);
        cap.transform.position = hub.transform.position + Vector3.left * 0.8f;
        cap.transform.localScale = Vector3.one * 3f;
    }

    static void Pendulum(Ctx ctx, float x, float period, float phase)
    {
        Transform g = Group(ctx, $"Pendulum_{x:0}");
        GameObject pivot = new GameObject("Pivot");
        pivot.transform.SetParent(g, false);
        float top = ctx.c.VerticalHeight(x);
        pivot.transform.position = ctx.c.P(x, top, 0f);
        Pendulum pd = pivot.AddComponent<Pendulum>();
        SetField(pd, "axis", Vector3.right);
        SetField(pd, "amplitude", S.pendulumAmplitude);
        SetField(pd, "period", period);
        SetField(pd, "phase", phase);
        Material rod = Mat("Pendulum_Rod", new Color(0.8f, 0.8f, 0.85f), 0.7f);
        Box(pivot.transform, "Rod", pivot.transform.position + Vector3.down * 8f, new Vector3(0.5f, 16f, 0.5f), 0f, rod);
        GameObject ball = Prim(PrimitiveType.Sphere, pivot.transform, "Ball", ctx.hazardMat, keepCollider: true);
        ball.transform.position = pivot.transform.position + Vector3.down * 17f;
        ball.transform.localScale = Vector3.one * 6f;
    }

    static void SpearDrop(Ctx ctx, float triggerX, float fromX, float toX, int count)
    {
        Transform g = Group(ctx, $"SpearDrop_{triggerX:0}");
        GameObject trig = FullTrigger(ctx, "SpearTrigger", triggerX, 2f);
        trig.transform.SetParent(g, true);
        DropOnTrigger drop = trig.AddComponent<DropOnTrigger>();
        List<Transform> spears = new List<Transform>();
        Material spearMat = Mat("Spear_Red", new Color(0.85f, 0.15f, 0.12f), 0.7f, new Color(0.6f, 0.05f, 0.03f));
        float[] zs = { -8f, 4f, -2f, 9f, -6f, 1f, 7f, -10f, 3f };
        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Lerp(fromX, toX, count == 1 ? 0f : i / (float)(count - 1));
            float top = ctx.c.VerticalHeight(x);
            GameObject spear = Prim(PrimitiveType.Cylinder, g, $"Spear_{i}", spearMat, keepCollider: true);
            spear.transform.position = ctx.c.P(x, top - 3f, zs[i % zs.Length]);
            spear.transform.localScale = new Vector3(0.8f, 3f, 0.8f);
            spears.Add(spear.transform);
        }
        SetField(drop, "targets", spears.ToArray());
        SetField(drop, "interval", R(S.spearInterval, ctx.D01));
    }

    // 아래에서 솟는 구름: x(트리거) → 구름 무리(cloudX, 바닥에서 시작) 가 rise만큼 솟음
    static void RisingCloud(Ctx ctx, float triggerX, float cloudX, float z, float startH, float rise)
    {
        Transform cloud = CloudCluster(ctx, cloudX, startH, z, 1f);
        CloudTrigger(ctx, triggerX, cloud, new Vector3(0f, rise, 0f));
    }

    // 위에서 내려오는 구름: 천장 근처에서 drop만큼 내려옴(drop은 음수)
    static void DescendingCloud(Ctx ctx, float triggerX, float cloudX, float z, float drop)
    {
        Transform cloud = CloudCluster(ctx, cloudX, ctx.c.VerticalHeight(cloudX) - 2f, z, 1f);
        CloudTrigger(ctx, triggerX, cloud, new Vector3(0f, drop, 0f));
    }

    static void CloudTrigger(Ctx ctx, float x, Transform cloud, Vector3 move)
    {
        GameObject trig = FullTrigger(ctx, $"CloudTrigger_{cloud.name}", x, 2f);
        RisingCloudTrigger rc = trig.AddComponent<RisingCloudTrigger>();
        SetField(rc, "targetObject", cloud.gameObject);
        SetField(rc, "positionPlus", move);
        SetField(rc, "targetTime", R(S.cloudMoveTime, ctx.D01));
    }

    static Transform CloudCluster(Ctx ctx, float x, float heightAboveFloor, float z, float scale)
    {
        GameObject group = new GameObject($"Cloud_{x:0}_{z:0}");
        group.transform.SetParent(Group(ctx, "Clouds"), false);
        group.transform.position = ctx.c.P(x, heightAboveFloor, z);
        if (ctx.cloudPrefab != null)
        {
            GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(ctx.cloudPrefab, group.scene);
            c.transform.SetParent(group.transform, false);
            c.transform.localPosition = Vector3.zero;
            c.transform.localScale = Vector3.one * S.cloudScale * scale;
        }
        return group.transform;
    }

    // 구름층 바닥: 바닥을 구름으로 덮음(= 구름 위로 올라왔다)
    static void CloudSea(Ctx ctx)
    {
        for (float x = 6f; x < ctx.c.Length - 4f; x += 9f) CloudFloorRow(ctx, x);
    }

    static void CloudFloorRow(Ctx ctx, float x)
    {
        foreach (float z in new[] { -8f, 0f, 8f })
        {
            Transform c = CloudCluster(ctx, x + Random.Range(-2f, 2f), -1.5f, z + Random.Range(-1.5f, 1.5f), 1.1f);
            foreach (Collider col in c.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        }
    }

    static void BoostRing(Ctx ctx, float x, float h, float z, float speed)
    {
        Transform g = Group(ctx, $"BoostRing_{x:0}");
        GameObject trig = new GameObject("BoostRing");
        trig.transform.SetParent(g, false);
        trig.transform.position = ctx.c.P(x, h, z);
        trig.tag = "Trigger";
        BoxCollider box = trig.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.5f, 5.5f, 5.5f);
        BoostRing ring = trig.AddComponent<BoostRing>();
        SetField(ring, "boostSpeed", speed);

        // 링 모양: 반지름 3.4m 원 위에 금색 조각 14개(충돌 없음)
        Material gold = Mat("Boost_Gold", new Color(1f, 0.85f, 0.25f), 0.8f, new Color(1f, 0.75f, 0.15f) * 2.2f);
        GameObject visual = new GameObject("RingVisual");
        visual.transform.SetParent(trig.transform, false);
        const int seg = 14;
        for (int i = 0; i < seg; i++)
        {
            float ang = i * Mathf.PI * 2f / seg;
            GameObject b = RemoveCollider(GameObject.CreatePrimitive(PrimitiveType.Cube));
            b.name = $"Seg_{i}";
            b.tag = "Trigger";
            b.GetComponent<Renderer>().sharedMaterial = gold;
            b.transform.SetParent(visual.transform, false);
            b.transform.localPosition = new Vector3(0f, Mathf.Cos(ang) * 3.4f, Mathf.Sin(ang) * 3.4f);
            b.transform.localRotation = Quaternion.Euler(ang * Mathf.Rad2Deg, 0f, 0f);
            b.transform.localScale = new Vector3(0.6f, 0.35f, 1.6f);
        }
        SetField(ring, "visual", visual.transform);
    }

    static void BouncePad(Ctx ctx, float x, float z, float launch)
    {
        Transform g = Group(ctx, $"BouncePad_{x:0}");
        GameObject trig = Box(g, "BouncePad", ctx.c.P(x, 2.5f, z), new Vector3(6f, 5f, 6f), 0f, ctx.triggerMat);
        trig.GetComponent<Collider>().isTrigger = true;
        trig.GetComponent<Renderer>().enabled = false;
        trig.tag = "Trigger";
        BouncePad pad = trig.AddComponent<BouncePad>();
        SetField(pad, "launchSpeed", launch);
        Material cyan = Mat("Bounce_Cyan", new Color(0.35f, 0.9f, 1f), 0.6f, new Color(0.2f, 0.8f, 1f) * 1.8f);
        GameObject vis = Prim(PrimitiveType.Cylinder, g, "PadVisual", cyan);
        vis.transform.position = ctx.c.P(x, 0.3f, z);
        vis.transform.localScale = new Vector3(5f, 0.3f, 5f);
        vis.tag = "Trigger";
        SetField(pad, "visual", vis.transform);
    }

    static void Checkpoint(Ctx ctx, float x)
    {
        ctx.checkpointCount++;
        Transform g = Group(ctx, $"Checkpoint_{ctx.checkpointCount}");
        GameObject trig = FullTrigger(ctx, "CheckpointTrigger", x, 3f);
        trig.transform.SetParent(g, true);
        Checkpoint cp = trig.AddComponent<Checkpoint>();
        SetField(cp, "order", ctx.checkpointCount);

        GameObject spawn = new GameObject("RespawnPoint");
        spawn.transform.SetParent(g, false);
        spawn.transform.position = ctx.c.P(x + 4f, H * 0.45f, 0f);
        SetField(cp, "respawnPoint", spawn.transform);

        Material pole = Mat("Checkpoint_Pole", new Color(0.9f, 0.9f, 0.95f), 0.6f);
        Material flag = Mat("Checkpoint_Flag", new Color(0.95f, 0.95f, 0.95f), 0.4f, new Color(0.3f, 0.3f, 0.3f));
        GameObject p = RemoveCollider(Box(g, "Pole", ctx.c.P(x, 4f, W / 2f - 2f), new Vector3(0.3f, 8f, 0.3f), 0f, pole));
        GameObject f = RemoveCollider(Box(g, "Flag", ctx.c.P(x + 1.3f, 7f, W / 2f - 2f), new Vector3(2.4f, 1.6f, 0.1f), 0f, flag));
        p.tag = "Trigger"; f.tag = "Trigger";
        SetField(cp, "flagRenderer", f.GetComponent<Renderer>());
    }

    static void SyncGate(Ctx ctx, float x)
    {
        GameObject trig = FullTrigger(ctx, $"HazardSyncGate_{x:0}", x, 1f);
        trig.AddComponent<HazardSyncGate>();
    }

    // 번개 한 줄: 세로 기둥 4개 + 가로 레이저 1개가 함께 깜빡임(같은 박자)
    static void LightningRow(Ctx ctx, float x, float syncSpeed, float phase = 0f)
    {
        Transform g = Group(ctx, $"Lightning_{x:0}");
        float top = ctx.c.VerticalHeight(x);
        Material bolt = Mat("Lightning_Bolt", new Color(1f, 0.95f, 0.8f), 0.9f, new Color(1f, 0.9f, 0.6f) * 2f);
        List<GameObject> parts = new List<GameObject>();
        foreach (float z in new[] { -9f, -3f, 3f, 9f })
        {
            parts.Add(Box(g, $"Bolt_{z:0}", ctx.c.P(x, top / 2f, z + Random.Range(-1f, 1f)), new Vector3(0.8f, top, 0.8f), 0f, bolt));
        }
        parts.Add(Box(g, "Laser", ctx.c.P(x, Random.Range(6f, top - 6f), 0f), new Vector3(0.6f, 0.6f, W), 0f, bolt));

        foreach (GameObject p in parts)
        {
            p.GetComponent<Collider>().isTrigger = true;
            TimedHazard th = p.AddComponent<TimedHazard>();
            SetField(th, "offTime", syncSpeed > 0f ? 1.0f : R(S.lightningOff, ctx.D01));
            SetField(th, "warnTime", 0.4f);
            SetField(th, "onTime", syncSpeed > 0f ? 0.8f : R(S.lightningOn, ctx.D01));
            SetField(th, "syncSpeed", syncSpeed);
            SetField(th, "phaseOffset", phase + Random.Range(0f, 0.3f));
        }
    }

    // 중력 반전(맵 회전) 구간 입구: 보라 포탈 + 큰 글자
    static void FlipZone(Ctx ctx, float x, bool reverse)
    {
        Transform g = Group(ctx, $"FlipZone_{x:0}");
        GameObject trig = FullTrigger(ctx, reverse ? "ReverseGravityTrigger" : "RestoreGravityTrigger", x, 2f);
        trig.transform.SetParent(g, true);
        ReverseGravity rg = trig.AddComponent<ReverseGravity>();
        SetField(rg, "reverse", reverse);

        Material portal = Mat("Flip_Portal", new Color(0.7f, 0.45f, 1f), 0.9f, new Color(0.6f, 0.3f, 1f) * 2.5f);
        float top = ctx.c.VerticalHeight(x);
        foreach (float z in new[] { -W / 2f + 0.8f, W / 2f - 0.8f })
            RemoveCollider(Box(g, "Portal_Side", ctx.c.P(x, top / 2f, z), new Vector3(0.8f, top, 0.8f), 0f, portal)).tag = "Trigger";
        RemoveCollider(Box(g, "Portal_Top", ctx.c.P(x, top - 0.6f, 0f), new Vector3(0.8f, 0.8f, W), 0f, portal)).tag = "Trigger";
        RemoveCollider(Box(g, "Portal_Bottom", ctx.c.P(x, 0.6f, 0f), new Vector3(0.8f, 0.8f, W), 0f, portal)).tag = "Trigger";
        Sign(ctx, x - 10f, H * 0.7f, 0f, reverse ? "GRAVITY FLIP AHEAD" : "BACK TO NORMAL", 2.8f, g);
    }

    static void NeonCorridor(Ctx ctx, float xa, float xb)
    {
        Material pink = Mat("Neon_Pink_Glow", new Color(1f, 0.3f, 0.65f), 0.8f, new Color(1f, 0.2f, 0.6f) * 2.5f);
        Material yellow = Mat("Neon_Yellow_Glow", new Color(1f, 0.95f, 0.3f), 0.8f, new Color(1f, 0.9f, 0.2f) * 2.5f);
        Transform g = Group(ctx, "NeonCorridor");
        int i = 0;
        for (float x = xa; x < xb - 2f; x += 5f, i++)
        {
            Material m = i % 2 == 0 ? pink : yellow;
            foreach (float z in new[] { -W / 2f + 0.3f, W / 2f - 0.3f })
                RemoveCollider(Box(g, "NeonStrip", ctx.c.P(x, H / 2f, z), new Vector3(0.4f, H - 1f, 0.2f), 0f, m));
        }
    }

    static void StarField(Ctx ctx)
    {
        GameObject fx = new GameObject("StarField");
        fx.transform.SetParent(ctx.root, false);
        float len = ctx.c.Length;
        fx.transform.position = new Vector3(len / 2f, (ctx.c.baseY + ctx.c.EndY) / 2f + H / 2f, 0f);
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.duration = 5f;
        main.startLifetime = 100000f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.45f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f), new Color(0.75f, 0.8f, 1f));
        main.maxParticles = 900;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)900) });
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(len, (ctx.c.EndY - ctx.c.baseY) + H, W - 1f);
        fx.GetComponent<ParticleSystemRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{FxFolder}/FX_Particle.mat");
    }

    static GameObject Sign(Ctx ctx, float x, float h, float z, string text, float size, Transform parent = null)
    {
        GameObject go = new GameObject($"Sign_{text}");
        go.transform.SetParent(parent != null ? parent : Group(ctx, "Signs"), false);
        go.transform.position = ctx.c.P(x, h, z);
        go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        go.tag = "Text";
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size * 10f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.rectTransform.sizeDelta = new Vector2(W * 1.6f, 8f);
        return go;
    }

    // ───────────────────────── 조명 · 하늘 · 카메라 · UI ─────────────────────────

    static void SetupLighting(Ctx ctx)
    {
        Theme t = ctx.t;
        Light sun = Object.FindAnyObjectByType<Light>();
        if (sun != null)
        {
            sun.color = t.light;
            sun.intensity = t.lightIntensity;
        }

        Material sky = AssetDatabase.LoadAssetAtPath<Material>($"{MatFolder}/{t.name}_Sky.mat");
        Shader skyShader = Shader.Find("Skybox/Procedural");
        if (sky == null && skyShader != null)
        {
            sky = new Material(skyShader);
            AssetDatabase.CreateAsset(sky, $"{MatFolder}/{t.name}_Sky.mat");
        }
        if (sky != null)
        {
            sky.SetColor("_SkyTint", t.skyTint);
            sky.SetColor("_GroundColor", t.ground);
            sky.SetFloat("_Exposure", t.exposure);
            sky.SetFloat("_AtmosphereThickness", t.stars ? 0.2f : 1f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = t.fog;
        RenderSettings.fogStartDistance = t.fogStart;
        RenderSettings.fogEndDistance = t.fogEnd;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(t.wall, Color.white, 0.35f) * (t.stars ? 0.35f : 0.75f);
    }

    static void SetupPlayerCameraUi(Ctx ctx)
    {
        GameObject player = GameObject.Find("Player");
        GameObject cam = GameObject.Find("Main Camera");
        Course c = ctx.c;

        if (player != null)
        {
            player.transform.position = new Vector3(0f, c.baseY + 2.4f, 0f);
            GameOverTrigger got = player.GetComponent<GameOverTrigger>();
            GameObject pad = GameObject.Find("FirstGroundStart");
            if (got != null && pad != null) SetField(got, "firstGroundStart", pad);
            Mover mover = player.GetComponent<Mover>();
            if (mover != null) SetField(mover, "jumpForce", S.playerJumpForce);
        }

        // 카메라: 닭 뒤 위쪽에서 살짝 내려다봐 앞 코스가 보이게(값은 StageBuildSettings에서 조절)
        Vector3 offset = S != null ? S.cameraOffset : new Vector3(-10f, 4.5f, 0f);
        CinemachineCamera vcam = Object.FindAnyObjectByType<CinemachineCamera>();
        if (vcam != null && player != null)
        {
            vcam.transform.position = player.transform.position + offset;
            LensSettings lens = vcam.Lens;
            lens.Dutch = 0f;
            lens.FieldOfView = S != null ? S.cameraFov : 60f;
            vcam.Lens = lens;

            CinemachineFollow follow = vcam.GetComponent<CinemachineFollow>();
            if (follow != null)
            {
                follow.FollowOffset = offset;
                TrackerSettings tracker = follow.TrackerSettings;
                tracker.PositionDamping = S != null ? S.cameraDamping : new Vector3(0.8f, 0.5f, 0.8f);
                follow.TrackerSettings = tracker;
            }
        }
        if (cam != null && player != null)
        {
            cam.transform.position = player.transform.position + offset;
            cam.transform.rotation = Quaternion.Euler(S != null ? S.cameraPitch : 10f, 0f, 0f);
        }

        // 결과 패널 · HUD 연결
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

        // 결과 줄 간격 유지: 높이 줄은 TimeText 위에 하나 더
        if (timeText != null)
        {
            TextMeshProUGUI alt = Object.Instantiate(timeText, timeText.transform.parent);
            alt.name = "AltitudeText";
            alt.rectTransform.anchoredPosition = timeText.rectTransform.anchoredPosition + new Vector2(0f, 40f);
            SetField(ending, "distanceText", alt);
        }

        // 버튼: 다시하기 / 다음 스테이지
        Transform restart = panel != null ? panel.Find("RestartButton") : null;
        if (restart != null)
        {
            Button rb = restart.GetComponent<Button>();
            ClearListeners(rb.onClick);
            UnityEventTools.AddVoidPersistentListener(rb.onClick, ending.Restart);

            GameObject next = Object.Instantiate(restart.gameObject, restart.parent);
            next.name = "NextStageButton";
            RectTransform nrt = next.GetComponent<RectTransform>();
            nrt.anchoredPosition = restart.GetComponent<RectTransform>().anchoredPosition + new Vector2(0f, -45f);
            TextMeshProUGUI label = next.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = "Next Stage";
            Button nb = next.GetComponent<Button>();
            ClearListeners(nb.onClick);
            UnityEventTools.AddVoidPersistentListener(nb.onClick, ending.NextStage);
            SetField(ending, "nextStageButton", next);
        }

        // 스테이지 정보 + 시작 배너 + HUD 스테이지 글자
        GameObject infoGo = new GameObject("StageInfo");
        StageInfo info = infoGo.AddComponent<StageInfo>();
        SetField(info, "stageNumber", ctx.def.number);
        SetField(info, "stageTitle", ctx.def.title);
        SetField(info, "hint", ctx.def.hint);
        new GameObject("GravityFlipDirector").AddComponent<GravityFlipDirector>();

        if (scoreT != null)
        {
            // HUD: 왼쪽 위 모서리(가운데를 비워 닭과 길이 보이게)
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

            // 시작 배너: 화면 위쪽 가운데(닭은 화면 가운데라 가리지 않음)
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

    // 복도 가로 전체를 덮는 트리거(태그 Trigger, 안 보임)
    static GameObject FullTrigger(Ctx ctx, string name, float x, float thickness)
    {
        float top = ctx.c.VerticalHeight(x);
        GameObject g = Box(Group(ctx, "Triggers"), name, ctx.c.P(x, top / 2f, 0f), new Vector3(thickness, top, W), 0f, ctx.triggerMat);
        g.GetComponent<Collider>().isTrigger = true;
        g.GetComponent<Renderer>().enabled = false;
        g.tag = "Trigger";
        return g;
    }

    static GameObject Slab(Transform parent, string name, Vector3 pos, Vector3 scale, float angleZ, Material mat)
    {
        return Box(parent, name, pos, scale, angleZ, mat);
    }

    static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale, float angleZ, Material mat)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.position = pos;
        g.transform.rotation = Quaternion.Euler(0f, 0f, angleZ);
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g;
    }

    static GameObject Prim(PrimitiveType type, Transform parent, string name, Material mat, bool keepCollider = true)
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
