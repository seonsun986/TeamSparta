using UnityEngine;

public class RandomZombieSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject zombiePrefab;
    public float minSpawnInterval = 0.6f;
    public float maxSpawnInterval = 2f;
    public bool waitForTowerSignal = true;

    [Header("Ground Settings")]
    public Transform[] spawnPoints;           // Ground1, 2, 3 ���� ��ġ��
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

        Debug.Log($"RandomZombieSpawner �غ� �Ϸ�. ���� ����: {spawnPoints.Length}��");
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
            Debug.LogWarning("���� ������ �������� �ʾҽ��ϴ�!");
            return;
        }

        // �����ϰ� Ground ���� (0, 1, 2)
        int randomGroundIndex = Random.Range(0, spawnPoints.Length);

        // ���õ� Ground���� ���� ����
        Transform selectedSpawnPoint = spawnPoints[randomGroundIndex];
        GameObject newZombie = Instantiate(zombiePrefab, selectedSpawnPoint.position, Quaternion.identity);

        // ���õ� Ground�� �´� ���̾� ����
        string selectedZombieLayer = zombieLayerNames[randomGroundIndex];
        string selectedGroundLayer = groundLayerNames[randomGroundIndex];

        newZombie.layer = LayerMask.NameToLayer(selectedZombieLayer);

        // ZombieController���� �׷� ���� ����
        ZombieController zombieController = newZombie.GetComponent<ZombieController>();
        if (zombieController != null)
        {
            zombieController.SetZombieGroup(selectedZombieLayer, selectedGroundLayer);
        }

        Debug.Log($"���� ���� ����: Ground{randomGroundIndex + 1}({selectedZombieLayer}), ��ġ: {selectedSpawnPoint.position}");
    }

    void SetRandomSpawnInterval()
    {
        currentSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
        Debug.Log($"���� ��������: {currentSpawnInterval:F2}��");
    }

    // Tower���� ȣ���ϴ� �Լ�
    public void StartSpawning()
    {
        canSpawn = true;
        Debug.Log("���� ������ Ȱ��ȭ!");
    }

    public void StopSpawning()
    {
        canSpawn = false;
        Debug.Log("���� ������ ��Ȱ��ȭ!");
    }

    // Inspector���� ���� �������� ���� Ȯ��
    void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                // Ground���� �ٸ� ����
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