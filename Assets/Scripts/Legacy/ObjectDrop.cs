using UnityEngine;

public class ObjectDrop : MonoBehaviour
{
    bool hasdropped;

    void Drop()
    {
        Vector3 position = transform.position;
        position.y = 15f;
        position.z = -15f;
        transform.position = position;
    }

    void OnCollisionEnter(Collision other)
    {
        if (hasdropped)
        {
            return;
        }

        hasdropped = true;
        Invoke(nameof(Drop), 1.5f);
    }


}
