using UnityEngine;

public class DoorInteractable : MonoBehaviour
{
    public enum DoorType { Normal, Locked }

    [Header("Door")]
    public DoorType doorType = DoorType.Normal;

    [Tooltip("真正的门对象（墙体缩放那块）。触发器是子物体时一定要拖这个")]
    public GameObject doorRoot;

    [Tooltip("门的实体Collider（不填就自动从 doorRoot 找）")]
    public Collider solidCollider;

    [Tooltip("门的可视对象（不填就用 doorRoot）")]
    public GameObject visualRoot;

    [Header("Normal Door (Ball)")]
    public bool requirePlayerThrown = true;   // 普通门是否必须玩家扔出的球
    public bool allowKeyAsBall = false;       // 普通门是否允许钥匙也能开（一般关掉）

    [Header("Locked Door (Key)")]
    public string requiredKeyId = "MainDoor"; // KeyItem.keyId 必须匹配

    [Header("Open Behavior")]
    public bool destroyDoorRoot = true;        // 开门是销毁门，还是隐藏/禁用
    public GameObject openVfxPrefab;

    private void Awake()
    {
        // 触发器自己必须是 trigger
        var triggerCol = GetComponent<Collider>();
        if (triggerCol != null) triggerCol.isTrigger = true;

        if (doorRoot == null) doorRoot = transform.root.gameObject;

        if (visualRoot == null) visualRoot = doorRoot;

        if (solidCollider == null && doorRoot != null)
            solidCollider = doorRoot.GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (doorType == DoorType.Locked)
        {
            TryOpenLocked(other);
        }
        else
        {
            TryOpenNormal(other);
        }
    }

    void TryOpenNormal(Collider other)
    {
        // 必须是可拾取物（球/钥匙都属于 PickupItem）
        var item = other.GetComponentInParent<PickupItem>();
        if (item == null) return;

        // 如果不允许钥匙当球开普通门，就排除 KeyItem
        bool isKey = item.GetComponent<KeyItem>() != null || item.GetComponentInChildren<KeyItem>() != null;
        if (isKey && !allowKeyAsBall) return;

        if (requirePlayerThrown && !item.wasThrownByPlayer) return;

        OpenDoor();
        // 普通门开门后不销毁球（按你游戏习惯），这里只清标记防连击
        item.wasThrownByPlayer = false;
    }

    void TryOpenLocked(Collider other)
    {
        // 锁门只认 KeyItem（钥匙）
        KeyItem key = other.GetComponent<KeyItem>() ?? other.GetComponentInParent<KeyItem>();
        if (key == null) return;

        if (key.keyId != requiredKeyId) return;

        OpenDoor();

        // 重点：钥匙消失
        Destroy(key.gameObject);
    }

    void OpenDoor()
    {
        if (openVfxPrefab != null)
            Instantiate(openVfxPrefab, transform.position, Quaternion.identity);

        // 关闭实体碰撞（玩家/敌人都能过）
        if (solidCollider != null) solidCollider.enabled = false;

        // 关掉可视
        if (visualRoot != null) visualRoot.SetActive(false);

        if (destroyDoorRoot && doorRoot != null)
            Destroy(doorRoot);
    }
}
