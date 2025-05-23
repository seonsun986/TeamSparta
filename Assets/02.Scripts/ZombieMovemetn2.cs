using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ZombieMovemetn2 : MonoBehaviour
{
    // [�ʵ�]
    public float walkSpeed = 2f;
    public float jumpWaitTime = 0.18f;  // �Ӹ� �� ���� �ð�
    public float jump1_XOffset = 0.4f;  // 1�� ��ǥ x (������ ��)
    public float jump1_YOffset = 0.32f; // 1�� ��ǥ y (�Ӹ� ��)
    public float jump2_XOffset = -0.42f; // 2�� ��ǥ x (��)
    public float jump2_YOffset = 0.11f; // 2�� ��ǥ y (�ణ ��)
    public float frontRayLength = 0.62f;
    public float downRayLength = 0.3f;
    public LayerMask zombieLayer, groundLayer;

    private Rigidbody2D rb;
    public Collider2D col;

    private Collider2D frontZombie; // ��ǥ�� ���� ����
    private int jumpStep = 0; // 0=�ƴ�, 1=����1, 2=����2
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
        // �ȴ� ���� ���� Ray�� �� ���� üũ
        if (state == State.Walking)
        {
            Vector2 ray = (Vector2)transform.position + Vector2.left * (col.bounds.extents.x + 0.03f); // �ݶ��̴� ���� �ٱ����� ����

            RaycastHit2D hit = Physics2D.Raycast(ray, Vector2.left, frontRayLength, zombieLayer);
            if (hit && hit.collider != col)
            {
                // �տ� ���� �߰� �� ����!
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

        // 1. JumpFirst: �밢�� ���� (�� ���� ������ �Ӹ� ��)
        if (jumpStep == 1 && !isJumping && frontZombie != null)
        {
            // ���� ��ǥ ����
            Vector2 target = (Vector2)frontZombie.transform.position
                + new Vector2(frontZombie.bounds.size.x * 0.5f + jump1_XOffset, frontZombie.bounds.size.y + jump1_YOffset);
            Vector2 dir = (target - (Vector2)transform.position).normalized;

            float jumpHeight = Mathf.Max(0.17f, target.y - transform.position.y);
            float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            float jumpForce = Mathf.Sqrt(2f * gravity * jumpHeight);

            // [�߿�] ���� ���� IgnoreCollision ON (��/�� ����)
            Physics2D.IgnoreCollision(col, frontZombie, true);
            rb.velocity = new Vector2(dir.x * walkSpeed * 1.38f, jumpForce);

            isJumping = true;
            jumpWaitTimer = 0f;
            // Debug.Log("Jump1! v=" + rb.velocity + ", dir=" + dir);
        }
        else if (jumpStep == 1 && isJumping && frontZombie != null)
        {
            // [�Ӹ� �� ���� �� ����+������]
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
        // 2. JumpSecond: �� ���� ����(����+��¦ ��)
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
        // 3. JumpSecond ����(�Ʒ� Ray)
        else if (jumpStep == 2 && isJumping && frontZombie != null)
        {
            RaycastHit2D down = Physics2D.Raycast(transform.position, Vector2.down, downRayLength, groundLayer | zombieLayer);
            if (down)
            {
                // [����: �� ���� �ε巴�� �б�]
                var frontRb = frontZombie.GetComponent<Rigidbody2D>();
                if (frontRb != null)
                    frontRb.velocity = new Vector2(frontRb.velocity.x + 0.65f, frontRb.velocity.y);

                Physics2D.IgnoreCollision(col, frontZombie, false); // IgnoreCollision ����

                state = State.Walking;
                jumpStep = 0;
                isJumping = false;
                frontZombie = null;
            }
        }
    }

    // [����׿� Ray �ð�ȭ]
    void OnDrawGizmosSelected()
    {
        Vector3 pos = (Vector2)transform.position + Vector2.left * (col.bounds.extents.x + 0.03f); // �ݶ��̴� ���� �ٱ����� ����

        Gizmos.color = Color.red;
        Gizmos.DrawLine(pos, pos + Vector3.left * frontRayLength);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(pos, pos + Vector3.down * downRayLength);
    }
}
