using System.Collections;
using UnityEngine;

// 낙석 · 우박: 플레이어가 이 트리거를 지나면 targets가 delay초 뒤 차례로 떨어집니다.
// 떨어지기 전에 살짝 흔들려 예고합니다. targets는 Rigidbody 없이 두고 여기서 붙입니다.
public class DropOnTrigger : PlayerTrigger
{
    [SerializeField] Transform[] targets;
    [SerializeField] float delay = 0.3f;
    [SerializeField] float interval = 0.15f;     // 다음 창까지 간격
    [SerializeField] float shakeTime = 0.35f;
    [SerializeField] float fallSpeed = 25f;      // 떨어지기 시작하는 속도(m/s)
    [Tooltip("떨어지면서 붙는 가속(m/s²). 0이면 일정 속도(우박), 크면 점점 빨라짐(낙석)")]
    [SerializeField] float fallAcceleration = 0f;
    [SerializeField] float fallDistance = 45f;   // 이만큼 떨어지면 사라짐(m)
    [Tooltip("켜면 다 떨어진 뒤 제자리로 돌아가 다시 떨어짐(계속 쏟아지는 낙석 지대)")]
    [SerializeField] bool loop = false;
    [Tooltip("반복할 때 다시 떨어지기까지 기다리는 시간(초, 최소 ~ 최대)")]
    [SerializeField] Vector2 loopDelay = new Vector2(0.2f, 0.9f);

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

        do
        {
            float t = 0f;
            while (t < shakeTime)
            {
                t += Time.deltaTime;
                target.position = start + new Vector3(Random.Range(-0.15f, 0.15f), 0f, Random.Range(-0.15f, 0.15f));
                yield return null;
            }

            target.position = start;

            // fallDistance만큼 떨어진 뒤 사라집니다(높은 곳에서도 같은 길이). 가속이 있으면 점점 빨라짐.
            float speed = fallSpeed;
            while (start.y - target.position.y < fallDistance)
            {
                speed += fallAcceleration * Time.deltaTime;
                target.position += Vector3.down * speed * Time.deltaTime;
                yield return null;
            }

            if (loop)
            {
                // 위로 되돌려 잠깐 숨겼다가 다시 떨어뜨림
                target.position = start;
                target.gameObject.SetActive(false);
                yield return new WaitForSeconds(Random.Range(loopDelay.x, loopDelay.y));
                target.gameObject.SetActive(true);
            }
        }
        while (loop);

        target.gameObject.SetActive(false);
    }
}
