using System.Collections;
using UnityEngine;

/// <summary>
/// 幽灵敌人：在空中飞行，会寻找并捡起地上静止的球，然后扔向玩家
/// 只捡 Layer 是 "Pickup" 且速度接近 0 的球
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyGhost : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 3f;

    [Header("Ball Pickup")]
    public float pickupRange = 2f;              // 捡球的范围
    public float pickupSpeedThreshold = 0.5f;   // 只捡速度低于这个值的球
    public Transform handTransform;             // 手的位置（球会被放在这里）

    [Header("Throw Settings")]
    public float throwSpeed = 15f;
    public float throwCooldown = 2f;            // 扔完球后的冷却时间

    private Transform player;
    private PickupItem heldBall;                // 当前手里的球
    private float lastThrowTime = -999f;

    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        if (handTransform == null)
        {
            handTransform = transform;
        }

        StartCoroutine(GhostAI());
    }

    IEnumerator GhostAI()
    {
        while (true)
        {
            if (player == null)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // 如果手里没有球，就去找球
            if (heldBall == null)
            {
                PickupItem nearestBall = FindNearestPickupableBall();
                if (nearestBall != null)
                {
                    // 飞向球
                    yield return StartCoroutine(FlyToBall(nearestBall));
                }
                else
                {
                    // 没有球可捡，就飞向玩家
                    FlyTowardsPlayer();
                }
            }
            else
            {
                // 手里有球，飞向玩家并准备扔球
                FlyTowardsPlayer();

                // 如果冷却时间到了，就扔球
                if (Time.time - lastThrowTime >= throwCooldown)
                {
                    ThrowBallAtPlayer();
                }
            }

            yield return null;
        }
    }

    /// <summary>
    /// 寻找最近的可捡的球（Layer 是 "Pickup" 且静止）
    /// </summary>
    PickupItem FindNearestPickupableBall()
    {
        PickupItem[] allItems = FindObjectsByType<PickupItem>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        PickupItem nearest = null;
        float nearestDist = float.MaxValue;

        int pickupLayer = LayerMask.NameToLayer("Pickup");

        foreach (var item in allItems)
        {
            if (item == null) continue;
            if (!item.isPickupable) continue;
            if (item.isHeld) continue;

            // 只捡 Layer 是 "Pickup" 的球
            if (item.gameObject.layer != pickupLayer) continue;

            // 只捡静止的球
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null && rb.linearVelocity.magnitude > pickupSpeedThreshold)
            {
                continue;
            }

            float dist = Vector3.Distance(transform.position, item.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = item;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 飞向球并捡起它
    /// </summary>
    IEnumerator FlyToBall(PickupItem ball)
    {
        while (ball != null && !ball.isHeld && heldBall == null)
        {
            Vector3 toBall = ball.transform.position - transform.position;
            float distance = toBall.magnitude;

            if (distance < pickupRange)
            {
                // 捡起球
                PickupBall(ball);
                yield break;
            }

            // 飞向球
            Vector3 direction = toBall.normalized;
            GetComponent<Rigidbody>().linearVelocity = direction * moveSpeed;

            // 转向球
            if (toBall.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            yield return null;
        }
    }

    /// <summary>
    /// 飞向玩家
    /// </summary>
    void FlyTowardsPlayer()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        Vector3 direction = toPlayer.normalized;

        GetComponent<Rigidbody>().linearVelocity = direction * moveSpeed;

        // 转向玩家
        if (toPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 捡起球
    /// </summary>
    void PickupBall(PickupItem ball)
    {
        if (ball == null) return;

        heldBall = ball;
        ball.isHeld = true;

        // 将球的 Layer 改为 "Picked"
        int pickedLayer = LayerMask.NameToLayer("Picked");
        if (pickedLayer != -1)
        {
            ball.gameObject.layer = pickedLayer;
        }

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        ball.transform.SetParent(handTransform);
        ball.transform.localPosition = Vector3.zero;
        ball.transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 向玩家扔球
    /// </summary>
    void ThrowBallAtPlayer()
    {
        if (heldBall == null || player == null) return;

        PickupItem ball = heldBall;
        heldBall = null;

        ball.isHeld = false;
        ball.transform.SetParent(null);

        // 将球的 Layer 改回 "Pickup"
        int pickupLayer = LayerMask.NameToLayer("Pickup");
        if (pickupLayer != -1)
        {
            ball.gameObject.layer = pickupLayer;
        }

        Vector3 throwDirection = (player.position - ball.transform.position).normalized;

        ThrowableBall projectile = ball.GetComponent<ThrowableBall>();
        if (projectile != null)
        {
            projectile.BeginKinematicThrow(throwDirection, throwSpeed);
        }
        else
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearVelocity = throwDirection * throwSpeed;
            }
        }

        lastThrowTime = Time.time;
    }
}
