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

    void Update()
    {
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