using UnityEngine;

public class TowerController : MonoBehaviour
{
    [Header("������ ����")]
    public float moveSpeed = 2f;              // Ÿ�� �̵� �ӵ�
    public Transform targetPosition;          // ��ǥ ���� (���� ���� ��ó)
    public float stopDistance = 1f;          // ��ǥ�������� ���� �Ÿ�

    [Header("ī�޶� ����")]
    public Transform cameraTransform;      
    public Vector3 cameraOffset = new Vector3(0, 0, -10); 

    private Vector3 startPosition;
    private bool isMoving = true;
    private bool hasReachedTarget = false;

    void Start()
    {
        startPosition = transform.position;

        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
        }
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
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, targetPosition.position);

        if (distanceToTarget <= stopDistance)
        {
            StopTower();
            return;
        }

        Vector3 movement = Vector3.right * moveSpeed * Time.deltaTime;
        transform.position += movement;
    }

    void UpdateCamera()
    {
        if (cameraTransform != null)
        {
            Vector3 targetCameraPos = transform.position + cameraOffset;
            cameraTransform.position = targetCameraPos;
        }
    }

    void StopTower()
    {
        isMoving = false;
        hasReachedTarget = true;
    }


    void OnDrawGizmos()
    {
        Vector3 startPos = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPos, 0.5f);

        if (targetPosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition.position, 0.5f);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(startPos, targetPosition.position);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPosition.position, stopDistance);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}