using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("공격 설정")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 2f;
    public int bulletDamage = 25;
    public float bulletSpeed = 10f;
    public float autoTargetRange = 5f;

    [Header("조준 부채꼴")]
    public float aimRange = 6f;           // 조준 거리
    public float aimAngle = 45f;          // 부채꼴 각도 (좌우 22.5도씩)
    public int aimSegments = 20;          // 부채꼴 세그먼트 수
    public Material aimMaterial;          // 부채꼴 재질

    [Header("수동 조준")]
    public bool manualAiming = false;
    public LineRenderer aimLine;

    private Health playerHealth;
    private float nextFireTime = 0f;
    private Transform currentTarget;
    private Camera mainCamera;

    // 부채꼴 관련
    private MeshRenderer aimRenderer;
    private MeshFilter aimMeshFilter;
    private Mesh aimMesh;

    void Start()
    {
        playerHealth = GetComponent<Health>();
        mainCamera = Camera.main;

        CreateAimFan();

        if (aimLine != null)
        {
            aimLine.enabled = false;
        }
    }

    void CreateAimFan()
    {
        // 부채꼴 오브젝트 생성
        GameObject aimFanObject = new GameObject("AimFan");
        aimFanObject.transform.SetParent(transform);
        aimFanObject.transform.localPosition = Vector3.zero;

        aimMeshFilter = aimFanObject.AddComponent<MeshFilter>();
        aimRenderer = aimFanObject.AddComponent<MeshRenderer>();

        // 재질 설정
        if (aimMaterial == null)
        {
            aimMaterial = new Material(Shader.Find("Sprites/Default"));
            aimMaterial.color = new Color(1f, 0f, 0f, 0.3f); // 반투명 빨간색
        }
        aimRenderer.material = aimMaterial;

        // 초기에는 비활성화
        aimFanObject.SetActive(false);

        CreateAimMesh();
    }

    void CreateAimMesh()
    {
        aimMesh = new Mesh();

        Vector3[] vertices = new Vector3[aimSegments + 2];
        int[] triangles = new int[aimSegments * 3];

        // 중심점
        vertices[0] = Vector3.zero;

        // 부채꼴 점들 생성
        float angleStep = aimAngle / aimSegments;
        float startAngle = -aimAngle / 2f;

        for (int i = 0; i <= aimSegments; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            float radian = currentAngle * Mathf.Deg2Rad;

            vertices[i + 1] = new Vector3(
                Mathf.Cos(radian) * aimRange,
                Mathf.Sin(radian) * aimRange,
                0
            );
        }

        // 삼각형 생성
        for (int i = 0; i < aimSegments; i++)
        {
            triangles[i * 3] = 0;           // 중심점
            triangles[i * 3 + 1] = i + 1;   // 현재 점
            triangles[i * 3 + 2] = i + 2;   // 다음 점
        }

        aimMesh.vertices = vertices;
        aimMesh.triangles = triangles;
        aimMesh.RecalculateNormals();

        aimMeshFilter.mesh = aimMesh;
    }

    void Update()
    {
        HandleInput();

        if (!manualAiming)
        {
            AutoTarget();
        }

        UpdateAimFan();

        if (Time.time >= nextFireTime)
        {
            if (manualAiming || currentTarget != null)
            {
                Fire();
                nextFireTime = Time.time + 1f / fireRate;
            }
        }
    }

    void HandleInput()
    {
        if (Input.GetMouseButton(0))
        {
            manualAiming = true;
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, mainCamera.nearClipPlane));

            // 마우스 방향으로 부채꼴 회전
            Vector3 aimDirection = (worldPos - firePoint.position).normalized;
            float aimAngleRad = Mathf.Atan2(aimDirection.y, aimDirection.x);

            // 부채꼴 회전
            if (aimRenderer != null)
            {
                aimRenderer.transform.rotation = Quaternion.AngleAxis(aimAngleRad * Mathf.Rad2Deg, Vector3.forward);
            }
        }
        else
        {
            manualAiming = false;
        }
    }

    void UpdateAimFan()
    {
        // 조준 중일 때만 부채꼴 표시
        if (aimRenderer != null)
        {
            bool shouldShowAim = manualAiming || currentTarget != null;
            aimRenderer.gameObject.SetActive(shouldShowAim);

            // 자동 조준 시 타겟 방향으로 회전
            if (!manualAiming && currentTarget != null)
            {
                Vector3 targetDirection = (currentTarget.position - firePoint.position).normalized;
                float targetAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
                aimRenderer.transform.rotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);
            }
        }
    }

    void AutoTarget()
    {
        // 부채꼴 범위 내의 좀비들 찾기
        GameObject[] zombies = GameObject.FindGameObjectsWithTag("Zombie");
        Transform closestZombie = null;
        float closestDistance = aimRange;

        foreach (GameObject zombie in zombies)
        {
            Vector3 directionToZombie = zombie.transform.position - firePoint.position;
            float distanceToZombie = directionToZombie.magnitude;

            // 거리 체크
            if (distanceToZombie <= aimRange)
            {
                // 각도 체크 (부채꼴 범위 내인지)
                float angleToZombie = Vector3.Angle(transform.right, directionToZombie); // transform.right는 기본 조준 방향

                if (angleToZombie <= aimAngle / 2f && distanceToZombie < closestDistance)
                {
                    closestDistance = distanceToZombie;
                    closestZombie = zombie.transform;
                }
            }
        }

        currentTarget = closestZombie;
    }

    void Fire()
    {
        if (bulletPrefab == null || firePoint == null) return;

        Vector3 targetDirection;

        if (manualAiming)
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, mainCamera.nearClipPlane));
            targetDirection = (worldPos - firePoint.position).normalized;
        }
        else if (currentTarget != null)
        {
            targetDirection = (currentTarget.position - firePoint.position).normalized;
        }
        else
        {
            return;
        }

        // 총알 생성
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            bulletScript.Init(targetDirection, bulletSpeed, bulletDamage);
        }

        Debug.Log("총알 발사!");
    }

    // Inspector에서 실시간으로 부채꼴 업데이트
    void OnValidate()
    {
        if (Application.isPlaying && aimMesh != null)
        {
            CreateAimMesh();
        }
    }

    // 기즈모로 조준 범위 표시 (Scene 뷰용)
    void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;

        Gizmos.color = Color.red;

        // 부채꼴 그리기
        Vector3 forward = transform.right; // 기본 조준 방향
        Vector3 leftBoundary = Quaternion.AngleAxis(-aimAngle / 2f, Vector3.forward) * forward;
        Vector3 rightBoundary = Quaternion.AngleAxis(aimAngle / 2f, Vector3.forward) * forward;

        // 경계선들
        Gizmos.DrawRay(firePoint.position, leftBoundary * aimRange);
        Gizmos.DrawRay(firePoint.position, rightBoundary * aimRange);

        // 호 그리기
        for (int i = 0; i < aimSegments; i++)
        {
            float angle1 = -aimAngle / 2f + (aimAngle / aimSegments) * i;
            float angle2 = -aimAngle / 2f + (aimAngle / aimSegments) * (i + 1);

            Vector3 point1 = firePoint.position + Quaternion.AngleAxis(angle1, Vector3.forward) * forward * aimRange;
            Vector3 point2 = firePoint.position + Quaternion.AngleAxis(angle2, Vector3.forward) * forward * aimRange;

            Gizmos.DrawLine(point1, point2);
        }
    }
}