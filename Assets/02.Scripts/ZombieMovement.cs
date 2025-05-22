using Unity.Burst.Intrinsics;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ZombieMovement : MonoBehaviour
{
    public float moveSpeed = 2f;                  // 걷는 속도
    public float minJumpForce = 5f;               // 최소 점프 파워(혹시 모르니 바닥에도 쓸 수 있게)
    public float backDistance = 1.0f;             // 뒤로 이동할 최소 거리

    [Header("앞쪽 레이")]
    public float frontRayOffset = 0.2f;          // 앞쪽 Ray 오프셋
    public float frontRayDistance = 0.5f;          // 앞쪽 Ray 거리

    [Header("위쪽 레이")]
    public float upRayOffset = 1.0f;             // 위쪽 Ray 오프셋
    public float upRayDistance = 0.5f;            // 위쪽 Ray 거리

    [Header("아래쪽 레이")]
    public float downRayOffset = 0.1f;           // 아래쪽 Ray 오프셋
    public float downRayDistance = 0.5f;          // 아래쪽 Ray 거리

    private float backTimer = 0f;
    public float backTime = 0.5f; // 뒤로 가는 시간

    public LayerMask zombieLayer;                 // 좀비 레이어
    public LayerMask groundLayer;

    private Rigidbody2D rb;

    private bool isJumping = false;
    private Vector2 backStartPos;                // 뒤로 이동 시작 위치

    private GameObject frontZombie;

    public enum ZombieState
    {
        Walking,
        Jumping,
        Back
    }


    public ZombieState zombieState = ZombieState.Walking;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 앞쪽 Ray
        RaycastHit2D frontRay = Physics2D.Raycast((Vector2)transform.position - new Vector2(frontRayOffset, 0), Vector2.left, frontRayDistance);
        if(frontRay)
        {
            if(frontRay.collider.CompareTag("Zombie"))
            {
                Debug.Log("앞에 좀비가 있다!");
                if (zombieState == ZombieState.Walking)
                {
                    SetState(ZombieState.Jumping);
                    frontZombie = frontRay.collider.gameObject; // 앞 좀비 저장
                }
            }
        }

        // 위쪽 Ray
        RaycastHit2D upRay = Physics2D.Raycast((Vector2)transform.position + new Vector2(0, upRayOffset), Vector2.up, upRayDistance);
        if (upRay)
        {
            if (upRay.collider.CompareTag("Zombie"))
            {
                Debug.Log("위에 좀비가 있다!");
                if (zombieState == ZombieState.Walking)
                {
                    SetState(ZombieState.Back);
                }
            }
        }

        // 아래쪽 Ray
        RaycastHit2D downRay = Physics2D.Raycast((Vector2)transform.position - new Vector2(0, downRayOffset), Vector2.down, downRayDistance);
        if (downRay)
        {
            Debug.Log("바닥에 닿았다!");
            if (zombieState == ZombieState.Jumping && isJumping)
            {
                SetState(ZombieState.Walking);

                Physics2D.IgnoreCollision(this.GetComponent<Collider2D>(), frontZombie.GetComponent<Collider2D>(), false);
                frontZombie = null;
                isJumping = false;
            }
        }

        if (zombieState == ZombieState.Back)
        {
            backTimer += Time.deltaTime;

            // Back 상태에서는 Ray/충돌 무시! (Update 내 Raycast/충돌 코드는 건너뜀)
            // 단, 아래처럼 거리+시간 기준으로만 Walking 복귀
            if (Vector2.Distance((Vector2)transform.position, backStartPos) >= backDistance || backTimer > backTime)
            {
                SetState(ZombieState.Walking);
                backTimer = 0f;
            }
        }
        else
        {
            backTimer = 0f;
            // 나머지 Ray, 상태 전환 코드 정상 진행
        }


        switch (zombieState)
        {
            case ZombieState.Walking:
                Walking();
                break;
            case ZombieState.Jumping:
                Jumping();
                break;
            case ZombieState.Back:
                Back();
                break;
        }
    }

    private void Walking()
    {
        Debug.Log($"{gameObject.name} 이동");

        // 이동
        rb.velocity = new Vector2(-moveSpeed,  rb.velocity.y); // Y는 기존 값 유지!
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Jumping()
    {
        // Y축 제한 해제(점프 순간에만)
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (!isJumping)
        {
            Debug.Log($"{gameObject.name} 점프 ");

            Physics2D.IgnoreCollision(this.GetComponent<Collider2D>(), frontZombie.GetComponent<Collider2D>(), true);
            rb.velocity = new Vector2(-moveSpeed, minJumpForce);
            isJumping = true;
        }
    }

    private void Back()
    {
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.velocity = Vector2.right * moveSpeed * 2f; // 충분히 빠르게!
    }

    public void SetState(ZombieState state)
    {
        zombieState = state;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;

        // 1. 앞쪽 Ray (왼쪽)
        Gizmos.color = Color.red;
        Vector3 frontStart = pos - new Vector3(frontRayOffset, 0, 0);
        Gizmos.DrawLine(frontStart, frontStart + Vector3.left * frontRayDistance);

        // 2. 위쪽 Ray (위)
        Gizmos.color = Color.green;
        Vector3 upStart = pos + new Vector3(0, upRayOffset);
        Gizmos.DrawLine(upStart, upStart + Vector3.up * upRayDistance);

        // 3. 아래쪽 Ray (아래)
        Gizmos.color = Color.blue;
        Vector3 downStart = pos - new Vector3(0, downRayOffset, 0);
        Gizmos.DrawLine(downStart, downStart + Vector3.down * downRayDistance);
    }

}
