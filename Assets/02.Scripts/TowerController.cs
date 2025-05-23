using UnityEngine;

public class TowerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;              // 타워 이동 속도
    public Transform targetPosition;          // 목표 지점 (좀비 스폰 근처)
    public float stopDistance = 1f;          // 목표지점에서 멈출 거리

    [Header("Camera Follow")]
    public Transform cameraTransform;        // 따라올 카메라
    public Vector3 cameraOffset = new Vector3(0, 0, -10);  // 카메라 오프셋

    private Vector3 startPosition;
    private bool isMoving = true;
    private bool hasReachedTarget = false;

    void Start()
    {
        startPosition = transform.position;

        // 카메라를 자동으로 찾기
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
        }

        Debug.Log($"타워 시작! 목표지점: {(targetPosition != null ? targetPosition.position.ToString() : "없음")}");
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
            Debug.LogWarning("목표 지점이 설정되지 않았습니다!");
            return;
        }

        // 목표 지점까지의 거리 계산
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition.position);

        // 목표 지점에 도달했는지 확인
        if (distanceToTarget <= stopDistance)
        {
            StopTower();
            return;
        }

        // 오른쪽으로 이동
        Vector3 movement = Vector3.right * moveSpeed * Time.deltaTime;
        transform.position += movement;

        Debug.Log($"타워 이동 중: 목표까지 거리 {distanceToTarget:F2}");
    }

    void UpdateCamera()
    {
        if (cameraTransform != null)
        {
            // 카메라가 타워를 따라 이동
            Vector3 targetCameraPos = transform.position + cameraOffset;
            cameraTransform.position = targetCameraPos;
        }
    }

    void StopTower()
    {
        isMoving = false;
        hasReachedTarget = true;

        Debug.Log("=== 타워 목표 지점 도달! 좀비 스폰 시작! ===");
    }


    // 타워 이동 재개
    public void ResumeTower()
    {
        if (hasReachedTarget) return;  // 이미 도달했으면 재개 안함

        isMoving = true;
        Debug.Log("타워 이동 재개!");
    }

    // 기즈모로 경로 표시
    void OnDrawGizmos()
    {
        // 시작 지점
        Vector3 startPos = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPos, 0.5f);

        // 목표 지점
        if (targetPosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition.position, 0.5f);

            // 이동 경로
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(startPos, targetPosition.position);

            // 정지 범위
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPosition.position, stopDistance);
        }

        // 현재 위치
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}