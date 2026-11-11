using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 플레이어에 붙여 사용하는 게임오버 처리 클래스입니다.
// 위험물(Text · Trigger · Ending 태그와 출발 발판을 뺀 모든 것)에 닿으면
// 짧게 멈칫(히트스톱) → 깃털이 터지며 닭이 사라짐 → 잠시 뒤 다시 시작합니다.
public class GameOverTrigger : MonoBehaviour
{
    // 게임 시작 위치의 오브젝트입니다.
    [SerializeField] GameObject firstGroundStart;

    // 게임오버 후 씬을 다시 시작하기까지의 시간(초, 실제 시간)
    [SerializeField] float returnDelay = 1f;

    // 부딪힌 순간 화면이 멈추는 시간(초). 0이면 끕니다.
    [SerializeField] float hitStopDuration = 0.08f;

    // 게임오버 시 캐릭터가 바뀔 색입니다.
    [SerializeField] Color gameOverColor = Color.red;

    bool isReturning;

    void OnCollisionEnter(Collision other)
    {
        CheckGameOverObject(other.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        CheckGameOverObject(other.gameObject);
    }

    // 바깥 감시(StageBounds) 같은 다른 장치가 직접 사망 처리할 때 씁니다.
    public void Kill(GameObject cause)
    {
        if (isReturning || cleared)
        {
            return;
        }

        isReturning = true;
        StartCoroutine(DeathSequence(cause));
    }

    // 도착한 뒤에는 죽지 않습니다(EndingCredits가 부름).
    public void MarkCleared()
    {
        cleared = true;
    }

    public bool IsDying => isReturning;
    bool cleared;

    void CheckGameOverObject(GameObject otherObject)
    {
        if (isReturning || cleared || IsExcludedObject(otherObject))
        {
            return;
        }

        Debug.Log($"게임오버 대상: {otherObject.name}, Tag: {otherObject.tag}");

        isReturning = true;
        StartCoroutine(DeathSequence(otherObject));
    }

    // 죽은 순간 원인을 알립니다(천장 경고가 "HIT THE CEILING!"을 띄우는 데 씀).
    public static event System.Action<GameObject> Died;

    IEnumerator DeathSequence(GameObject hitObject)
    {
        Died?.Invoke(hitObject);
        ChangeColor(gameOverColor);

        // 무엇에 죽었는지 보이도록 부딪힌 물체를 깜빡입니다.
        HazardFlash.Flash(hitObject);

        // 히트스톱: 부딪힌 순간을 아주 짧게 멈춰 '맞았다'를 느끼게 합니다.
        if (hitStopDuration > 0f && FeedbackFX.ScreenEffectsEnabled)
        {
            float previous = Time.timeScale;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(hitStopDuration);
            Time.timeScale = previous;
        }

        FeedbackFX.Death(transform.position);

        // 닭을 숨기고 멈춥니다(조작 끄기).
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }

        Mover mover = GetComponent<Mover>();
        if (mover != null)
        {
            mover.enabled = false;
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        yield return new WaitForSecondsRealtime(returnDelay);
        RestartScene();
    }

    bool IsExcludedObject(GameObject targetObject)
    {
        GameObject rootObject = targetObject.transform.root.gameObject;

        if (targetObject.CompareTag("Text") || rootObject.CompareTag("Text"))
        {
            return true;
        }

        if (targetObject.CompareTag("Trigger") || rootObject.CompareTag("Trigger"))
        {
            return true;
        }

        if (targetObject.CompareTag("Ending") || rootObject.CompareTag("Ending"))
        {
            return true;
        }

        // 복도 양옆 · 위의 보이지 않는 경계(막기만 하고 죽이지 않음)
        if (targetObject.CompareTag("Boundary"))
        {
            return true;
        }

        if (firstGroundStart != null)
        {
            Transform targetTransform = targetObject.transform;
            Transform startTransform = firstGroundStart.transform;

            if (targetObject == firstGroundStart ||
                targetTransform.IsChildOf(startTransform) ||
                startTransform.IsChildOf(targetTransform))
            {
                return true;
            }
        }

        if (targetObject.name == "FirstGroundStart" || rootObject.name == "FirstGroundStart")
        {
            return true;
        }

        return false;
    }

    void ChangeColor(Color color)
    {
        foreach (Renderer targetRenderer in GetComponentsInChildren<Renderer>())
        {
            if (targetRenderer is ParticleSystemRenderer)
            {
                continue;
            }

            targetRenderer.material.color = color;
        }
    }

    // 사망 횟수와 이번 목숨의 시간은 GameSession에 남기고 씬을 다시 불러옵니다.
    void RestartScene()
    {
        GameSession.RecordDeath(Time.timeSinceLevelLoad);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
