using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ThrowableBall : MonoBehaviour
{
    public LayerMask hitLayers;
    public float maxFlightTime = 5f;

    [Header("Catch Stability")]
    public bool keepColliderEnabledWhileFlying = true;

    [Header("Hit Detection")]
    public float sphereCastRadiusMultiplier = 0.95f;
    public float extraCastDistance = 0.02f;

    [Header("Post-Hit Physics")]
    public float hitNudge = 0.02f;
    public float bounceSpeedMultiplier = 0.9f;

    [Header("Fireball")]
    public bool destroyFireballOnRaycastHit = true;
    public bool normalizeEnemyBallAfterHittingPlayer = true;

    Rigidbody rb;
    Collider col;
    PickupItem pickupItem;
    BallDamage ballDamage;

    bool isFlyingKinematic = false;
    Vector3 flyVelocity;
    float flightTimer = 0f;

    bool originalIsTrigger;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        pickupItem = GetComponent<PickupItem>();
        ballDamage = GetComponent<BallDamage>();
        originalIsTrigger = col.isTrigger;
    }

    public void BeginKinematicThrow(Vector3 direction, float speed)
    {
        isFlyingKinematic = true;
        flyVelocity = direction.normalized * speed;
        flightTimer = 0f;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (keepColliderEnabledWhileFlying)
        {
            col.enabled = true;
            col.isTrigger = true; // 飞行阶段不产生物理弹跳，但能被抓取/触发检测到
        }
        else
        {
            col.enabled = false;
        }
    }

    public void StopForPickupOrHold()
    {
        isFlyingKinematic = false;
        flightTimer = 0f;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        col.enabled = true;
        col.isTrigger = originalIsTrigger;
    }

    void Update()
    {
        if (!isFlyingKinematic) return;

        float dt = Time.deltaTime;
        Vector3 start = transform.position;
        Vector3 displacement = flyVelocity * dt;
        float distance = displacement.magnitude;

        if (distance > 0f)
        {
            int mask = (hitLayers.value == 0) ? Physics.AllLayers : hitLayers.value;

            float radius = GetCastRadius() * sphereCastRadiusMultiplier;
            float castDist = distance + extraCastDistance;

            if (Physics.SphereCast(
                    start,
                    radius,
                    flyVelocity.normalized,
                    out RaycastHit hit,
                    castDist,
                    mask,
                    QueryTriggerInteraction.Collide))
            {
                // 先把球放到命中点附近，避免卡进 collider
                transform.position = hit.point + hit.normal * hitNudge;

                // ✅ 伤害：敌人/玩家都在这里结算
                ApplyDamage(hit.collider);

                // 火球：命中就销毁（不依赖 OnCollision）
                if (destroyFireballOnRaycastHit && CompareTag("fireball"))
                {
                    Destroy(gameObject);
                    return;
                }

                EnterPhysicsModeAfterHit(hit.normal);
                return;
            }
        }

        transform.position = start + displacement;

        flightTimer += dt;
        if (flightTimer > maxFlightTime)
            EnterPhysicsModeNoHit();
    }

    float GetCastRadius()
    {
        // 最稳：用 bounds 的最小半径（适配任意 collider）
        Bounds b = col.bounds;
        return Mathf.Min(b.extents.x, b.extents.y, b.extents.z);
    }

    void ApplyDamage(Collider other)
    {
        if (other == null) return;

        // 手里拿着不结算
        if (pickupItem != null && pickupItem.isHeld) return;

        // ---------- 命中敌人：完全沿用你“原始方法” ----------
        EnemyHealth eh = other.GetComponentInParent<EnemyHealth>();
        if (eh != null)
        {
            if (pickupItem == null) return;
            if (!pickupItem.wasThrownByPlayer) return;
            if (!pickupItem.dealsDamageWhenThrown) return;

            eh.TakeDamage(pickupItem.damageAmount);

            // 防止同一次飞行反复结算
            pickupItem.wasThrownByPlayer = false;
            return;
        }

        // ---------- 命中玩家：敌人球/火球用 BallDamage ----------
        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            if (ballDamage == null) return;
            if (!ballDamage.damagesPlayer) return;

            int dmg = Mathf.Max(0, Mathf.RoundToInt(ballDamage.damageToPlayer));
            ph.TakeDamage(dmg);

            // 防止连续多次触发
            ballDamage.damagesPlayer = false;

            if (normalizeEnemyBallAfterHittingPlayer)
                ballDamage.isEnemyProjectile = false;

            return;
        }
    }

    void EnterPhysicsModeAfterHit(Vector3 normal)
    {
        if (!isFlyingKinematic) return;
        isFlyingKinematic = false;

        rb.isKinematic = false;
        rb.useGravity = true;

        col.enabled = true;
        col.isTrigger = originalIsTrigger;

        Vector3 v = flyVelocity;
        if (normal.sqrMagnitude > 0.001f)
            v = Vector3.Reflect(flyVelocity, normal);

        rb.linearVelocity = v * bounceSpeedMultiplier;
    }

    void EnterPhysicsModeNoHit()
    {
        if (!isFlyingKinematic) return;
        isFlyingKinematic = false;

        rb.isKinematic = false;
        rb.useGravity = true;

        col.enabled = true;
        col.isTrigger = originalIsTrigger;

        rb.linearVelocity = flyVelocity;
    }
}
