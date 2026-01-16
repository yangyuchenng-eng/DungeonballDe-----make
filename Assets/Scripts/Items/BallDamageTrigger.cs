using UnityEngine;

/// <summary>
/// 挂在球的子物体 Trigger Collider 上（DamageTrigger）
/// 作用：在球飞行阶段（父物理 collider 关闭）仍能触发伤害
/// 同时支持：打玩家 + 打敌人
/// </summary>
public class BallDamageTrigger : MonoBehaviour
{
    [Header("Player Hit")]
    public string playerTag = "Player";
    public float playerHitCooldown = 0.15f;

    [Header("Enemy Hit")]
    public float enemyHitCooldown = 0.05f;   // 一般不需要太大，主要防止同帧多次
    public bool requireWasThrownByPlayer = true;

    [Header("Fireball")]
    public bool destroyFireballOnPlayerHit = true; // 火球命中玩家销毁
    public bool destroyFireballOnEnemyHit = true;  // 火球命中敌人销毁（如果你希望火球碰敌人就没）

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
        if (pickupItem != null && pickupItem.isHeld) return; // 手里不触发

        // ---------- 1) 打玩家 ----------
        if (other.CompareTag(playerTag))
        {
            if (Time.time < nextPlayerHitTime) return;
            nextPlayerHitTime = Time.time + playerHitCooldown;

            if (ballDamage == null || !ballDamage.damagesPlayer) return;

            PlayerHealth ph = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
            if (ph == null) return;

            int dmg = Mathf.Max(0, Mathf.RoundToInt(ballDamage.damageToPlayer));
            ph.TakeDamage(dmg);

            // 命中后防止连扣
            ballDamage.damagesPlayer = false;
            ballDamage.isEnemyProjectile = false;

            // 火球命中玩家就销毁（因为飞行阶段父 collider 关着，FireballBehavior 不一定触发）
            if (destroyFireballOnPlayerHit && transform.root.CompareTag("fireball"))
            {
                Destroy(transform.root.gameObject);
            }

            return;
        }

        // ---------- 2) 打敌人 ----------
        // 直接找 EnemyHealth（你敌人身上一定有）
        EnemyHealth eh = other.GetComponent<EnemyHealth>() ?? other.GetComponentInParent<EnemyHealth>();
        if (eh == null) return;

        if (Time.time < nextEnemyHitTime) return;
        nextEnemyHitTime = Time.time + enemyHitCooldown;

        if (ballDamage == null || !ballDamage.damagesEnemies) return;

        if (requireWasThrownByPlayer)
        {
            if (pickupItem == null) return;
            if (!pickupItem.wasThrownByPlayer) return; // 必须是玩家扔出的球
        }

        float dmgToEnemy = ballDamage.damageToEnemies;
        eh.TakeDamage(dmgToEnemy);

        // ✅ 关键：命中敌人后清掉 wasThrownByPlayer，避免 EnemyHitByBall 再扣一次
        if (pickupItem != null) pickupItem.wasThrownByPlayer = false;

        // 如果是火球，你希望打到敌人也消失
        if (destroyFireballOnEnemyHit && transform.root.CompareTag("fireball"))
        {
            Destroy(transform.root.gameObject);
        }
    }
}
