using System.Drawing;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("총알 설정")]
    public float lifeTime = 5f;           // 총알 생존 시간
    public LayerMask enemyLayers;         // 적 레이어 마스크

    private Vector3 direction;
    private float speed;
    private int damage;
    private Rigidbody2D rb;
    private bool hasHit = false;          // 이미 타격했는지 체크

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Vector3 moveDirection, float moveSpeed, int bulletDamage)
    {
        direction = moveDirection.normalized;
        speed = moveSpeed;
        damage = bulletDamage;

        if (rb != null)
        {
            rb.velocity = direction * speed;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Debug.Log($"총알 생성: 방향={direction}, 속도={speed}");
    }

    void Update()
    {
        if (rb == null || rb.velocity.magnitude < speed * 0.5f)
        {
            transform.position += direction * speed * Time.deltaTime;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // 좀비와 충돌
        if (other.CompareTag("Zombie"))
        {
            hasHit = true;

            Health zombieHealth = other.GetComponent<Health>();
            if (zombieHealth != null)
            {
                zombieHealth.TakeDamage(damage);
                Debug.Log($"<color=yellow>좀비 {other.name}에게 {damage} 데미지!</color>");
            }

            DestroyBullet();
        }
        // 블록이나 벽과 충돌
        else if(other.CompareTag("Ground"))
        {
            hasHit = true;
            Debug.Log("총알이 바닥에 충돌");

            DestroyBullet();
        }

        else
        {
            Debug.Log($"<color=red> {other.name}에게 충돌!</color>");

        }
    }

    void DestroyBullet()
    {
        Destroy(gameObject);
    }

    void OnBecameInvisible()
    {
        if (!hasHit)
        {
            Destroy(gameObject);
        }
    }
}