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
    public float maxClimbTime = 2f;
    public float forcedLandingHeight = 3f;

    [Header("밀기 관련")]
    public float pushForce = 1.5f;
    public float pushTime = 0.1f;

    [Header("레이어 조정")]
    public bool autoAdjustOrderLayer = true;  
    public float orderLayerScale = 100f;      

    private SpriteRenderer[] allSpriteRenderers; 
    private int[] originalOrderLayers;          
    private int groundOrderOffset;

    // 그룹 설정
    private string myZombieLayer = "Zombie"; 
    private string myGroundLayer = "Ground"; 
    private LayerMask zombieLayerMask;
    private LayerMask groundLayerMask;

    public enum ZombieState
    {
        Moving,
        Climbing,
        Pushed,
        ForcedLanding
    }

    public ZombieState currentState = ZombieState.Moving;
    private Rigidbody2D rb;
    private CapsuleCollider2D col;

    private Transform targetZombie;
    private bool isGrounded = true;
    private bool isClimbing = false;
    private float climbStartTime;

    private float pushedStartTime;

    private Vector2 zombieDetectionRayOrigin;
    private bool hasFrontZombie = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();

        allSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalOrderLayers = new int[allSpriteRenderers.Length];

        for (int i = 0; i < allSpriteRenderers.Length; i++)
        {
            originalOrderLayers[i] = allSpriteRenderers[i].sortingOrder;
        }
        col.size = new Vector2(0.5f, 1.0f);
        col.direction = CapsuleDirection2D.Vertical;
        col.isTrigger = false;

        rb.mass = 1f;
        rb.drag = 0f;
        rb.angularDrag = 0f;
        rb.gravityScale = 2f;
        rb.freezeRotation = true;

        currentState = ZombieState.Moving;

        // 이 부분 추가
        UpdateLayerMasks();
    }

    void Update()
    {
        CheckGrounded();

        if (autoAdjustOrderLayer)
        {
            UpdateOrderLayer();
        }

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

        if (timeSincePushed > pushTime || Mathf.Abs(rb.velocity.x) < 0.3f)
        {
            currentState = ZombieState.Moving;
        }
    }

    void HandleForcedLandingState()
    {
        rb.velocity = new Vector2(-moveSpeed * 0.5f, rb.velocity.y);

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
            zombieLayerMask 
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
        climbStartTime = Time.time;

        Debug.Log("=== 기어올라가기 시작! ===");
    }

    void HandleClimbingState()
    {
        float climbTime = Time.time - climbStartTime;

        bool shouldForceLand =
            climbTime > maxClimbTime ||                        
            transform.position.y > forcedLandingHeight ||      
            targetZombie == null;                              

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

            if (myPos.x < targetPos.x - 1f)
            {
                StartForcedLanding();
                return;
            }
        }

        rb.velocity = climbDirection;

    }

    void StartForcedLanding()
    {
        currentState = ZombieState.ForcedLanding;
        targetZombie = null;
        isClimbing = false;

    }

    void PushAllZombiesAhead()
    {
        if (targetZombie != null)
        {
            Rigidbody2D zombieRb = targetZombie.GetComponent<Rigidbody2D>();
            ZombieController otherZombie = targetZombie.GetComponent<ZombieController>();

            if (zombieRb != null && otherZombie != null)
            {
                otherZombie.GetPushed();

                Vector2 currentVel = zombieRb.velocity;
                zombieRb.velocity = new Vector2(pushForce, currentVel.y);

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
    }

    void BackToMoving()
    {
        currentState = ZombieState.Moving;
        targetZombie = null;
        hasFrontZombie = false;
        isClimbing = false;
    }

    void CheckGrounded()
    {
        RaycastHit2D groundHit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            groundCheckDistance,
            groundLayerMask 
        );

        isGrounded = groundHit.collider != null;
    }

    void FixedUpdate()
    {
        if (rb.velocity.x < -moveSpeed * 4f)
        {
            rb.velocity = new Vector2(-moveSpeed * 4f, rb.velocity.y);
        }
        if (rb.velocity.x > moveSpeed * 3f)
        {
            rb.velocity = new Vector2(moveSpeed * 3f, rb.velocity.y);
        }
    }

    public void SetZombieGroup(string zombieLayer, string groundLayer)
    {
        myZombieLayer = zombieLayer;
        myGroundLayer = groundLayer;
        UpdateLayerMasks();

        SetGroundOrderOffset(groundLayer);
    }

    void SetGroundOrderOffset(string groundLayer)
    {
        switch (groundLayer)
        {
            case "Ground1":
                groundOrderOffset = 1000;   
                break;
            case "Ground2":
                groundOrderOffset = 2000; 
                break;
            case "Ground3":
                groundOrderOffset = 3000;
                break;
            default:
                groundOrderOffset = 0;
                break;
        }
    }

    void UpdateLayerMasks()
    {
        zombieLayerMask = LayerMask.GetMask(myZombieLayer);
        groundLayerMask = LayerMask.GetMask(myGroundLayer);
    }

    void UpdateOrderLayer()
    {
        if (allSpriteRenderers == null || allSpriteRenderers.Length == 0) return;

        int positionOrderLayer = Mathf.RoundToInt(-transform.position.x * orderLayerScale);

        for (int i = 0; i < allSpriteRenderers.Length; i++)
        {
            if (allSpriteRenderers[i] == null) continue;

            int finalOrderLayer = groundOrderOffset + positionOrderLayer + originalOrderLayers[i];

            int minOrder = groundOrderOffset;
            int maxOrder = groundOrderOffset + 999;
            finalOrderLayer = Mathf.Clamp(finalOrderLayer, minOrder, maxOrder);

            allSpriteRenderers[i].sortingOrder = finalOrderLayer;
        }
    }

    void OnDrawGizmos()
    {
        if (col == null) return;

        Vector2 currentZombieRayOrigin = new Vector2(
            transform.position.x - col.size.x * 0.5f,
            transform.position.y + col.size.y * 0.5f
        );

        Gizmos.color = hasFrontZombie ? Color.red : Color.yellow;
        Gizmos.DrawRay(currentZombieRayOrigin, Vector2.left * overlapDistance);

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

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(-10, forcedLandingHeight, 0), new Vector3(10, forcedLandingHeight, 0));

        if (isClimbing && targetZombie != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetZombie.position);
        }
    }
}