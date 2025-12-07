using UnityEngine;

// AI-assisted script for Dungeonball-like throwing behaviour:
// 扔的时候使用直线运动，不受重力影响；
// 每帧用 Raycast 检测前进路径，命中后切换到刚体物理弹跳模式。
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ThrowableBall : MonoBehaviour
{
    [Tooltip("哪些 Layer 会触发进入物理弹跳模式（地板、墙、敌人等）。留空=所有碰撞体。")]
    public LayerMask hitLayers;

    [Tooltip("安全兜底：如果一直没撞到任何东西，飞行超过这个时间也强制切换物理。")]
    public float maxFlightTime = 5f;

    private Rigidbody rb;
    private Collider col;

    private bool isFlyingKinematic = false;   // 正在直线飞行阶段
    private Vector3 flyVelocity;              // 直线飞行速度（方向 * 速度）
    private float flightTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    /// <summary>
    /// 开始一次“扔球”：关闭刚体物理，用脚本直线飞。
    /// </summary>
    public void BeginKinematicThrow(Vector3 direction, float speed)
    {
        isFlyingKinematic = true;
        flyVelocity = direction.normalized * speed;
        flightTimer = 0f;

        // 关闭刚体物理，改用脚本控制位移
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 在飞行阶段，不需要碰撞体参与物理解算，用射线检测
        col.enabled = false;
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
            RaycastHit hit;
            int mask = (hitLayers.value == 0) ? Physics.AllLayers : hitLayers.value;

            // 从当前位置沿着飞行方向发射一条射线，长度=本帧位移
            if (Physics.Raycast(start, flyVelocity.normalized, out hit, distance, mask, QueryTriggerInteraction.Ignore))
            {
                // 移动到碰撞点，稍微沿法线方向抬一下，避免卡进碰撞体里
                transform.position = hit.point + hit.normal * 0.01f;

                // 切换到物理弹跳模式
                EnterPhysicsMode();
                return;
            }
        }

        // 如果本帧没有撞到东西，就正常直线飞
        transform.position = start + displacement;

        // 安全时间上限，避免飞到宇宙尽头
        flightTimer += dt;
        if (flightTimer > maxFlightTime)
        {
            EnterPhysicsMode();
        }
    }

    /// <summary>
    /// 切换为刚体物理弹跳模式。
    /// </summary>
    void EnterPhysicsMode()
    {
        if (!isFlyingKinematic) return;
        isFlyingKinematic = false;

        rb.isKinematic = false;
        rb.useGravity = true;
        col.enabled = true;   // 碰撞体重新参与物理

        // 继承飞行速度作为刚体初速度（之后的弹跳由 Physics Material 决定）
        rb.linearVelocity = flyVelocity;
    }
}
