using System;
using System.Collections.Generic;
using UnityEngine;

// 월드 생성기가 쓰는 숫자 모음입니다. 인스펙터에서 바꾸고 메뉴 Chicken Fly ▸ Build World (10 Stages)를 다시 누르세요.
// 위치: Assets/Settings/ChickenFly/StageBuildSettings.asset (없으면 생성기가 기본값으로 만듭니다)
// 난이도 범위의 x = 쉬움(난이도 1), y = 어려움(난이도 8). 스테이지 난이도에 따라 그 사이 값을 씁니다.
[CreateAssetMenu(menuName = "Chicken Fly/Stage Build Settings", fileName = "StageBuildSettings")]
public class StageBuildSettings : ScriptableObject
{
    [Header("경계")]
    [Tooltip("양옆 경계: 끄면 닿아도 죽지 않는 막(기본), 켜면 닿으면 죽음")]
    public bool deadlyBoundaries = false;
    [Tooltip("위 경계(천장): 켜면 닿으면 죽음(기본). 가까워지면 화면에 경고가 뜸. 끄면 막 + 아래로 미는 바람 띠")]
    public bool deadlyCeiling = true;
    [Tooltip("천장 경고가 시작되는 거리(m)")]
    public float ceilingWarnDistance = 6f;
    [Tooltip("높이(m)가 이 값 이하인 스테이지는 천장에 구름 지붕을 깔아 천장을 눈에 보이게 함(5 · 6스테이지 구름 통로)")]
    public float cloudRoofBelowHeight = 25f;
    [Tooltip("구름 지붕 아랫면이 천장(판정)보다 얼마나 아래로 내려와 보일지(m). 0이면 구름 아랫면 = 천장, 클수록 구름 속을 지나가도 안 죽는 부분이 생김")]
    public float cloudRoofOffset = 0.3f;

    [Header("세이브 포인트(체크포인트)")]
    [Tooltip("몇 스테이지마다 세이브 포인트를 둘지. 2면 3 · 5 · 7 · 9스테이지 시작, 1이면 2 ~ 10스테이지 시작마다, 3이면 4 · 7 · 10")]
    [Min(1)] public int checkpointEveryStages = 2;
    [Tooltip("스테이지 중간 체크포인트도 둘지(기본 끔)")]
    public bool midStageCheckpoints = false;
    [Tooltip("구간이 바뀔 때 폭 · 높이가 서서히 바뀌는 거리(m)")]
    public float sizeBlend = 25f;

    [Header("난이도 범위 (x = 쉬움, y = 어려움)")]
    [Tooltip("바위 · 구름 · 소행성 사이 틈 크기(m)")]
    public Vector2 holeSize = new Vector2(13f, 8f);
    [Tooltip("새 떼 · 굴러오는 바위 · 혜성이 가로지르는 속도(m/s)")]
    public Vector2 crossSpeed = new Vector2(7f, 15f);
    [Tooltip("가로지른 뒤 다시 나오기까지(초)")]
    public Vector2 crossInterval = new Vector2(3.5f, 1.8f);
    [Tooltip("흘러가는 구름 속도(m/s)")]
    public Vector2 driftSpeed = new Vector2(3f, 7f);
    [Tooltip("상승풍 · 하강풍 세기(m/s²)")]
    public Vector2 windLift = new Vector2(12f, 20f);
    [Tooltip("옆바람 · 태양풍 세기(m/s²)")]
    public Vector2 crossWind = new Vector2(10f, 16f);
    [Tooltip("우박 · 낙석이 차례로 떨어지는 간격(초)")]
    public Vector2 hailInterval = new Vector2(0.2f, 0.1f);
    [Tooltip("구름이 솟거나 내려오는 시간(초)")]
    public Vector2 cloudMoveTime = new Vector2(3f, 2f);
    [Tooltip("번개 · 펄서 빛줄기가 꺼져 있는 시간(초)")]
    public Vector2 lightningOff = new Vector2(1.4f, 0.9f);
    [Tooltip("번개 · 펄서 빛줄기가 켜져 있는 시간(초)")]
    public Vector2 lightningOn = new Vector2(0.7f, 1.1f);

