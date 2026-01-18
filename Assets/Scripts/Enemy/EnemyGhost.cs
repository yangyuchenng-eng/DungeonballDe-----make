using UnityEngine;

public class EnemyGhost : MonoBehaviour
{
    [Header("Movement (Transform)")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 10f;
    public float arriveDistance = 0.3f;

    [Tooltip("锁定出生时的 Y，只在 XZ 平移")]
    public bool lockY = true;

    [Header("Search")]
    public float searchInterval = 0.5f;

    [Header("Pickup")]
    [Tooltip("幽灵只捡这个 layer 的球（Key 不在这个 layer 就不会捡）")]
    public string pickupLayerName = "Pickup";

    [Tooltip("拿在手里时改成这个 layer（可选）")]
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

        // 没球：定期找最近的可捡球
        if (heldBall == null && Time.time >= nextSearchTime)
        {
            targetBall = FindNearestPickupBall();
            nextSearchTime = Time.time + searchInterval;

            if (debugLogs)
                Debug.Log($"[EnemyGhost] targetBall = {(targetBall ? targetBall.name : "null")}", this);
        }

        // 有球：追玩家 + 冷却到就扔
        if (heldBall != null)
        {
            MoveTowards(player.position);

            if (Time.time - lastThrowTime >= throwCooldown)
                ThrowHeldBall();

            return;
        }

        // 没球：优先追球，否则追玩家
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

        // 旋转朝向（只转 Y）
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // 纯平移（按你要求：不做任何防穿墙处理）
        Vector3 move = dir * moveSpeed * Time.deltaTime;
        Vector3 next = pos + move;

        if (lockY) next.y = fixedY;

        transform.position = next;
    }

    PickupItem FindNearestPickupBall()
    {
        int pickupLayer = LayerMask.NameToLayer(pickupLayerName); // -1 if not exists

        PickupItem[] all = FindObjectsByType<PickupItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        PickupItem best = null;
        float bestDist = float.MaxValue;

        foreach (var it in all)
        {
            if (it == null) continue;
            if (!it.isPickupable) continue;
            if (it.isHeld) continue;

            if (pickupLayer != -1 && it.gameObject.layer != pickupLayer) continue;

            float d = Vector3.Distance(transform.position, it.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = it;
            }
        }

        return best;
    }

    // 捡球触发：你可以把这个脚本挂在“Trigger 子物体”上，
    // 或者把 Trigger 子物体的 Collider 设成 IsTrigger 并确保它能触发到这里
    void OnTriggerEnter(Collider other)
    {
        if (heldBall != null) return;

        PickupItem ball = other.GetComponentInParent<PickupItem>();
        if (ball == null) return;
        if (!ball.isPickupable || ball.isHeld) return;

        int pickupLayer = LayerMask.NameToLayer(pickupLayerName);
        if (pickupLayer != -1 && ball.gameObject.layer != pickupLayer) return;

        PickupBall(ball);
    }

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

        if (debugLogs) Debug.Log("[EnemyGhost] Threw ball", this);
    }

    // 幽灵死了，球别跟着一起没了
    void OnDisable() => DropHeldBallIfAny();
    void OnDestroy() => DropHeldBallIfAny();

    private void DropHeldBallIfAny()
    {
        if (heldBall == null) return;

        PickupItem ball = heldBall;
        heldBall = null;

        ball.transform.SetParent(null, true);
        ball.isHeld = false;
        ball.wasThrownByPlayer = false;

        int pickupLayer = LayerMask.NameToLayer(pickupLayerName);
        if (pickupLayer != -1) ball.gameObject.layer = pickupLayer;

        Rigidbody brb = ball.GetComponent<Rigidbody>();
        if (brb != null)
        {
            brb.isKinematic = false;
            brb.useGravity = true;
            brb.linearVelocity = Vector3.zero;
            brb.angularVelocity = Vector3.zero;
        }

        BallDamage bd = ball.GetComponent<BallDamage>();
        if (bd != null)
        {
            bd.isEnemyProjectile = false;
            bd.damagesPlayer = false;
            bd.damagesEnemies = true;
        }
    }
}
