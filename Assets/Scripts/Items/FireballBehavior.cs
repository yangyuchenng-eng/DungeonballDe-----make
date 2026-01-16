using UnityEngine;

/// <summary>
/// 火球行为脚本：检测 tag 为 "fireball" 的球，被扔出后碰到任何东西就销毁自己
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FireballBehavior : MonoBehaviour
{
    private Rigidbody rb;
    private PickupItem pickupItem;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pickupItem = GetComponent<PickupItem>();
    }

    void OnCollisionEnter(Collision collision)
    {
        // 只有当自己的 tag 是 "fireball" 时才执行销毁逻辑
        if (!gameObject.CompareTag("fireball"))
        {
            return;
        }

        // 只有在不被持有的状态下才销毁（避免在玩家手里就销毁）
        if (pickupItem != null && pickupItem.isHeld)
        {
            return;
        }

        // 碰到任何东西都销毁自己
        Destroy(gameObject);
    }
}
