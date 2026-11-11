using UnityEngine;

public class Spinning : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {
        transform.Rotate(0f, 0.2f, 0f * Time.deltaTime);
    }
}
