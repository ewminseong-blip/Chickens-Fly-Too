using UnityEngine;
using UnityEngine.UIElements;

public class Dropper : MonoBehaviour
{
    [SerializeField] float timeToWait =30f;
     void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            
            other.gameObject.tag = "Hit";
            Debug.Log("ºÎµúÈù È½¼ö ");
        }
    }
    void Update()
    {
        if(Time.time >= timeToWait)
        {
            GetComponent<Rigidbody>().useGravity = true;
        }
    }
}
