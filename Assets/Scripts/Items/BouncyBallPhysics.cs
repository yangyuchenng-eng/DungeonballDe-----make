using UnityEngine;

// AI-assisted script, tuned by the student for Dungeonball-like behaviour.
[RequireComponent(typeof(Rigidbody))]
public class BouncyBallPhysics : MonoBehaviour
{
    public float horizontalDamping = 10f; // 水平速度衰减系数
    public float minHorizontalSpeed = 0.05f; // 低于这个就直接归零
    public float maxVerticalSpeed = 20f; // 防止竖直速度爆炸

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

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
