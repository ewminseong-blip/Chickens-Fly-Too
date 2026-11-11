using System.Collections.Generic;
using UnityEngine;

// 기믹 트리거의 공통 부모입니다.
// 닭은 몸통 · 날개 · 발에 Collider가 여러 개라 OnTriggerEnter가 여러 번 옵니다.
// 여기서 Mover 하나당 한 번만 Enter / Exit를 알려 줍니다.
// (이 오브젝트의 태그는 "Trigger"여야 닿아도 죽지 않습니다. StageBuilder가 맞춰 둡니다.)
[RequireComponent(typeof(Collider))]
public abstract class PlayerTrigger : MonoBehaviour
{
    readonly Dictionary<Mover, int> inside = new Dictionary<Mover, int>();

    protected IEnumerable<Mover> MoversInside => inside.Keys;

    protected virtual void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        Mover mover = other.GetComponentInParent<Mover>();
        if (mover == null || !mover.enabled)
        {
            return;
        }

        inside.TryGetValue(mover, out int count);
        inside[mover] = count + 1;

        if (count == 0)
        {
            OnPlayerEnter(mover);
        }
    }

    void OnTriggerExit(Collider other)
    {
        Mover mover = other.GetComponentInParent<Mover>();
        if (mover == null || !inside.TryGetValue(mover, out int count))
        {
            return;
        }

        if (count <= 1)
        {
            inside.Remove(mover);
            OnPlayerExit(mover);
        }
        else
        {
            inside[mover] = count - 1;
        }
    }

    protected virtual void OnPlayerEnter(Mover mover) { }
    protected virtual void OnPlayerExit(Mover mover) { }
}
