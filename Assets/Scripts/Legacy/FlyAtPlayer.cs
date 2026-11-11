using UnityEngine;

public class FlyAtPlayer : MonoBehaviour
{

    [SerializeField] Transform player; //쉬운 설명: 플레이어의 위치를 가져오기 위해 사용
    [SerializeField] float speed = 5f;
    Vector3 targetPosition; //설명 : 플레이어의 위치를 저장하기 위해 사용


    private void Awake()
    {
        gameObject.SetActive(true);
    }

    void Start()
    {
        targetPosition = player.position;
    }

    // Update is called once per frame
    void Update()
    {
        MoveToPlayer();
        DestroyWhenReached();

    }

    void MoveToPlayer()
    {
        transform.position = 
            Vector3.MoveTowards(transform.position, targetPosition,speed * Time.deltaTime);
    }

    void DestroyWhenReached()
    {
               if (transform.position == targetPosition)
        {
            gameObject.SetActive(false);
        }
    }
}
