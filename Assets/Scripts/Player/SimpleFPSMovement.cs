using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleFPSMovement : MonoBehaviour
{
    public CharacterController controller;
    public float moveSpeed = 5f;
    public float turnSpeed = 120f;
    public float gravity = -9.81f;

    private Vector3 velocity;

    void Reset()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (controller == null) return;

        // WS 前后移动（键位用 Unity 默认的 "Vertical"）
        float moveInput = Input.GetAxisRaw("Vertical");   // W = 1, S = -1

        // AD 左右旋转（键位用 Unity 默认的 "Horizontal"）
        float turnInput = Input.GetAxisRaw("Horizontal"); // A = -1, D = 1

        // 计算前进方向（忽略 Y）
        Vector3 forward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 move = forward * moveInput * moveSpeed;

        // 应用移动
        controller.Move(move * Time.deltaTime);

        // 左右转身（旋转玩家本身）
        transform.Rotate(Vector3.up, turnInput * turnSpeed * Time.deltaTime);

        // 简单重力：CharacterController 需要手动施加 Y 速度
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f; // 贴地一点
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
