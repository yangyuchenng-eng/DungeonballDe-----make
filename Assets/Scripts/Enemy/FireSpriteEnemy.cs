using UnityEngine;

public class FireSpriteEnemy : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public string playerTag = "Player";
    public float aimHeightOffset = 1.0f;

    [Header("Shoot")]
    public Transform firePoint;
    public GameObject fireballPrefab;     
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

        
        BallDamage bd = go.GetComponent<BallDamage>();
        if (bd != null)
        {
            bd.isEnemyProjectile = true;
            bd.damagesPlayer = true;
            bd.damagesEnemies = false;
            bd.damageToPlayer = fireballDamageToPlayer;
        }

       
        PickupItem pi = go.GetComponent<PickupItem>();
        if (pi != null)
        {
            pi.isPickupable = true;
            pi.isHeld = false;
            pi.wasThrownByPlayer = false;
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