    [Header("번개 (8스테이지 · 노란색 한 가지, 켜져 있으면 닿음 / 꺼지면 안 닿음)")]
    [Tooltip("번개 중 꺼졌다 켜졌다 번쩍이는 비율(나머지는 늘 켜져 있음)")]
    [Range(0f, 1f)] public float lightningFlickerRatio = 0.3f;
    [Tooltip("번쩍이는 번개 중 '짧게 꺼지는' 번개의 꺼짐 시간(초, 고정). 짧은 것 · 긴 것을 번갈아 배치")]
    public float lightningOffShort = 1f;
    [Tooltip("번쩍이는 번개 중 '길게 꺼지는' 번개의 꺼짐 시간(초, 고정)")]
    public float lightningOffLong = 2f;
    [Tooltip("번쩍이는 번개가 켜져 있는 시간(초, 고정)")]
    public float lightningOnDuration = 1.2f;

    [Header("카메라")]
    [Tooltip("닭 기준 카메라 위치(x = 뒤(-), y = 위)")]
    public Vector3 cameraOffset = new Vector3(-10f, 4.5f, 0f);
    [Tooltip("따라오는 지연(작을수록 바로 따라옴)")]
    public Vector3 cameraDamping = new Vector3(0.8f, 0.5f, 0.8f);
    [Range(-10f, 40f)] public float cameraPitch = 10f;
    [Range(40f, 90f)] public float cameraFov = 60f;

    [Header("기타")]
    [Tooltip("8스테이지 번개 박자의 기본 속도(m/s). 실제로는 게이트에서 잰 속도로 맞춤")]
    public float syncedLightningSpeed = 10f;
    [Tooltip("바람 고리(꼬리바람)를 지날 때 최소 속도(m/s)")]
    public float boostRingSpeed = 19f;
    [Tooltip("상승 기류(열기둥)가 올려 주는 속도(m/s)")]
    public float bounceLaunchSpeed = 20f;
    [Tooltip("바위 · 구름 · 소행성 장애물의 판정 크기(보이는 모양 대비 배율). 1이면 보이는 그대로, 작을수록 스쳐도 안 죽음")]
    [Range(0.7f, 1f)] public float hitShrink = 0.92f;
    [Tooltip("우주 구간 구멍 테두리 밝기(발광 배율)")]
    public float holeMarkerGlow = 4f;
    [Tooltip("우주 구간 구멍 가운데 초록 빛 세기")]
    public float holeMarkerLight = 6f;
    [Tooltip("구멍 테두리를 벽 앞으로 얼마나 내밀지(m). 바위에 가려지면 키움")]
    public float holeMarkerForward = 2.6f;
    [Tooltip("낙석이 떨어지기 시작하는 속도(m/s)")]
    public float rockfallSpeed = 30f;
    [Tooltip("낙석이 떨어지며 붙는 가속(m/s²). 높을수록 바닥 가까이에서 훨씬 빨라짐")]
    public float rockfallAcceleration = 45f;
    [Tooltip("코스 안 구름(장애물 · 바닥 구름) 크기")]
    public float cloudScale = 3.6f;
    [Tooltip("양옆 구름 벽 구름 크기(최소 ~ 최대)")]
    public Vector2 cloudWallScale = new Vector2(4.5f, 6.5f);
    [Tooltip("분위기(하늘 · 안개 · 빛)가 다음 구간으로 섞이는 거리(m)")]
    public float atmosphereBlend = 60f;
    [Tooltip("플레이어 날갯짓 힘")]
    public float playerJumpForce = 13f;

    [Header("스테이지 (비우면 기본값으로 채움)")]
    public List<StageEntry> stages = new List<StageEntry>();
    [HideInInspector] public int stagesVersion;
}

[Serializable]
public class StageEntry
{
    public int number = 1;
    public string title = "";
    public string hint = "";
    [Range(1f, 10f)] public float difficulty = 1f;
    [Tooltip("복도 폭(m). 좁을수록 정밀한 조작이 필요(구름 구간처럼 좁음 자체를 난이도로)")]
    public float width = 30f;
    [Tooltip("바닥에서 위 경계까지(m). 높을수록 자유롭게 날 수 있음")]
    public float height = 45f;
    [Tooltip("x = 길이(m), y = 오르막(°), z = 좌우로 꺾는 각도(°, +오른쪽 / -왼쪽). 기믹 위치는 코드 기준이라 길이를 크게 줄이면 기믹이 구간 밖으로 나갈 수 있습니다.")]
    public List<Vector3> path = new List<Vector3>();
}
