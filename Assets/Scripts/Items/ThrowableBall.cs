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
            col.isTrigger = true; 
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
                
                transform.position = hit.point + hit.normal * hitNudge;

                
                ApplyDamage(hit.collider);

                
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
        
        Bounds b = col.bounds;
        return Mathf.Min(b.extents.x, b.extents.y, b.extents.z);
    }

    void ApplyDamage(Collider other)
    {
        if (other == null) return;

        
        if (pickupItem != null && pickupItem.isHeld) return;

        
        EnemyHealth eh = other.GetComponentInParent<EnemyHealth>();
        if (eh != null)
        {
            if (pickupItem == null) return;
            if (!pickupItem.wasThrownByPlayer) return;
            if (!pickupItem.dealsDamageWhenThrown) return;

            eh.TakeDamage(pickupItem.damageAmount);

            
            AudioManager.I?.PlayEnemyHit();

           
            pickupItem.wasThrownByPlayer = false;
            return;
        }

        
        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            if (ballDamage == null) return;
            if (!ballDamage.damagesPlayer) return;

            int dmg = Mathf.Max(0, Mathf.RoundToInt(ballDamage.damageToPlayer));
            ph.TakeDamage(dmg);

          
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
