using UnityEngine;

// 敌人身体的 Trigger 触碰到玩家时，每隔一段时间对玩家造成一次伤害。
// AI-assisted: structure & parts generated with ChatGPT, then adapted by the student.
[RequireComponent(typeof(Collider))]
public class EnemyTouchDamage : MonoBehaviour
{
    [Header("Damage")]
    public int damagePerHit = 10;          // 每次伤害多少血（改成 int）
    public float damageInterval = 0.5f;    // 间隔多久再伤害一次（保持 float）

    [Header("Target")]
    public string playerTag = "Player";     // 玩家 Tag

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
        if (playerHealthInRange == null) return;

        if (Time.time >= nextDamageTime)
        {
            playerHealthInRange.TakeDamage(damagePerHit);
            nextDamageTime = Time.time + damageInterval;
        }
    }
}
