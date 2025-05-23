using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    [Header("HP 설정")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("HP 바")]
    public Slider healthBar;
    public Transform healthBarParent;
    public Vector3 healthBarOffset = new Vector3(0, 1.2f, 0);

    // 이벤트
    public System.Action<int, int> OnHealthChanged; // (현재HP, 최대HP)
    public System.Action OnDeath;

    void Start()
    {
        currentHealth = maxHealth;
        CreateHealthBar();
        UpdateHealthBar();
    }

    void CreateHealthBar()
    {
        if (healthBar == null)
        {
            // HP 바 프리팹을 리소스에서 로드하거나 직접 생성
            GameObject healthBarPrefab = Resources.Load<GameObject>("HealthBarPrefab");
            if (healthBarPrefab != null)
            {
                GameObject healthBarObj = Instantiate(healthBarPrefab, transform);
                healthBarObj.transform.localPosition = healthBarOffset;
                healthBar = healthBarObj.GetComponentInChildren<Slider>();
            }
        }

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealthBar();

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"{gameObject.name} HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthBar();

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth;

            // HP 비율에 따라 색상 변경
            Image fillImage = healthBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                float healthPercent = (float)currentHealth / maxHealth;

                if (healthPercent > 0.6f)
                    fillImage.color = Color.green;
                else if (healthPercent > 0.3f)
                    fillImage.color = Color.yellow;
                else
                    fillImage.color = Color.red;
            }
        }
    }

    void Die()
    {
        OnDeath?.Invoke();
        Debug.Log($"{gameObject.name} 사망!");

        // 좀비인 경우 파괴, 플레이어인 경우 게임 오버
        if (gameObject.CompareTag("Zombie"))
        {
            Destroy(gameObject);
        }
        else if (gameObject.CompareTag("Player"))
        {
            // 게임 오버 처리
            GameManager.Instance?.GameOver();
        }
    }

    void Update()
    {
        // HP 바가 카메라를 향하도록
        if (healthBar != null && Camera.main != null)
        {
            healthBar.transform.LookAt(Camera.main.transform);
            healthBar.transform.Rotate(0, 180, 0); // 뒤집어서 올바른 방향
        }
    }
}