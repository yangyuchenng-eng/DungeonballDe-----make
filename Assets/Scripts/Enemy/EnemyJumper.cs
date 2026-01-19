using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyJumper : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float hopHeight = 1.0f;
    public float hopDuration = 0.6f;
    public float idleBetweenHops = 0.3f;

    [Header("Target")]
    public Transform player;
    public float stopDistance = 1.5f;

    [Header("Anti-Wall-Clipping (Cast)")]
    public LayerMask obstacleLayers = ~0;
    public float skinWidth = 0.02f;
    public bool slideAlongWall = true;

    [Tooltip("✅ 本帧位移会拆成多个小步，每步最大长度。越小越稳（0.05~0.15）。")]
    public float maxStepDistance = 0.1f;

    [Tooltip("✅ 每帧移动后做去穿透修正（XZ平面）。")]
    public bool depenetrateAfterMove = true;

    [Tooltip("去穿透最多迭代次数（通常 2~4 足够）")]
    public int depenetrateIterations = 3;

    [Header("Debug")]
    public bool debugDrawCasts = false;

    private bool isMoving = true;
    private float groundY;
    private Collider bodyCol;

    void Start()
    {
        bodyCol = GetComponent<Collider>();

        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        groundY = transform.position.y;
        StartCoroutine(JumpRoutine());
    }

    IEnumerator JumpRoutine()
    {
        while (isMoving)
        {
            if (!gameObject.CompareTag("slime"))
            {
                yield return null;
                continue;
            }

            if (player == null)
            {
                yield return null;
                continue;
            }

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;

            if (distance > 0.01f)
            {
                Vector3 dir = toPlayer.normalized;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir, Vector3.up),
                    0.2f
                );

                if (distance > stopDistance)
                {
                    yield return StartCoroutine(HopStep(dir));
                    yield return new WaitForSeconds(idleBetweenHops);
                    continue;
                }
            }

            yield return null;
        }
    }

    IEnumerator HopStep(Vector3 dir)
    {
        if (!gameObject.CompareTag("slime"))
            yield break;

        float t = 0f;

        while (t < hopDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / hopDuration);

            // 原逻辑：本帧水平位移
            Vector3 desiredHorizontal = dir * moveSpeed * Time.deltaTime;

            // ✅ 关键：把本帧位移拆小步，每步都做 Cast
            Vector3 moved = StepMoveWithCasts(desiredHorizontal);

            Vector3 pos = transform.position + moved;

            // 原逻辑：抛物线高度（不改）
            float yOffset = hopHeight * 4f * normalized * (1f - normalized);
            pos.y = groundY + yOffset;

            transform.position = pos;

            // ✅ 去穿透（只推XZ），避免“已经插进墙里导致下一帧直接穿”
            if (depenetrateAfterMove)
                DepenetrateXZ();

            yield return null;
        }

        Vector3 endPos = transform.position;
        endPos.y = groundY;
        transform.position = endPos;

        if (depenetrateAfterMove)
            DepenetrateXZ();
    }

    Vector3 StepMoveWithCasts(Vector3 desired)
    {
        float totalDist = desired.magnitude;
        if (totalDist < 0.000001f) return Vector3.zero;

        float step = Mathf.Max(0.001f, maxStepDistance);
        int steps = Mathf.CeilToInt(totalDist / step);

        Vector3 perStep = desired / steps;
        Vector3 accum = Vector3.zero;

        for (int i = 0; i < steps; i++)
        {
            Vector3 safe = ResolveHorizontalMoveWithCast(perStep);
            transform.position += safe;     // 这里先“走一步”
            accum += safe;

            if (depenetrateAfterMove)
                DepenetrateXZ();
        }

        // 注意：我们在循环里已经把 transform.position 改了
        // 所以返回值给外层只是“统计量”，外层会用 pos 再覆盖 y
        // 为避免外层重复加位移，这里返回 Vector3.zero
        // ✅ 外层会用 transform.position 当前值再设置 y
        return Vector3.zero;
    }

    Vector3 ResolveHorizontalMoveWithCast(Vector3 desiredMove)
    {
        if (desiredMove.sqrMagnitude < 0.0000001f) return Vector3.zero;
        if (bodyCol == null) return desiredMove;

        Vector3 move = desiredMove;
        Vector3 dir = move.normalized;
        float dist = move.magnitude;

        Bounds b = bodyCol.bounds;
        float radius = Mathf.Max(0.01f, Mathf.Min(b.extents.x, b.extents.z));
        float halfHeight = Mathf.Max(radius, b.extents.y);
        Vector3 center = b.center;

        Vector3 p1 = center + Vector3.up * (halfHeight - radius);
        Vector3 p2 = center - Vector3.up * (halfHeight - radius);

        if (Physics.CapsuleCast(p1, p2, radius, dir, out RaycastHit hit, dist + skinWidth, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            float allowed = Mathf.Max(0f, hit.distance - skinWidth);
            Vector3 firstMove = dir * allowed;

            if (debugDrawCasts)
            {
                Debug.DrawLine(p1, p1 + dir * (dist + skinWidth), Color.red, 0.05f);
                Debug.DrawRay(hit.point, hit.normal, Color.yellow, 0.1f);
            }

            if (!slideAlongWall)
                return firstMove;

            Vector3 remaining = move - firstMove;
            Vector3 slide = Vector3.ProjectOnPlane(remaining, hit.normal);

            if (slide.sqrMagnitude < 0.0000001f)
                return firstMove;

            Vector3 slideDir = slide.normalized;
            float slideDist = slide.magnitude;

            Vector3 sp1 = p1 + firstMove;
            Vector3 sp2 = p2 + firstMove;

            if (Physics.CapsuleCast(sp1, sp2, radius, slideDir, out RaycastHit hit2, slideDist + skinWidth, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
                float allowed2 = Mathf.Max(0f, hit2.distance - skinWidth);
                return firstMove + slideDir * allowed2;
            }

            return firstMove + slide;
        }

        if (debugDrawCasts)
            Debug.DrawLine(p1, p1 + dir * (dist + skinWidth), Color.green, 0.05f);

        return move;
    }

    void DepenetrateXZ()
    {
        if (bodyCol == null) return;

        // 用 bounds 近似一个 capsule 来做 Overlap + ComputePenetration 推出
        Bounds b = bodyCol.bounds;
        float radius = Mathf.Max(0.01f, Mathf.Min(b.extents.x, b.extents.z));
        float halfHeight = Mathf.Max(radius, b.extents.y);
        Vector3 center = b.center;

        Vector3 p1 = center + Vector3.up * (halfHeight - radius);
        Vector3 p2 = center - Vector3.up * (halfHeight - radius);

        for (int iter = 0; iter < Mathf.Max(1, depenetrateIterations); iter++)
        {
            Collider[] hits = Physics.OverlapCapsule(p1, p2, radius, obstacleLayers, QueryTriggerInteraction.Ignore);
            bool movedAny = false;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider other = hits[i];
                if (other == null) continue;
                if (other == bodyCol) continue;

                if (Physics.ComputePenetration(
                        bodyCol, transform.position, transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 dir, out float dist))
                {
                    // 只在 XZ 推出，避免把它推上天/推下地
                    dir.y = 0f;
                    float len = dir.magnitude;
                    if (len > 0.0001f)
                    {
                        Vector3 push = (dir / len) * (dist + skinWidth);
                        transform.position += push;
                        movedAny = true;
                    }
                }
            }

            if (!movedAny) break;

            // 更新 capsule 端点（因为 transform.position 变了）
            b = bodyCol.bounds;
            center = b.center;
            p1 = center + Vector3.up * (halfHeight - radius);
            p2 = center - Vector3.up * (halfHeight - radius);
        }
    }
}
