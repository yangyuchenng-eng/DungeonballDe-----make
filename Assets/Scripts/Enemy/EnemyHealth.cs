using UnityEngine;


public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 1f;      
    [HideInInspector]
    public float currentHealth;

    [Header("Death Behaviour")]
    public bool destroyOnDeath = true; 

    void Start()
    {
        currentHealth = maxHealth;

       
        EnemyManager manager = EnemyManager.Instance;
        if (manager != null)
        {
            manager.RegisterEnemy(this);
        }
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
