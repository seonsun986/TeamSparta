using System;
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

    public Action<int, int> OnHealthChanged; // 현재HP, 최대HP
    public Action OnDeath;

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

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }
    }

    void Die()
    {
        OnDeath?.Invoke();
        Debug.Log($"{gameObject.name} 죽었음!");

        if (gameObject.CompareTag("Zombie"))
        {
            Destroy(gameObject);
        }
        else if (gameObject.CompareTag("Player"))
        {
            GameManager.Instance?.GameOver();
        }
    }
}