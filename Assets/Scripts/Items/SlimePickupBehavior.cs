using UnityEngine;

/// <summary>
/// 史莱姆被捡起时的行为脚本：
/// - 被捡起时，tag 从 "slime" 变成 "fireball"
/// - 这样史莱姆的 AI 脚本会停止运行，球的脚本会开始运行
/// </summary>
[RequireComponent(typeof(PickupItem))]
public class SlimePickupBehavior : MonoBehaviour
{
    private PickupItem pickupItem;

    void Awake()
    {
        pickupItem = GetComponent<PickupItem>();
    }

    void Update()
    {
        // 检查是否被捡起
        if (pickupItem != null && pickupItem.isHeld)
        {
            // 如果当前 tag 还是 "slime"，就改成 "fireball"
            if (gameObject.CompareTag("slime"))
            {
                gameObject.tag = "fireball";
            }
        }
    }
}
