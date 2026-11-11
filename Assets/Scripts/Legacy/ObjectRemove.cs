using UnityEngine;

public class ObjectRemove : MonoBehaviour
{
    private void OnCollisionEnter(Collision cother)
    {
        GetComponent<Transform>().position = new Vector3(0, -15f, 150f);
    }
}
