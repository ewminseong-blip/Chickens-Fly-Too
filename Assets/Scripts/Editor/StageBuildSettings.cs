using System;
using System.Collections.Generic;
using UnityEngine;

// 월드 생성기가 쓰는 숫자 모음입니다. 인스펙터에서 바꾸고 메뉴 Chicken Fly ▸ Build World (10 Stages)를 다시 누르세요.
// 위치: Assets/Settings/ChickenFly/StageBuildSettings.asset (없으면 생성기가 기본값으로 만듭니다)
// 난이도 범위의 x = 쉬움(난이도 1), y = 어려움(난이도 8). 스테이지 난이도에 따라 그 사이 값을 씁니다.
[CreateAssetMenu(menuName = "Chicken Fly/Stage Build Settings", fileName = "StageBuildSettings")]
public class StageBuildSettings : ScriptableObject
{
    [Header("복도 크기 (m)")]
    [Min(8f)] public float corridorWidth = 24f;
    [Min(10f)] public float corridorHeight = 26f;
    [Tooltip("끄면 양옆 · 위 경계는 닿아도 죽지 않는 막(기본). 켜면 시즌1처럼 닿으면 죽음")]
    public bool deadlyBoundaries = false;

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

    [Header("카메라")]
    [Tooltip("닭 기준 카메라 위치(x = 뒤(-), y = 위)")]
    public Vector3 cameraOffset = new Vector3(-10f, 4.5f, 0f);
    [Tooltip("따라오는 지연(작을수록 바로 따라옴)")]
    public Vector3 cameraDamping = new Vector3(0.8f, 0.5f, 0.8f);
    [Range(-10f, 40f)] public float cameraPitch = 10f;
    [Range(40f, 90f)] public float cameraFov = 60f;

    [Header("기타")]
    [Tooltip("8스테이지 '초록 신호' 번개가 맞춰지는 속도(m/s). 플레이어 최고 속도와 같게")]
    public float syncedLightningSpeed = 10f;
    [Tooltip("바람 고리(꼬리바람)를 지날 때 속도(m/s)")]
    public float boostRingSpeed = 15f;
    [Tooltip("상승 기류(열기둥)가 올려 주는 속도(m/s)")]
    public float bounceLaunchSpeed = 20f;
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
    [Tooltip("x = 가로 길이(m), y = 오르막 각도(°). 첫 구간과 마지막 구간은 평평하게 두세요. 기믹 위치(x)는 코드 기준이라 길이를 크게 줄이면 기믹이 구간 밖으로 나갈 수 있습니다.")]
    public List<Vector2> segments = new List<Vector2>();
}
