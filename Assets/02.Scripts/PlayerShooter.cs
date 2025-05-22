using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    public float fireRate = 0.3f;              // 초당 발사 간격
    public float bulletSpeed = 15f;
    public GameObject bulletPrefab;
    public Transform firePoint;                // 총알 나가는 위치

    private float fireTimer = 0f;
    private Vector2? targetPoint = null;       // 마우스/터치로 지정된 위치 (없으면 자동 조준)
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        fireTimer += Time.deltaTime;

        // 1. 화면을 누르면(터치/클릭) 타겟팅 위치 업데이트
        if (Input.GetMouseButton(0))  // 터치도 마찬가지
        {
            Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            targetPoint = new Vector2(worldPos.x, worldPos.y);
        }
        else
        {
            targetPoint = null;
        }

        // 2. 발사 주기 체크
        if (fireTimer >= fireRate)
        {
            if (targetPoint.HasValue)
            {
                FireToPoint(targetPoint.Value);   // 수동 지정 방향
            }
            else
            {
                // 자동 조준: 가까운 좀비 찾아서 쏘기
                ZombieController nearestZombie = FindNearestZombie();
                if (nearestZombie != null)
                {
                    FireToPoint(nearestZombie.transform.position);
                }
            }

            fireTimer = 0f;
        }
    }

    // 지정 위치로 총알 발사
    void FireToPoint(Vector2 point)
    {
        Vector2 dir = ((Vector2)point - (Vector2)firePoint.position).normalized;
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.GetComponent<Rigidbody2D>().velocity = dir * bulletSpeed;
    }

    // 씬 내의 모든 좀비 중 가장 가까운 놈 찾기
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
