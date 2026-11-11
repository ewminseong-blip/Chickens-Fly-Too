using UnityEngine;

public class ObjectBounce : MonoBehaviour
{
    [SerializeField] float bounceForce = 100f;

    private void OnCollisionEnter(Collision other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            Rigidbody rb = other.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 bounceDirection = other.contacts[0].normal;
                rb.AddForce(bounceDirection * bounceForce, ForceMode.Impulse);
            }
        }
        Debug.Log("Æ¨±è : " + other.gameObject.name);
    }
        
}
