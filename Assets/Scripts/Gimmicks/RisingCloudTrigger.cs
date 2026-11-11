using UnityEngine;

// 구름 이동 트리거(시즌1 TriggerUpCloude를 정리한 것. 필드 이름은 그대로라 옛 씬 값도 유지됩니다).
// 플레이어가 지나가면 targetObject가
//   1) warnTime초 동안 흔들리며 먹구름 색으로 변하고(예고)
//   2) targetTime초 동안 positionPlus만큼 움직입니다(끝에서 부드럽게 멈춤).
// positionPlus를 음수로 주면 구름이 '내려와' 길을 막습니다.
public class RisingCloudTrigger : PlayerTrigger
{
    [SerializeField] GameObject targetObject;
    [SerializeField] Vector3 positionPlus = new Vector3(0f, 20f, 0f);
    [SerializeField] float targetTime = 3f;
    [SerializeField] float warnTime = 0.6f;
    [SerializeField] float shakeAmount = 0.4f;
    [SerializeField] Color stormColor = new Color(0.45f, 0.45f, 0.55f);

    bool started;
    float timer;
    Vector3 startPosition;
    Renderer[] renderers;
    MaterialPropertyBlock block;

    protected override void OnPlayerEnter(Mover mover)
    {
        if (started || targetObject == null)
        {
            return;
        }

        started = true;
        timer = 0f;
        startPosition = targetObject.transform.position;
        renderers = targetObject.GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (!started || targetObject == null)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer < warnTime)
        {
            float k = timer / warnTime;
            Vector3 shake = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * shakeAmount;
            targetObject.transform.position = startPosition + shake;
            SetColor(Color.Lerp(Color.white, stormColor, k));
            return;
        }

        float u = Mathf.Clamp01((timer - warnTime) / Mathf.Max(0.01f, targetTime));
        // ease-out: 빠르게 출발해 끝에서 부드럽게
        float e = 1f - (1f - u) * (1f - u);
        targetObject.transform.position = startPosition + positionPlus * e;

        if (u >= 1f)
        {
            enabled = false;
        }
    }

    void SetColor(Color c)
    {
        block.SetColor("_BaseColor", c);
        foreach (Renderer r in renderers)
        {
            r.SetPropertyBlock(block);
        }
    }
}
