using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BatEnemy : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Flight")]
    public float flySpeed = 6f;
    public float turnSpeed = 8f;
    public float hoverHeightOffset = 1.2f;
    public float slowDownDistance = 1.5f;

    [Header("Touch Damage")]
    public int damagePerHit = 10;
    public float damageInterval = 0.5f;

    private Rigidbody rb;
    private float nextDamageTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 target = player.position + Vector3.up * hoverHeightOffset;
        Vector3 toTarget = target - transform.position;

        float dist = toTarget.magnitude;
        if (dist < 0.01f) return;

        Vector3 dir = toTarget.normalized;

        // 转向
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);

        // 前进（近距离减速避免抖动）
        float speed = flySpeed;
        if (dist < slowDownDistance) speed *= (dist / slowDownDistance);

        rb.linearVelocity = transform.forward * speed;
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (Time.time < nextDamageTime) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damagePerHit);
            nextDamageTime = Time.time + damageInterval;
        }
    }
}
