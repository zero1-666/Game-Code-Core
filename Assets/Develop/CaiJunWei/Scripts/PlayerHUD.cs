using UnityEngine;
using UnityEngine.UI;
using TMPro; // 如果你使用了 TextMeshPro

public class PlayerHUD : MonoBehaviour
{
    [Header("引用设置")]
    public PlayerInteraction playerScript; // 拖入 Player 物体

    [Header("HP 心形图标")]
    public Image[] heartIcons; // 在 Inspector 中拖入 3 个心形 Image
    public Color heartActiveColor = Color.red;
    public Color heartEmptyColor = Color.gray;

    [Header("牺牲进度条")]
    public GameObject sacrificeUI;   // 进度条的父物体
    public Image progressCircle;    // 设置为 Filled 模式的图片

    void Update()
    {
        if (playerScript == null) return;

        UpdateHealthUI();
        UpdateSacrificeProgress();
    }

    // 更新血量显示逻辑
    private void UpdateHealthUI()
    {
        // 假设 playerScript.hp 已经改为 public
        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (i < playerScript.hp)
                heartIcons[i].color = heartActiveColor;
            else
                heartIcons[i].color = heartEmptyColor;
        }
    }

    // 更新圆形进度条逻辑
    private void UpdateSacrificeProgress()
    {
        // 读取 PlayerInteraction 里的牺牲计时
        // 只有当计时大于 0 时才显示 UI
        float progress = playerScript.currentHoldTime / playerScript.sacrificeHoldTime;

        if (progress > 0.01f)
        {
            sacrificeUI.SetActive(true);
            progressCircle.fillAmount = progress;
        }
        else
        {
            sacrificeUI.SetActive(false);
        }
    }
}