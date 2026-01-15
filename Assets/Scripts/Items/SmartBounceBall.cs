using UnityEngine;

// AI-assisted script for custom bounce behaviour:
// - 撞 "墙"（wallLayers）：强烈反弹，适合在墙角来回横弹
// - 撞 "地"（floorLayers）：不再往上弹，只保留一点水平速度
// - 其他碰撞体：用法线判定，大概归类成地/墙
// IMPROVED: 整合了 BouncyBallPhysics 的功能（水平阻尼、速度限制）到这个脚本中
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class SmartBounceBall : MonoBehaviour
{
    [Header("Layer-based classification")]
   
    public LayerMask floorLayers;

   
    public LayerMask wallLayers;

    [Header("Wall bounce settings")]
    [Tooltip("撞墙后的速度倍率（1 = 完全弹性，<1 = 逐渐减速）")]
    public float wallBounceMultiplier = 0.9f;

    [Tooltip("撞墙时竖直方向速度的保留比例（0 = 只保留水平，1 = 完全保留原来的竖直分量）")]
    public float wallVerticalKeep = 0.3f;

    [Header("Floor behaviour")]
    [Tooltip("撞地板时，水平速度乘以这个系数（<1 = 减速，0 = 直接停住）")]
    public float floorHorizontalDamping = 0.6f;

    [Tooltip("如果撞地板后速度低于这个值，就直接停住")]
    public float minSpeedToStop = 1.0f;

    [Header("Continuous Damping (IMPROVED)")]
    [Tooltip("水平速度衰减系数（每秒衰减的百分比）")]
    public float horizontalDamping = 10f;

    [Tooltip("低于这个速度就直接归零")]
    public float minHorizontalSpeed = 0.05f;

    [Tooltip("防止竖直速度爆炸的上限")]
    public float maxVerticalSpeed = 20f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (rb == null) return;

        Vector3 v = rb.linearVelocity;
        if (v.sqrMagnitude < 0.0001f) return; // 已经很慢就不处理

        GameObject other = collision.gameObject;
        int otherLayer = other.layer;

        // 1️⃣ 先用 Layer 判断：floorLayers 优先
        if (IsInLayerMask(otherLayer, floorLayers))
        {
            HandleFloorHit(v);
            return;
        }

        // 2️⃣ 再看 wallLayers
        if (IsInLayerMask(otherLayer, wallLayers))
        {
            Vector3 normal = GetAverageNormal(collision);
            HandleWallHit(v, normal);
            return;
        }

        // 3️⃣ 都不是，再用法线兜底（比如有些斜坡、柱子等）
        Vector3 avgNormal = GetAverageNormal(collision);
        if (avgNormal == Vector3.zero) return;

        float upDot = Vector3.Dot(avgNormal, Vector3.up);

        // 法线比较朝上 ≈ 地面
        if (upDot > 0.7f)
        {
            HandleFloorHit(v);
        }
        else
        {
            HandleWallHit(v, avgNormal);
        }
    }

    bool IsInLayerMask(int layer, LayerMask mask)
    {
        if (mask.value == 0) return false; // 没设置任何层
        return (mask.value & (1 << layer)) != 0;
    }

    Vector3 GetAverageNormal(Collision collision)
    {
        Vector3 avgNormal = Vector3.zero;
        int count = collision.contactCount;

        for (int i = 0; i < count; i++)
        {
            avgNormal += collision.GetContact(i).normal;
        }

        if (count > 0)
        {
            avgNormal /= count;
            avgNormal.Normalize();
        }

        return avgNormal;
    }

    void HandleFloorHit(Vector3 v)
    {
        // 不再往上弹：竖直分量清零
        Vector3 horizontal = new Vector3(v.x, 0f, v.z) * floorHorizontalDamping;
        float speed = horizontal.magnitude;

        if (speed < minSpeedToStop)
        {
            rb.linearVelocity = Vector3.zero;
        }
        else
        {
            rb.linearVelocity = horizontal;
        }
    }

    void HandleWallHit(Vector3 v, Vector3 normal)
    {
        // IMPROVED: 增加了对法线有效性的检查，避免无效的反射
        if (normal == Vector3.zero || normal.sqrMagnitude < 0.1f)
        {
            // 没拿到正常法线就直接略减速
            rb.linearVelocity = v * wallBounceMultiplier;
            return;
        }

        // 反射向量：像子弹一样从墙上弹回去
        Vector3 reflected = Vector3.Reflect(v, normal);

        // 控制弹性程度
        reflected *= wallBounceMultiplier;

        // 压低竖直分量：更多是横向乱弹，不是弹到天花板
        reflected.y = reflected.y * wallVerticalKeep;

        rb.linearVelocity = reflected;
    }

    // IMPROVED: 在 FixedUpdate 中应用连续的物理衰减，整合了 BouncyBallPhysics 的功能
    void FixedUpdate()
    {
        if (rb == null) return;

        Vector3 v = rb.linearVelocity;

        // 水平方向（XZ）衰减，避免长时间滚动/滑动
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);
        float speed = horizontal.magnitude;

        if (speed > 0f)
        {
            // 指数衰减
            float damp = 1f - horizontalDamping * Time.fixedDeltaTime;
            if (damp < 0f) damp = 0f;
            horizontal *= damp;

            // 如果已经很慢，就直接归零
            if (horizontal.magnitude < minHorizontalSpeed)
            {
                horizontal = Vector3.zero;
            }
        }

        // 限制竖直速度，避免弹到天上乱飞
        float vy = Mathf.Clamp(v.y, -maxVerticalSpeed, maxVerticalSpeed);

        rb.linearVelocity = new Vector3(horizontal.x, vy, horizontal.z);
        // 额外保险：基本不转动
        rb.angularVelocity = Vector3.zero;
    }
}
