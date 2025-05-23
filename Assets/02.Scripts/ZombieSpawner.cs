using UnityEngine;

public class RandomZombieSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject zombiePrefab;
    public float minSpawnInterval = 0.6f;
    public float maxSpawnInterval = 2f;
    public bool waitForTowerSignal = true;

    [Header("Ground Settings")]
    public Transform[] spawnPoints;           // Ground1, 2, 3 스폰 위치들
    public string[] groundLayerNames = { "Ground1", "Ground2", "Ground3" };
    public string[] zombieLayerNames = { "Zombie1", "Zombie2", "Zombie3" };

    private float timer;
    private float currentSpawnInterval;
    private bool canSpawn = false;

    void Start()
    {
        SetRandomSpawnInterval();

        if (!waitForTowerSignal)
        {
            canSpawn = true;
        }

        Debug.Log($"RandomZombieSpawner 준비 완료. 스폰 지점: {spawnPoints.Length}개");
    }

    void Update()
    {
        if (!canSpawn) return;

        timer += Time.deltaTime;

        if (timer >= currentSpawnInterval)
        {
            SpawnRandomZombie();
            timer = 0f;
            SetRandomSpawnInterval();
        }
    }

    void SpawnRandomZombie()
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("스폰 지점이 설정되지 않았습니다!");
            return;
        }

        // 랜덤하게 Ground 선택 (0, 1, 2)
        int randomGroundIndex = Random.Range(0, spawnPoints.Length);

        // 선택된 Ground에서 좀비 생성
        Transform selectedSpawnPoint = spawnPoints[randomGroundIndex];
        GameObject newZombie = Instantiate(zombiePrefab, selectedSpawnPoint.position, Quaternion.identity);

        // 선택된 Ground에 맞는 레이어 설정
        string selectedZombieLayer = zombieLayerNames[randomGroundIndex];
        string selectedGroundLayer = groundLayerNames[randomGroundIndex];

        newZombie.layer = LayerMask.NameToLayer(selectedZombieLayer);

        // ZombieController에게 그룹 정보 전달
        ZombieController zombieController = newZombie.GetComponent<ZombieController>();
        if (zombieController != null)
        {
            zombieController.SetZombieGroup(selectedZombieLayer, selectedGroundLayer);
        }

        Debug.Log($"랜덤 좀비 생성: Ground{randomGroundIndex + 1}({selectedZombieLayer}), 위치: {selectedSpawnPoint.position}");
    }

    void SetRandomSpawnInterval()
    {
        currentSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
        Debug.Log($"다음 스폰까지: {currentSpawnInterval:F2}초");
    }

    // Tower에서 호출하는 함수
    public void StartSpawning()
    {
        canSpawn = true;
        Debug.Log("랜덤 스포너 활성화!");
    }

    public void StopSpawning()
    {
        canSpawn = false;
        Debug.Log("랜덤 스포너 비활성화!");
    }

    // Inspector에서 스폰 지점들을 쉽게 확인
    void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                // Ground별로 다른 색깔
                switch (i)
                {
                    case 0: Gizmos.color = Color.red; break;      // Ground1
                    case 1: Gizmos.color = Color.green; break;    // Ground2  
                    case 2: Gizmos.color = Color.blue; break;     // Ground3
                    default: Gizmos.color = Color.white; break;
                }

                Gizmos.DrawWireSphere(spawnPoints[i].position, 0.5f);
                Gizmos.DrawWireCube(spawnPoints[i].position + Vector3.up * 0.8f, Vector3.one * 0.3f);
            }
        }
    }
}