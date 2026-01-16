using UnityEngine;

/// <summary>
/// 简单的 Tag 切换脚本：被捡起时切换 Tag，被放下时恢复
/// 适用于石头、木箱等任何需要被捡起后改变 Tag 的物体
/// </summary>
public class PickupTagChanger : MonoBehaviour
{
    [Header("Tag Settings")]
    [Tooltip("物体初始的 Tag")]
    public string initialTag = "rock";

    [Tooltip("物体被捡起后的 Tag")]
    public string pickedUpTag = "fireball";

    private PickupItem pickupItem;
    private bool wasPickedUp = false;

    void Awake()
    {
        pickupItem = GetComponent<PickupItem>();
    }

    void Update()
    {
        if (pickupItem == null) return;

        // 检测是否被捡起
        if (pickupItem.isHeld && !wasPickedUp)
        {
            // 刚被捡起，切换 Tag
            gameObject.tag = pickedUpTag;
            wasPickedUp = true;
        }
        
    }
}
