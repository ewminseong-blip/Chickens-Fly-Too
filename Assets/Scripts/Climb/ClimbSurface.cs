using UnityEngine;

// 발판 표면 성질. 붙이지 않으면 보통 표면(마찰 1, 튕김 없음)입니다.
//  - 얼음: friction 0.15 (완만해도 미끄러지고, 멈추기 어려움)
//  - 버섯 · 탄력 구름: launchSpeed > 0 (위에서 내려오면 그 속도로 튀어 오름)
//  - 고무 · 스펀지: bounce > 0 (더 크게 튕김)
public class ClimbSurface : MonoBehaviour
{
    [Tooltip("마찰(1 = 보통, 작을수록 미끄러움)")]
    [Range(0f, 1.5f)] public float friction = 1f;
    [Tooltip("부딪혔을 때 더 튕기는 정도(0 = 보통)")]
    [Range(0f, 0.8f)] public float bounce = 0f;
    [Tooltip("0보다 크면 위에서 내려올 때 이 속도(m/s)로 튀어 오름")]
    public float launchSpeed = 0f;
    [Tooltip("이 속도(m/s) 이상으로 내려와야 튀어 오름. 10이면 약 2m 위에서 떨어졌을 때부터")]
    public float launchMinImpact = 10f;
}
