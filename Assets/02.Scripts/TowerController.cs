using UnityEngine;

public class TowerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;              // Ÿ�� �̵� �ӵ�
    public Transform targetPosition;          // ��ǥ ���� (���� ���� ��ó)
    public float stopDistance = 1f;          // ��ǥ�������� ���� �Ÿ�

    [Header("Camera Follow")]
    public Transform cameraTransform;        // ����� ī�޶�
    public Vector3 cameraOffset = new Vector3(0, 0, -10);  // ī�޶� ������

    private Vector3 startPosition;
    private bool isMoving = true;
    private bool hasReachedTarget = false;

    void Start()
    {
        startPosition = transform.position;

        // ī�޶� �ڵ����� ã��
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
        }

        Debug.Log($"Ÿ�� ����! ��ǥ����: {(targetPosition != null ? targetPosition.position.ToString() : "����")}");
    }

    void Update()
    {
        if (isMoving && !hasReachedTarget)
        {
            MoveTower();
            UpdateCamera();
        }
    }

    void MoveTower()
    {
        if (targetPosition == null)
        {
            Debug.LogWarning("��ǥ ������ �������� �ʾҽ��ϴ�!");
            return;
        }

        // ��ǥ ���������� �Ÿ� ���
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition.position);

        // ��ǥ ������ �����ߴ��� Ȯ��
        if (distanceToTarget <= stopDistance)
        {
            StopTower();
            return;
        }

        // ���������� �̵�
        Vector3 movement = Vector3.right * moveSpeed * Time.deltaTime;
        transform.position += movement;

        Debug.Log($"Ÿ�� �̵� ��: ��ǥ���� �Ÿ� {distanceToTarget:F2}");
    }

    void UpdateCamera()
    {
        if (cameraTransform != null)
        {
            // ī�޶� Ÿ���� ���� �̵�
            Vector3 targetCameraPos = transform.position + cameraOffset;
            cameraTransform.position = targetCameraPos;
        }
    }

    void StopTower()
    {
        isMoving = false;
        hasReachedTarget = true;

        Debug.Log("=== Ÿ�� ��ǥ ���� ����! ���� ���� ����! ===");
    }


    // Ÿ�� �̵� �簳
    public void ResumeTower()
    {
        if (hasReachedTarget) return;  // �̹� ���������� �簳 ����

        isMoving = true;
        Debug.Log("Ÿ�� �̵� �簳!");
    }

    // ������ ��� ǥ��
    void OnDrawGizmos()
    {
        // ���� ����
        Vector3 startPos = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPos, 0.5f);

        // ��ǥ ����
        if (targetPosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition.position, 0.5f);

            // �̵� ���
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(startPos, targetPosition.position);

            // ���� ����
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPosition.position, stopDistance);
        }

        // ���� ��ġ
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}