using System.Collections.Generic;
using UnityEngine;

// 바람 구역: 안에 있는 동안 닭을 한 방향으로 계속 밉니다.
// 위아래 바람은 바로 힘으로, 옆 · 앞뒤 바람은 Mover의 '밀림 속도'로 들어갑니다.
public class WindArea : PlayerTrigger
{
    // 월드 기준 가속도(m/s²). 예: (0, 25, 0) 상승풍, (0, 0, 15) 옆바람
    [SerializeField] Vector3 acceleration = new Vector3(0f, 25f, 0f);

    readonly List<Mover> buffer = new List<Mover>();

    public Vector3 Acceleration => acceleration;

    void FixedUpdate()
    {
        buffer.Clear();
        buffer.AddRange(MoversInside);

        foreach (Mover mover in buffer)
        {
            if (mover != null && mover.enabled)
            {
                mover.AddWind(acceleration);
            }
        }
    }
}
