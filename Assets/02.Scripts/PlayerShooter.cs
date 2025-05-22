using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    public float fireRate = 0.3f;              // �ʴ� �߻� ����
    public float bulletSpeed = 15f;
    public GameObject bulletPrefab;
    public Transform firePoint;                // �Ѿ� ������ ��ġ

    private float fireTimer = 0f;
    private Vector2? targetPoint = null;       // ���콺/��ġ�� ������ ��ġ (������ �ڵ� ����)
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        fireTimer += Time.deltaTime;

        // 1. ȭ���� ������(��ġ/Ŭ��) Ÿ���� ��ġ ������Ʈ
        if (Input.GetMouseButton(0))  // ��ġ�� ��������
        {
            Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            targetPoint = new Vector2(worldPos.x, worldPos.y);
        }
        else
        {
            targetPoint = null;
        }

        // 2. �߻� �ֱ� üũ
        if (fireTimer >= fireRate)
        {
            if (targetPoint.HasValue)
            {
                FireToPoint(targetPoint.Value);   // ���� ���� ����
            }
            else
            {
                // �ڵ� ����: ����� ���� ã�Ƽ� ���
                ZombieController nearestZombie = FindNearestZombie();
                if (nearestZombie != null)
                {
                    FireToPoint(nearestZombie.transform.position);
                }
            }

            fireTimer = 0f;
        }
    }

    // ���� ��ġ�� �Ѿ� �߻�
    void FireToPoint(Vector2 point)
    {
        Vector2 dir = ((Vector2)point - (Vector2)firePoint.position).normalized;
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.GetComponent<Rigidbody2D>().velocity = dir * bulletSpeed;
    }

    // �� ���� ��� ���� �� ���� ����� �� ã��
    ZombieController FindNearestZombie()
    {
        ZombieController[] zombies = FindObjectsOfType<ZombieController>();
        ZombieController closest = null;
        float minDist = float.MaxValue;

        foreach (var z in zombies)
        {
            float dist = Vector2.Distance(transform.position, z.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = z;
            }
        }
        return closest;
    }
}
