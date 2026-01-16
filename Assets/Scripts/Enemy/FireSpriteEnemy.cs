using UnityEngine;

public class FireSpriteEnemy : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public string playerTag = "Player";
    public float aimHeightOffset = 1.0f;

    [Header("Shoot")]
    public Transform firePoint;
    public GameObject fireballPrefab;      // 你的“火球 prefab”（建议带 ThrowableBall + BallDamage + PickupItem）
    public float shootInterval = 1.2f;
    public float shootSpeed = 18f;
    public float minShootDistance = 3f;
    public float maxShootDistance = 30f;

    [Header("Fireball Damage")]
    public float fireballDamageToPlayer = 20f;

    private float nextShootTime;

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }

        if (firePoint == null) firePoint = transform;

        nextShootTime = Time.time + Random.Range(0f, shootInterval);
    }

    void Update()
    {
        if (player == null || fireballPrefab == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < minShootDistance || dist > maxShootDistance) return;

        if (Time.time >= nextShootTime)
        {
            ShootOnce();
            nextShootTime = Time.time + shootInterval;
        }
    }

    void ShootOnce()
    {
        Vector3 targetPos = player.position + Vector3.up * aimHeightOffset;
        Vector3 dir = (targetPos - firePoint.position).normalized;

        GameObject go = Instantiate(fireballPrefab, firePoint.position, Quaternion.LookRotation(dir));

        // ✅ 敌人投射物标记（协同你现有球/伤害体系）
        BallDamage bd = go.GetComponent<BallDamage>();
        if (bd != null)
        {
            bd.isEnemyProjectile = true;
            bd.damagesPlayer = true;
            bd.damagesEnemies = false;
            bd.damageToPlayer = fireballDamageToPlayer;
        }

        // ✅ 火焰精灵火球：可被接住
        PickupItem pi = go.GetComponent<PickupItem>();
        if (pi != null)
        {
            pi.isPickupable = true;
            pi.isHeld = false;
            pi.wasThrownByPlayer = false;
        }

        // ✅ 用你现有 ThrowableBall 直线飞行
        ThrowableBall tb = go.GetComponent<ThrowableBall>();
        if (tb != null)
        {
            tb.BeginKinematicThrow(dir, shootSpeed);
        }
        else
        {
            // 兜底：没有 ThrowableBall 就用刚体
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearVelocity = dir * shootSpeed;
            }
        }
    }
}
