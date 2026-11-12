using UnityEngine;

// 생성기가 남기는 발판 길 정보(자동 점검 봇 · 개발용). 순서대로 발판 윗면 가운데와 반폭.
public class ClimbPathData : MonoBehaviour
{
    public Transform[] platforms = new Transform[0];
    public Vector3[] tops = new Vector3[0];
    public float[] radii = new float[0];
    public int[] sections = new int[0];

    // 움직이는 발판은 지금 위치(루트 = 윗면 가운데)
    public Vector3 Top(int i) => platforms[i] != null && platforms[i].GetComponent<ClimbMovingPlatform>() != null ? platforms[i].position : tops[i];
}
