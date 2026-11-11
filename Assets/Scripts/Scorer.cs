using UnityEngine;

public class Scorer : MonoBehaviour
{
   int hits = 0;

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            hits+=10;
            other.gameObject.tag = "Hit";
            Debug.Log("ºÎµúÈù È½¼ö " + hits);
        }
    }
}
