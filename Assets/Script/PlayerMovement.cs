using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("移动设置")]
    public float walkSpeed = 5f;      // 走路速度
    public float runSpeed = 10f;     // 跑步速度
    public float gravity = -9.81f;

    [Header("视角旋转设置")]
    public float mouseSensitivity = 200f;
    [Tooltip("角色转身的平滑度，数值越大转得越快，建议20-40")]
    public float rotationSmoothSpeed = 20f;
    // 提示：拖入你创建的那个 CameraTarget 空物体
    public Transform cameraTarget;

    [Header("动画设置")]
    private Animator anim;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();

        // 【关键】强制关闭根运动，防止动画位移与脚本位移叠加导致的相机缩进错位
        if (anim != null)
        {
            anim.applyRootMotion = false;
        }

        // 锁定鼠标光标
        Cursor.lockState = CursorLockMode.Locked;

        // 初始化旋转角度，防止第一帧突然“瞬移”
        yRotation = transform.localEulerAngles.y;
    }

    void Update()
    {
        // 1. 处理鼠标输入与旋转
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 左右旋转：使用 Slerp 平滑过渡，给相机提供稳定的追踪点
        yRotation += mouseX;
        Quaternion targetRotation = Quaternion.Euler(0, yRotation, 0);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);

        // 上下视角 (旋转 CameraTarget)
        if (cameraTarget != null)
        {
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -45f, 45f); // 限制抬头低头角度
            cameraTarget.localRotation = Quaternion.Euler(xRotation, 0, 0);
        }

        // 2. 移动逻辑
        // 使用 GetAxis 带来自然的输入平滑，缓解相机“抢跑”感
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentMoveSpeed = isRunning ? runSpeed : walkSpeed;

        // 计算移动方向
        Vector3 move = (transform.right * x + transform.forward * z);

        // 只有在摇杆/键盘输入较大时才执行移动，防止细微抖动
        if (move.magnitude > 0.1f)
        {
            if (move.magnitude > 1f) move.Normalize();
            controller.Move(move * currentMoveSpeed * Time.deltaTime);
        }

        // 3. 动画参数传递
        float inputMagnitude = new Vector2(x, z).magnitude;
        float animSpeed = inputMagnitude * (isRunning ? 2f : 1f);

        if (anim != null)
        {
            // 确保你的 Animator 里的参数名是 Speed
            anim.SetFloat("Speed", animSpeed);
        }

        // 4. 处理额外动作
        HandleActions();

        // 5. 处理重力
        ApplyGravity();
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleActions()
    {
        if (anim == null) return;

        // 攻击
        if (Input.GetMouseButtonDown(0))
        {
            anim.SetTrigger("DoPunch");
        }

        // 搬运状态
        if (Input.GetKeyDown(KeyCode.E))
        {
            anim.SetBool("IsHolding", true);
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            anim.SetBool("IsHolding", false);
        }
    }
}