using UnityEngine;


public class BallDamageTrigger : MonoBehaviour
{
    [Header("Player Hit")]
    public string playerTag = "Player";
    public float playerHitCooldown = 0.15f;

    [Header("Enemy Hit")]
    public float enemyHitCooldown = 0.05f;   
    public bool requireWasThrownByPlayer = true;

    [Header("Fireball")]
    public bool destroyFireballOnPlayerHit = true; 
    public bool destroyFireballOnEnemyHit = true;  

    private BallDamage ballDamage;
    private PickupItem pickupItem;
    private Rigidbody parentRb;

    private float nextPlayerHitTime = 0f;
    private float nextEnemyHitTime = 0f;

    void Awake()
    {
        ballDamage = GetComponentInParent<BallDamage>();
        pickupItem = GetComponentInParent<PickupItem>();
        parentRb = GetComponentInParent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (pickupItem != null && pickupItem.isHeld) return; 

        
        if (other.CompareTag(playerTag))
        {
            if (Time.time < nextPlayerHitTime) return;
            nextPlayerHitTime = Time.time + playerHitCooldown;

            if (ballDamage == null || !ballDamage.damagesPlayer) return;

            PlayerHealth ph = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
            if (ph == null) return;

            int dmg = Mathf.Max(0, Mathf.RoundToInt(ballDamage.damageToPlayer));
            ph.TakeDamage(dmg);

            
            ballDamage.damagesPlayer = false;
            ballDamage.isEnemyProjectile = false;

            
            if (destroyFireballOnPlayerHit && transform.root.CompareTag("fireball"))
            {
                Destroy(transform.root.gameObject);
            }

            return;
        }

      
        EnemyHealth eh = other.GetComponent<EnemyHealth>() ?? other.GetComponentInParent<EnemyHealth>();
        if (eh == null) return;

        if (Time.time < nextEnemyHitTime) return;
        nextEnemyHitTime = Time.time + enemyHitCooldown;

        if (ballDamage == null || !ballDamage.damagesEnemies) return;

        if (requireWasThrownByPlayer)
        {
            if (pickupItem == null) return;
            if (!pickupItem.wasThrownByPlayer) return; 
        }

        float dmgToEnemy = ballDamage.damageToEnemies;
        eh.TakeDamage(dmgToEnemy);

        
        if (pickupItem != null) pickupItem.wasThrownByPlayer = false;

       
        if (destroyFireballOnEnemyHit && transform.root.CompareTag("fireball"))
        {
            Destroy(transform.root.gameObject);
        }
    }
}
