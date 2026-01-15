using UnityEngine;

// IMPROVED: 升级为现代FPS控制模式
// - 鼠标控制视角方向（水平旋转玩家，垂直旋转摄像机）
// - WASD 移动（W/S 前后，A/D 左右平移）
[RequireComponent(typeof(CharacterController))]
public class SimpleFPSMovement : MonoBehaviour
{
    public CharacterController controller;
    public Camera cam;

    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float maxLookUp = 20f;      // 向上看的最大角度（不需要看天花板）
    public float maxLookDown = 30f;    // 向下看的最大角度（看脚底的球）

    [Header("Physics")]
    public float gravity = -9.81f;

    private Vector3 velocity;
    private float currentPitch = 0f;  // 摄像机的俯仰角

    void Reset()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
    }

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        // 锁定鼠标光标到游戏窗口中央，隐藏光标
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (controller == null) return;

        // ========== 鼠标视角控制 ==========
        HandleMouseLook();

        // ========== 键盘移动控制 ==========
        HandleMovement();

        // ========== 重力 ==========
        HandleGravity();
    }

    void HandleMouseLook()
    {
        // 获取鼠标移动量
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 水平旋转玩家（绕 Y 轴）
        transform.Rotate(Vector3.up, mouseX);

        // 垂直旋转摄像机（绕 X 轴），使用不对称的视野限制
        // 向下看 30 度：看脚底的球
        // 向上看 20 度：不需要看天花板，但保留一点上视野
        currentPitch -= mouseY;
        currentPitch = Mathf.Clamp(currentPitch, -maxLookUp, maxLookDown);

        if (cam != null)
        {
            cam.transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }
    }

    void HandleMovement()
    {
        // 获取 WASD 输入
        float moveForward = Input.GetAxisRaw("Vertical");   // W = 1, S = -1
        float moveRight = Input.GetAxisRaw("Horizontal");   // D = 1, A = -1

        // 计算移动方向（相对于玩家面向的方向）
        Vector3 move = transform.forward * moveForward + transform.right * moveRight;

        // 保持水平方向的移动（不受俯仰角影响）
        move.y = 0f;
        move = move.normalized * moveSpeed;

        // 应用移动
        controller.Move(move * Time.deltaTime);
    }

    void HandleGravity()
    {
        // 检查是否在地面上
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;  // 贴地一点
        }

        // 应用重力
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
