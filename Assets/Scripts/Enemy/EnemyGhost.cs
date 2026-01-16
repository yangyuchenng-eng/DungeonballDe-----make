using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyGhost : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 8f;
    public bool ignoreVertical = true;

    [Header("Search")]
    public float searchInterval = 0.5f;

    [Header("Pickup Trigger")]
    [Tooltip("放一个 SphereCollider(Trigger) 在这个物体或子物体上，用来检测球")]
    public Collider pickupTrigger;

    [Tooltip("只捡 Pickup layer 的球（推荐）")]
    public string pickupLayerName = "Pickup";

    [Tooltip("拿在手里时改成这个 layer（并在矩阵里关闭 Picked<->Enemy 碰撞）")]
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

    private Rigidbody rb;
    private Transform player;

    private PickupItem targetBall;
    private PickupItem heldBall;

    private float nextSearchTime = 0f;
    private float lastThrowTime = -999f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (handTransform == null) handTransform = transform;

        if (pickupTrigger == null)
            Debug.LogWarning("[EnemyGhost] pickupTrigger 没设置！请拖一个 SphereCollider(Trigger) 进来。", this);

        FindPlayer();
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        if (heldBall == null && Time.time >= nextSearchTime)
        {
            targetBall = FindNearestPickupBall();
            nextSearchTime = Time.time + searchInterval;

            if (debugLogs)
                Debug.Log($"[EnemyGhost] Search targetBall = {(targetBall ? targetBall.name : "null")}", this);
        }

        if (heldBall == null)
        {
            if (targetBall != null && !targetBall.isHeld)
                MoveTowards(targetBall.transform.position);
            else
                MoveTowards(player.position);
        }
        else
        {
            MoveTowards(player.position);

            if (Time.time - lastThrowTime >= throwCooldown)
                ThrowHeldBall();
        }
    }

    void FindPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        player = p ? p.transform : null;

        if (debugLogs && player == null)
            Debug.LogWarning("[EnemyGhost] 找不到 Tag=Player 的对象！", this);
    }

    void MoveTowards(Vector3 targetPos)
    {
        Vector3 to = targetPos - transform.position;
        if (ignoreVertical) to.y = 0f;
        if (to.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 dir = to.normalized;
        rb.linearVelocity = dir * moveSpeed;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion newRot = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newRot);
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

    // ✅ 一碰到球就捡：用 Trigger 检测
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

        // 放到手上
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

        // 扔出去恢复物理
        Rigidbody brb = ball.GetComponent<Rigidbody>();
        if (brb != null)
        {
            brb.isKinematic = false;
            brb.useGravity = true;
        }

        // 敌人扔球：标记伤害玩家
        BallDamage bd = ball.GetComponent<BallDamage>();
        if (bd != null)
        {
            bd.isEnemyProjectile = true;
            bd.damagesPlayer = true;
            bd.damagesEnemies = false;
        }

        Vector3 dir = (player.position - ball.transform.position).normalized;

        ThrowableBall tb = ball.GetComponent<ThrowableBall>();
        if (tb != null)
            tb.BeginKinematicThrow(dir, throwSpeed);
        else if (brb != null)
            brb.linearVelocity = dir * throwSpeed;

        lastThrowTime = Time.time;

        if (debugLogs) Debug.Log("[EnemyGhost] Threw ball", this);
    }
}
