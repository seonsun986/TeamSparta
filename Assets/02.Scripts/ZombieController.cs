using UnityEngine;

public class ZombieController : MonoBehaviour
{
    [Header("������ ����")]
    public float moveSpeed = 2f;
    public float climbSpeed = 3f;
    public float overlapDistance = 0.6f;

    [Header("�������� ����")]
    public float climbForce = 5f;
    public float groundCheckDistance = 0.2f;
    public float pushRecoveryTime = 1f;
    public float maxClimbTime = 2f;
    public float forcedLandingHeight = 3f;

    [Header("�б� ����")]
    public float pushForce = 1.5f;

    public enum ZombieState
    {
        Moving,
        Climbing,
        Pushed,
        ForcedLanding
    }

    private ZombieState currentState = ZombieState.Moving;
    private Rigidbody2D rb;
    private CapsuleCollider2D col;

    // ���ö󰡱� ����
    private Transform targetZombie;
    private bool isGrounded = true;
    private bool isClimbing = false;
    private float climbStartTime;

    // �и� ���� ����
    private float pushedStartTime;

    // ������
    private Vector2 zombieDetectionRayOrigin;
    private bool hasFrontZombie = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();

        col.size = new Vector2(0.5f, 1.0f);
        col.direction = CapsuleDirection2D.Vertical;
        col.isTrigger = false;

        rb.mass = 1f;
        rb.drag = 0f;
        rb.angularDrag = 0f;
        rb.gravityScale = 2f;
        rb.freezeRotation = true;

