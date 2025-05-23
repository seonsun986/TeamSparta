using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("총알 설정")]
    public float lifeTime = 3f;

    private Vector3 direction;
    private float speed;
    private int damage;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // 일정 시간 후 자동 파괴
        Destroy(gameObject, lifeTime);
    }

    public void Init(Vector3 moveDirection, float moveSpeed, int bulletDamage)
    {
        direction = moveDirection;
        speed = moveSpeed;
        damage = bulletDamage;

        // 물리적으로 이동
        if (rb != null)
        {
            rb.velocity = direction * speed;
        }

        // 총알 회전 (이동 방향을 향하도록)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 좀비와 충돌
        if (other.CompareTag("Zombie"))
        {
            Health zombieHealth = other.GetComponent<Health>();
            if (zombieHealth != null)
            {
                zombieHealth.TakeDamage(damage);
            }

            // 타격 이펙트 생성 (선택사항)
            CreateHitEffect();

            Destroy(gameObject);
        }
        // 블록이나 벽과 충돌
        else if (other.CompareTag("Block") || other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }

    void CreateHitEffect()
    {
        // 간단한 타격 이펙트 (파티클이나 이미지)
        GameObject hitEffect = new GameObject("HitEffect");
        hitEffect.transform.position = transform.position;

        // 여기에 파티클 시스템이나 애니메이션 추가 가능

        Destroy(hitEffect, 0.5f);
    }
}