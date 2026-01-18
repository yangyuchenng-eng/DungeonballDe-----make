using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BatEnemy : MonoBehaviour
{
    [Header("Player Target (drag the BODY / model / CharacterController object here)")]
    public Transform playerTarget;
    public Collider playerTargetCollider;

    [Header("Look / Front Point")]
    public float frontDistance = 2.5f;
    public float arriveTolerance = 0.25f;
    public float flySpeed = 6f;
    public float returnSpeed = 7f;
    public float turnSpeed = 8f;
    public float repathDistance = 0.6f;

    [Header("Dash Attack")]
    public int damagePerHit = 10;
    public float attackCooldown = 1.0f;
    public float dashDuration = 0.18f;
    public float dashSpeed = 12f;

    [Tooltip("命中判定半径（建议 0.8~1.2）")]
    public float hitRadius = 1.0f;

    [Header("Height Lock")]
    public bool lockHeight = true;

    [Header("Model Pose Lock (optional)")]
    public Transform modelRoot;

    Rigidbody rb;
    Quaternion modelInitialLocalRot;

    PlayerHealth playerHealth;

    enum State { Approach, Wait, Dash, Return }
    State state = State.Approach;

    float nextAttackTime;
    float dashEndTime;
    bool didDamageThisDash;

    Vector3 lastFrontPointXZ;

    // 固定高度
    float fixedY;

    // ✅ 新增：允许外部（SpawnPoint）覆盖出生高度
    bool heightOverridden = false;
    float overriddenY = 0f;

    /// <summary>
    /// ✅ 由 SpawnPoint 调用：告诉蝙蝠应该锁在哪个高度出生
    /// </summary>
    public void SetSpawnHeight(float y)
    {
        heightOverridden = true;
        overriddenY = y;
        fixedY = y;

        // 如果刚体已初始化，立刻把位置抬到该高度
        if (rb != null)
        {
            Vector3 pos = rb.position;
            pos.y = fixedY;
            rb.position = pos;
        }
        else
        {
            // rb 还没 Awake 也没关系：Start 时会用 overriddenY
            Vector3 pos = transform.position;
            pos.y = fixedY;
            transform.position = pos;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        if (modelRoot != null)
            modelInitialLocalRot = modelRoot.localRotation;

        ResolvePlayerRefs();

        // ✅ 如果外部已经指定高度，就先用它把位置对齐
        if (heightOverridden)
        {
            Vector3 pos = rb.position;
            pos.y = overriddenY;
            rb.position = pos;
            fixedY = overriddenY;
        }
    }

    void Start()
    {
        // ✅ 如果外部没指定，才用“出生时的 y”
        if (!heightOverridden)
        {
            fixedY = rb.position.y;
        }

        lastFrontPointXZ = GetFrontPointXZ();
        nextAttackTime = Time.time + Random.Range(0.2f, 0.6f);
    }

    void ResolvePlayerRefs()
    {
        if (playerTarget == null)
        {
#if UNITY_2023_1_OR_NEWER
            playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
#else
            playerHealth = Object.FindObjectOfType<PlayerHealth>();
#endif
            if (playerHealth != null) playerTarget = playerHealth.transform;
        }

        if (playerTarget != null)
        {
            if (playerHealth == null)
                playerHealth = playerTarget.GetComponent<PlayerHealth>()
                            ?? playerTarget.GetComponentInParent<PlayerHealth>()
                            ?? playerTarget.GetComponentInChildren<PlayerHealth>();

            if (playerTargetCollider == null)
                playerTargetCollider = playerTarget.GetComponent<Collider>()
                                   ?? playerTarget.GetComponentInChildren<Collider>();
        }
    }

    void FixedUpdate()
    {
        if (playerTarget == null || playerHealth == null)
        {
            ResolvePlayerRefs();
            if (playerTarget == null || playerHealth == null)
            {
                rb.linearVelocity = Vector3.zero;
                return;
            }
        }

        // ✅高度锁定
        if (lockHeight)
        {
            Vector3 pos = rb.position;
            if (!Mathf.Approximately(pos.y, fixedY))
            {
                pos.y = fixedY;
                rb.position = pos;
            }
        }

        if (modelRoot != null)
            modelRoot.localRotation = modelInitialLocalRot;

        Vector3 frontXZ = GetFrontPointXZ();
        if ((frontXZ - lastFrontPointXZ).sqrMagnitude > repathDistance * repathDistance)
        {
            lastFrontPointXZ = frontXZ;
            if (state == State.Wait) state = State.Approach;
        }

        switch (state)
        {
            case State.Approach: DoApproach(); break;
            case State.Wait: DoWait(); break;
            case State.Dash: DoDash(); break;
            case State.Return: DoReturn(); break;
        }
    }

    Vector3 GetForwardFlat()
    {
        Vector3 f = playerTarget.forward;
        f.y = 0f;
        if (f.sqrMagnitude < 0.0001f) f = Vector3.forward;
        return f.normalized;
    }

    Vector3 GetFrontPoint()
    {
        Vector3 f = GetForwardFlat();
        Vector3 p = playerTarget.position + f * frontDistance;

        float y = lockHeight ? fixedY : transform.position.y;
        return new Vector3(p.x, y, p.z);
    }

    Vector3 GetFrontPointXZ()
    {
        Vector3 p = GetFrontPoint();
        p.y = 0f;
        return p;
    }

    void DoApproach()
    {
        Vector3 targetPos = GetFrontPoint();
        Vector3 toTarget = targetPos - transform.position;
        float dist = toTarget.magnitude;

        FaceYaw(playerTarget.position - transform.position);

        if (dist <= arriveTolerance)
        {
            rb.linearVelocity = Vector3.zero;
            state = State.Wait;
            return;
        }

        rb.linearVelocity = (toTarget / Mathf.Max(0.001f, dist)) * flySpeed;
    }

    void DoWait()
    {
        rb.linearVelocity = Vector3.zero;
        FaceYaw(playerTarget.position - transform.position);

        float distToFront = (GetFrontPoint() - transform.position).magnitude;
        if (distToFront > arriveTolerance * 2f)
        {
            state = State.Approach;
            return;
        }

        if (Time.time >= nextAttackTime)
        {
            state = State.Dash;
            didDamageThisDash = false;
            dashEndTime = Time.time + dashDuration;
        }
    }

    void DoDash()
    {
        Vector3 toPlayer = playerTarget.position - transform.position;
        Vector3 dir = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : transform.forward;

        FaceYaw(dir);
        rb.linearVelocity = dir * dashSpeed;

        if (!didDamageThisDash)
        {
            Vector3 batPos = transform.position;
            Vector3 closest = playerTargetCollider != null ? playerTargetCollider.ClosestPoint(batPos) : playerTarget.position;

            Vector3 d = closest - batPos;
            d.y = 0f;

            if (d.sqrMagnitude <= hitRadius * hitRadius)
            {
                playerHealth.TakeDamage(damagePerHit);
                didDamageThisDash = true;
            }
        }

        if (Time.time >= dashEndTime)
        {
            state = State.Return;
        }
    }

    void DoReturn()
    {
        Vector3 targetPos = GetFrontPoint();
        Vector3 toTarget = targetPos - transform.position;
        float dist = toTarget.magnitude;

        FaceYaw(playerTarget.position - transform.position);

        if (dist <= arriveTolerance)
        {
            rb.linearVelocity = Vector3.zero;
            nextAttackTime = Time.time + attackCooldown;
            lastFrontPointXZ = GetFrontPointXZ();
            state = State.Wait;
            return;
        }

        rb.linearVelocity = (toTarget / Mathf.Max(0.001f, dist)) * returnSpeed;
    }

    void FaceYaw(Vector3 dir)
    {
        Vector3 flat = new Vector3(dir.x, 0f, dir.z);
        if (flat.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(flat.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
    }
}
