using UnityEngine;

// 敌人身体的 Trigger 触碰到玩家时，每隔一段时间对玩家造成一次伤害。
// AI-assisted: structure & parts generated with ChatGPT, then adapted by the student.
// 修改：添加了 Tag 检测，只有当敌人的 Tag 匹配时才会造成伤害
[RequireComponent(typeof(Collider))]
public class EnemyTouchDamage : MonoBehaviour
{
    [Header("Damage")]
    public int damagePerHit = 10;          // 每次伤害多少血（改成 int）
    public float damageInterval = 0.5f;    // 间隔多久再伤害一次（保持 float）

    [Header("Target")]
    public string playerTag = "Player";     // 玩家 Tag

    [Header("Tag Check")]
    [Tooltip("只有当这个敌人的 Tag 是以下列表中的某一个时，才会造成伤害。留空表示不检查 Tag。")]
    public string[] allowedTags = new string[] { "slime" };  // 默认只有 "slime" Tag 才会造成伤害

    private PlayerHealth playerHealthInRange;
    private float nextDamageTime = 0f;

    void Awake()
    {
        // 确保自己这个 Collider 是 Trigger，不会挤推玩家
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // --- 新增：Tag 检测 ---
        if (!IsAllowedToDealtDamage())
        {
            return;
        }

        if (!other.CompareTag(playerTag)) return;

        // 从该物体或父物体上找 PlayerHealth
        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null)
        {
            ph = other.GetComponentInParent<PlayerHealth>();
        }

        if (ph != null)
        {
            playerHealthInRange = ph;
            // 一碰到就可以立刻打一轮
            nextDamageTime = Time.time;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>()
                          ?? other.GetComponentInParent<PlayerHealth>();

        if (ph == playerHealthInRange)
        {
            playerHealthInRange = null;
        }
    }

    void Update()
    {
        // --- 新增：在每次造成伤害前也检查 Tag ---
        if (!IsAllowedToDealtDamage())
        {
            // 如果 Tag 不匹配了，清空玩家引用
            playerHealthInRange = null;
            return;
        }

        if (playerHealthInRange == null) return;

        if (Time.time >= nextDamageTime)
        {
            playerHealthInRange.TakeDamage(damagePerHit);
            nextDamageTime = Time.time + damageInterval;
        }
    }

    /// <summary>
    /// 检查当前敌人的 Tag 是否允许造成伤害
    /// </summary>
    bool IsAllowedToDealtDamage()
    {
        if (allowedTags == null || allowedTags.Length == 0) return true;

        // ✅ 改成检查 root 的 tag（脚本挂子物体也能正确识别 slime）
        Transform root = transform.root;

        foreach (string allowedTag in allowedTags)
        {
            if (root != null && root.CompareTag(allowedTag)) return true;
        }
        return false;
    }

}
