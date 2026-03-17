using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI 面板引用")]
    public GameObject gameOverPanel;
    public GameObject hintTextObject;

    [Header("文字组件引用")]
    public TMP_Text hintText;
    public TMP_Text hpText;

    [Header("设置")]
    public float hintDuration = 2.0f;
    private float hintTimer = 0f;

    [Header("死亡效果设置")]
    [Tooltip("死亡后延迟多少秒显示GameOver面板")]
    public float gameOverDelay = 1.0f;
    private bool isGameOverTriggered = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hintTextObject != null) hintTextObject.SetActive(false);
        isGameOverTriggered = false;
    }

    void Update()
    {
        if (hintTimer > 0)
        {
            hintTimer -= Time.deltaTime;
            if (hintTimer <= 0 && hintTextObject != null)
                hintTextObject.SetActive(false);
        }
    }

    // 显示提示（保持原有功能）
    public void ShowHint(string message)
    {
        if (hintText != null && hintTextObject != null)
        {
            hintText.text = message;
            hintTextObject.SetActive(true);
            hintTimer = hintDuration;
        }
    }

    /// <summary>
    /// 触发游戏结束（带延迟效果）
    /// 由 PlayerInteraction 的 Die() 方法调用
    /// </summary>
    public void TriggerGameOver()
    {
        if (isGameOverTriggered) return; // 防止重复触发
        isGameOverTriggered = true;

        StartCoroutine(GameOverSequence());
    }

    // 延迟显示GameOver的协程
    private IEnumerator GameOverSequence()
    {
        // 等待设定的延迟时间
        yield return new WaitForSeconds(gameOverDelay);

        // 显示GameOver面板
        ShowGameOver();
    }

    // 实际显示面板的方法（现在为private，通过TriggerGameOver调用）
    private void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 可选：暂停游戏时间（如果需要完全暂停）
            // Time.timeScale = 0f;
        }
    }

    public void UpdateHPDisplay(int currentHP)
    {
        if (hpText != null) hpText.text = "Remaining life: " + currentHP;
    }

    // 重新开始游戏（绑定到Restart按钮）
    public void RetryGame()
    {
        // 重置时间缩放（如果之前暂停过）
        Time.timeScale = 1f;

        // 重置状态（防止重新加载后仍标记为已触发）
        isGameOverTriggered = false;

        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 如果需要在不重启场景的情况下重置状态（如关卡内复活），调用此方法
    public void ResetGameOverState()
    {
        isGameOverTriggered = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}