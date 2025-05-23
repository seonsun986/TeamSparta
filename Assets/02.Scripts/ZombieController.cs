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
    public float maxClimbTime = 2f;
    public float forcedLandingHeight = 3f;

    [Header("�б� ����")]
    public float pushForce = 1.5f;
    public float pushTime = 0.1f;

    [Header("���̾� ����")]
    public bool autoAdjustOrderLayer = true;  // Order Layer �ڵ� ���� ����
    public float orderLayerScale = 100f;      // ��ġ �� Order Layer ��ȯ ����

    private SpriteRenderer[] allSpriteRenderers;  // ��� SpriteRenderer �迭
    private int[] originalOrderLayers;            // ���� Order Layer ����
    private int groundOrderOffset;  // Ground�� �⺻ ������

    // �׷� ����
    private string myZombieLayer = "Zombie";  // �⺻��
    private string myGroundLayer = "Ground";  // �⺻��
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

        // ���� Order Layer ���� (����� ���� ������)
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

        // �� �κ� �߰�
        UpdateLayerMasks();
    }

    void Update()
    {
        CheckGrounded();

        // Order Layer �ڵ� ����
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
        // �� �����Ӹ��� velocity �������� ����, �ڿ������� ���α�
        float timeSincePushed = Time.time - pushedStartTime;

        // �ð��� �����ų� �ӵ��� ����� �پ��� Moving ���·� ����
        if (timeSincePushed > pushTime || Mathf.Abs(rb.velocity.x) < 0.3f)
        {
            currentState = ZombieState.Moving;
            Debug.Log($"���� {gameObject.name} �и� ���¿��� ����!");
        }

        Debug.Log($"�и� ����: {timeSincePushed:F2}��, �ӵ�: {rb.velocity.x:F2}");
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
            groundLayerMask 
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

    // �����ʿ��� ȣ���ϴ� �Լ�
    public void SetZombieGroup(string zombieLayer, string groundLayer)
    {
        myZombieLayer = zombieLayer;
        myGroundLayer = groundLayer;
        UpdateLayerMasks();

        SetGroundOrderOffset(groundLayer);

        Debug.Log($"���� �׷� ����: {zombieLayer}, �ٴ�: {groundLayer}");
    }

    void SetGroundOrderOffset(string groundLayer)
    {
        switch (groundLayer)
        {
            case "Ground1":
                groundOrderOffset = 1000;      // ���� �� (0~999)
                break;
            case "Ground2":
                groundOrderOffset = 2000;   // �߰� (1000~1999)
                break;
            case "Ground3":
                groundOrderOffset = 3000;   // ���� �� (2000~2999)
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

        // X ��ġ�� ���� �⺻ Order Layer
        int positionOrderLayer = Mathf.RoundToInt(-transform.position.x * orderLayerScale);

        // ��� SpriteRenderer�� ����
        for (int i = 0; i < allSpriteRenderers.Length; i++)
        {
            if (allSpriteRenderers[i] == null) continue;

            // ���� Order Layer = Ground ������ + ��ġ�� Order Layer + ���� ��� ����
            int finalOrderLayer = groundOrderOffset + positionOrderLayer + originalOrderLayers[i];

            // ���� ���� (�� Ground�� 1000 ���� �ȿ���)
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