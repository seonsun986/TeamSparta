using System.Drawing;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("�Ѿ� ����")]
    public float lifeTime = 5f;           // �Ѿ� ���� �ð�
    public LayerMask enemyLayers;         // �� ���̾� ����ũ

    private Vector3 direction;
    private float speed;
    private int damage;
    private Rigidbody2D rb;
    private bool hasHit = false;          // �̹� Ÿ���ߴ��� üũ

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

        Debug.Log($"�Ѿ� ����: ����={direction}, �ӵ�={speed}");
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

        // ����� �浹
        if (other.CompareTag("Zombie"))
        {
            hasHit = true;

            Health zombieHealth = other.GetComponent<Health>();
            if (zombieHealth != null)
            {
                zombieHealth.TakeDamage(damage);
                Debug.Log($"<color=yellow>���� {other.name}���� {damage} ������!</color>");
            }

            DestroyBullet();
        }
        // ����̳� ���� �浹
        else if(other.CompareTag("Ground"))
        {
            hasHit = true;
            Debug.Log("�Ѿ��� �ٴڿ� �浹");

            DestroyBullet();
        }

        else
        {
            Debug.Log($"<color=red> {other.name}���� �浹!</color>");

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