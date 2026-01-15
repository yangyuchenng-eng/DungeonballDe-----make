using UnityEngine;

// IMPROVED: 添加向 PickupItemManager 的注册和注销
public class PickupItem : MonoBehaviour
{
    [Header("Basic")]
    public string itemName = "Item";

    [Header("Pickup Settings")]
    public bool isPickupable = true;   // 能不能被抓
    public bool holdable = true;       // 能不能拿在手里

    [Header("Throw Settings")]
    public bool throwable = true;              // 能不能被扔
    public bool dealsDamageWhenThrown = true;  // 扔出去会不会伤害敌人
    public int damageAmount = 1;

    [Header("Key Settings")]
    public bool isKey = false;
    public string keyID;   // 用来和门对应

    // 运行时状态（脚本内部用）
    [HideInInspector] public bool isHeld = false;
    [HideInInspector] public bool wasThrownByPlayer = false;

    void OnEnable()
    {
        // IMPROVED: 向 PickupItemManager 注册自己
        PickupItemManager manager = PickupItemManager.Instance;
        if (manager != null)
        {
            manager.RegisterPickupItem(this);
        }
    }

    void OnDisable()
    {
        // IMPROVED: 向 PickupItemManager 注销自己
        PickupItemManager manager = PickupItemManager.Instance;
        if (manager != null)
        {
            manager.UnregisterPickupItem(this);
        }
    }
}
