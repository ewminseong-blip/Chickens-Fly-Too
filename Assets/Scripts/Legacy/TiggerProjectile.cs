using UnityEngine;

public class TiggerProjectile : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] GameObject projectile;

    bool hasTriggered;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered)
        {
            return;
        }

        if (other.CompareTag("Player") && projectile != null)
        {
            hasTriggered = true;
            Invoke(nameof(DeactivateProjectile), 1.5f);
        }
    }

    void DeactivateProjectile()
    {
        if (projectile != null)
        {
            projectile.SetActive(false);
        }
    }
}