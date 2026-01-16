using UnityEngine;

// 扔的时候使用直线运动，不受重力影响；
// 每帧用 Raycast 检测前进路径，命中后切换到刚体物理弹跳模式。
// FIX: 飞行阶段也能对敌人造成伤害（调用 EnemyHealth.TakeDamage）
// FIX: Raycast 允许命中 Trigger（QueryTriggerInteraction.Collide）
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ThrowableBall : MonoBehaviour
{
    [Tooltip("哪些 Layer 会触发进入物理弹跳模式（地板、墙、敌人等）。留空=所有碰撞体。")]
    public LayerMask hitLayers;

    [Tooltip("安全兜底：如果一直没撞到任何东西，飞行超过这个时间也强制切换物理。")]
    public float maxFlightTime = 5f;

    private Rigidbody rb;
    private Collider col;
    private PickupItem pickupItem;

    private bool isFlyingKinematic = false;   // 正在直线飞行阶段
    private Vector3 flyVelocity;              // 直线飞行速度（方向 * 速度）
    private float flightTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        pickupItem = GetComponent<PickupItem>();
    }

    /// <summary>
    /// 开始一次“扔球”：关闭刚体物理，用脚本直线飞。
    /// </summary>
    public void BeginKinematicThrow(Vector3 direction, float speed)
    {
        isFlyingKinematic = true;
        flyVelocity = direction.normalized * speed;
        flightTimer = 0f;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 飞行阶段用射线检测，先关闭碰撞体
        col.enabled = false;
    }

    void Update()
    {
        if (!isFlyingKinematic) return;

        float dt = Time.deltaTime;
        Vector3 start = transform.position;
        Vector3 displacement = flyVelocity * dt;
        float distance = displacement.magnitude;

        if (distance > 0f)
        {
            RaycastHit hit;
            int mask = (hitLayers.value == 0) ? Physics.AllLayers : hitLayers.value;

            // 关键：Trigger 也算命中（否则敌人若是 Trigger 会被忽略）
            if (Physics.Raycast(start, flyVelocity.normalized, out hit, distance, mask, QueryTriggerInteraction.Collide))
            {
                // ===== 1) 伤害：命中敌人就扣血（飞行阶段也能生效） =====
                TryDealDamage(hit.collider);

                // 移动到碰撞点，稍微沿法线方向抬一下，避免卡进碰撞体里
                transform.position = hit.point + hit.normal * 0.01f;

                // ===== 2) 切换到物理弹跳模式（原逻辑保留） =====
                EnterPhysicsMode();
                return;
            }
        }

        // 本帧没命中：正常直线飞
        transform.position = start + displacement;

        flightTimer += dt;
        if (flightTimer > maxFlightTime)
        {
            EnterPhysicsMode();
        }
    }

    void TryDealDamage(Collider hitCol)
    {
        if (hitCol == null) return;

        // 必须是“玩家扔的”且设置了“扔出会伤害”
        if (pickupItem == null) return;
        if (!pickupItem.wasThrownByPlayer) return;
        if (!pickupItem.dealsDamageWhenThrown) return;

        // 敌人血量组件（可能挂在父物体上）
        EnemyHealth enemyHealth = hitCol.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null) return;

        // EnemyHealth.TakeDamage(float)
        enemyHealth.TakeDamage(pickupItem.damageAmount);
    }

    /// <summary>
    /// 切换为刚体物理弹跳模式。
    /// </summary>
    void EnterPhysicsMode()
    {
        if (!isFlyingKinematic) return;
        isFlyingKinematic = false;

        rb.isKinematic = false;
        rb.useGravity = true;
        col.enabled = true;

        // 继承飞行速度作为刚体初速度
        rb.linearVelocity = flyVelocity;
    }
}
