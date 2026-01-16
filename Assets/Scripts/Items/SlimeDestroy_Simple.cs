using UnityEngine;

/// <summary>
/// 史莱姆销毁脚本：Tag 为 "fireball" 的史莱姆碰到玩家不销毁，碰到其他东西都销毁
/// 使用双重检测（Collision 和 Trigger）确保万无一失
/// </summary>
public class SlimeDestroy : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("玩家的 Tag")]
    public string playerTag = "Player";

    private PickupItem pickupItem;

    void Awake()
    {
        pickupItem = GetComponent<PickupItem>();
    }

    void OnCollisionEnter(Collision collision)
    {
        TryDestroy(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        TryDestroy(other.gameObject);
    }

    void TryDestroy(GameObject hitObject)
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

        // 如果碰到的是玩家，不销毁
        if (hitObject.CompareTag(playerTag))
        {
            return;
        }

        // 碰到其他任何东西都销毁
        Destroy(gameObject);
    }
}
