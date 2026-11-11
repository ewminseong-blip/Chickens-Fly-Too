using UnityEngine;

public class GrowTrigger : MonoBehaviour
{
    [SerializeField] GameObject targetObject;
    [SerializeField] Vector3 scaleMultiplier = new Vector3(2f, 5f, 2f);

    bool hasGrown;

    void OnTriggerEnter(Collider other)
    {
        if (hasGrown || !other.CompareTag("Player"))
        {
            return;
        }

        if (targetObject == null)
        {
            Debug.LogWarning("Target Object�� �������� �ʾҽ��ϴ�.", this);
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