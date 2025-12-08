using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyHitByBall : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damageFromPlayerBall = 999f; // 基础怪一击必杀

    [Header("Debug")]
    public bool debugLogs = true;

    private EnemyHealth enemyHealth;

    void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null)
            enemyHealth = GetComponentInParent<EnemyHealth>();

        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (debugLogs)
        {
            if (enemyHealth == null)
                Debug.LogWarning($"[EnemyHitByBall] {name} 上没找到 EnemyHealth。", this);
            else
                Debug.Log($"[EnemyHitByBall] 初始化完成，绑定 EnemyHealth：{enemyHealth.gameObject.name}", this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (enemyHealth == null) return;

        if (debugLogs)
            Debug.Log($"[EnemyHitByBall] Trigger with {other.name}", this);

        // 找球
        PickupItem item = other.GetComponent<PickupItem>() ?? other.GetComponentInParent<PickupItem>();
        if (item == null)
        {
            if (debugLogs)
                Debug.Log($"[EnemyHitByBall] {other.name} 没有 PickupItem，忽略", this);
            return;
        }

        // 必须是玩家扔出的球，且不在手上
        if (!item.wasThrownByPlayer)
        {
            if (debugLogs)
                Debug.Log($"[EnemyHitByBall] 球 {item.name} wasThrownByPlayer = false，忽略", this);
            return;
        }

        if (item.isHeld)
        {
            if (debugLogs)
                Debug.Log($"[EnemyHitByBall] 球 {item.name} 还在玩家手上，忽略", this);
            return;
        }

        // ✅ 不再判断速度，任何玩家扔出的球都能杀怪
        if (debugLogs)
            Debug.Log($"[EnemyHitByBall] {name} 被球 {item.name} 击中，造成 {damageFromPlayerBall} 伤害", this);

        enemyHealth.TakeDamage(damageFromPlayerBall);

        // 命中一次后取消“玩家扔出的球”标记，避免连锁秒一串
        item.wasThrownByPlayer = false;
    }
}
