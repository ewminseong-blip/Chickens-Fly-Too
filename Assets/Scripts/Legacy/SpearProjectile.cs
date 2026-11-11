using UnityEngine;

public class SpearProjectile : MonoBehaviour
{

    [SerializeField] Transform player; //쉬운 설명: 플레이어의 위치를 가져오기 위해 사용
    [SerializeField] float speed = 3f;
    Vector3 targetPosition; //설명 : 플레이어의 위치를 저장하기 위해 사용

    void Start()
    {
        targetPosition = player.position;
        //설명 : Start() 메서드에서 targetPosition을 플레이어의 현재 위치로 초기화
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            player.position,
            speed * Time.deltaTime);

    }
}
