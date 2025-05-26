using UnityEngine;

public class RandomZombieSpawner : MonoBehaviour
{
    [Header("좀비 생성 관련")]
    public GameObject zombiePrefab;
    public float minSpawnInterval = 0.6f;
    public float maxSpawnInterval = 2f;
    public bool waitForTowerSignal = true;

    [Header("땅 설정")]
    public Transform[] spawnPoints;           // Ground1, 2, 3 스폰 위치들
    public string[] groundLayerNames = { "Ground1", "Ground2", "Ground3" };
    public string[] zombieLayerNames = { "Zombie1", "Zombie2", "Zombie3" };

    private float timer;
    private float currentSpawnInterval;

    void Start()
    {
        SetRandomSpawnInterval();
    }

    void Update()
    {
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
            return;
        }

        int randomGroundIndex = Random.Range(0, spawnPoints.Length);

        Transform selectedSpawnPoint = spawnPoints[randomGroundIndex];
        GameObject newZombie = Instantiate(zombiePrefab, selectedSpawnPoint.position, Quaternion.identity, selectedSpawnPoint);

        string selectedZombieLayer = zombieLayerNames[randomGroundIndex];
        string selectedGroundLayer = groundLayerNames[randomGroundIndex];

        newZombie.layer = LayerMask.NameToLayer(selectedZombieLayer);

        ZombieController zombieController = newZombie.GetComponent<ZombieController>();
        if (zombieController != null)
        {
            zombieController.SetZombieGroup(selectedZombieLayer, selectedGroundLayer);
        }
    }

    void SetRandomSpawnInterval()
    {
        currentSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
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