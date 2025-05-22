using UnityEngine;

public class ZombieController : MonoBehaviour
{
    [Header("움직임 관련")]
    public float moveSpeed = 2f;
    public float climbSpeed = 3f;
    public float overlapDistance = 0.6f;

    [Header("기어오르기 관련")]
    public float climbForce = 5f;
    public float groundCheckDistance = 0.2f;
    public float pushRecoveryTime = 1f;
    public float maxClimbTime = 2f;
    public float forcedLandingHeight = 3f;

    [Header("밀기 관련")]
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

    // 기어올라가기 관련
    private Transform targetZombie;
    private bool isGrounded = true;
    private bool isClimbing = false;
    private float climbStartTime;

    // 밀린 상태 관련
    private float pushedStartTime;

    // 기즈모용
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
            Debug.Log($"좀비 {gameObject.name} 밀린 상태에서 복귀!");
        }
    }

    void HandleForcedLandingState()
    {
        // 강제로 바닥으로 내려가기
        rb.velocity = new Vector2(-moveSpeed * 0.5f, rb.velocity.y);

        // 바닥에 착지했으면 Moving 상태로
        if (isGrounded && rb.velocity.y <= 0.5f)
        {
            BackToMoving();
        }

        Debug.Log($"강제 착지 중: {transform.position.y:F1}, 착지됨: {isGrounded}");
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
        climbStartTime = Time.time; // 기어올라가기 시작 시간 기록

        Debug.Log("=== 기어올라가기 시작! ===");
    }

    void HandleClimbingState()
    {
        float climbTime = Time.time - climbStartTime;

        // 강제 착지 조건들
        bool shouldForceLand =
            climbTime > maxClimbTime ||                           // 시간 초과
            transform.position.y > forcedLandingHeight ||         // 너무 높이 올라감
            targetZombie == null;                                 // 대상 좀비 사라짐

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

            // 충분히 앞으로 갔으면 강제 착지
            if (myPos.x < targetPos.x - 1f)
            {
                StartForcedLanding();
                return;
            }
        }

        rb.velocity = climbDirection;

        Debug.Log($"기어올라가는 중: {climbTime:F1}초, 높이: {transform.position.y:F1}");
    }

    void StartForcedLanding()
    {
        currentState = ZombieState.ForcedLanding;
        targetZombie = null;
        isClimbing = false;

        Debug.Log($"=== 좀비 {gameObject.name} 강제 착지 시작! ===");
    }

    void PushAllZombiesAhead()
    {
        // targetZombie만 밀기
        if (targetZombie != null)
        {
            Rigidbody2D zombieRb = targetZombie.GetComponent<Rigidbody2D>();
            ZombieController otherZombie = targetZombie.GetComponent<ZombieController>();

            if (zombieRb != null && otherZombie != null)
            {
                // 대상 좀비를 Pushed 상태로 만들기
                otherZombie.GetPushed();

                // velocity 직접 설정
                Vector2 currentVel = zombieRb.velocity;
                zombieRb.velocity = new Vector2(pushForce, currentVel.y);

                Debug.Log($"타겟 좀비 {targetZombie.name} 밀기! 새 속도: {zombieRb.velocity}");
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

        Debug.Log($"좀비 {gameObject.name} 밀림!");
    }

    void BackToMoving()
    {
        currentState = ZombieState.Moving;
        targetZombie = null;
        hasFrontZombie = false;
        isClimbing = false;
        Debug.Log("=== 이동 상태로 복귀! ===");
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
        // 수평 속도 제한
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

        // 좀비 감지 레이
        Gizmos.color = hasFrontZombie ? Color.red : Color.yellow;
        Gizmos.DrawRay(currentZombieRayOrigin, Vector2.left * overlapDistance);

        // 상태 표시
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

        // 강제 착지 높이 표시
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(-10, forcedLandingHeight, 0), new Vector3(10, forcedLandingHeight, 0));

        if (isClimbing && targetZombie != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetZombie.position);
        }
    }
}