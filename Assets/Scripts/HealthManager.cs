using UnityEngine;
using UnityEngine.UI;

public class HealthManager : MonoBehaviour
{
    public static HealthManager Instance;

    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Damage & Healing Values")]
    public float missDamage = 15f;
    public float bombDamage = 30f;
    public float hitHeal = 5f;

    [Header("UI")]
    public Slider healthBar;

    private bool isDead = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (maxHealth <= 0) maxHealth = 100f;
            currentHealth = maxHealth * 0.5f;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateUI();
    }

    public void AddEnergy()
    {
        if (isDead) return;
        currentHealth += hitHeal;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateUI();
    }

    public void TakeMissDamage()
    {
        if (isDead) return;
        currentHealth -= missDamage;
        UpdateUI();
        CheckDeath();
    }

    public void TakeBombDamage()
    {
        if (isDead) return;
        currentHealth -= bombDamage;
        UpdateUI();
        CheckDeath();
    }

    private void CheckDeath()
    {
        if (currentHealth <= 0 && !isDead)
        {
            currentHealth = 0;
            isDead = true;

            if (UIManager.Instance != null)
                UIManager.Instance.TriggerGameOver(false);

            if (LiDARSpawner.Instance != null)
                LiDARSpawner.Instance.currentState = LiDARSpawner.GameState.Placed;
        }
    }

    private void UpdateUI()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }
    }
}
