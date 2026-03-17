using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("可选：首次激活时的提示")]
    public string activationMessage = "检查点已激活";

    [Header("可选：视觉效果")]
    public GameObject activeVisual;  // 激活后显示（如绿色光效）
    public GameObject inactiveVisual; // 未激活时（如灰色）

    private bool isActivated = false;

    void Start()
    {
        // 初始状态：如果是 Start_SpawnPoint 或 Checkpoint_01，默认静默激活
        if (gameObject.name.Contains("Start") || gameObject.name.Contains("01"))
        {
            ActivateCheckpoint(true); // 静默激活，不显示提示
        }
        else
        {
            // 其他检查点初始为未激活状态
            if (inactiveVisual != null) inactiveVisual.SetActive(true);
            if (activeVisual != null) activeVisual.SetActive(false);
        }
    }

    // 当玩家进入触发区域
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            ActivateCheckpoint();
        }
    }

    // 激活检查点
    void ActivateCheckpoint(bool silent = false)
    {
        isActivated = true;

        // 调用 PlayerInteraction 的方法更新重生点（规范做法）
        PlayerInteraction player = FindObjectOfType<PlayerInteraction>();
        if (player != null)
        {
            player.UpdateSpawnPoint(transform.position);
        }

        // 视觉反馈切换
        if (activeVisual != null) activeVisual.SetActive(true);
        if (inactiveVisual != null) inactiveVisual.SetActive(false);

        // UI提示（非静默模式）
        if (!silent && UIManager.Instance != null)
        {
            UIManager.Instance.ShowHint(activationMessage);
        }

        Debug.Log($"检查点激活：{gameObject.name}");
    }
}