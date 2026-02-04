using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;      // 拖入你的 Player
    public Vector3 offset = new Vector3(0, 2, -4); // 相对玩家的偏移量（高2米，后退4米）
    public float smoothSpeed = 0.125f; // 跟随平滑度

    void LateUpdate() // 使用 LateUpdate 确保在玩家移动后更新相机，减少抖动
    {
        // 计算目标位置
        Vector3 desiredPosition = target.position + offset;
        // 平滑移动到目标位置
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // 始终盯着玩家看
        transform.LookAt(target);
    }
}