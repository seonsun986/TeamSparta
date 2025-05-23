using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("�Ѿ� ����")]
    public float lifeTime = 3f;

    private Vector3 direction;
    private float speed;
    private int damage;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // ���� �ð� �� �ڵ� �ı�
        Destroy(gameObject, lifeTime);
    }

    public void Init(Vector3 moveDirection, float moveSpeed, int bulletDamage)
    {
        direction = moveDirection;
        speed = moveSpeed;
        damage = bulletDamage;

        // ���������� �̵�
        if (rb != null)
        {
            rb.velocity = direction * speed;
        }

        // �Ѿ� ȸ�� (�̵� ������ ���ϵ���)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // ����� �浹
        if (other.CompareTag("Zombie"))
        {
            Health zombieHealth = other.GetComponent<Health>();
            if (zombieHealth != null)
            {
                zombieHealth.TakeDamage(damage);
            }

            // Ÿ�� ����Ʈ ���� (���û���)
            CreateHitEffect();

            Destroy(gameObject);
        }
        // ����̳� ���� �浹
        else if (other.CompareTag("Block") || other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }

    void CreateHitEffect()
    {
        // ������ Ÿ�� ����Ʈ (��ƼŬ�̳� �̹���)
        GameObject hitEffect = new GameObject("HitEffect");
        hitEffect.transform.position = transform.position;

        // ���⿡ ��ƼŬ �ý����̳� �ִϸ��̼� �߰� ����

        Destroy(hitEffect, 0.5f);
    }
}