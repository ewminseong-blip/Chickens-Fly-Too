using System.Collections;
using UnityEngine;

// 떨어지는 창: 플레이어가 이 트리거를 지나면 targets가 delay초 뒤 차례로 떨어집니다.
// 떨어지기 전에 살짝 흔들려 예고합니다. targets는 Rigidbody 없이 두고 여기서 붙입니다.
public class DropOnTrigger : PlayerTrigger
{
    [SerializeField] Transform[] targets;
    [SerializeField] float delay = 0.3f;
    [SerializeField] float interval = 0.15f;     // 다음 창까지 간격
    [SerializeField] float shakeTime = 0.35f;
    [SerializeField] float fallSpeed = 25f;      // 떨어지는 속도(m/s)

    bool fired;

    protected override void OnPlayerEnter(Mover mover)
    {
        if (fired)
        {
            return;
        }

        fired = true;
        StartCoroutine(DropAll());
    }

    IEnumerator DropAll()
    {
        yield return new WaitForSeconds(delay);

        foreach (Transform target in targets)
        {
            if (target != null)
            {
                StartCoroutine(DropOne(target));
            }

            yield return new WaitForSeconds(interval);
        }
    }

    IEnumerator DropOne(Transform target)
    {
        Vector3 start = target.position;
        float t = 0f;

        while (t < shakeTime)
        {
            t += Time.deltaTime;
            target.position = start + new Vector3(Random.Range(-0.15f, 0.15f), 0f, Random.Range(-0.15f, 0.15f));
            yield return null;
        }

        target.position = start;

        // 바닥(y=0) 아래까지 일정 속도로 떨어집니다.
        while (target.position.y > -10f)
        {
            target.position += Vector3.down * fallSpeed * Time.deltaTime;
            yield return null;
        }

        target.gameObject.SetActive(false);
    }
}
