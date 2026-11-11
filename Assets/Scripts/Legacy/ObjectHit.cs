using UnityEngine;

public class ObjectHit : MonoBehaviour
{
    void OnCollisionEnter(Collision other)
    {   
       
        Debug.Log("ºÎµúÈû:" + other.gameObject.name);
        GetComponent<MeshRenderer>().material.color = Color.red;
    }
    
}
