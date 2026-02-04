using UnityEngine;

public class VoidDetector : MonoBehaviour
{
    public GameObject bridgePrefab; // 填补后的桥梁模型

    // 当有物体进入触发器时执行
    private void OnTriggerEnter(Collider other)
    {
        // 检查进入的是不是箱子
        if (other.CompareTag("Crate"))
        {
            Debug.Log("箱子已填入坑位！");

            // 1. 在当前坑位位置生成桥
            if (bridgePrefab != null)
            {
                Instantiate(bridgePrefab, transform.position, transform.rotation);
            }

            // 2. 销毁掉进来的箱子
            Destroy(other.gameObject);

            // 3. 销毁坑位自己
            Destroy(gameObject);
        }
    }
}