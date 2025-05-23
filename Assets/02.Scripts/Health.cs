using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    [Header("HP ����")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("HP ��")]
    public Slider healthBar;
    public Transform healthBarParent;
    public Vector3 healthBarOffset = new Vector3(0, 1.2f, 0);

    // �̺�Ʈ
    public System.Action<int, int> OnHealthChanged; // (����HP, �ִ�HP)
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
            // HP �� �������� ���ҽ����� �ε��ϰų� ���� ����
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

            // HP ������ ���� ���� ����
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
        Debug.Log($"{gameObject.name} ���!");

        // ������ ��� �ı�, �÷��̾��� ��� ���� ����
        if (gameObject.CompareTag("Zombie"))
        {
            Destroy(gameObject);
        }
        else if (gameObject.CompareTag("Player"))
        {
            // ���� ���� ó��
            GameManager.Instance?.GameOver();
        }
    }

    void Update()
    {
        // HP �ٰ� ī�޶� ���ϵ���
        if (healthBar != null && Camera.main != null)
        {
            healthBar.transform.LookAt(Camera.main.transform);
            healthBar.transform.Rotate(0, 180, 0); // ����� �ùٸ� ����
        }
    }
}