using UnityEngine;

// AI-assisted: simple enemy health for Dungeonball demo.
public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 1f;       // demo：基础怪 1 血
    [HideInInspector]
    public float currentHealth;

    [Header("Death Behaviour")]
    public bool destroyOnDeath = true; // 勾上就会 Destroy

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log($"[EnemyHealth] {name} 受伤 -{amount}，当前血量 = {currentHealth}", this);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"[EnemyHealth] {name} 死亡。", this);

        // 告诉 EnemyManager 有一只怪死了
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemyDeath(this);
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
