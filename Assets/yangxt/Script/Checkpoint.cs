using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 检查碰撞的是否是玩家
        if (other.CompareTag("Player"))
        {
            PlayerInteraction pi = other.GetComponent<PlayerInteraction>();
            if (pi != null)
            {
                // 更新玩家脚本中的变量为当前触发器的位置
                pi.spawnPoint = transform.position;
                Debug.Log("复活点更新完毕！");
            }
        }
    }

    
}