using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyHitByBall : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damageFromPlayerBall = 999f;

    [Header("Double Safety")]
    [Tooltip("只接受来自这个 Layer 的球（建议=Pickup）。")]
    public string requiredBallLayerName = "Pickup";

    [Tooltip("球的速度必须大于这个值才算有效命中（防止静止球误杀）。")]
    public float minHitSpeed = 2.0f;

    [Tooltip("速度不足时是否清掉 wasThrownByPlayer，避免静止球以后又误杀。")]
    public bool clearThrownFlagWhenTooSlow = true;

    [Tooltip("如果球有 BallDamage，要求它 damagesEnemies=true 才能伤害敌人。")]
    public bool requireDamagesEnemiesFlag = true;

    [Header("Debug")]
    public bool debugLogs = true;

    private EnemyHealth enemyHealth;
    private int requiredLayer;

    void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null) enemyHealth = GetComponentInParent<EnemyHealth>();

        var col = GetComponent<Collider>();
        col.isTrigger = true;

        requiredLayer = LayerMask.NameToLayer(requiredBallLayerName);

        if (debugLogs)
        {
            if (enemyHealth == null)
                Debug.LogWarning($"[EnemyHitByBall] {name} 上没找到 EnemyHealth。", this);
            else
                Debug.Log($"[EnemyHitByBall] 初始化完成，绑定 EnemyHealth：{enemyHealth.gameObject.name}", this);

            if (requiredLayer == -1)
                Debug.LogWarning($"[EnemyHitByBall] requiredBallLayerName='{requiredBallLayerName}' 不存在，请检查 Layer 设置。", this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (enemyHealth == null) return;

        
        PickupItem item = other.GetComponent<PickupItem>() ?? other.GetComponentInParent<PickupItem>();
        if (item == null) return;

        
        if (!item.wasThrownByPlayer) return;
        if (item.isHeld) return;

        
        if (requiredLayer != -1 && item.gameObject.layer != requiredLayer)
        {
            if (debugLogs) Debug.Log($"[EnemyHitByBall] 忽略：{item.name} layer={LayerMask.LayerToName(item.gameObject.layer)} 不是 {requiredBallLayerName}", this);
            return;
        }

        
        if (requireDamagesEnemiesFlag)
        {
            BallDamage bd = item.GetComponent<BallDamage>();
            if (bd != null && !bd.damagesEnemies)
            {
                if (debugLogs) Debug.Log($"[EnemyHitByBall] 忽略：{item.name} damagesEnemies=false", this);
                return;
            }
        }

        
        float speed = 0f;
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null) speed = rb.linearVelocity.magnitude;

        if (speed < minHitSpeed)
        {
            if (debugLogs) Debug.Log($"[EnemyHitByBall] 忽略：{item.name} speed={speed:F2} < {minHitSpeed}", this);

            if (clearThrownFlagWhenTooSlow)
                item.wasThrownByPlayer = false; 

            return;
        }

        if (debugLogs)
            Debug.Log($"[EnemyHitByBall] {name} 被球 {item.name} 命中（speed={speed:F2}），造成 {damageFromPlayerBall} 伤害", this);

        enemyHealth.TakeDamage(damageFromPlayerBall);

        
        AudioManager.I?.PlayEnemyHit();

        
        item.wasThrownByPlayer = false;
    }
}
