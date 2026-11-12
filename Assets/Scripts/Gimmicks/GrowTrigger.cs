using UnityEngine;

// 닭이 지나가면 targetObject(나무 등)를 scaleMultiplier배로 키웁니다(한 번만).
public class GrowTrigger : MonoBehaviour
{
    [SerializeField] GameObject targetObject;
    [SerializeField] Vector3 scaleMultiplier = new Vector3(2f, 5f, 2f);

    bool hasGrown;

    void OnTriggerEnter(Collider other)
    {
        // 닭의 판정은 자식(Hitbox)에 있으므로 태그 대신 부모 쪽 Mover로 닭인지 확인
        if (hasGrown || other.GetComponentInParent<Mover>() == null)
        {
            return;
        }

        if (targetObject == null)
        {
            Debug.LogWarning("Target Object가 지정되지 않았습니다.", this);
            return;
        }

        hasGrown = true;

        Vector3 currentScale = targetObject.transform.localScale;

        targetObject.transform.localScale = new Vector3(
            currentScale.x * scaleMultiplier.x,
            currentScale.y * scaleMultiplier.y,
            currentScale.z * scaleMultiplier.z);
    }
}
