using UnityEngine;

public class EnemyGhost : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 10f;
    public float arriveDistance = 0.3f;
    public bool lockY = true;

    [Header("Search")]
    public float searchInterval = 0.5f;

    [Header("Pickup Range (Child Trigger Collider)")]
    [Tooltip("把子物体上的 SphereCollider(Trigger) 拖到这里，用来当捡球检测范围。")]
    public SphereCollider pickupRange;

    [Tooltip("真正捡起球需要离得更近（防止边缘抖动）。<=0 则使用 pickupRange 半径的 0.6 倍。")]
    public float pickupDistance = 0f;

    [Header("Pickup")]
    public string pickupLayerName = "Pickup";
    public string pickedLayerName = "Picked";

    [Header("Hand / Holding")]
    public Transform handTransform;
    public Vector3 holdLocalPos = Vector3.zero;
    public Vector3 holdLocalEuler = Vector3.zero;

    [Header("Throw")]
    public float throwSpeed = 15f;
    public float throwCooldown = 2f;

    [Header("Debug")]
    public bool debugLogs = false;

    private Transform player;

    private PickupItem targetBall;
    private PickupItem heldBall;

    private float nextSearchTime = 0f;
    private float lastThrowTime = -999f;
    private float fixedY;

    void Awake()
    {
        if (handTransform == null) handTransform = transform;
        fixedY = transform.position.y;
    }

    void Start()
    {
        FindPlayer();
        fixedY = transform.position.y;
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        // 没拿球：按间隔在范围内找球
        if (heldBall == null && Time.time >= nextSearchTime)
        {
            targetBall = FindNearestPickupBallInRange();
            nextSearchTime = Time.time + searchInterval;

            if (debugLogs)
                Debug.Log($"[EnemyGhost] targetBall(inRange) = {(targetBall ? targetBall.name : "null")}", this);
        }

        // 如果目标球在范围内并且已经足够近，则直接捡起来（不依赖 OnTrigger）
        if (heldBall == null && targetBall != null && !targetBall.isHeld)
        {
            float pickDist = GetPickupDistance();
            float d2 = (targetBall.transform.position - transform.position).sqrMagnitude;
            if (d2 <= pickDist * pickDist)
            {
                PickupBall(targetBall);
            }
        }

        // 有球：追玩家 + 到点就扔
        if (heldBall != null)
        {
            MoveTowards(player.position);

            if (Time.time - lastThrowTime >= throwCooldown)
                ThrowHeldBall();

            return;
        }

        // 没球：有目标球就追球，否则追玩家
        if (targetBall != null && !targetBall.isHeld)
            MoveTowards(targetBall.transform.position);
        else
            MoveTowards(player.position);
    }

    void FindPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        player = p ? p.transform : null;
    }

    void MoveTowards(Vector3 targetPos)
    {
        Vector3 pos = transform.position;

        Vector3 to = targetPos - pos;
        to.y = 0f;

        float dist = to.magnitude;
        if (dist <= arriveDistance) return;

        Vector3 dir = to / Mathf.Max(0.001f, dist);

        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        Vector3 move = dir * moveSpeed * Time.deltaTime;
        Vector3 next = pos + move;
        if (lockY) next.y = fixedY;

        transform.position = next;
    }

    // ---------------- Range-based ball query ----------------

    Vector3 GetPickupRangeCenter()
    {
        if (pickupRange == null) return transform.position;
        return pickupRange.transform.TransformPoint(pickupRange.center);
    }

    float GetPickupRangeRadius()
    {
        if (pickupRange == null) return 0f;
        // SphereCollider 半径受 transform scale 影响，这里取最大缩放轴作为近似
        Vector3 lossy = pickupRange.transform.lossyScale;
        float scale = Mathf.Max(lossy.x, lossy.y, lossy.z);
        return pickupRange.radius * scale;
    }

    float GetPickupDistance()
    {
        if (pickupDistance > 0f) return pickupDistance;

        float r = GetPickupRangeRadius();
        if (r <= 0f) return 1.5f; // 没设置范围时的兜底
        return r * 0.6f;
    }

    PickupItem FindNearestPickupBallInRange()
    {
        int pickupLayer = LayerMask.NameToLayer(pickupLayerName);

        float rangeR = GetPickupRangeRadius();
        if (rangeR <= 0.0001f)
        {
            // 没拖 pickupRange 就退回原逻辑（但会跨图找球），建议必须设置
            return FindNearestPickupBall_FallbackGlobal(pickupLayer);
        }

        Vector3 center = GetPickupRangeCenter();

        // ✅ 只在范围球里找碰撞体（性能更好，也不会跨地图）
        Collider[] hits = Physics.OverlapSphere(center, rangeR, ~0, QueryTriggerInteraction.Collide);

        PickupItem best = null;
        float bestD2 = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (c == null) continue;

            PickupItem it = c.GetComponentInParent<PickupItem>();
            if (it == null) continue;

            if (!it.isPickupable) continue;
            if (it.isHeld) continue;

            if (pickupLayer != -1 && it.gameObject.layer != pickupLayer) continue;

            float d2 = (it.transform.position - transform.position).sqrMagnitude;
            if (d2 < bestD2)
            {
                bestD2 = d2;
                best = it;
            }
        }

        return best;
    }

    // 如果你忘了拖 pickupRange，这个兜底会回到全局找（不推荐，但至少不会空指针）
    PickupItem FindNearestPickupBall_FallbackGlobal(int pickupLayer)
    {
        PickupItem[] all = FindObjectsByType<PickupItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        PickupItem best = null;
        float bestDist = float.MaxValue;

        Vector3 ghostPos = transform.position;

        foreach (var it in all)
        {
            if (it == null) continue;
            if (!it.isPickupable) continue;
            if (it.isHeld) continue;

            if (pickupLayer != -1 && it.gameObject.layer != pickupLayer) continue;

            float d2 = (it.transform.position - ghostPos).sqrMagnitude;
            if (d2 < bestDist)
            {
                bestDist = d2;
                best = it;
            }
        }

        return best;
    }

    // ---------------- Pickup / Throw (原逻辑保持) ----------------

    void PickupBall(PickupItem ball)
    {
        heldBall = ball;
        targetBall = null;

        ball.isHeld = true;
        ball.wasThrownByPlayer = false;

        int pickedLayer = LayerMask.NameToLayer(pickedLayerName);
        if (pickedLayer != -1) ball.gameObject.layer = pickedLayer;

        Rigidbody brb = ball.GetComponent<Rigidbody>();
        if (brb != null)
        {
            brb.isKinematic = true;
            brb.useGravity = false;
            brb.linearVelocity = Vector3.zero;
            brb.angularVelocity = Vector3.zero;
        }

        BallDamage bd = ball.GetComponent<BallDamage>();
        if (bd != null)
        {
            bd.isEnemyProjectile = false;
            bd.damagesPlayer = false;
            bd.damagesEnemies = false;
        }

        ball.transform.SetParent(handTransform);
        ball.transform.localPosition = holdLocalPos;
        ball.transform.localRotation = Quaternion.Euler(holdLocalEuler);

        if (debugLogs) Debug.Log($"[EnemyGhost] Picked {ball.name}", this);
    }

    void ThrowHeldBall()
    {
        if (heldBall == null || player == null) return;

        PickupItem ball = heldBall;
        heldBall = null;

        ball.isHeld = false;
        ball.wasThrownByPlayer = false;
        ball.transform.SetParent(null);

        int pickupLayer = LayerMask.NameToLayer(pickupLayerName);
        if (pickupLayer != -1) ball.gameObject.layer = pickupLayer;

        Rigidbody brb = ball.GetComponent<Rigidbody>();
        if (brb != null)
        {
            brb.isKinematic = false;
            brb.useGravity = true;
        }

        BallDamage bd = ball.GetComponent<BallDamage>();
        if (bd != null)
        {
            bd.isEnemyProjectile = true;
            bd.damagesPlayer = true;
            bd.damagesEnemies = false;
        }

        Vector3 dir = (player.position - ball.transform.position);
        dir.y = 0f;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;

        ThrowableBall tb = ball.GetComponent<ThrowableBall>();
        if (tb != null)
            tb.BeginKinematicThrow(dir, throwSpeed);
        else if (brb != null)
            brb.linearVelocity = dir * throwSpeed;

        lastThrowTime = Time.time;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (pickupRange != null)
        {
            Vector3 c = pickupRange.transform.TransformPoint(pickupRange.center);
            float r = pickupRange.radius * Mathf.Max(pickupRange.transform.lossyScale.x, pickupRange.transform.lossyScale.y, pickupRange.transform.lossyScale.z);
            Gizmos.DrawWireSphere(c, r);
        }
    }
#endif
}
