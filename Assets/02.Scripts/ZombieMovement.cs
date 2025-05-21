using Unity.Burst.Intrinsics;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ZombieMovement : MonoBehaviour
{
    public float moveSpeed = 2f;                  // 걷는 속도
    public float minJumpForce = 5f;               // 최소 점프 파워(혹시 모르니 바닥에도 쓸 수 있게)
    public float rayDistance = 0.5f;              // 앞 좀비 감지 거리
    public LayerMask zombieLayer;                 // 좀비 레이어
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool isJumping = false;

    private GameObject frontZombie;
    private GameObject backZombie;

    public enum ZombieState
    {
        Walking,
        Jumping,
        Back
    }


    private ZombieState zombieState = ZombieState.Walking;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Debug.Log(gameObject.name + " : " + rb.velocity);
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
        rb.velocity = new Vector2(-moveSpeed, 0);
        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
    }

    private void Jumping()
    {
        // Y축 제한 해제(점프 순간에만)
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (!isJumping)
        {
            Debug.Log($"{gameObject.name} 점프 ");
            // 대각선 왼쪽 위 점프를 더 강하게, X축 이동력을 확실히 주기!
            float jumpX = -moveSpeed * 1.5f;  // ← 튜닝!
            float jumpY = minJumpForce;       // 점프력
            Debug.Log($"점프 전 moveSpeed: {moveSpeed}");
            rb.velocity = new Vector2(-moveSpeed, minJumpForce);
            Debug.Log($"점프 직후 velocity: {rb.velocity}");
            isJumping = true;
        }
        else
        {
            Debug.Log($"{gameObject.name} 점프 중");
        }
    }

    private void Back()
    {
        Debug.Log($"{gameObject.name} 뒤로");

        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;

        rb.velocity = Vector2.right * moveSpeed;


    }

    public void SetState(ZombieState state)
    {
        zombieState = state;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        foreach(var contact in collision.contacts)
        {
            if (contact.normal.x > 0.7f)
            {
                Debug.Log($"{gameObject.name} 기준 왼쪽으로 {collision.gameObject.name}이 닿음");

                CheckOnLeft(collision);
            }

            if (contact.normal.y < -0.7f)
            {
                Debug.Log($"{gameObject.name} 기준 위쪽으로 {collision.gameObject.name}이 닿음");
                CheckOnUp(collision);
            }

            if (contact.normal.y > 0.7f)
            {
                Debug.Log($"{gameObject.name} 기준 아래쪽으로 {collision.gameObject.name}이 닿음");
                CheckDown(collision);

            }
        }
    }

    private void CheckOnLeft(Collision2D col)
    {
        if(col.collider.CompareTag("Zombie"))
        {
            if (zombieState == ZombieState.Walking)
            {
                frontZombie = col.gameObject;
                SetState(ZombieState.Jumping);
            }

            else if (zombieState == ZombieState.Back)
            {
                zombieState = ZombieState.Walking;
            }
        }

    }

    private void CheckOnUp(Collision2D col)
    {
        if(col.collider.CompareTag("Zombie"))
        {
            if(zombieState == ZombieState.Walking)
            {
                SetState(ZombieState.Back);
            }
        }
    }

    private void CheckDown(Collision2D col)
    {
        // Jump -> Walk(바닥에 닿았을 때, y값 변화)
        if(col.collider.CompareTag("Ground"))
        {
            if (zombieState == ZombieState.Jumping && isJumping)
            {
                SetState(ZombieState.Walking);
                isJumping = false;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Ray 시각화 (디버그용)
        Gizmos.color = Color.red;
        Vector2 left = (Vector2)transform.position - new Vector2(0.7f, 0);

        Gizmos.DrawLine(left, left + Vector2.left * rayDistance);

        Gizmos.color = Color.blue;
        Vector2 down = (Vector2)transform.position - new Vector2(0, 0.5f);

        Gizmos.DrawLine(down, down + Vector2.down * rayDistance);
    }
}
