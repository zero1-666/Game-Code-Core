using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    [Header("视角旋转")]
    public float mouseSensitivity = 200f;
    public Transform playerCamera;

    [Header("相机平滑设置")]
    public Vector3 cameraOffset = new Vector3(0, 2, -4); // 对应你之前的偏移
    public float smoothSpeed = 0.125f;

    //[yangxt]新增：抖动相关变量
    private float shakeDuration = 0f;          // 抖动持续时间
    private float shakeMagnitude = 0.2f;       // 抖动强度
    private Vector3 currentShakeOffset = Vector3.zero;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        // 自动初始化相机位置
        if (playerCamera != null)
        {
            playerCamera.localPosition = cameraOffset;
        }
    }

    //[yangxt]新增：触发抖动的公共方法
    public void TriggerShake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }

    void Update()
    {
        //[yangxt]新增：处理抖动倒计时和随机位移计算
        if (shakeDuration > 0)
        {
            // 计算一个随机的目标偏移
            Vector3 targetShakeOffset = Random.insideUnitSphere * shakeMagnitude;

            // 使用 Lerp 让当前偏移平滑地向目标偏移靠拢，而不是瞬间跳过去
            // 5.0f 是平滑系数，数字越大越敏捷，数字越小越柔和
            currentShakeOffset = Vector3.Lerp(currentShakeOffset, targetShakeOffset, Time.deltaTime * 10f);

            shakeDuration -= Time.deltaTime;
        }
        else
        {
            // 停止抖动时，也要平滑地回到原点，避免相机“瞬移”回位
            currentShakeOffset = Vector3.Lerp(currentShakeOffset, Vector3.zero, Time.deltaTime * 10f);
        
        }

        // 1. 旋转逻辑 (直接操作数值，不再被 LookAt 干扰)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -45f, 45f);

        // 旋转身体
        transform.localRotation = Quaternion.Euler(0, yRotation, 0);

        // 旋转相机
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0, 0);
            playerCamera.localPosition = cameraOffset + currentShakeOffset;//[yangxt]强制更新坐标
        }

        // 2. 移动逻辑
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // 重力
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}