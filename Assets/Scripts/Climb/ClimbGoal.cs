using UnityEngine;

// 꼭대기 금빛 알 둥지(도착).
public class ClimbGoal : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        ClimbController p = other.GetComponentInParent<ClimbController>();
        if (p == null || ClimbGame.Instance == null) return;
        ClimbGame.Instance.ReachGoal(transform.position);
    }
}