        currentState = ZombieState.Moving;
    }

    void Update()
    {
        CheckGrounded();

        switch (currentState)
        {
            case ZombieState.Moving:
                HandleMovingState();
                break;

            case ZombieState.Climbing:
                HandleClimbingState();
                break;

            case ZombieState.Pushed:
                HandlePushedState();
                break;

            case ZombieState.ForcedLanding:
                HandleForcedLandingState();
                break;
        }
    }

    void HandleMovingState()
    {
        if (isGrounded)
        {
            rb.velocity = new Vector2(-moveSpeed, rb.velocity.y);
        }

        if (CheckForZombieAhead())
        {
            StartClimbing();
        }
    }

    void HandlePushedState()
    {
        float timeSincePushed = Time.time - pushedStartTime;

        if (timeSincePushed > pushRecoveryTime || Mathf.Abs(rb.velocity.x) < 0.5f)
        {
            currentState = ZombieState.Moving;
            Debug.Log($"���� {gameObject.name} �и� ���¿��� ����!");
        }
    }

    void HandleForcedLandingState()
    {
        // ������ �ٴ����� ��������
        rb.velocity = new Vector2(-moveSpeed * 0.5f, rb.velocity.y);

        // �ٴڿ� ���������� Moving ���·�
        if (isGrounded && rb.velocity.y <= 0.5f)
        {
            BackToMoving();
        }

        Debug.Log($"���� ���� ��: {transform.position.y:F1}, ������: {isGrounded}");
    }

    bool CheckForZombieAhead()
    {
        zombieDetectionRayOrigin = new Vector2(
            transform.position.x - col.size.x * 0.5f,
            transform.position.y + col.size.y * 0.5f
        );

        RaycastHit2D hit = Physics2D.Raycast(
            zombieDetectionRayOrigin,
            Vector2.left,
            overlapDistance,
            LayerMask.GetMask("Zombie")
        );

        hasFrontZombie = hit.collider != null && hit.collider.gameObject != gameObject;

        if (hasFrontZombie)
        {
            targetZombie = hit.collider.transform;
            return true;
        }

        return false;
    }

    void StartClimbing()
    {
        currentState = ZombieState.Climbing;
        isClimbing = true;
        climbStartTime = Time.time; // ���ö󰡱� ���� �ð� ���

        Debug.Log("=== ���ö󰡱� ����! ===");
    }

    void HandleClimbingState()
    {
        float climbTime = Time.time - climbStartTime;

        // ���� ���� ���ǵ�
        bool shouldForceLand =
            climbTime > maxClimbTime ||                           // �ð� �ʰ�
            transform.position.y > forcedLandingHeight ||         // �ʹ� ���� �ö�
            targetZombie == null;                                 // ��� ���� �����

        if (shouldForceLand)
        {
            StartForcedLanding();
            return;
        }

        Vector2 targetPos = targetZombie.position;
        Vector2 myPos = transform.position;

        Vector2 climbDirection;

        if (myPos.y < targetPos.y + 0.8f)
        {
            climbDirection = new Vector2(-climbSpeed * 0.3f, climbSpeed);
            PushAllZombiesAhead();
        }
        else
        {
            climbDirection = new Vector2(-climbSpeed, 0);
            PushAllZombiesAhead();

            // ����� ������ ������ ���� ����
            if (myPos.x < targetPos.x - 1f)
            {
                StartForcedLanding();
                return;
            }
        }

        rb.velocity = climbDirection;

        Debug.Log($"���ö󰡴� ��: {climbTime:F1}��, ����: {transform.position.y:F1}");
    }

    void StartForcedLanding()
    {
        currentState = ZombieState.ForcedLanding;
        targetZombie = null;
        isClimbing = false;

        Debug.Log($"=== ���� {gameObject.name} ���� ���� ����! ===");
    }

    void PushAllZombiesAhead()
    {
        // targetZombie�� �б�
        if (targetZombie != null)
        {
            Rigidbody2D zombieRb = targetZombie.GetComponent<Rigidbody2D>();
            ZombieController otherZombie = targetZombie.GetComponent<ZombieController>();

            if (zombieRb != null && otherZombie != null)
            {
                // ��� ���� Pushed ���·� �����
                otherZombie.GetPushed();

                // velocity ���� ����
                Vector2 currentVel = zombieRb.velocity;
                zombieRb.velocity = new Vector2(pushForce, currentVel.y);

                Debug.Log($"Ÿ�� ���� {targetZombie.name} �б�! �� �ӵ�: {zombieRb.velocity}");
            }
        }
    }

    public void GetPushed()
    {
        currentState = ZombieState.Pushed;
        pushedStartTime = Time.time;
        targetZombie = null;
        hasFrontZombie = false;
        isClimbing = false;

        Debug.Log($"���� {gameObject.name} �и�!");
    }

    void BackToMoving()
    {
        currentState = ZombieState.Moving;
        targetZombie = null;
        hasFrontZombie = false;
        isClimbing = false;
        Debug.Log("=== �̵� ���·� ����! ===");
    }

    void CheckGrounded()
    {
        RaycastHit2D groundHit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            groundCheckDistance,
            LayerMask.GetMask("Ground")
        );

        isGrounded = groundHit.collider != null;
    }

    void FixedUpdate()
    {
        // ���� �ӵ� ����
        if (rb.velocity.x < -moveSpeed * 4f)
        {
            rb.velocity = new Vector2(-moveSpeed * 4f, rb.velocity.y);
        }
        if (rb.velocity.x > moveSpeed * 3f)
        {
            rb.velocity = new Vector2(moveSpeed * 3f, rb.velocity.y);
        }
    }

    void OnDrawGizmos()
    {
        if (col == null) return;

        Vector2 currentZombieRayOrigin = new Vector2(
            transform.position.x - col.size.x * 0.5f,
            transform.position.y + col.size.y * 0.5f
        );

        // ���� ���� ����
        Gizmos.color = hasFrontZombie ? Color.red : Color.yellow;
        Gizmos.DrawRay(currentZombieRayOrigin, Vector2.left * overlapDistance);

        // ���� ǥ��
        switch (currentState)
        {
            case ZombieState.Moving:
                Gizmos.color = Color.green;
                break;
            case ZombieState.Climbing:
                Gizmos.color = Color.yellow;
                break;
            case ZombieState.Pushed:
                Gizmos.color = Color.red;
                break;
            case ZombieState.ForcedLanding:
                Gizmos.color = Color.blue;
                break;
        }
        Gizmos.DrawWireSphere(transform.position, 0.15f);

        // ���� ���� ���� ǥ��
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(-10, forcedLandingHeight, 0), new Vector3(10, forcedLandingHeight, 0));

        if (isClimbing && targetZombie != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetZombie.position);
        }
    }
}