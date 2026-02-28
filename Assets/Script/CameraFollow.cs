using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;      // 拖入你的 Player
    public Vector3 offset = new Vector3(0, 2, -4); // 相对玩家的偏移量（高2米，后退4米）
    public float smoothSpeed = 0.125f; // 跟随平滑度

    //[yangxt]新增：抖动设置
    private float shakeDuration = 0f;    // 剩余抖动时间
    private float shakeMagnitude = 0.1f; // 抖动强度
    private Vector3 currentShakeOffset = Vector3.zero; // 当前抖动产生的偏移
    

    void LateUpdate() // 使用 LateUpdate 确保在玩家移动后更新相机，减少抖动
    {
        //[yangxt]新增：计算抖动偏移
        if (shakeDuration > 0)
        {
            currentShakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeDuration -= Time.deltaTime;
        }
        else
        {
            currentShakeOffset = Vector3.zero;
        }

        // 计算目标位置
        //[yangxt]在原有desirePosition基础上加上抖动偏移
        Vector3 desiredPosition = target.position + offset + currentShakeOffset;
        // 平滑移动到目标位置
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // 始终盯着玩家看
        transform.LookAt(target);
    }

    //[yangxt]新增：触发抖动的公共方法
    public void TriggerShake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }
}