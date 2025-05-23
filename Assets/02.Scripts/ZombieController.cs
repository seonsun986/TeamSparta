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
    public bool autoAdjustOrderLayer = true;  // Order Layer 자동 조정 여부
    public float orderLayerScale = 100f;      // 위치 → Order Layer 변환 비율

    private SpriteRenderer[] allSpriteRenderers;  // 모든 SpriteRenderer 배열
    private int[] originalOrderLayers;            // 원본 Order Layer 저장
    private int groundOrderOffset;  // Ground별 기본 오프셋

    // 그룹 설정
    private string myZombieLayer = "Zombie";  // 기본값
    private string myGroundLayer = "Ground";  // 기본값
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

        // 원본 Order Layer 저장 (상대적 순서 유지용)
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

        // Order Layer 자동 조정
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
        // 매 프레임마다 velocity 설정하지 말고, 자연스럽게 놔두기
        float timeSincePushed = Time.time - pushedStartTime;

        // 시간이 지나거나 속도가 충분히 줄어들면 Moving 상태로 복귀
        if (timeSincePushed > pushTime || Mathf.Abs(rb.velocity.x) < 0.3f)
        {
            currentState = ZombieState.Moving;
            Debug.Log($"좀비 {gameObject.name} 밀린 상태에서 복귀!");
        }

        Debug.Log($"밀린 상태: {timeSincePushed:F2}초, 속도: {rb.velocity.x:F2}");
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
            groundLayerMask 
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

    // 스포너에서 호출하는 함수
    public void SetZombieGroup(string zombieLayer, string groundLayer)
    {
        myZombieLayer = zombieLayer;
        myGroundLayer = groundLayer;
        UpdateLayerMasks();

        SetGroundOrderOffset(groundLayer);

        Debug.Log($"좀비 그룹 설정: {zombieLayer}, 바닥: {groundLayer}");
    }

    void SetGroundOrderOffset(string groundLayer)
    {
        switch (groundLayer)
        {
            case "Ground1":
                groundOrderOffset = 1000;      // 가장 뒤 (0~999)
                break;
            case "Ground2":
                groundOrderOffset = 2000;   // 중간 (1000~1999)
                break;
            case "Ground3":
                groundOrderOffset = 3000;   // 가장 앞 (2000~2999)
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

        // X 위치에 따른 기본 Order Layer
        int positionOrderLayer = Mathf.RoundToInt(-transform.position.x * orderLayerScale);

        // 모든 SpriteRenderer에 적용
        for (int i = 0; i < allSpriteRenderers.Length; i++)
        {
            if (allSpriteRenderers[i] == null) continue;

            // 최종 Order Layer = Ground 오프셋 + 위치별 Order Layer + 원본 상대 순서
            int finalOrderLayer = groundOrderOffset + positionOrderLayer + originalOrderLayers[i];

            // 범위 제한 (각 Ground당 1000 범위 안에서)
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