using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ZombieMovemetn2 : MonoBehaviour
{
    // [필드]
    public float walkSpeed = 2f;
    public float jumpWaitTime = 0.18f;  // 머리 위 멈춤 시간
    public float jump1_XOffset = 0.4f;  // 1차 목표 x (오른쪽 위)
    public float jump1_YOffset = 0.32f; // 1차 목표 y (머리 위)
    public float jump2_XOffset = -0.42f; // 2차 목표 x (앞)
    public float jump2_YOffset = 0.11f; // 2차 목표 y (약간 위)
    public float frontRayLength = 0.62f;
    public float downRayLength = 0.3f;
    public LayerMask zombieLayer, groundLayer;

    private Rigidbody2D rb;
    public Collider2D col;

    private Collider2D frontZombie; // 목표로 삼을 좀비
    private int jumpStep = 0; // 0=아님, 1=점프1, 2=점프2
    private float jumpWaitTimer = 0f;
    private bool isJumping = false;

    enum State { Walking, Jumping }
    private State state = State.Walking;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        // 걷는 중일 때만 Ray로 앞 좀비 체크
        if (state == State.Walking)
        {
            Vector2 ray = (Vector2)transform.position + Vector2.left * (col.bounds.extents.x + 0.03f); // 콜라이더 왼쪽 바깥에서 시작

            RaycastHit2D hit = Physics2D.Raycast(ray, Vector2.left, frontRayLength, zombieLayer);
            if (hit && hit.collider != col)
            {
                // 앞에 좀비 발견 → 점프!
                frontZombie = hit.collider;
                jumpStep = 1;
                isJumping = false;
                state = State.Jumping;
            }
        }
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case State.Walking:
                rb.velocity = new Vector2(-walkSpeed, rb.velocity.y);
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                break;
            case State.Jumping:
                JumpFSM();
                break;
        }
    }

    void JumpFSM()
    {
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 1. JumpFirst: 대각선 위로 (앞 좀비 오른쪽 머리 위)
        if (jumpStep == 1 && !isJumping && frontZombie != null)
        {
            // 점프 목표 지점
            Vector2 target = (Vector2)frontZombie.transform.position
                + new Vector2(frontZombie.bounds.size.x * 0.5f + jump1_XOffset, frontZombie.bounds.size.y + jump1_YOffset);
            Vector2 dir = (target - (Vector2)transform.position).normalized;

            float jumpHeight = Mathf.Max(0.17f, target.y - transform.position.y);
            float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            float jumpForce = Mathf.Sqrt(2f * gravity * jumpHeight);

            // [중요] 점프 직전 IgnoreCollision ON (내/앞 좀비)
            Physics2D.IgnoreCollision(col, frontZombie, true);
            rb.velocity = new Vector2(dir.x * walkSpeed * 1.38f, jumpForce);

            isJumping = true;
            jumpWaitTimer = 0f;
            // Debug.Log("Jump1! v=" + rb.velocity + ", dir=" + dir);
        }
        else if (jumpStep == 1 && isJumping && frontZombie != null)
        {
            // [머리 위 도달 시 멈춤+딜레이]
            Vector2 headTarget = (Vector2)frontZombie.transform.position
                + new Vector2(frontZombie.bounds.size.x * 0.5f + jump1_XOffset, frontZombie.bounds.size.y + jump1_YOffset);

            if (Vector2.Distance(transform.position, headTarget) < 0.13f)
            {
                rb.velocity = Vector2.zero;
                jumpWaitTimer += Time.fixedDeltaTime;
                if (jumpWaitTimer > jumpWaitTime)
                {
                    jumpStep = 2;
                    isJumping = false;
                }
            }
        }
        // 2. JumpSecond: 앞 좀비 앞쪽(왼쪽+살짝 위)
        else if (jumpStep == 2 && !isJumping && frontZombie != null)
        {
            Vector2 target = (Vector2)frontZombie.transform.position
                + new Vector2(-frontZombie.bounds.size.x * 0.5f + jump2_XOffset, jump2_YOffset);
            Vector2 dir = (target - (Vector2)transform.position).normalized;

            float jumpHeight = Mathf.Max(0.09f, target.y - transform.position.y);
            float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            float jumpForce = Mathf.Sqrt(2f * gravity * jumpHeight);

            rb.velocity = new Vector2(dir.x * walkSpeed * 1.18f, jumpForce);

            isJumping = true;
            // Debug.Log("Jump2! v=" + rb.velocity + ", dir=" + dir);
        }
        // 3. JumpSecond 착지(아래 Ray)
        else if (jumpStep == 2 && isJumping && frontZombie != null)
        {
            RaycastHit2D down = Physics2D.Raycast(transform.position, Vector2.down, downRayLength, groundLayer | zombieLayer);
            if (down)
            {
                // [착지: 앞 좀비 부드럽게 밀기]
                var frontRb = frontZombie.GetComponent<Rigidbody2D>();
                if (frontRb != null)
                    frontRb.velocity = new Vector2(frontRb.velocity.x + 0.65f, frontRb.velocity.y);

                Physics2D.IgnoreCollision(col, frontZombie, false); // IgnoreCollision 해제

                state = State.Walking;
                jumpStep = 0;
                isJumping = false;
                frontZombie = null;
            }
        }
    }

    // [디버그용 Ray 시각화]
    void OnDrawGizmosSelected()
    {
        Vector3 pos = (Vector2)transform.position + Vector2.left * (col.bounds.extents.x + 0.03f); // 콜라이더 왼쪽 바깥에서 시작

        Gizmos.color = Color.red;
        Gizmos.DrawLine(pos, pos + Vector3.left * frontRayLength);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(pos, pos + Vector3.down * downRayLength);
    }
}
