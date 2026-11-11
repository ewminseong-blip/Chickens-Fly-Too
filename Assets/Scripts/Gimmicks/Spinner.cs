using UnityEngine;

public class Spinner : MonoBehaviour
{

    [SerializeField] float xAngle = 0f;
    [SerializeField] float yAngle = -180f;
    [SerializeField] float zAngle = 0f;
    //[SerializeField] float xPosition = 0f;
    //[SerializeField] float yPosition = 0f;
    //[SerializeField] float zPosition = 1f;

    private void Update()
    {
        transform.Rotate(xAngle * Time.deltaTime, yAngle * Time.deltaTime, zAngle * Time.deltaTime);
        //transform.position = Time.time * new Vector3(xPosition, yPosition, zPosition);
    }
}
