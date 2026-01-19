using UnityEngine;

public class CannonEnemy : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public string playerTag = "Player";
    public float aimHeightOffset = 1.0f;

    [Header("Shoot")]
    public Transform muzzle;
    public GameObject cannonballPrefab;    
    public float shootSpeed = 22f;
    public float minShootDistance = 5f;
    public float maxShootDistance = 45f;

    [Header("Cannonball Damage")]
    public float cannonDamageToPlayer = 60f;

    [Header("Uncatchable (recommended)")]
    public bool forceUncatchable = true; 

    private float nextShootTime;

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }

        if (muzzle == null) muzzle = transform;

        nextShootTime = Time.time + Random.Range(0f, shootInterval);
    }

    void Update()
    {
        if (player == null || cannonballPrefab == null) return;

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
        Vector3 dir = (targetPos - muzzle.position).normalized;

        GameObject go = Instantiate(cannonballPrefab, muzzle.position, Quaternion.LookRotation(dir));

        
        BallDamage bd = go.GetComponent<BallDamage>();
        if (bd != null)
        {
            bd.isEnemyProjectile = true;
            bd.damagesPlayer = true;
            bd.damagesEnemies = false;
            bd.damageToPlayer = cannonDamageToPlayer;
        }

        
        if (forceUncatchable)
        {
            PickupItem pi = go.GetComponent<PickupItem>();
            if (pi != null)
            {
                pi.isPickupable = false; 
                pi.isHeld = false;
                pi.wasThrownByPlayer = false;
            }
        }

        ThrowableBall tb = go.GetComponent<ThrowableBall>();
        if (tb != null)
        {
            tb.BeginKinematicThrow(dir, shootSpeed);
        }
        else
        {
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
