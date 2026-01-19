using UnityEngine;


[RequireComponent(typeof(CharacterController))]
public class SimpleFPSMovement : MonoBehaviour
{
    public CharacterController controller;
    public Camera cam;

    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float maxLookUp = 20f;      
    public float maxLookDown = 30f;   

    [Header("Bullet Time")]
    [Tooltip("子弹时间(Time.timeScale < 1)时，视角速度也跟着变慢")]
    public bool scaleLookWithTimeScale = true;

    [Header("Physics")]
    public float gravity = -9.81f;

    private Vector3 velocity;
    private float currentPitch = 0f;

    void Reset()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
    }

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (controller == null) return;

        HandleMouseLook();
        HandleMovement();
        HandleGravity();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        
        if (scaleLookWithTimeScale)
        {
            float s = Mathf.Clamp(Time.timeScale, 0f, 1f);
            mouseX *= s;
            mouseY *= s;
        }

        transform.Rotate(Vector3.up, mouseX);

        currentPitch -= mouseY;
        currentPitch = Mathf.Clamp(currentPitch, -maxLookUp, maxLookDown);

        if (cam != null)
        {
            cam.transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }
    }

    void HandleMovement()
    {
        float moveForward = Input.GetAxisRaw("Vertical");
        float moveRight = Input.GetAxisRaw("Horizontal");

        Vector3 move = transform.forward * moveForward + transform.right * moveRight;
        move.y = 0f;
        move = move.normalized * moveSpeed;

        controller.Move(move * Time.deltaTime);
    }

    void HandleGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
