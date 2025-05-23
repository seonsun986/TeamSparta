using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("���� ����")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 2f;
    public int bulletDamage = 25;
    public float bulletSpeed = 10f;
    public float autoTargetRange = 5f;

    [Header("���� ��ä��")]
    public float aimRange = 6f;           // ���� �Ÿ�
    public float aimAngle = 45f;          // ��ä�� ���� (�¿� 22.5����)
    public int aimSegments = 20;          // ��ä�� ���׸�Ʈ ��
    public Material aimMaterial;          // ��ä�� ����

    [Header("���� ����")]
    public bool manualAiming = false;
    public LineRenderer aimLine;

    private Health playerHealth;
    private float nextFireTime = 0f;
    private Transform currentTarget;
    private Camera mainCamera;

    // ��ä�� ����
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
        // ��ä�� ������Ʈ ����
        GameObject aimFanObject = new GameObject("AimFan");
        aimFanObject.transform.SetParent(transform);
        aimFanObject.transform.localPosition = Vector3.zero;

        aimMeshFilter = aimFanObject.AddComponent<MeshFilter>();
        aimRenderer = aimFanObject.AddComponent<MeshRenderer>();

        // ���� ����
        if (aimMaterial == null)
        {
            aimMaterial = new Material(Shader.Find("Sprites/Default"));
            aimMaterial.color = new Color(1f, 0f, 0f, 0.3f); // ������ ������
        }
        aimRenderer.material = aimMaterial;

        // �ʱ⿡�� ��Ȱ��ȭ
        aimFanObject.SetActive(false);

        CreateAimMesh();
    }

    void CreateAimMesh()
    {
        aimMesh = new Mesh();

        Vector3[] vertices = new Vector3[aimSegments + 2];
        int[] triangles = new int[aimSegments * 3];

        // �߽���
        vertices[0] = Vector3.zero;

        // ��ä�� ���� ����
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

        // �ﰢ�� ����
        for (int i = 0; i < aimSegments; i++)
        {
            triangles[i * 3] = 0;           // �߽���
            triangles[i * 3 + 1] = i + 1;   // ���� ��
            triangles[i * 3 + 2] = i + 2;   // ���� ��
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

            // ���콺 �������� ��ä�� ȸ��
            Vector3 aimDirection = (worldPos - firePoint.position).normalized;
            float aimAngleRad = Mathf.Atan2(aimDirection.y, aimDirection.x);

            // ��ä�� ȸ��
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
        // ���� ���� ���� ��ä�� ǥ��
        if (aimRenderer != null)
        {
            bool shouldShowAim = manualAiming || currentTarget != null;
            aimRenderer.gameObject.SetActive(shouldShowAim);

            // �ڵ� ���� �� Ÿ�� �������� ȸ��
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
        // ��ä�� ���� ���� ����� ã��
        GameObject[] zombies = GameObject.FindGameObjectsWithTag("Zombie");
        Transform closestZombie = null;
        float closestDistance = aimRange;

        foreach (GameObject zombie in zombies)
        {
            Vector3 directionToZombie = zombie.transform.position - firePoint.position;
            float distanceToZombie = directionToZombie.magnitude;

            // �Ÿ� üũ
            if (distanceToZombie <= aimRange)
            {
                // ���� üũ (��ä�� ���� ������)
                float angleToZombie = Vector3.Angle(transform.right, directionToZombie); // transform.right�� �⺻ ���� ����

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

        // �Ѿ� ����
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            bulletScript.Init(targetDirection, bulletSpeed, bulletDamage);
        }

        Debug.Log("�Ѿ� �߻�!");
    }

    // Inspector���� �ǽð����� ��ä�� ������Ʈ
    void OnValidate()
    {
        if (Application.isPlaying && aimMesh != null)
        {
            CreateAimMesh();
        }
    }

    // ������ ���� ���� ǥ�� (Scene ���)
    void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;

        Gizmos.color = Color.red;

        // ��ä�� �׸���
        Vector3 forward = transform.right; // �⺻ ���� ����
        Vector3 leftBoundary = Quaternion.AngleAxis(-aimAngle / 2f, Vector3.forward) * forward;
        Vector3 rightBoundary = Quaternion.AngleAxis(aimAngle / 2f, Vector3.forward) * forward;

        // ��輱��
        Gizmos.DrawRay(firePoint.position, leftBoundary * aimRange);
        Gizmos.DrawRay(firePoint.position, rightBoundary * aimRange);

        // ȣ �׸���
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