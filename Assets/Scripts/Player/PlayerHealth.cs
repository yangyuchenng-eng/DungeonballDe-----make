using UnityEngine;
using UnityEngine.UI;

// AI-assisted: player health with smooth UI and death handling.
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("UI")]
    public Slider healthSlider;
    [Tooltip("血条 UI 平滑变化速度")]
    public float uiLerpSpeed = 8f;

    private float targetHealth01 = 1f;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        targetHealth01 = 1f;

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
        }
    }

    void Update()
    {
        // 只负责让 UI 慢慢接近 targetHealth01
        if (healthSlider != null)
        {
            float current = healthSlider.value;
            float desired = targetHealth01;
            healthSlider.value = Mathf.MoveTowards(
                current,
                desired,
                uiLerpSpeed * Time.unscaledDeltaTime
            );
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        targetHealth01 = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        targetHealth01 = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        var ui = FindFirstObjectByType<GameStateUI>();
        if (ui != null)
        {
            ui.ShowDeath();
        }

        // TODO：可以在这里禁用玩家移动、输入脚本等
    }
}
