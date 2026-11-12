using UnityEditor;
using UnityEngine;

// 닭 모델을 유니티 기본 도형으로 새로 만듭니다(시즌1 색: 흰 몸 · 노란 날개 · 주황 부리 · 빨간 볏).
// 메뉴: Chicken Fly ▸ Build Chicken Model  →  Assets/Prefabs/Player.prefab을 고칩니다.
//  - 예전 모양(캡슐 몸 · 판 날개 · 상자 다리)과 그 충돌은 지우고, 충돌은 몸통 상자 하나만 남깁니다(날개 · 다리 끝 스침은 안 죽음).
//  - 모든 부품은 Model 아래에 두고, 진행 방향 = +X, 위 = +Y 기준으로 만듭니다(루트의 기울기를 Model이 되돌림).
//  - 움직임은 ChickenAnimator가 이름으로 부품을 찾아 처리합니다. 숫자(크기 · 위치)는 아래 상수에서 바꿀 수 있습니다.
public static class ChickenModelBuilder
{
    const string PrefabPath = "Assets/Prefabs/Player.prefab";
    const string MatFolder = "Assets/Art/Materials/Chicken";

    [MenuItem("Chicken Fly/Build Chicken Model")]
    public static void Build()
    {
        EnsureFolder(MatFolder);
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            // 1) 예전 부품 지우기(이름으로)
            foreach (string n in new[] { "Sphere_1", "Sphere", "wing_1", "wing_2", "LeftFoot", "RightFoot", "Model", "Hitbox" })
            {
                Transform old = root.transform.Find(n);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            // 루트 캡슐은 안 보이게
            MeshRenderer rootRenderer = root.GetComponent<MeshRenderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;
            // 루트 상자 판정은 지움: 루트가 시즌1 그대로 Z -61° 기울어져 있어서 상자도 같이 기울어
            // 옆에서 보면 마름모가 됐음 → 머리 앞 · 위와 몸 앞 아래가 판정 밖, 꼬리 쪽 빈 공간이 판정 안.
            // 대신 모델과 같은 방향(똑바로 선)의 Hitbox 자식에 몸통 캡슐 + 머리 구를 둠(아래 4번).
            BoxCollider box = root.GetComponent<BoxCollider>();
            if (box != null) Object.DestroyImmediate(box, true);

            // 예전 날개 스크립트 대신 새 애니메이터
            WingMove oldWing = root.GetComponent<WingMove>();
            if (oldWing != null) Object.DestroyImmediate(oldWing, true);
            if (root.GetComponent<ChickenAnimator>() == null) root.AddComponent<ChickenAnimator>();

            // 2) 재질(시즌1 색)
            Material white = LoadOr("Assets/Materials/Chicken_White.mat", "Chicken_White", new Color(1f, 1f, 1f), 0.35f);
            Material cream = Mat("Chicken_Belly_Cream", new Color(1f, 0.96f, 0.86f), 0.3f);
            Material yellow = LoadOr("Assets/Materials/Chicken_Wing_Yellow.mat", "Chicken_Wing_Yellow", new Color(1f, 0.89f, 0.17f), 0.35f);
            Material yellowTip = Mat("Chicken_Wing_Tip", new Color(1f, 0.76f, 0.1f), 0.35f);
            Material orange = Mat("Chicken_Beak_Orange", new Color(1f, 0.55f, 0f), 0.45f);   // 시즌1 주황(1, 0.55, 0)
            Material red = Mat("Chicken_Comb_Red", new Color(0.93f, 0.13f, 0.12f), 0.45f);
            Material eye = Mat("Chicken_Eye", new Color(0.05f, 0.05f, 0.07f), 0.55f);
            Material blush = Mat("Chicken_Blush", new Color(1f, 0.55f, 0.65f), 0.3f);

            // 3) Model: 루트 기울기(Z -61.28°)를 되돌려 진행 방향 +X, 위 +Y로
            Transform model = Empty(root.transform, "Model", Vector3.zero);
            model.localRotation = Quaternion.Inverse(Quaternion.Euler(0f, 0f, root.transform.localEulerAngles.z));

            // 4) 판정(Hitbox): 모델과 같은 방향. 보이는 몸 · 머리보다 살짝 작게(스치면 안 죽는 작은 관용),
            //    날개 · 다리 · 꼬리 · 볏은 판정 없음(시즌1부터의 규칙). 애니메이션이 움직이지 않는 별도 자식이라 판정이 흔들리지 않음.
            //    몸(Body) 보이는 크기: 앞뒤 1.6 · 높이 1.35 · 폭 1.3 / 머리(Head): 가운데 (0.66, 0.68), 지름 1.12
            Transform hitbox = Empty(root.transform, "Hitbox", Vector3.zero);
            hitbox.localRotation = model.localRotation;
            hitbox.gameObject.tag = "Player";
            CapsuleCollider bodyHit = hitbox.gameObject.AddComponent<CapsuleCollider>();
            bodyHit.direction = 0;                       // 앞뒤(+X)로 긴 몸
            bodyHit.radius = 0.6f;                       // 보이는 반높이 0.675 · 반폭 0.65보다 조금 작게
            bodyHit.height = 1.5f;                       // 보이는 길이 1.6보다 조금 짧게
            bodyHit.center = new Vector3(0f, -0.05f, 0f);
            SphereCollider headHit = hitbox.gameObject.AddComponent<SphereCollider>();
            headHit.center = new Vector3(0.66f, 0.68f, 0f);
            headHit.radius = 0.5f;                       // 보이는 반지름 0.56보다 조금 작게

            // 몸
            Transform bodyPivot = Empty(model, "BodyPivot", Vector3.zero);
            Part(PrimitiveType.Sphere, bodyPivot, "Body", white, new Vector3(0f, 0f, 0f), new Vector3(1.6f, 1.35f, 1.3f), new Vector3(0f, 0f, 8f));
            Part(PrimitiveType.Sphere, bodyPivot, "Belly", cream, new Vector3(0.38f, -0.18f, 0f), new Vector3(1.0f, 0.95f, 1.05f), Vector3.zero);

            // 머리: 귀여운 캐릭터의 기본(아기 얼굴 비율) - 큰 둥근 머리, 낮고 넓게 벌어진 점 눈, 작은 부리, 부품 수는 최소로
            Transform head = Empty(bodyPivot, "HeadPivot", new Vector3(0.66f, 0.68f, 0f));
            Part(PrimitiveType.Sphere, head, "Head", white, Vector3.zero, Vector3.one * 1.12f, Vector3.zero);
            // 볏: 둥근 덩어리 2개가 겹친 부드러운 한 덩어리
            Part(PrimitiveType.Sphere, head, "Comb_Back", red, new Vector3(-0.06f, 0.56f, 0f), new Vector3(0.3f, 0.3f, 0.2f), Vector3.zero);
            Part(PrimitiveType.Sphere, head, "Comb_Front", red, new Vector3(0.14f, 0.6f, 0f), new Vector3(0.34f, 0.34f, 0.22f), Vector3.zero);
            // 부리: 작은 둥근 부리 하나
            Part(PrimitiveType.Sphere, head, "Beak", orange, new Vector3(0.56f, -0.06f, 0f), new Vector3(0.26f, 0.17f, 0.24f), new Vector3(0f, 0f, -10f));
            foreach (float side in new[] { 1f, -1f })
            {
                string s = side > 0f ? "L" : "R";
                // 점 눈: 머리 표면에 살짝 묻힌 납작한 검은 점(튀어나온 하이라이트 없음)
                Part(PrimitiveType.Sphere, head, $"Eye_{s}", eye, new Vector3(0.43f, 0.08f, side * 0.3f), new Vector3(0.1f, 0.17f, 0.13f), new Vector3(0f, side * -35f, 0f));
                // 볼터치: 얼굴 표면에 붙은 연한 분홍
                Part(PrimitiveType.Sphere, head, $"Blush_{s}", blush, new Vector3(0.36f, -0.12f, side * 0.38f), new Vector3(0.08f, 0.1f, 0.16f), new Vector3(0f, side * -45f, 0f));
            }

            // 날개: 어깨 피벗에서 바깥(±Z)으로 둥근 깃털 3장(+Z = 왼쪽). 쉴 때는 살짝 접어 내림(18°)
            foreach (float side in new[] { 1f, -1f })
            {
                Transform w = Empty(bodyPivot, side > 0f ? "WingPivot_L" : "WingPivot_R", new Vector3(0.05f, 0.3f, side * 0.52f));
                w.localRotation = Quaternion.AngleAxis(side * 18f, Vector3.right);
                float[] lengths = { 1.5f, 1.25f, 1.0f };
                for (int k = 0; k < lengths.Length; k++)
                {
                    float len = lengths[k];
                    Part(PrimitiveType.Sphere, w, $"Feather_{k + 1}", k == 0 ? yellowTip : yellow,
                        new Vector3(-0.1f - k * 0.2f, -k * 0.03f, side * (len / 2f - 0.05f)),
                        new Vector3(0.55f, 0.13f, len),
                        new Vector3(0f, side * (8f + k * 10f), 0f));
                }
            }

            // 꽁지: 짧고 둥근 깃 뭉치(둥근 덩어리 3개가 위로 살짝 뻗침)
            Transform tail = Empty(bodyPivot, "TailPivot", new Vector3(-0.72f, 0.3f, 0f));
            Part(PrimitiveType.Sphere, tail, "Tail_Center", white, new Vector3(-0.12f, 0.18f, 0f), new Vector3(0.38f, 0.5f, 0.34f), new Vector3(0f, 0f, 30f));
            Part(PrimitiveType.Sphere, tail, "Tail_L", white, new Vector3(-0.06f, 0.1f, 0.16f), new Vector3(0.32f, 0.4f, 0.28f), new Vector3(15f, 0f, 25f));
            Part(PrimitiveType.Sphere, tail, "Tail_R", white, new Vector3(-0.06f, 0.1f, -0.16f), new Vector3(0.32f, 0.4f, 0.28f), new Vector3(-15f, 0f, 25f));

            // 다리: 엉덩이 피벗 → 다리 → 발가락 3개(앞으로)
            foreach (float side in new[] { 1f, -1f })
            {
                Transform leg = Empty(model, side > 0f ? "LegPivot_L" : "LegPivot_R", new Vector3(0.08f, -0.55f, side * 0.3f));
                Part(PrimitiveType.Cylinder, leg, "Leg", orange, new Vector3(0f, -0.3f, 0f), new Vector3(0.1f, 0.3f, 0.1f), Vector3.zero);
                for (int t = -1; t <= 1; t++)
                {
                    Part(PrimitiveType.Capsule, leg, $"Toe_{t + 2}", orange,
                        new Vector3(0.14f, -0.62f, t * 0.09f),
                        new Vector3(0.08f, 0.17f, 0.08f),
                        new Vector3(t * 22f, 0f, 90f));
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("[ChickenModelBuilder] 닭 모델을 새로 만들었습니다: " + PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static Transform Empty(Transform parent, string name, Vector3 localPos)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = localPos;
        return t;
    }

    // 장식 부품(충돌 없음)
    static Transform Part(PrimitiveType type, Transform parent, string name, Material mat, Vector3 localPos, Vector3 localScale, Vector3 localEuler)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localRotation = Quaternion.Euler(localEuler);
        g.transform.localScale = localScale;
        Renderer r = g.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        return g.transform;
    }

    static Material LoadOr(string path, string fallbackName, Color color, float smooth)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        return m != null ? m : Mat(fallbackName, color, smooth);
    }

    static Material Mat(string name, Color color, float smooth, Color? emission = null)
    {
        string path = $"{MatFolder}/{name}.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smooth);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
        }
        EditorUtility.SetDirty(m);
        return m;
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
