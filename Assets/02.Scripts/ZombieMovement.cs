using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ZombieMovement : MonoBehaviour
{
    public string zombieName = "aaa"; // 좀비 이름   

    [Header("이동 속도")]
    public float walkSpeed = 2f;    // 좌(–) 방향 걷기 속도
    public float jumpSpeed = 2f;    // 수직 오르기 속도
    public float descendSpeed = 3f;    // 수직 내려오기 속도

    [Header("밀어내기 세팅")]
    public float pushBackForce = 5f;    // 앞좀비를 뒤로 밀어낼 힘

    [Header("센서 세팅")]
    public LayerMask groundLayer;       // 블록/바닥/좀비 레이어 포함
    public float sensorDistance = 0.1f; // Ray 길이

    enum State { Walking, Jump }
    State state = State.Walking;

    Rigidbody2D rb;
    Collider2D col;
    float halfHeight;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        // Dynamic으로 두어 AddForce가 먹히게
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        halfHeight = col.bounds.extents.y;
    }

    void Update()
    {
        switch (state)
        {
            case State.Walking:
                HandleWalking();
                break;

            case State.Jump:
                HandleJumping();
                break;
        }
        UpdateSortingOrder();
    }

    void HandleWalking()
    {
        rb.gravityScale = 0f;
        rb.velocity = Vector2.left * walkSpeed;

        Physics2D.queriesStartInColliders = false;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.left, sensorDistance);

        if (hit)
        {
            if (hit.collider.tag == "Zombie")
            {
                Debug.Log($"{zombieName} 앞에 있는 좀비 발견 {hit.collider.name}");
                state = State.Jump;
            }

            else
            {

            }
        }


    }

    void HandleJumping()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
    }


    void UpdateSortingOrder()
    {
        //// y값 기반으로 렌더링 순서 자동 조절
        //var sr = GetComponent<SpriteRenderer>();
        //sr.sortingOrder = -(int)(transform.position.y * 100);
    }

    void OnDrawGizmosSelected()
    {
        // 디버그용 Ray 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + Vector2.left * sensorDistance);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Zombie"))
        {
            // 좀비와 충돌 시 밀어내기
            Rigidbody2D otherRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (otherRb != null)
            {
                Vector2 pushDirection = (Vector2)transform.position - (Vector2)collision.transform.position;
                otherRb.AddForce(pushDirection.normalized * pushBackForce, ForceMode2D.Impulse);
                state = State.Walking; // 밀어내기 후 다시 걷기 상태로 전환
            }
        }
    }
}