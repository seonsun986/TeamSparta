using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("공격 설정")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireInterval = 2f; 
    public int bulletDamage = 25;
    public float bulletSpeed = 10f;
    public float autoTargetRange = 5f;

    public float aimRange = 6f;
    public float aimAngle = 45f;
    public int aimSegments = 20;
    public Material aimMaterial;

    private Health playerHealth;
    private float nextFireTime = 0f;
    private Transform currentTarget;
    private Camera mainCamera;

    // 조준 관련
    private bool isAiming = false;
    private Vector3 aimDirection;

    private MeshRenderer aimRenderer;
    private MeshFilter aimMeshFilter;
    private Mesh aimMesh;

    void Start()
    {
        playerHealth = GetComponent<Health>();
        mainCamera = Camera.main;

        CreateAimFan();
    }

    void CreateAimFan()
    {
        GameObject aimFanObject = new GameObject("Aim");
        aimFanObject.transform.SetParent(transform);
        aimFanObject.transform.localPosition = Vector3.zero;

        aimMeshFilter = aimFanObject.AddComponent<MeshFilter>();
        aimRenderer = aimFanObject.AddComponent<MeshRenderer>();

        if (aimMaterial == null)
        {
            aimMaterial = new Material(Shader.Find("Sprites/Default"));
            aimMaterial.color = new Color(1f, 0f, 0f, 0.3f);
        }
        aimRenderer.material = aimMaterial;

        aimFanObject.SetActive(false);
        CreateAimMesh();
    }

    void CreateAimMesh()
    {
        aimMesh = new Mesh();

        Vector3[] vertices = new Vector3[aimSegments + 2];
        int[] triangles = new int[aimSegments * 3];

        vertices[0] = Vector3.zero;

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

        for (int i = 0; i < aimSegments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        aimMesh.vertices = vertices;
        aimMesh.triangles = triangles;
        aimMesh.RecalculateNormals();

        aimMeshFilter.mesh = aimMesh;
    }

    void Update()
    {
        HandleInput();

        if (!isAiming)
        {
            AutoTarget();
        }

        UpdateAimFan();

        if (Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireInterval; 
        }
    }

    void HandleInput()
    {
        if (Input.GetMouseButton(0))
        {
            isAiming = true;

            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, mainCamera.nearClipPlane));

            aimDirection = (worldPos - firePoint.position).normalized;

        }
        else
        {
            isAiming = false;
        }
    }

    void UpdateAimFan()
    {
        if (aimRenderer != null)
        {
            // 항상 부채꼴 표시 (조준 중이거나 타겟이 있을 때)
            bool shouldShowAim = isAiming || currentTarget != null;
            aimRenderer.gameObject.SetActive(shouldShowAim);

            if (isAiming)
            {
                float aimAngleRad = Mathf.Atan2(aimDirection.y, aimDirection.x);
                aimRenderer.transform.rotation = Quaternion.AngleAxis(aimAngleRad * Mathf.Rad2Deg, Vector3.forward);
            }
            else if (currentTarget != null)
            {
                Vector3 targetDirection = (currentTarget.position - firePoint.position).normalized;
                float targetAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
                aimRenderer.transform.rotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);
            }
        }
    }

    void AutoTarget()
    {
        GameObject[] zombies = GameObject.FindGameObjectsWithTag("Zombie");
        Transform closestZombie = null;
        float closestDistance = autoTargetRange;

        foreach (GameObject zombie in zombies)
        {
            float distance = Vector3.Distance(transform.position, zombie.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestZombie = zombie.transform;
            }
        }

        currentTarget = closestZombie;
    }

    void Fire()
    {
        if (bulletPrefab == null || firePoint == null) return;

        Vector3 targetDirection;

        if (isAiming)
        {
            targetDirection = aimDirection;
            Debug.Log("수동 조준 발사!");
        }
        else if (currentTarget != null)
        {
            targetDirection = (currentTarget.position - firePoint.position).normalized;
        }
        else
        {
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            bulletScript.Initialize(targetDirection, bulletSpeed, bulletDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;

        Gizmos.color = Color.red;

        Vector3 forward = transform.right;
        Vector3 leftBoundary = Quaternion.AngleAxis(-aimAngle / 2f, Vector3.forward) * forward;
        Vector3 rightBoundary = Quaternion.AngleAxis(aimAngle / 2f, Vector3.forward) * forward;

        Gizmos.DrawRay(firePoint.position, leftBoundary * aimRange);
        Gizmos.DrawRay(firePoint.position, rightBoundary * aimRange);

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